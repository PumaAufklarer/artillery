namespace artillery;

using Godot;
using QFramework;

public interface ICameraModel : IModel
{
    BindableProperty<Vector2> Position { get; }

    BindableProperty<float> Zoom { get; }
}

public class CameraModel : AbstractModel, ICameraModel
{
    public BindableProperty<Vector2> Position { get; } = new(Vector2.Zero);

    public BindableProperty<float> Zoom { get; } = new(1f);

    protected override void OnInit() { }
}
