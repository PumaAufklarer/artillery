namespace artillery.Tests;

using System.Threading.Tasks;
using Chickensoft.GoDotTest;
using Chickensoft.GodotTestDriver;
using Godot;
using Shouldly;

// GodotTestDriver 集成测试示例：在 Godot 进程内运行（CI 里是 godot --run-tests）。
// 这类测试用于驱动节点/场景、模拟输入等；纯逻辑测试继续走 xUnit 的 `dotnet test`。
public class GameTest : TestClass
{
    private Fixture _fixture = default!;

    public GameTest(Node testScene)
        : base(testScene) { }

    [SetupAll]
    public void Setup()
    {
        _fixture = new Fixture(TestScene.GetTree());
    }

    [CleanupAll]
    public void Cleanup() => _fixture.Cleanup();

    [Test]
    public async Task CanLoadGameScene()
    {
        var game = await _fixture.LoadAndAddScene<Node3D>("res://src/Game.tscn");
        game.ShouldNotBeNull();
        ((string)game.Name).ShouldBe("Game");
    }
}
