namespace artillery;

using QFramework;

public class SetHoveredUnitCommand : AbstractCommand
{
    private readonly int _unitId;

    public SetHoveredUnitCommand(int unitId) => _unitId = unitId;

    protected override void OnExecute()
    {
        this.GetModel<IHoverModel>().HoveredUnitId.Value = _unitId;
    }
}
