namespace artillery;

using Godot;

/// <summary>爆炸特效：目标点短暂扩散并淡出的橙色圆环。</summary>
public partial class ExplosionFx : Node2D
{
    private float _age;

    private const float Lifetime = 0.9f;
    private const float MaxRadius = 48f;

    public override void _Process(double delta)
    {
        _age += (float)delta;
        QueueRedraw();

        if (_age >= Lifetime)
        {
            QueueFree();
        }
    }

    public override void _Draw()
    {
        var t = Mathf.Clamp(_age / Lifetime, 0f, 1f);
        var radius = Mathf.Lerp(10f, MaxRadius, t);
        var fade = 1f - t;

        DrawCircle(Vector2.Zero, radius, new Color(1f, 0.3f, 0.08f, fade * 0.7f));
        DrawArc(Vector2.Zero, radius, 0f, Mathf.Tau, 32, new Color(1f, 0.85f, 0.3f, fade), 2f);
    }
}
