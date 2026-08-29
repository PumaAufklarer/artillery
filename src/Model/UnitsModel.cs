namespace artillery;

using System.Collections.Generic;
using Godot;
using QFramework;

public interface IUnitsModel : IModel
{
    IReadOnlyList<UnitInfo> Units { get; }

    UnitInfo? GetUnit(int id);
}

public class UnitsModel : AbstractModel, IUnitsModel
{
    private readonly List<UnitInfo> _units = new();

    public IReadOnlyList<UnitInfo> Units => _units;

    public UnitInfo? GetUnit(int id) => _units.Find(u => u.Id == id);

    protected override void OnInit()
    {
        const float faceUp = -Mathf.Pi / 2f;
        Add(0, UnitType.Artillery, "A1", new Vector2(-400f, 150f), faceUp);
        Add(1, UnitType.Artillery, "A2", new Vector2(-400f, -150f), faceUp);
        Add(2, UnitType.OpticalObserver, "O1", new Vector2(0f, 300f), faceUp);
        Add(3, UnitType.OpticalObserver, "O2", new Vector2(0f, -300f), faceUp);
    }

    private void Add(int id, UnitType type, string designation, Vector2 position, float facing)
    {
        _units.Add(new UnitInfo(id, type, designation, position, facing));
    }
}
