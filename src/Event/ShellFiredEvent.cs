namespace artillery;

using Godot;

/// <summary>炮弹发射事件（用于生成飞行中的炮弹实体）。</summary>
public struct ShellFiredEvent
{
    public Vector2 Origin;

    public Vector2 Target;
}
