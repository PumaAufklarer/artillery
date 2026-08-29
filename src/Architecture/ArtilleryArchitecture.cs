namespace artillery;

using QFramework;

public class ArtilleryArchitecture : Architecture<ArtilleryArchitecture>
{
    protected override void Init()
    {
        RegisterModel<IUnitsModel>(new UnitsModel());
        RegisterModel<ISelectionModel>(new SelectionModel());
        RegisterModel<ICameraModel>(new CameraModel());
        RegisterModel<IGameStateModel>(new GameStateModel());
        RegisterModel<IHoverModel>(new HoverModel());

        RegisterSystem<IUnitSystem>(new UnitSystem());
        RegisterSystem<ICameraSystem>(new CameraSystem());
    }
}
