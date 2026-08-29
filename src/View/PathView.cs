namespace artillery;

using System.Collections.Generic;
using Godot;
using QFramework;

/// <summary>
/// 轨迹线绘制层（世界空间）：红色进攻线，屏幕恒定线宽。
/// 已下达未走完的路径按部队锚定逐段绘制；正在绘制时叠加末端橡皮筋。
/// </summary>
public partial class PathView : Node2D, IController
{
    private IReadOnlyList<Vector2> _drawVertices = System.Array.Empty<Vector2>();
    private Vector2 _rubberEnd;
    private bool _drawing;

    private Vector2 _closestPoint;
    private bool _showClosestPoint;

    private Vector2 _fireAimOrigin;
    private Vector2 _fireAimTarget;
    private bool _showFireAim;

    private float _zoom = 1f;

    private const float LineWidthPx = 3f;
    private const float MarkerRadiusPx = 4f;
    private const float FireAimRadiusPx = 6f;

    /// <summary>中饱和度红（类苏军条令友方进攻线）。</summary>
    private static readonly Color LineColor = new(0.8f, 0.15f, 0.15f, 0.85f);

    public IArchitecture GetArchitecture() => ArtilleryArchitecture.Interface;

    public override void _Ready()
    {
        _zoom = this.GetModel<ICameraModel>().Zoom.Value;
        this.GetModel<ICameraModel>()
            .Zoom.Register(z =>
            {
                _zoom = z;
                QueueRedraw();
            })
            .UnRegisterWhenNodeExitTree(this);
    }

    public void SetDrawingVertices(IReadOnlyList<Vector2> vertices)
    {
        _drawVertices = vertices;
        QueueRedraw();
    }

    public void SetRubberEnd(Vector2 end, bool show)
    {
        _rubberEnd = end;
        _drawing = show;
        QueueRedraw();
    }

    public void SetClosestPoint(Vector2? point)
    {
        _showClosestPoint = point.HasValue;
        if (point.HasValue)
        {
            _closestPoint = point.Value;
        }

        QueueRedraw();
    }

    public void SetFireAim(Vector2? origin, Vector2? target)
    {
        _showFireAim = origin.HasValue && target.HasValue;
        if (origin.HasValue && target.HasValue)
        {
            _fireAimOrigin = origin.Value;
            _fireAimTarget = target.Value;
        }

        QueueRedraw();
    }

    public override void _Process(double delta)
    {
        // 有部队沿路径移动时，锚点每帧变化，需持续重绘
        foreach (var unit in this.GetModel<IUnitsModel>().Units)
        {
            if (unit.Path.Count > 0)
            {
                QueueRedraw();
                return;
            }
        }
    }

    public override void _Draw()
    {
        var width = LineWidthPx / _zoom;

        // 已下达、未走完的路径（锚定各自部队）
        foreach (var unit in this.GetModel<IUnitsModel>().Units)
        {
            if (unit.Path.Count == 0)
            {
                continue;
            }

            DrawWaypointPath(unit.Position.Value, unit.Path, width);
            DrawWaypointMarkers(unit.Path);
        }

        // 正在绘制中的轨迹（锚定选中部队 + 末端橡皮筋）
        if (_drawing && _drawVertices.Count >= 1)
        {
            var selected = this.SendQuery(new GetSelectedUnitQuery());
            var anchor = selected?.Position.Value ?? _drawVertices[0];
            DrawPolyline(anchor, _drawVertices, width);
            DrawLine(_drawVertices[^1], _rubberEnd, LineColor, width);
        }

        // 靠近已绘制路径时的最近点标记：红边白点
        if (_showClosestPoint)
        {
            DrawMarker(_closestPoint, Colors.White);
        }

        // 火力目标指定中的打击标志：虚线 + 实心红圆
        if (_showFireAim)
        {
            DrawFireTarget(_fireAimOrigin, _fireAimTarget);
        }
    }

    private void DrawFireTarget(Vector2 origin, Vector2 target)
    {
        var width = LineWidthPx / _zoom;
        DrawDashedLine(origin, target, LineColor, width, 8f / _zoom);

        var r = FireAimRadiusPx / _zoom;
        DrawCircle(target, r, new Color(0.9f, 0.1f, 0.1f, 0.85f));
        DrawArc(target, r, 0f, Mathf.Tau, 24, LineColor, 1.5f / _zoom);
    }

    private void DrawWaypointMarkers(IReadOnlyList<Waypoint> waypoints)
    {
        foreach (var wp in waypoints)
        {
            var fire = wp.GetAction<FireAction>();
            if (fire != null)
            {
                // 火力路径点：红色标记 + 持续显示打击目标
                DrawMarker(wp.Position, new Color(0.9f, 0.1f, 0.1f));
                DrawFireTarget(wp.Position, fire.Target);
            }
            else if (wp.GetAction<WaitSignalAction>() != null)
            {
                // 等待信号路径点：白色标记
                DrawMarker(wp.Position, Colors.White);
            }
        }
    }

    private void DrawMarker(Vector2 pos, Color fill)
    {
        var r = MarkerRadiusPx / _zoom;
        DrawCircle(pos, r, fill);
        DrawArc(pos, r, 0f, Mathf.Tau, 24, LineColor, 1.5f / _zoom);
    }

    private void DrawWaypointPath(Vector2 start, IReadOnlyList<Waypoint> waypoints, float width)
    {
        var prev = start;
        foreach (var w in waypoints)
        {
            DrawLine(prev, w.Position, LineColor, width);
            prev = w.Position;
        }
    }

    private void DrawPolyline(Vector2 start, IReadOnlyList<Vector2> points, float width)
    {
        var prev = start;
        foreach (var p in points)
        {
            DrawLine(prev, p, LineColor, width);
            prev = p;
        }
    }
}
