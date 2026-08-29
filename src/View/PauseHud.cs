namespace artillery;

using Godot;
using QFramework;

/// <summary>
/// 暂停状态 HUD：屏幕左上角徽标。红底 = 暂停，绿底 = 运行中。
/// </summary>
public partial class PauseHud : Label, IController
{
    private StyleBoxFlat _box = default!;

    public IArchitecture GetArchitecture() => ArtilleryArchitecture.Interface;

    public override void _Ready()
    {
        AddThemeFontSizeOverride("font_size", 20);
        AddThemeColorOverride("font_color", Colors.Black);
        HorizontalAlignment = HorizontalAlignment.Center;
        VerticalAlignment = VerticalAlignment.Center;

        _box = new StyleBoxFlat
        {
            ContentMarginLeft = 14f,
            ContentMarginRight = 14f,
            ContentMarginTop = 5f,
            ContentMarginBottom = 5f,
        };
        AddThemeStyleboxOverride("normal", _box);

        Position = new Vector2(16f, 16f);
        Size = new Vector2(128f, 32f);

        this.GetModel<IGameStateModel>()
            .Paused.RegisterWithInitValue(OnPaused)
            .UnRegisterWhenNodeExitTree(this);
    }

    private void OnPaused(bool paused)
    {
        Text = paused ? "PAUSED" : "RUNNING";
        _box.BgColor = paused
            ? new Color(0.92f, 0.28f, 0.22f, 0.95f)
            : new Color(0.3f, 0.72f, 0.34f, 0.95f);
        QueueRedraw();
    }
}
