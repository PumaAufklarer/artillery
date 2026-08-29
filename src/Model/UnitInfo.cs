namespace artillery;

using System.Collections.Generic;
using Godot;
using QFramework;

public class UnitInfo
{
    public UnitInfo(int id, UnitType type, string designation, Vector2 position, float facing)
    {
        Id = id;
        Type = type;
        Designation = designation;
        Position = new BindableProperty<Vector2>(position);
        Facing = new BindableProperty<float>(facing);
    }

    public int Id { get; }

    public UnitType Type { get; }

    public string Designation { get; }

    public BindableProperty<Vector2> Position { get; }

    public BindableProperty<float> Facing { get; }

    public BindableProperty<UnitState> State { get; } = new(UnitState.Idle);

    /// <summary>当前移动目标（瞬态）。</summary>
    public Vector2 CurrentTarget { get; set; }

    /// <summary>等待中的信号（瞬态，仅 State==Waiting 时有意义）。</summary>
    public SignalType WaitingSignal { get; set; }

    /// <summary>开火流程阶段（瞬态）。</summary>
    public FirePhase FirePhase { get; set; }

    /// <summary>开火流程当前阶段剩余时间（瞬态）。</summary>
    public float FireTimer { get; set; }

    /// <summary>火力打击目标点（瞬态）。</summary>
    public Vector2 FireTarget { get; set; }

    /// <summary>开火等待信号（瞬态，来自路径点上的等待信号动作）。</summary>
    public SignalType FireWaitSignal { get; set; }

    /// <summary>开火流程进度（0..1）；非读条阶段返回 0。</summary>
    public float GetFireProgress()
    {
        return FirePhase switch
        {
            FirePhase.Deploying => 1f - Mathf.Clamp(FireTimer / FireTimings.DeployTime, 0f, 1f),
            FirePhase.Laying => 1f - Mathf.Clamp(FireTimer / FireTimings.LayTime, 0f, 1f),
            FirePhase.PackingUp => 1f - Mathf.Clamp(FireTimer / FireTimings.PackUpTime, 0f, 1f),
            _ => 0f,
        };
    }

    /// <summary>剩余路径点；移动时从头部逐个消费，清空即完成。</summary>
    public List<Waypoint> Path { get; } = new();
}
