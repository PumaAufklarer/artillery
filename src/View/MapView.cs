namespace artillery;

using Godot;

/// <summary>
/// 战术地图背景：新闻纸灰白底 + 网格 + 边框。
/// </summary>
public partial class MapView : Node2D
{
    private const float HalfSize = 1200f;
    private const float GridStep = 100f;

    public override void _Draw()
    {
        var bounds = new Rect2(-HalfSize, -HalfSize, HalfSize * 2f, HalfSize * 2f);
        DrawRect(bounds, new Color(0.85f, 0.83f, 0.78f));

        var gridColor = new Color(0.6f, 0.58f, 0.53f, 0.7f);
        for (var x = -HalfSize; x <= HalfSize; x += GridStep)
        {
            DrawLine(new Vector2(x, -HalfSize), new Vector2(x, HalfSize), gridColor, 1f);
        }

        for (var y = -HalfSize; y <= HalfSize; y += GridStep)
        {
            DrawLine(new Vector2(-HalfSize, y), new Vector2(HalfSize, y), gridColor, 1f);
        }

        DrawRect(bounds, new Color(0.35f, 0.34f, 0.32f), false, 3f);
    }
}
