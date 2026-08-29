namespace artillery;

using QFramework;

public interface IGameStateModel : IModel
{
    BindableProperty<bool> Paused { get; }
}

public class GameStateModel : AbstractModel, IGameStateModel
{
    public BindableProperty<bool> Paused { get; } = new(false);

    protected override void OnInit() { }
}
