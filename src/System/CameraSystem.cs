namespace artillery;

using Godot;
using QFramework;

public interface ICameraSystem : ISystem
{
    void Pan(Vector2 worldDelta);

    void ZoomAt(float factor, Vector2 worldAnchor);
}

public class CameraSystem : AbstractSystem, ICameraSystem
{
    private const float MinZoom = 0.25f;
    private const float MaxZoom = 4f;

    public void Pan(Vector2 worldDelta)
    {
        var model = this.GetModel<ICameraModel>();
        model.Position.Value -= worldDelta;
    }

    public void ZoomAt(float factor, Vector2 worldAnchor)
    {
        var model = this.GetModel<ICameraModel>();
        var oldZoom = model.Zoom.Value;
        var newZoom = Mathf.Clamp(oldZoom * factor, MinZoom, MaxZoom);
        var oldPos = model.Position.Value;
        var newPos = oldPos + ((worldAnchor - oldPos) * (1f - (oldZoom / newZoom)));

        model.Zoom.Value = newZoom;
        model.Position.Value = newPos;
    }

    protected override void OnInit() { }
}
