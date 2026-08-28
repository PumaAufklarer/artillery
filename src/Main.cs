namespace artillery;

using Godot;
#if RUN_TESTS
using System.Reflection;
using Chickensoft.GoDotTest;
#endif

// 入口场景：根据命令行参数决定「跑集成测试」还是「进入游戏」。
// 游戏实际内容见 res://src/Game.tscn。
public partial class Main : Node
{
#if RUN_TESTS
    private TestEnvironment _environment = default!;
#endif

    public override void _Ready()
    {
#if RUN_TESTS
        // GoDotTest 通过 --run-tests / --quit-on-finish 等命令行参数触发测试。
        _environment = TestEnvironment.From(OS.GetCmdlineArgs());
        if (_environment.ShouldRunTests)
        {
            Callable.From(RunTests).CallDeferred();
            return;
        }
#endif
        // 正常运行：切到游戏场景。
        Callable.From(RunScene).CallDeferred();
    }

#if RUN_TESTS
    private void RunTests() =>
        _ = GoTest.RunTests(Assembly.GetExecutingAssembly(), this, _environment);
#endif

    private void RunScene() => GetTree().ChangeSceneToFile("res://src/Game.tscn");
}
