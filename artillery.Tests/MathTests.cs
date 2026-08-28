using Godot;

namespace artillery.Tests;

public class MathTests
{
    // 纯逻辑单元测试：不依赖 Godot 原生运行时，直接 `dotnet test`。
    // GodotSharp 的内建类型（Vector3/Mathf 等）是纯 C# 结构体，可在测试进程内安全使用；
    // 只有 Node/GodotObject 子类需要 Godot 运行时，那类集成测试用 GodotTestDriver/GoDotTest 在无头进程里跑。
    [Fact]
    public void Vector3_DistanceTo_ReturnsEuclideanDistance()
    {
        var a = new Vector3(0, 0, 0);
        var b = new Vector3(3, 4, 0);
        Assert.Equal(5f, a.DistanceTo(b));
    }
}
