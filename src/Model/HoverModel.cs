namespace artillery;

using QFramework;

public interface IHoverModel : IModel
{
    BindableProperty<int> HoveredUnitId { get; }
}

public class HoverModel : AbstractModel, IHoverModel
{
    public BindableProperty<int> HoveredUnitId { get; } = new(-1);

    protected override void OnInit() { }
}
