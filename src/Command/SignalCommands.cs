namespace artillery;

using QFramework;

public class TriggerSignalCommand : AbstractCommand
{
    private readonly SignalType _signal;

    public TriggerSignalCommand(SignalType signal) => _signal = signal;

    protected override void OnExecute()
    {
        this.GetSystem<IUnitSystem>().TriggerSignal(_signal);
    }
}
