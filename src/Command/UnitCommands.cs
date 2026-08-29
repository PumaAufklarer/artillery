namespace artillery;

using System.Collections.Generic;
using Godot;
using QFramework;

public class MoveAlongPathCommand : AbstractCommand
{
    private readonly int _unitId;
    private readonly IReadOnlyList<Vector2> _path;

    public MoveAlongPathCommand(int unitId, IReadOnlyList<Vector2> path)
    {
        _unitId = unitId;
        _path = path;
    }

    protected override void OnExecute()
    {
        var unit = this.GetModel<IUnitsModel>().GetUnit(_unitId);
        if (unit == null)
        {
            return;
        }

        // 轨迹坐标 → 普通路径点
        unit.Path.Clear();
        foreach (var point in _path)
        {
            unit.Path.Add(new Waypoint(point));
        }
    }
}

public class AddSignalActionCommand : AbstractCommand
{
    private readonly int _unitId;
    private readonly int _insertIndex;
    private readonly Vector2 _position;
    private readonly int _waypointIndex;
    private readonly SignalType _signal;

    public AddSignalActionCommand(
        int unitId,
        int insertIndex,
        Vector2 position,
        int waypointIndex,
        SignalType signal
    )
    {
        _unitId = unitId;
        _insertIndex = insertIndex;
        _position = position;
        _waypointIndex = waypointIndex;
        _signal = signal;
    }

    protected override void OnExecute()
    {
        var unit = this.GetModel<IUnitsModel>().GetUnit(_unitId);
        if (unit == null)
        {
            return;
        }

        // 靠近已有路径点：修改其动作
        if (_waypointIndex >= 0 && _waypointIndex < unit.Path.Count)
        {
            unit.Path[_waypointIndex].SetAction(new WaitSignalAction(_signal));
            return;
        }

        // 否则在插值点新建路径点
        var waypoint = new Waypoint(_position);
        waypoint.SetAction(new WaitSignalAction(_signal));
        WaypointCommandHelper.Insert(unit, _insertIndex, waypoint);
    }
}

public class RemoveSignalActionCommand : AbstractCommand
{
    private readonly int _unitId;
    private readonly int _waypointIndex;

    public RemoveSignalActionCommand(int unitId, int waypointIndex)
    {
        _unitId = unitId;
        _waypointIndex = waypointIndex;
    }

    protected override void OnExecute()
    {
        var unit = this.GetModel<IUnitsModel>().GetUnit(_unitId);
        if (unit == null || _waypointIndex < 0 || _waypointIndex >= unit.Path.Count)
        {
            return;
        }

        unit.Path[_waypointIndex].RemoveAction<WaitSignalAction>();
    }
}

public class AddFireActionCommand : AbstractCommand
{
    private readonly int _unitId;
    private readonly int _insertIndex;
    private readonly Vector2 _position;
    private readonly int _waypointIndex;
    private readonly Vector2 _target;
    private readonly ShellType _shellType;

    public AddFireActionCommand(
        int unitId,
        int insertIndex,
        Vector2 position,
        int waypointIndex,
        Vector2 target,
        ShellType shellType
    )
    {
        _unitId = unitId;
        _insertIndex = insertIndex;
        _position = position;
        _waypointIndex = waypointIndex;
        _target = target;
        _shellType = shellType;
    }

    protected override void OnExecute()
    {
        var unit = this.GetModel<IUnitsModel>().GetUnit(_unitId);
        if (unit == null)
        {
            return;
        }

        // 靠近已有路径点：修改其动作
        if (_waypointIndex >= 0 && _waypointIndex < unit.Path.Count)
        {
            unit.Path[_waypointIndex].SetAction(new FireAction(_target, _shellType));
            return;
        }

        // 否则在插值点新建路径点
        var waypoint = new Waypoint(_position);
        waypoint.SetAction(new FireAction(_target, _shellType));
        WaypointCommandHelper.Insert(unit, _insertIndex, waypoint);
    }
}

public class DeleteWaypointCommand : AbstractCommand
{
    private readonly int _unitId;
    private readonly int _waypointIndex;

    public DeleteWaypointCommand(int unitId, int waypointIndex)
    {
        _unitId = unitId;
        _waypointIndex = waypointIndex;
    }

    protected override void OnExecute()
    {
        var unit = this.GetModel<IUnitsModel>().GetUnit(_unitId);
        if (unit == null || _waypointIndex < 0 || _waypointIndex >= unit.Path.Count)
        {
            return;
        }

        unit.Path.RemoveAt(_waypointIndex);

        // 删除的是队首（当前目标/等待点）：需要重定向
        if (_waypointIndex != 0)
        {
            return;
        }

        switch (unit.State.Value)
        {
            case UnitState.Moving:
                if (unit.Path.Count == 0)
                {
                    unit.State.Value = UnitState.Idle;
                }
                else
                {
                    unit.CurrentTarget = unit.Path[0].Position;
                }

                break;

            case UnitState.Waiting:
                unit.WaitingSignal = SignalType.None;
                if (unit.Path.Count == 0)
                {
                    unit.State.Value = UnitState.Idle;
                }
                else
                {
                    unit.CurrentTarget = unit.Path[0].Position;
                    unit.State.Value = UnitState.Moving;
                }

                break;

            case UnitState.Firing:
                // 取消开火流程（已发射的炮弹仍会命中）；清空瞬态后继续移动
                unit.FirePhase = FirePhase.None;
                unit.FireWaitSignal = SignalType.None;
                if (unit.Path.Count == 0)
                {
                    unit.State.Value = UnitState.Idle;
                }
                else
                {
                    unit.CurrentTarget = unit.Path[0].Position;
                    unit.State.Value = UnitState.Moving;
                }

                break;
        }
    }
}

internal static class WaypointCommandHelper
{
    public static void Insert(UnitInfo unit, int index, Waypoint waypoint)
    {
        var i = Mathf.Clamp(index, 0, unit.Path.Count);
        unit.Path.Insert(i, waypoint);

        // 插入点在当前目标之前：重定向，让部队先经过该点
        if (i == 0 && unit.State.Value == UnitState.Moving)
        {
            unit.CurrentTarget = waypoint.Position;
        }
    }
}
