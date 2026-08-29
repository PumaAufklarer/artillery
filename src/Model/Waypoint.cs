namespace artillery;

using System.Collections.Generic;
using System.Linq;
using Godot;

/// <summary>等待信号类型。</summary>
public enum SignalType
{
    None,
    Alpha,
    Bravo,
    Charly,
}

/// <summary>炮弹种类。</summary>
public enum ShellType
{
    He,
}

/// <summary>路径点动作基类：路径点可挂载零到多个动作。</summary>
public abstract class WaypointAction { }

/// <summary>等待信号动作：抵达后停下等待指定信号。</summary>
public class WaitSignalAction : WaypointAction
{
    public WaitSignalAction(SignalType signal) => Signal = signal;

    /// <summary>等待信号（恒非 None）。</summary>
    public SignalType Signal { get; }
}

/// <summary>火力打击动作：抵达后转向目标，执行部署/装定/开炮/装车流程。</summary>
public class FireAction : WaypointAction
{
    public FireAction(Vector2 target, ShellType shellType)
    {
        Target = target;
        ShellType = shellType;
    }

    /// <summary>火力打击目标点（世界坐标）。</summary>
    public Vector2 Target { get; }

    /// <summary>炮弹种类。</summary>
    public ShellType ShellType { get; }
}

/// <summary>
/// 路径点：一个位置 + 零到多个动作。普通路径点 = 无动作；动作路径点 = 挂有动作。
/// 动作可组合（如「等待信号 + 火力打击」），未来新增动作类型无需改动本类。
/// </summary>
public class Waypoint
{
    public Waypoint(Vector2 position) => Position = position;

    /// <summary>路径点世界坐标。</summary>
    public Vector2 Position { get; }

    /// <summary>挂载的动作集合。</summary>
    public List<WaypointAction> Actions { get; } = new();

    public T? GetAction<T>()
        where T : WaypointAction => Actions.OfType<T>().FirstOrDefault();

    /// <summary>替换同类动作（信号/火力等每种动作至多一个）。</summary>
    public void SetAction<T>(T action)
        where T : WaypointAction
    {
        Actions.RemoveAll(a => a is T);
        Actions.Add(action);
    }

    public void RemoveAction<T>()
        where T : WaypointAction => Actions.RemoveAll(a => a is T);
}
