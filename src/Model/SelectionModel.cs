namespace artillery;

using QFramework;

public interface ISelectionModel : IModel
{
    BindableProperty<int> SelectedUnitId { get; }
}

public class SelectionModel : AbstractModel, ISelectionModel
{
    public BindableProperty<int> SelectedUnitId { get; } = new(-1);

    protected override void OnInit() { }
}
