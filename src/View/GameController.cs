namespace artillery;

using System.Collections.Generic;
using Godot;
using QFramework;

/// <summary>
/// 根控制器：输入路由 → 发 Command，驱动相机与单位视图，按帧 tick 系统。
/// 右键画轨迹线（破门而入式），松开后单位沿线移动。
/// </summary>
public partial class GameController : Node2D, IController
{
    private Camera2D _camera = default!;
    private Node2D _counterLayer = default!;
    private PathView _pathView = default!;
    private PopupMenu _actionMenu = default!;
    private PopupMenu _signalSubmenu = default!;
    private PopupMenu _fireSubmenu = default!;
    private PathPoint? _pendingActionPoint;
    private PathPoint? _closestPathPoint;

    private bool _panning;
    private bool _drawing;
    private bool _designatingFireTarget;
    private int _hoveredUnitId = -1;
    private int _fireUnitId;
    private int _fireInsertIndex;
    private int _fireWaypointIndex;
    private Vector2 _fireOrigin;
    private ShellType _fireShellType;
    private readonly List<Vector2> _pathVertices = new();

    private const float ZoomStep = 1.15f;
    private const float ScreenHitRadius = 40f;
    private const float PathVertexThreshold = 40f;
    private const int DeleteMenuItemId = 1000;
    private const int RemoveSignalMenuItemId = 2000;

    /// <summary>一级菜单中「删除」项的索引（0=等待信号，1=火力打击，2=删除）。</summary>
    private const int DeleteItemIndex = 2;

    /// <summary>信号子菜单中「移除信号」项的索引（0/1/2=Alpha/Bravo/Charly，3=移除信号）。</summary>
    private const int RemoveSignalItemIndex = 3;

    public IArchitecture GetArchitecture() => ArtilleryArchitecture.Interface;

    public override void _Ready()
    {
        _ = ArtilleryArchitecture.Interface;

        _camera = GetNode<Camera2D>("Camera");
        _counterLayer = GetNode<Node2D>("CounterLayer/Counters");
        _camera.MakeCurrent();

        _pathView = new PathView();
        AddChild(_pathView);

        var cameraModel = this.GetModel<ICameraModel>();
        cameraModel
            .Position.RegisterWithInitValue(p => _camera.Position = p)
            .UnRegisterWhenNodeExitTree(this);
        cameraModel
            .Zoom.RegisterWithInitValue(z => _camera.Zoom = new Vector2(z, z))
            .UnRegisterWhenNodeExitTree(this);

        foreach (var unit in this.GetModel<IUnitsModel>().Units)
        {
            var counter = new UnitCounter();
            counter.Bind(unit, _camera);
            _counterLayer.AddChild(counter);
        }

        var pauseHud = new PauseHud();
        GetNode<CanvasLayer>("Hud").AddChild(pauseHud);

        this.RegisterEvent<ShellFiredEvent>(OnShellFired).UnRegisterWhenNodeExitTree(this);

        _signalSubmenu = new PopupMenu();
        _signalSubmenu.AddItem("Alpha (1)", (int)SignalType.Alpha);
        _signalSubmenu.AddItem("Bravo (2)", (int)SignalType.Bravo);
        _signalSubmenu.AddItem("Charly (3)", (int)SignalType.Charly);
        _signalSubmenu.AddItem("移除信号", RemoveSignalMenuItemId);
        _signalSubmenu.IdPressed += OnSignalSubmenuIdPressed;

        _fireSubmenu = new PopupMenu();
        _fireSubmenu.AddItem("HE 高爆", (int)ShellType.He);
        _fireSubmenu.IdPressed += OnFireSubmenuIdPressed;

        _actionMenu = new PopupMenu();
        AddChild(_actionMenu);
        _actionMenu.AddChild(_signalSubmenu);
        _actionMenu.AddChild(_fireSubmenu);
        _actionMenu.AddSubmenuNodeItem("等待信号", _signalSubmenu);
        _actionMenu.AddSubmenuNodeItem("火力打击", _fireSubmenu);
        _actionMenu.AddItem("删除", DeleteMenuItemId);
        _actionMenu.IdPressed += OnActionMenuIdPressed;
    }

    public override void _PhysicsProcess(double delta)
    {
        // 用固定物理帧 tick 移动，避免渲染帧率波动导致移动忽快忽慢
        this.GetSystem<IUnitSystem>().Tick((float)delta);
    }

    public override void _UnhandledInput(InputEvent @event)
    {
        switch (@event)
        {
            case InputEventKey key when key.Pressed && !key.Echo && key.Keycode == Key.Space:
                this.SendCommand(new TogglePauseCommand());
                break;
            case InputEventKey key when key.Pressed && !key.Echo && key.Keycode == Key.Key1:
                this.SendCommand(new TriggerSignalCommand(SignalType.Alpha));
                break;
            case InputEventKey key when key.Pressed && !key.Echo && key.Keycode == Key.Key2:
                this.SendCommand(new TriggerSignalCommand(SignalType.Bravo));
                break;
            case InputEventKey key when key.Pressed && !key.Echo && key.Keycode == Key.Key3:
                this.SendCommand(new TriggerSignalCommand(SignalType.Charly));
                break;
            case InputEventKey key when key.Pressed && !key.Echo && key.Keycode == Key.Escape:
                if (_designatingFireTarget)
                {
                    CancelFireDesignation();
                }

                break;
            case InputEventMouseButton mouseButton:
                HandleMouseButton(mouseButton);
                break;
            case InputEventMouseMotion mouseMotion:
                HandleMouseMotion(mouseMotion);
                break;
        }
    }

    private void HandleMouseButton(InputEventMouseButton e)
    {
        switch (e.ButtonIndex)
        {
            case MouseButton.Middle:
                _panning = e.Pressed;
                break;

            case MouseButton.WheelUp when e.Pressed:
                this.SendCommand(new ZoomCameraCommand(ZoomStep, GetGlobalMousePosition()));
                break;

            case MouseButton.WheelDown when e.Pressed:
                this.SendCommand(new ZoomCameraCommand(1f / ZoomStep, GetGlobalMousePosition()));
                break;

            case MouseButton.Left when e.Pressed:
                if (_designatingFireTarget)
                {
                    DesignateFireTarget();
                }
                else
                {
                    HandleLeftClick();
                }

                break;

            case MouseButton.Right:
                if (e.Pressed)
                {
                    if (_designatingFireTarget)
                    {
                        CancelFireDesignation();
                    }
                    else
                    {
                        HandleRightPress();
                    }
                }
                else
                {
                    FinishPath();
                }

                break;
        }
    }

    private void HandleMouseMotion(InputEventMouseMotion e)
    {
        if (_designatingFireTarget)
        {
            UpdateFireAim();
            return;
        }

        UpdateHover();
        UpdateClosestPoint();

        if (_panning)
        {
            var worldDelta = e.Relative / this.GetModel<ICameraModel>().Zoom.Value;
            this.SendCommand(new PanCameraCommand(worldDelta));
        }
        else if (_drawing)
        {
            UpdatePath();
        }
    }

    private void UpdateHover()
    {
        var zoom = this.GetModel<ICameraModel>().Zoom.Value;
        var unit = this.SendQuery(
            new GetUnitAtPositionQuery
            {
                WorldPos = GetGlobalMousePosition(),
                WorldRadius = ScreenHitRadius / zoom,
            }
        );
        var id = unit?.Id ?? -1;
        if (id == _hoveredUnitId)
        {
            return;
        }

        _hoveredUnitId = id;
        this.SendCommand(new SetHoveredUnitCommand(id));
    }

    private void UpdateClosestPoint()
    {
        if (_drawing)
        {
            _closestPathPoint = null;
            _pathView.SetClosestPoint(null);
            return;
        }

        var zoom = this.GetModel<ICameraModel>().Zoom.Value;
        _closestPathPoint = this.SendQuery(
            new GetClosestPointOnPathQuery
            {
                WorldPos = GetGlobalMousePosition(),
                WorldRadius = ScreenHitRadius / zoom,
            }
        );
        _pathView.SetClosestPoint(_closestPathPoint?.Point);
    }

    private void HandleRightPress()
    {
        // 靠近已绘制路径 → 弹出添加动作点菜单
        UpdateClosestPoint();
        if (_closestPathPoint is { } p)
        {
            ShowActionPointMenu(p);
            return;
        }

        StartPath();
    }

    private void ShowActionPointMenu(PathPoint point)
    {
        _pendingActionPoint = point;

        var hasSignal = point.WaypointIndex >= 0 && WaypointHasWaitSignal(point);
        _signalSubmenu.SetItemDisabled(RemoveSignalItemIndex, !hasSignal);
        _actionMenu.SetItemDisabled(DeleteItemIndex, point.WaypointIndex < 0);

        _actionMenu.Position = (Vector2I)GetViewport().GetMousePosition();
        _actionMenu.Popup();
    }

    private bool WaypointHasWaitSignal(PathPoint point)
    {
        var unit = this.GetModel<IUnitsModel>().GetUnit(point.UnitId);
        return unit != null
            && point.WaypointIndex >= 0
            && point.WaypointIndex < unit.Path.Count
            && unit.Path[point.WaypointIndex].GetAction<WaitSignalAction>() != null;
    }

    private void OnSignalSubmenuIdPressed(long id)
    {
        if (id == RemoveSignalMenuItemId)
        {
            if (_pendingActionPoint is { } p && p.WaypointIndex >= 0)
            {
                this.SendCommand(new RemoveSignalActionCommand(p.UnitId, p.WaypointIndex));
            }
        }
        else if (_pendingActionPoint is { } p)
        {
            this.SendCommand(
                new AddSignalActionCommand(
                    p.UnitId,
                    p.InsertIndex,
                    p.Point,
                    p.WaypointIndex,
                    (SignalType)id
                )
            );
        }

        _pendingActionPoint = null;
    }

    private void OnActionMenuIdPressed(long id)
    {
        if (id == DeleteMenuItemId)
        {
            if (_pendingActionPoint is { } p && p.WaypointIndex >= 0)
            {
                this.SendCommand(new DeleteWaypointCommand(p.UnitId, p.WaypointIndex));
            }
        }

        _pendingActionPoint = null;
    }

    private void OnFireSubmenuIdPressed(long id)
    {
        var shellType = (ShellType)id;
        if (_pendingActionPoint is { } p)
        {
            StartFireDesignation(p, shellType);
        }

        _pendingActionPoint = null;
    }

    private void StartFireDesignation(PathPoint point, ShellType shellType)
    {
        _fireUnitId = point.UnitId;
        _fireInsertIndex = point.InsertIndex;
        _fireWaypointIndex = point.WaypointIndex;
        _fireOrigin = point.Point;
        _fireShellType = shellType;
        _designatingFireTarget = true;

        _pathView.SetClosestPoint(null);
        UpdateFireAim();
    }

    private void UpdateFireAim()
    {
        _pathView.SetFireAim(_fireOrigin, GetGlobalMousePosition());
    }

    private void DesignateFireTarget()
    {
        var target = GetGlobalMousePosition();
        this.SendCommand(
            new AddFireActionCommand(
                _fireUnitId,
                _fireInsertIndex,
                _fireOrigin,
                _fireWaypointIndex,
                target,
                _fireShellType
            )
        );
        CancelFireDesignation();
    }

    private void CancelFireDesignation()
    {
        _designatingFireTarget = false;
        _pathView.SetFireAim(null, null);
    }

    private void OnShellFired(ShellFiredEvent e)
    {
        var shell = new ShellProjectile();
        shell.Position = e.Origin;
        shell.Setup(e.Target);
        AddChild(shell);
    }

    private void StartPath()
    {
        // 轨迹必须锚定某支部队，未选中时不画
        var selected = this.SendQuery(new GetSelectedUnitQuery());
        if (selected == null)
        {
            return;
        }

        // 鼠标须位于选中单位上才开始绘制起始点
        var mouse = GetGlobalMousePosition();
        var worldRadius = ScreenHitRadius / this.GetModel<ICameraModel>().Zoom.Value;
        if (mouse.DistanceTo(selected.Position.Value) > worldRadius)
        {
            return;
        }

        _drawing = true;
        _pathVertices.Clear();
        _pathVertices.Add(mouse);
        _pathView.SetDrawingVertices(_pathVertices);
        _pathView.SetRubberEnd(mouse, true);
    }

    private void UpdatePath()
    {
        var mouse = GetGlobalMousePosition();
        var last = _pathVertices[^1];

        if (mouse.DistanceTo(last) > PathVertexThreshold)
        {
            _pathVertices.Add(mouse);

            // 递归消除来回短波：只要末顶点贴近倒数第三个，就删倒数第二个
            while (
                _pathVertices.Count >= 3
                && _pathVertices[^1].DistanceTo(_pathVertices[^3]) < PathVertexThreshold
            )
            {
                _pathVertices.RemoveAt(_pathVertices.Count - 2);
            }

            _pathView.SetDrawingVertices(_pathVertices);
        }

        // 末端始终跟随鼠标
        _pathView.SetRubberEnd(mouse, true);
    }

    private void FinishPath()
    {
        _drawing = false;
        _pathView.SetRubberEnd(Vector2.Zero, false);

        var selected = this.SendQuery(new GetSelectedUnitQuery());
        if (selected != null && _pathVertices.Count > 0)
        {
            // 路径下发后由 PathView 从 Model 持续绘制（随部队经过逐步清除）
            this.SendCommand(new MoveAlongPathCommand(selected.Id, _pathVertices));
        }

        _pathVertices.Clear();
        _pathView.SetDrawingVertices(_pathVertices);
    }

    private void HandleLeftClick()
    {
        // 命中圈按屏幕像素固定（不随缩放变细），换算成世界半径
        var zoom = this.GetModel<ICameraModel>().Zoom.Value;
        var worldRadius = ScreenHitRadius / zoom;
        var unit = this.SendQuery(
            new GetUnitAtPositionQuery
            {
                WorldPos = GetGlobalMousePosition(),
                WorldRadius = worldRadius,
            }
        );
        if (unit == null)
        {
            this.SendCommand(new DeselectUnitCommand());
        }
        else
        {
            this.SendCommand(new SelectUnitCommand(unit.Id));
        }
    }
}
