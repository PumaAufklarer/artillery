namespace artillery;

using Godot;
using QFramework;

public class PanCameraCommand : AbstractCommand
{
    private readonly Vector2 _worldDelta;

    public PanCameraCommand(Vector2 worldDelta) => _worldDelta = worldDelta;

    protected override void OnExecute()
    {
        this.GetSystem<ICameraSystem>().Pan(_worldDelta);
    }
}

public class ZoomCameraCommand : AbstractCommand
{
    private readonly float _factor;
    private readonly Vector2 _worldAnchor;

    public ZoomCameraCommand(float factor, Vector2 worldAnchor)
    {
        _factor = factor;
        _worldAnchor = worldAnchor;
    }

    protected override void OnExecute()
    {
        this.GetSystem<ICameraSystem>().ZoomAt(_factor, _worldAnchor);
    }
}
