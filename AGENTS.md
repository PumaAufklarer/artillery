# artillery — 工程约定

全局约定见 `~/.dsh/AGENTS.md`（commit message 通用规则、type 枚举、summary/length 规则等）。
本文件补充本项目专属规则。

## Issue

- Issue 标题遵循 commit message 格式：`<type>(<scope>): <summary>`（不含 `(#<id>)`）。

## Pull Request

- 从 Issue 发起 PR（"Create a branch / Create pull request"），得到对应分支与 PR。

## Scope 枚举（本项目）

scope 可选；提供时必须是下列之一（全小写、无空格、用下划线）：

```
# 物理 / 模拟
physics, collision, scene_query, joint, rigid_body, determinism, math

# 渲染 / 表现
shader, vfx, animation, camera

# 玩法 / 系统
gameplay, ai, ui, audio, input, save, net, level, localization

# 工程 / 工具
editor, api, profiler
```

## 归一化说明（相对原始规范做的修正）

- 托管平台为 GitHub："Merge Request" → "Pull Request (PR)"。
- 缺失的"纯资产提交"标签补为 `assets`。
- type 统一加入 `style`、`revert`；`revert` 使用头部形式 `revert: <原 commit 摘要>`。
- scope 第二组名称归一化（conventional scope 要求小写、无空格）：
  - "Determinism" → `determinism`
  - "Scene Query" → `scene_query`
  - "Joint & Rigid Body" → `joint`、`rigid_body`（拆为两个）

## 工程工具

- 代码格式：CSharpier（锁定于 `.config/dotnet-tools.json`）。提交前 `dotnet csharpier format .`，CI 用 `dotnet csharpier check .` 校验。
- Godot 版本：**4.7.2**（`artillery.csproj` 的 `Godot.NET.Sdk` 与编辑器需保持一致）。
- 测试分两层：
  - 纯逻辑单元测试：`dotnet test`（xUnit，见 `artillery.Tests`）。
  - 节点/场景集成测试：Chickensoft.GoDotTest + GodotTestDriver（见 `test/`）；本地 `godot --run-tests --quit-on-finish`，CI 里 `godot --headless --audio-driver Dummy --run-tests --quit-on-finish`。
- CI：`.github/workflows/ci.yml`（format → build → unit test → godot integration test → export）。
