# 图形与非交互探针：代码及验收说明

从原 setup.md 按主题迁移，保留详细步骤与学习内容。按需查阅，不是新对话必读。
当前完成状态以 [status.md](../status.md) 为准；[返回搭建导航](../setup.md)。已完成步骤不要重复执行。

> 复查说明：以下是已使用的临时探针示例，实际代码以 Sandbox/Program.cs 为准。`--smoke` 的 PASS 仅表示打印流程成功，版本、架构仍需人工对照；缩放、高 DPI、Escape 与关闭按钮的逐项回归证据未完整收集。改进建议见 [阶段复查](../reviews/foundation-review-2026-10-02.md)，不据此重启已暂缓的 D5。

### 4.3 最小 OpenGL 环境探针

OpenGL 的实际能力由显卡及驱动提供；OpenTK 提供 C# 调用接口。Windows 的驱动加载机制见 [OpenGL 驱动说明](https://learn.microsoft.com/en-us/windows-hardware/drivers/display/loading-an-opengl-installable-client-driver)。本工程不要求先下载一个独立的“OpenGL 4.3 SDK”。

包安装成功不等于 GPU 环境验证通过。本节探针只验证以下内容：

- 请求 **OpenGL 4.3 Core** 上下文，记录实际版本、厂商、渲染设备与配置。
- 创建并显示窗口，调整大小，正常关闭并释放资源。
- 不支持时保留完整错误，按驱动、上下文创建和设备选择诊断，不静默降级。

它不是正式渲染器、窗口抽象或引擎主循环，不决定未来类名、程序集边界和生命周期设计。代码暂放 Sandbox，验证通过后再讨论保留、拆分或替换方式；不要据此开始设计引擎架构。

#### 学习说明：这一步究竟验证什么

NuGet 只证明 OpenTK 包能够还原和编译；真正创建窗口时，OpenTK 还要调用本机的 GLFW 原生库和显卡驱动。只有创建上下文后，`GL.GetString`、`GL.GetInteger` 等 OpenGL 查询才有意义。因此这一步同时穿过：

```text
C# Sandbox
    → OpenTK 窗口与 OpenGL 绑定
    → GLFW 原生窗口层
    → Windows 图形系统与显卡驱动
    → 实际 OpenGL 上下文
```

看到显卡型号或驱动已安装，不能代替这条调用链的真实运行。窗口能出现也不够，还要读取实际版本并确认是 Core Profile、至少 4.3。

#### 用户操作：替换 Sandbox 的临时入口

1. 停止调试，选择 `Debug | x64`，打开 `samples/G104.Sandbox/Program.cs`。将当前 Hello World 模板完整替换为下面代码并保存。只改这个文件，不移动包、不修改 Engine 或锁文件。

```csharp
using OpenTK.Graphics.OpenGL4;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;

// 这是环境探针，只验证窗口、驱动和 OpenGL 上下文，不代表正式引擎架构。
using var window = new OpenGlProbeWindow();
window.Run();

internal sealed class OpenGlProbeWindow : GameWindow
{
    public OpenGlProbeWindow()
        : base(
            GameWindowSettings.Default,
            new NativeWindowSettings
            {
                ClientSize = new Vector2i(960, 540),
                Title = "G104Engine - OpenGL Environment Probe",
                API = ContextAPI.OpenGL,
                APIVersion = new Version(4, 3),
                Profile = ContextProfile.Core,
                Flags = ContextFlags.ForwardCompatible
            })
    {
    }

    protected override void OnLoad()
    {
        base.OnLoad();

        // OpenGL 查询必须在上下文创建并绑定到当前线程后执行。
        int major = GL.GetInteger(GetPName.MajorVersion);
        int minor = GL.GetInteger(GetPName.MinorVersion);
        int profileMask = GL.GetInteger(GetPName.ContextProfileMask);
        bool isCoreProfile =
            (profileMask & (int)ContextProfileMask.ContextCoreProfileBit) != 0;

        Console.WriteLine($"OpenGL vendor:   {GL.GetString(StringName.Vendor)}");
        Console.WriteLine($"OpenGL renderer: {GL.GetString(StringName.Renderer)}");
        Console.WriteLine($"OpenGL version:  {GL.GetString(StringName.Version)}");
        Console.WriteLine($"GLSL version:    {GL.GetString(StringName.ShadingLanguageVersion)}");
        Console.WriteLine($"Numeric version: {major}.{minor}");
        Console.WriteLine($"Core profile:    {isCoreProfile}");
        Console.WriteLine("Resize the window; press Escape or close the window to exit.");

        if (major < 4 || (major == 4 && minor < 3) || !isCoreProfile)
        {
            throw new NotSupportedException(
                $"Expected OpenGL 4.3 Core or newer, got {major}.{minor}, Core={isCoreProfile}.");
        }

        VSync = VSyncMode.On;
        GL.ClearColor(0.06f, 0.10f, 0.16f, 1.0f);
    }

    protected override void OnResize(ResizeEventArgs e)
    {
        base.OnResize(e);
        GL.Viewport(0, 0, e.Width, e.Height);
    }

    protected override void OnUpdateFrame(FrameEventArgs e)
    {
        base.OnUpdateFrame(e);

        if (KeyboardState.IsKeyDown(Keys.Escape))
        {
            Close();
        }
    }

    protected override void OnRenderFrame(FrameEventArgs e)
    {
        base.OnRenderFrame(e);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        SwapBuffers();
    }
}
```

2. 在 VS 中生成 `Debug | x64`。有编译错误时提供完整错误列表，不自行改 API 名称或包版本；助手会按 OpenTK 4.9.4 核对。
3. 按 F5 运行。预期出现深蓝灰色窗口和控制台文本；拖动窗口边缘确认可调整大小，然后按 Escape 退出。也测试一次右上角关闭按钮，确认程序正常结束。
4. 复制控制台中的 vendor、renderer、OpenGL version、GLSL version、numeric version 和 Core profile 六项结果给助手。显卡名称本身不是敏感凭据，可以记录到进度文档；无需把个人截图目录纳入工程。
5. Debug 通过后选择 `Release | x64`，使用 Ctrl+F5 运行同一探针，重复窗口显示、版本输出、缩放和退出检查。Release 普通运行不需要关闭“仅我的代码”。

#### 验收标准和失败处理

- Debug 与 Release 都能生成、启动窗口、调整大小并通过 Escape/关闭按钮正常退出。
- `Numeric version` 至少为 `4.3`，`Core profile` 必须为 `True`；更高版本符合要求，不要求强制显示恰好 4.3。
- vendor、renderer、OpenGL/GLSL 版本均能读取，不能用空字符串或猜测值代替。
- 若创建上下文或加载原生库失败，保留异常类型、完整消息和调用栈；不要静默改成较低 OpenGL 版本，也不要先升级驱动、重装包或关闭其他安全设置。
- 当前没有分配 GPU 对象，因此 `using` 结束时由 `GameWindow.Dispose` 释放窗口和上下文。后续引入缓冲、纹理等 GPU 资源时，需要在上下文有效的生命周期内显式释放。

用户反馈显示效果和输出，助手再读取真实代码并核对运行证据。显卡驱动只有在发现具体问题后再讨论更新；用户负责相应可视化操作。`--smoke` 非交互入口已在第 4.4 节创建并通过检查，其结果不能替代本机 GPU 验证。

API 依据：[OpenTK 创建窗口教程](https://opentk.net/learn/chapter1/1-creating-a-window.html)、[NativeWindowSettings](https://opentk.net/api/OpenTK.Windowing.Desktop.NativeWindowSettings.html)。本节代码还依据本机已安装 OpenTK 4.9.4 的 XML API 文档核对了构造函数、上下文设置、帧回调及 OpenGL 查询签名。

**本机验收通过（2026-10-02）：** 用户在 VS 写入上述探针，反馈 Debug/Release 均可显示和关闭；窗口截图符合预期，控制台输出为 NVIDIA GeForce RTX 5060 Ti、OpenGL 4.3.0、GLSL 4.30、数值版本 4.3、Core Profile=True。助手使用锁定后的现有依赖独立构建两个 x64 配置，均为 0 警告、0 错误。图形运行证据来自用户的可见会话，助手没有代为启动窗口。

### 4.4 非交互 `--smoke` 检查

窗口探针验证 GPU 和图形上下文；`--smoke` 解决另一个问题：在不创建窗口的情况下，快速检查当前程序能否启动、是否为 x64，以及三个 OpenTK 程序集能否由 .NET 加载。以后可用于命令行、CI 和故障定位。

它不调用 GLFW 初始化、不创建 OpenGL 上下文，因此不能证明 GPU、驱动或窗口可用，也不能取代第 4.3 节。两个结果应分别记录：

| 检查 | 能证明 | 不能证明 |
| --- | --- | --- |
| 第 4.3 节窗口探针 | 本机 OpenGL/GLFW/驱动/窗口调用链可用 | 无图形会话的 CI 一定可运行 |
| `--smoke` | 托管程序、架构和 OpenTK 程序集可加载，无需窗口 | OpenGL 上下文和 GPU 功能可用 |

#### 用户操作：为现有 Program.cs 增加分支

1. 停止调试，打开 Sandbox 的 `Program.cs`。在当前 using 列表最上方增加：

```csharp
using System.Reflection;
using System.Runtime.InteropServices;
```

2. 在当前注释“这是环境探针”及 `using var window` **之前**插入：

```csharp
if (Array.Exists(
        args,
        argument => string.Equals(
            argument,
            "--smoke",
            StringComparison.OrdinalIgnoreCase)))
{
    // 非交互检查只加载托管程序集，不创建窗口或 OpenGL 上下文。
    Console.WriteLine("Mode:             smoke");
    Console.WriteLine($"Framework:        {RuntimeInformation.FrameworkDescription}");
    Console.WriteLine($"OS:               {RuntimeInformation.OSDescription}");
    Console.WriteLine($"Process arch:     {RuntimeInformation.ProcessArchitecture}");
    Console.WriteLine($"OpenTK.Graphics:  {GetAssemblyVersion(typeof(GL).Assembly)}");
    Console.WriteLine($"OpenTK.Windowing: {GetAssemblyVersion(typeof(GameWindow).Assembly)}");
    Console.WriteLine($"OpenTK.Math:      {GetAssemblyVersion(typeof(Vector2i).Assembly)}");
    Console.WriteLine("Smoke result:     PASS");
    return;
}
```

3. 在 `window.Run();` 之后、`OpenGlProbeWindow` 类声明之前插入这个本地函数：

```csharp
static string GetAssemblyVersion(Assembly assembly)
{
    return assembly
        .GetCustomAttribute<AssemblyInformationalVersionAttribute>()
        ?.InformationalVersion
        ?? assembly.GetName().Version?.ToString()
        ?? "unknown";
}
```

原窗口代码保持不变。`return` 只结束 `--smoke` 分支；没有该参数时仍进入第 4.3 节窗口探针。

#### 用户操作：在 VS 终端运行

1. 保存后，通过 VS 生成一次 `Debug | x64`。
2. 打开“视图 → 终端”。确认提示符当前目录为 `E:\game_study\games104\g104engine`；若不是，先在终端执行：

```powershell
Set-Location E:\game_study\games104\g104engine
```

3. 执行 Debug 检查：

```powershell
dotnet run --project samples/G104.Sandbox/G104.Sandbox.csproj --configuration Debug -p:Platform=x64 --no-restore -- --smoke
```

4. 执行 Release 检查：

```powershell
dotnet run --project samples/G104.Sandbox/G104.Sandbox.csproj --configuration Release -p:Platform=x64 --no-restore -- --smoke
```

这里第一个 `--` 是 `dotnet run` 与程序参数的分隔符，后面的 `--smoke` 才会传给 `Program.cs`。`--no-restore` 使用已经严格还原并锁定的依赖，不在这次检查中下载或更新包。

#### 验收标准

- 两个配置都输出 `Mode: smoke`、框架、操作系统、`Process arch: X64`、三个 OpenTK 程序集版本及 `Smoke result: PASS`，退出码为 0。
- 运行期间不出现图形窗口；若仍出现窗口，优先检查参数分隔符和代码插入位置。
- 完成后再不带 `--smoke` 运行一次 Debug 窗口探针，确认默认路径未被破坏。
- 把两次终端输出发给助手核对。助手随后检查实际代码，并用现有锁定依赖重复非交互命令。

该分支仍属于环境验证代码，不是正式命令行系统或引擎启动架构。基础阶段完成后的设计讨论可以决定保留、迁移或替换它。

**本机验收通过（2026-10-02）：** 用户在 VS 终端执行 Debug/Release 两条命令，截图显示均为 X64、.NET 10.0.12、三个 OpenTK 程序集 4.9.4，并输出 `Smoke result: PASS`；助手使用相同的 `--no-restore` 命令重复检查，两次退出码均为 0。实际源码核对确认无参数时仍进入第 4.3 节窗口路径。

