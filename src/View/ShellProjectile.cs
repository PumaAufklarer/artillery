namespace artillery;

using Godot;
using QFramework;

/// <summary>飞行中的炮弹实体：从发射点飞向目标，命中后生成爆炸特效。</summary>
public partial class ShellProjectile : Node2D, IController
{
    private Vector2 _target;

    private const float Speed = 600f;

    public IArchitecture GetArchitecture() => ArtilleryArchitecture.Interface;

    public void Setup(Vector2 target)
    {
        _target = target;
        Rotation = (target - Position).Angle();
    }

    public override void _Process(double delta)
    {
        if (this.GetModel<IGameStateModel>().Paused.Value)
        {
            return;
        }

        var toTarget = _target - Position;
        var distance = toTarget.Length();
        var step = Speed * (float)delta;

        if (distance <= step)
        {
            var fx = new ExplosionFx();
            fx.Position = _target;
            GetParent().AddChild(fx);
            QueueFree();
            return;
        }

        var dir = toTarget / distance;
        Position += dir * step;
        Rotation = dir.Angle();
    }

    public override void _Draw()
    {
        // 炮弹：短线段表示飞行方向
        DrawLine(new Vector2(-4f, 0f), new Vector2(4f, 0f), new Color(0.1f, 0.1f, 0.1f), 2f);
        DrawCircle(new Vector2(3f, 0f), 1.5f, new Color(1f, 0.5f, 0.1f));
    }
}
