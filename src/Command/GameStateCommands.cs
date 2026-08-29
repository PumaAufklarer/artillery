namespace artillery;

using QFramework;

public class TogglePauseCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        var model = this.GetModel<IGameStateModel>();
        model.Paused.Value = !model.Paused.Value;
    }
}
