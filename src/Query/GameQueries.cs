namespace artillery;

using Godot;
using QFramework;

public class GetSelectedUnitQuery : AbstractQuery<UnitInfo?>
{
    protected override UnitInfo? OnDo()
    {
        var selection = this.GetModel<ISelectionModel>();
        if (selection.SelectedUnitId.Value < 0)
        {
            return null;
        }

        return this.GetModel<IUnitsModel>().GetUnit(selection.SelectedUnitId.Value);
    }
}

public class GetUnitAtPositionQuery : AbstractQuery<UnitInfo?>
{
    public Vector2 WorldPos { get; set; }

    public float WorldRadius { get; set; } = 36f;

    protected override UnitInfo? OnDo()
    {
        foreach (var unit in this.GetModel<IUnitsModel>().Units)
        {
            if (unit.Position.Value.DistanceTo(WorldPos) <= WorldRadius)
            {
                return unit;
            }
        }

        return null;
    }
}

/// <summary>路径上距鼠标最近的点（用于插入动作点 / 删除路径点）。</summary>
public struct PathPoint
{
    public int UnitId;

    /// <summary>插入位置（段索引）：段 i 连接 Path[i-1]→Path[i]，段 0 连接单位→Path[0]。</summary>
    public int InsertIndex;

    public Vector2 Point;

    /// <summary>该单位路径上最近的路径点索引；-1 表示不靠近路径点（用于删除）。</summary>
    public int WaypointIndex;
}

public class GetClosestPointOnPathQuery : AbstractQuery<PathPoint?>
{
    public Vector2 WorldPos { get; set; }

    public float WorldRadius { get; set; } = 40f;

    protected override PathPoint? OnDo()
    {
        var units = this.GetModel<IUnitsModel>().Units;

        // 1. 吸附已有「动作」路径点（最高优先级）：仅对有动作的路径点吸附，便于修改/删除；
        //    普通路径点（顶点）不吸附，仍按插值处理。
        foreach (var unit in units)
        {
            for (var i = 0; i < unit.Path.Count; i++)
            {
                var wp = unit.Path[i];
                if (wp.Actions.Count > 0 && WorldPos.DistanceTo(wp.Position) <= WorldRadius)
                {
                    return new PathPoint
                    {
                        UnitId = unit.Id,
                        InsertIndex = i,
                        Point = wp.Position,
                        WaypointIndex = i,
                    };
                }
            }
        }

        // 2. 吸附单位（有路径的单位，路径起点）
        foreach (var unit in units)
        {
            if (unit.Path.Count == 0)
            {
                continue;
            }

            if (WorldPos.DistanceTo(unit.Position.Value) <= WorldRadius)
            {
                return new PathPoint
                {
                    UnitId = unit.Id,
                    InsertIndex = 0,
                    Point = unit.Position.Value,
                    WaypointIndex = -1,
                };
            }
        }

        // 3. 吸附普通路径（插值最近点，最低优先级）
        PathPoint? best = null;
        var bestDist = float.MaxValue;
        foreach (var unit in units)
        {
            if (unit.Path.Count == 0)
            {
                continue;
            }

            // 折线：单位当前位置 → 各剩余路径点
            var prev = unit.Position.Value;
            for (var i = 0; i < unit.Path.Count; i++)
            {
                var next = unit.Path[i].Position;
                var closest = Geometry2D.GetClosestPointToSegment(WorldPos, prev, next);
                var dist = WorldPos.DistanceTo(closest);
                if (dist < bestDist)
                {
                    bestDist = dist;
                    best = new PathPoint
                    {
                        UnitId = unit.Id,
                        InsertIndex = i,
                        Point = closest,
                        WaypointIndex = -1,
                    };
                }

                prev = next;
            }
        }

        if (best == null || bestDist > WorldRadius)
        {
            return null;
        }

        return best;
    }
}
