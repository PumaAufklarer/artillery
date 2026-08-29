namespace artillery;

public enum UnitState
{
    Idle,
    Moving,
    Waiting,
    Firing,
}

/// <summary>开火流程阶段。</summary>
public enum FirePhase
{
    None,
    TurningToTarget,
    Deploying,
    Laying,
    AwaitingCommand,
    PackingUp,
}

/// <summary>开火流程各阶段时长（秒）。</summary>
public static class FireTimings
{
    public const float DeployTime = 3f;
    public const float LayTime = 1f;
    public const float PackUpTime = 2f;
}
