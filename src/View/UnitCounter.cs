namespace artillery;

using Godot;
using QFramework;

/// <summary>
/// 屏幕空间兵牌：放在 CanvasLayer 里，按单位世界坐标投影到屏幕，固定像素大小显示。
/// 朝向由 Model 的 Facing 驱动（Facing = 炮尾方向），此处只做视觉匹配。
/// 火炮仅在「部署后」显示阵地圈。
/// </summary>
public partial class UnitCounter : Node2D, IController
{
    private UnitInfo _unit = default!;
    private Camera2D _camera = default!;
    private Sprite2D _sprite = default!;
    private Sprite2D _overlay = default!;
    private Label _label = default!;

    private Vector2 _prevWorldPos;
    private Vector2 _currWorldPos;
    private bool _selected;
    private bool _hovered;
    private bool _wasFiring;

    private const float MarkerSize = 80f;
    private const float BaseAngle = -Mathf.Pi / 2f;
    private static readonly Color ProgressColor = new(0.8f, 0.15f, 0.15f);

    private static Texture2D _artilleryGun = default!;
    private static Texture2D _artilleryPos = default!;
    private static Texture2D _observer = default!;

    public IArchitecture GetArchitecture() => ArtilleryArchitecture.Interface;

    public void Bind(UnitInfo unit, Camera2D camera)
    {
        _unit = unit;
        _camera = camera;
    }

    public override void _Ready()
    {
        LoadTextures();

        _sprite = new Sprite2D();
        _sprite.Texture = GetBaseTexture(_unit.Type);
        var scale = Vector2.One * (MarkerSize / _sprite.Texture.GetWidth());
        _sprite.Scale = scale;
        AddChild(_sprite);

        _overlay = new Sprite2D();
        _overlay.Texture = _artilleryPos;
        _overlay.Scale = scale;
        _overlay.Visible = false;
        AddChild(_overlay);

        _label = new Label();
        _label.AddThemeFontSizeOverride("font_size", 13);
        _label.AddThemeColorOverride("font_color", new Color(0.08f, 0.08f, 0.08f));
        _label.HorizontalAlignment = HorizontalAlignment.Center;
        _label.Position = new Vector2(-40f, MarkerSize / 2f + 4f);
        _label.Size = new Vector2(80f, 0f);
        AddChild(_label);

        _prevWorldPos = _unit.Position.Value;
        _currWorldPos = _unit.Position.Value;
        _unit.Position.Register(OnPositionChanged).UnRegisterWhenNodeExitTree(this);
        _unit.State.RegisterWithInitValue(OnStateChanged).UnRegisterWhenNodeExitTree(this);
        _unit
            .Facing.RegisterWithInitValue(a => _sprite.Rotation = a + BaseAngle)
            .UnRegisterWhenNodeExitTree(this);
        this.GetModel<ISelectionModel>()
            .SelectedUnitId.RegisterWithInitValue(id => OnSelectionChanged(id == _unit.Id))
            .UnRegisterWhenNodeExitTree(this);
        this.GetModel<IHoverModel>()
            .HoveredUnitId.RegisterWithInitValue(id => OnHoverChanged(id == _unit.Id))
            .UnRegisterWhenNodeExitTree(this);
    }

    public override void _Process(double delta)
    {
        // 渲染帧：插值世界坐标 → 投影到屏幕
        var fraction = (float)Engine.GetPhysicsInterpolationFraction();
        var worldPos = _prevWorldPos.Lerp(_currWorldPos, fraction);
        Position = WorldToScreen(worldPos);

        UpdateOverlay();

        // 开火期间每帧重绘（扫描环/进度条动画）；离开开火状态后再重绘一次，清除残留
        var firing = _unit.State.Value == UnitState.Firing;
        if (firing || _wasFiring)
        {
            QueueRedraw();
        }

        _wasFiring = firing;
    }

    private void UpdateOverlay()
    {
        if (_unit.Type != UnitType.Artillery)
        {
            _overlay.Visible = false;
            return;
        }

        var state = _unit.State.Value;
        var phase = _unit.FirePhase;

        if (state == UnitState.Firing && phase == FirePhase.Deploying)
        {
            // 部署中：阵地圈隐藏，由扫描环表示进度
            _overlay.Visible = false;
        }
        else if (
            state == UnitState.Firing
            && phase is FirePhase.Laying or FirePhase.AwaitingCommand
        )
        {
            // 已部署（装定/等待指令）：完整阵地圈
            _overlay.Visible = true;
            _overlay.Modulate = Colors.White;
        }
        else if (state == UnitState.Firing && phase == FirePhase.PackingUp)
        {
            // 装车中：阵地圈随进度淡出
            _overlay.Visible = true;
            _overlay.Modulate = new Color(1f, 1f, 1f, 1f - _unit.GetFireProgress());
        }
        else
        {
            _overlay.Visible = false;
        }
    }

    public override void _Draw()
    {
        if (_hovered)
        {
            // 悬停：粗圈
            DrawArc(
                Vector2.Zero,
                MarkerSize / 2f + 5f,
                0f,
                Mathf.Tau,
                64,
                new Color(1f, 0.6f, 0.1f),
                5f
            );
        }
        else if (_selected)
        {
            // 选中：细圈
            DrawArc(
                Vector2.Zero,
                MarkerSize / 2f + 5f,
                0f,
                Mathf.Tau,
                64,
                new Color(1f, 0.6f, 0.1f),
                2f
            );
        }

        if (_unit.State.Value == UnitState.Firing)
        {
            DrawFireProgress();
        }
    }

    private void DrawFireProgress()
    {
        var progress = _unit.GetFireProgress();

        if (_unit.FirePhase == FirePhase.Deploying)
        {
            // 部署：圆形扫描环
            var radius = (MarkerSize / 2f) + 8f;
            var start = -Mathf.Pi / 2f;
            var end = start + (progress * Mathf.Tau);
            DrawArc(Vector2.Zero, radius, start, end, 48, ProgressColor, 3f);
        }
        else if (_unit.FirePhase is FirePhase.Laying or FirePhase.PackingUp)
        {
            // 装定/装车：进度条
            var bar = new Rect2(-20f, (MarkerSize / 2f) + 22f, 40f, 4f);
            DrawRect(bar, new Color(0f, 0f, 0f, 0.5f));
            DrawRect(
                new Rect2(bar.Position, new Vector2(bar.Size.X * progress, bar.Size.Y)),
                ProgressColor
            );
        }
    }

    private Vector2 WorldToScreen(Vector2 worldPos)
    {
        var center = GetViewport().GetVisibleRect().Size / 2f;
        return center + ((worldPos - _camera.Position) * _camera.Zoom);
    }

    private static void LoadTextures()
    {
        if (_artilleryGun != null)
        {
            return;
        }

        _artilleryGun = GD.Load<Texture2D>("res://assets/counters/artillery_122.png");
        _artilleryPos = GD.Load<Texture2D>("res://assets/counters/artillery_pos.png");
        _observer = GD.Load<Texture2D>("res://assets/counters/observer_optical.png");
    }

    private static Texture2D GetBaseTexture(UnitType type) =>
        type == UnitType.Artillery ? _artilleryGun : _observer;

    private void OnPositionChanged(Vector2 newPos)
    {
        _prevWorldPos = _currWorldPos;
        _currWorldPos = newPos;
    }

    private void OnStateChanged(UnitState state)
    {
        _sprite.Texture = GetBaseTexture(_unit.Type);
        _label.Text = $"{_unit.Designation} {TypeCode(_unit.Type)} {state}";

        if (state != UnitState.Moving)
        {
            _prevWorldPos = _unit.Position.Value;
            _currWorldPos = _unit.Position.Value;
        }
    }

    private void OnSelectionChanged(bool selected)
    {
        _selected = selected;
        QueueRedraw();
    }

    private void OnHoverChanged(bool hovered)
    {
        _hovered = hovered;
        QueueRedraw();
    }

    private static string TypeCode(UnitType type) => type == UnitType.Artillery ? "ART" : "OBS";
}
