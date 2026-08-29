namespace artillery;

using Godot;
using QFramework;

public interface IUnitSystem : ISystem
{
    void Tick(float delta);

    void TriggerSignal(SignalType signal);
}

public class UnitSystem : AbstractSystem, IUnitSystem
{
    private const float MoveSpeed = 160f;
    private const float TurnSpeed = 4f;

    private const float MuzzleOffset = 20f;
    private const float TurnInPlaceThreshold = Mathf.Pi / 2f;

    public void Tick(float delta)
    {
        // 暂停时单位不移动/不转向/不读条
        if (this.GetModel<IGameStateModel>().Paused.Value)
        {
            return;
        }

        var model = this.GetModel<IUnitsModel>();
        foreach (var unit in model.Units)
        {
            switch (unit.State.Value)
            {
                case UnitState.Idle:
                    ProcessNextWaypoint(unit);
                    break;
                case UnitState.Moving:
                    TickMove(unit, delta);
                    break;
                case UnitState.Waiting:
                    // 等待信号触发，不移动
                    break;
                case UnitState.Firing:
                    TickFire(unit, delta);
                    break;
            }
        }
    }

    private void ProcessNextWaypoint(UnitInfo unit)
    {
        if (unit.Path.Count == 0)
        {
            return;
        }

        unit.CurrentTarget = unit.Path[0].Position;
        unit.State.Value = UnitState.Moving;
    }

    private void TickMove(UnitInfo unit, float delta)
    {
        var toTarget = unit.CurrentTarget - unit.Position.Value;
        var distance = toTarget.Length();
        var step = MoveSpeed * delta;

        if (distance <= step)
        {
            unit.Position.Value = unit.CurrentTarget;
            AdvanceWaypoint(unit);
            return;
        }

        // 炮尾朝行进方向
        var dir = toTarget / distance;
        var targetFacing = dir.Angle();
        var diff = Mathf.AngleDifference(unit.Facing.Value, targetFacing);
        var maxTurn = TurnSpeed * delta;
        unit.Facing.Value += Mathf.Clamp(diff, -maxTurn, maxTurn);

        // 差别过大：本帧只原地转，不移动
        if (Mathf.Abs(diff) > TurnInPlaceThreshold)
        {
            return;
        }

        unit.Position.Value += dir * step;
    }

    /// <summary>
    /// 抵达当前路径点：按挂载的动作组合执行——火力进入开火流程（可叠加等待信号），
    /// 仅等待信号则停下等信号，无动作则立即续行（不进入 Idle，避免卡顿）。
    /// </summary>
    private void AdvanceWaypoint(UnitInfo unit)
    {
        var waypoint = unit.Path[0];
        var fire = waypoint.GetAction<FireAction>();
        var wait = waypoint.GetAction<WaitSignalAction>();

        if (fire != null)
        {
            StartFire(unit, fire, wait?.Signal ?? SignalType.None);
            return;
        }

        if (wait != null)
        {
            unit.WaitingSignal = wait.Signal;
            unit.State.Value = UnitState.Waiting;
            return;
        }

        unit.Path.RemoveAt(0);
        ContinueOrIdle(unit);
    }

    private void ContinueOrIdle(UnitInfo unit)
    {
        if (unit.Path.Count == 0)
        {
            unit.State.Value = UnitState.Idle;
            return;
        }

        unit.CurrentTarget = unit.Path[0].Position;
        unit.State.Value = UnitState.Moving;
    }

    private void StartFire(UnitInfo unit, FireAction fire, SignalType waitSignal)
    {
        unit.FireTarget = fire.Target;
        unit.FireWaitSignal = waitSignal;
        unit.FirePhase = FirePhase.TurningToTarget;
        unit.FireTimer = 0f;
        unit.State.Value = UnitState.Firing;
    }

    private void TickFire(UnitInfo unit, float delta)
    {
        switch (unit.FirePhase)
        {
            case FirePhase.TurningToTarget:
                TickFireTurning(unit, delta);
                break;

            case FirePhase.Deploying:
                unit.FireTimer -= delta;
                if (unit.FireTimer <= 0f)
                {
                    unit.FirePhase = FirePhase.Laying;
                    unit.FireTimer = FireTimings.LayTime;
                }

                break;

            case FirePhase.Laying:
                unit.FireTimer -= delta;
                if (unit.FireTimer <= 0f)
                {
                    if (unit.FireWaitSignal != SignalType.None)
                    {
                        unit.FirePhase = FirePhase.AwaitingCommand;
                    }
                    else
                    {
                        Fire(unit);
                    }
                }

                break;

            case FirePhase.AwaitingCommand:
                // 等待开火指令（由 TriggerSignal 触发）
                break;

            case FirePhase.PackingUp:
                unit.FireTimer -= delta;
                if (unit.FireTimer <= 0f)
                {
                    FinishFire(unit);
                }

                break;
        }
    }

    private void TickFireTurning(UnitInfo unit, float delta)
    {
        var targetFacing = (unit.FireTarget - unit.Position.Value).Angle() + Mathf.Pi;
        var diff = Mathf.AngleDifference(unit.Facing.Value, targetFacing);
        var maxTurn = TurnSpeed * delta;

        if (Mathf.Abs(diff) <= maxTurn)
        {
            unit.Facing.Value = targetFacing;
            unit.FirePhase = FirePhase.Deploying;
            unit.FireTimer = FireTimings.DeployTime;
        }
        else
        {
            unit.Facing.Value += Mathf.Clamp(diff, -maxTurn, maxTurn);
        }
    }

    private void Fire(UnitInfo unit)
    {
        // 发射：炮口（沿炮管方向偏移）发射飞行中的炮弹；Facing 是炮尾方向，炮口 = Facing + π
        var dir = Vector2.Right.Rotated(unit.Facing.Value + Mathf.Pi);
        var origin = unit.Position.Value + (dir * MuzzleOffset);

        this.SendEvent(new ShellFiredEvent { Origin = origin, Target = unit.FireTarget });

        unit.FirePhase = FirePhase.PackingUp;
        unit.FireTimer = FireTimings.PackUpTime;
    }

    private void FinishFire(UnitInfo unit)
    {
        unit.FirePhase = FirePhase.None;
        unit.Path.RemoveAt(0);
        ContinueOrIdle(unit);
    }

    public void TriggerSignal(SignalType signal)
    {
        foreach (var unit in this.GetModel<IUnitsModel>().Units)
        {
            // 仅等待信号的路径点：继续移动
            if (unit.State.Value == UnitState.Waiting && unit.WaitingSignal == signal)
            {
                unit.WaitingSignal = SignalType.None;
                unit.Path.RemoveAt(0);
                ContinueOrIdle(unit);
            }
            // 火力路径点等待开火指令：开炮
            else if (
                unit.State.Value == UnitState.Firing
                && unit.FirePhase == FirePhase.AwaitingCommand
                && unit.FireWaitSignal == signal
            )
            {
                Fire(unit);
            }
        }
    }

    protected override void OnInit() { }
}
