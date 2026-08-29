namespace artillery;

using QFramework;

public class SelectUnitCommand : AbstractCommand
{
    private readonly int _unitId;

    public SelectUnitCommand(int unitId) => _unitId = unitId;

    protected override void OnExecute()
    {
        this.GetModel<ISelectionModel>().SelectedUnitId.Value = _unitId;
    }
}

public class DeselectUnitCommand : AbstractCommand
{
    protected override void OnExecute()
    {
        this.GetModel<ISelectionModel>().SelectedUnitId.Value = -1;
    }
}
