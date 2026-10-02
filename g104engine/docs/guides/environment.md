# 环境安装、工程创建与排错

从原 setup.md 按主题迁移，保留详细步骤与学习内容。按需查阅，不是新对话必读。
当前完成状态以 [status.md](../status.md) 为准；[返回搭建导航](../setup.md)。已完成步骤不要重复执行。

## 1. 安装 VS2026 与 .NET 10（本机已通过核验）

2026-10-02 核验通过：本机已安装 **VS2026 Community 18.10.3（Stable）**、**.NET 10 SDK 10.0.401（x64）**，VS 的“.NET 桌面开发”、SDK、C# 编译器、MSBuild 和 NuGet 组件均已检出；.NET 与桌面运行时为 10.0.12。第 2 节项目配置、第 3 节三个包的引用和还原结果已核对。用户随后关闭本机 SAC，反馈 Debug/Release 均恢复 Hello World 输出；最新复核结果见 [进度](../status.md)。已完成步骤保留供复现，不必重复安装或创建工程。

GitHub 建仓使用网页，本地 Git 操作由用户通过 Git Bash／Git GUI 完成，不要求安装 GitHub CLI；VS 继续用于工程编辑与调试。

### 用户操作

1. 打开 [Visual Studio 官方下载页](https://visualstudio.microsoft.com/downloads/)，选择 **Visual Studio 2026 正式稳定版**。个人学习默认使用 Community；已有其他合适版本也可继续使用。保留原有 VS2022。
2. 运行安装器，选择手动配置工作负载，在“工作负载”中勾选 **“.NET 桌面开发”**。本阶段使用 C# 控制台和类库，这个工作负载满足工程创建需要。
3. 查看安装详细信息或“单个组件”。SDK 组件通常显示为 **“.NET SDK”**，并不一定叫“.NET 10 SDK”；组件 ID 为 `Microsoft.NetCore.Component.SDK`。它与“.NET 10.0 Runtime”是不同组件，不能只凭运行时条目判定 SDK 是否安装。该 SDK 项属于“.NET 桌面开发”的必需组件，见 [微软组件清单](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-community?view=visualstudio)。安装完成后，在 VS2026“帮助 → 关于 Microsoft Visual Studio”中查看版本，再由助手检查实际 SDK 版本。
4. 把“安装完成”、VS 的完整版本号告诉助手；若模板没有 .NET 10 选项，也一并说明。**此时先不要创建项目或安装 OpenTK。**

VS Installer 可以与原有版本并行安装，并按工作负载选择组件；上述流程采用微软的 [安装说明](https://learn.microsoft.com/en-us/visualstudio/install/install-visual-studio?view=visualstudio)。

只有检查发现 .NET 10 SDK 缺失时，才在 [.NET 10 官方下载页](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) 的 **SDK** 区选择 **Windows x64 Installer**，通过界面补装。开发需要 SDK，单独安装 Runtime 不够；SDK 已包含相应运行时。参考 [.NET Windows 安装说明](https://learn.microsoft.com/en-us/dotnet/core/install/windows)。

### 助手随后验证

- 核对 VS2026 实例、版本及 .NET 桌面工作负载。
- 运行 `dotnet --list-sdks`、`dotnet --info`，核对 .NET 10 完整 SDK 版本、x64 架构及路径。
- `dotnet --info` 中可选 SDK 工作负载的列表，不能代替 VS 安装组件核对。本机该列表为空，但已通过 VS 实例组件清单确认“.NET 桌面开发”安装完成；不为这一提示自动安装额外工作负载。
- 在进度文档记录真实版本。如果新安装未被当前进程识别，先重启相关应用或终端再检查。
- 给出下一步的具体操作；此步骤不创建解决方案、不下载 NuGet 包。

## 2. 用户在 VS 中创建解决方案与项目

### 目标文件位置

本机目标如下；换设备时可以更换仓库所在盘符，仓库内部相对结构保持一致。

```text
E:\game_study\games104\
└─ g104engine\
   ├─ g104engine.slnx                # 已包含下面两个项目
   ├─ src\G104.Engine\G104.Engine.csproj
   ├─ samples\G104.Sandbox\G104.Sandbox.csproj
   └─ docs\                         # 助手已经创建的文档，保留
```

**此时不勾选任何创建 Git 仓库或发布到 GitHub 的选项。** 之后的唯一新增 Git 根是 `games104`，不是解决方案所在的 `g104engine`。

### 2.1 空解决方案

**本机已完成：** `E:\game_study\games104\g104engine\g104engine.slnx`。第 2.1 节验收时内容为 `<Solution />`，XML 检查与 `dotnet sln g104engine.slnx list` 均通过；目前已按第 2.2 节加入两个项目，不再是空方案，不要重复创建。

`.slnx` 是受 .NET CLI 支持的解决方案文件格式；当前环境已实际读取通过，不影响本项目后续添加 C# 项目和 NuGet。后续统一使用实际文件名，参见 [微软 dotnet sln 文档](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln)。

1. 在 VS2026 选择“创建新项目”，搜索“空白解决方案 / Blank Solution”。
2. 填写 **解决方案名称 `g104engine`**、**位置 `E:\game_study\games104`**。当前项目采用实际生成的 `E:\game_study\games104\g104engine\g104engine.slnx`。不要因为已有工程目录就把“位置”再选到 `g104engine` 内，以免向导重复追加一层同名目录。
3. 创建后，在资源管理器核对目录和解决方案文件基名均为 **`g104engine`**。保留目录中现有的 `docs`、`src` 和 `samples`；这次命名已统一，**不需要再执行解决方案改名**。
4. 保留 `.slnx` 的真实格式，不仅修改扩展名或另建同名 `.sln`。在其他环境复现时若实际格式、位置不同或提示覆盖文件，先核对再继续；已有仓库应直接打开克隆得到的方案，不重新创建。

空解决方案及添加项目是 [VS 官方支持的流程](https://learn.microsoft.com/en-us/visualstudio/ide/creating-solutions-and-projects?view=visualstudio)。目录名和解决方案文件基名均采用 `g104engine`，项目展示名称保持 G104Engine；最终以磁盘实际路径为准。

### 2.2 添加两个 .NET 项目

在 VS2026 中打开现有 `g104engine.slnx`，右键解决方案 →“添加 → 新建项目”，依次创建：

| 设置 | 引擎类库 | 演示启动程序 |
|---|---|---|
| 模板 | C#“类库 / Class Library” | C#“控制台应用 / Console App” |
| 项目名 | `G104.Engine` | `G104.Sandbox` |
| 位置 | `E:\game_study\games104\g104engine\src` | `E:\game_study\games104\g104engine\samples` |
| 框架 | `.NET 10.0` | `.NET 10.0` |

选择现代 .NET 模板，避免名称带 “.NET Framework” 的旧模板。确认向导只追加一层项目名；创建后核对表格上方的实际目标路径。模板说明参见 [.NET 控制台应用教程](https://learn.microsoft.com/en-us/dotnet/core/tutorials/create-console-app?pivots=vs)。

1. 右键 `G104.Sandbox` 的“依赖项” →“添加项目引用”，勾选 `G104.Engine`。引用方向是 **Sandbox → Engine**。
2. 右键 `G104.Sandbox` →“设为启动项目”。
3. 按下方“x64 平台设置”配置两个项目的 Debug/Release。`Any CPU` 是正常默认值，不代表安装错误；为使本阶段 Windows x64 目标明确，按约定统一配置为 x64。
4. 先确认 Sandbox 是控制台应用且具有入口，再通过 VS“生成解决方案”检查；`Ctrl+F5` 应能看到预期控制台输出，`F5` 可验证断点调试。创建、生成时由 VS 发起的框架还原是本次用户操作的一部分。

### 2.2.1 Sandbox 误建为类库时如何修正

**此问题已修正，以下保留为排错参考，不需要再次执行。** 2026-10-02 早先核查发现 Sandbox 误建为类库，只有 `Class1.cs`、没有入口。用户随后按 A 路线重建；最新核验已确认 `OutputType=Exe`、`Program.cs` 入口和正确引用。仅“设为启动项目”不会将类库改成可执行程序。

当时 Sandbox 只有空模板、项目文件及 `bin`/`obj` 产物，没有业务代码或额外包引用，因此按用户意向推荐 **A 路线**。A、B 是两种可选修正方式，完成 A 后无需再执行 B。

#### A：已采用，按正确模板重建 Sandbox

1. 停止调试，保存需要保留的内容。在“解决方案资源管理器”中右键 **G104.Sandbox 项目节点 → 移除 / Remove**，保存解决方案。保留 `G104.Engine` 和 `g104engine.slnx`。
2. VS 中“移除”通常只是从解决方案解除该项目，不等于清除磁盘文件。在 Windows 资源管理器核对完整路径后，只将 **`E:\game_study\games104\g104engine\samples\G104.Sandbox`** 这个目录普通删除到回收站。不要删除父目录 `samples`、整个 `g104engine` 或 `src/G104.Engine`。若出现永久删除提示，先保留备份；若文件占用，关闭相关文档/调试后再操作，不强制清理其他进程。
3. 回到现有解决方案，右键解决方案 →“添加 → 新建项目”，语言选择 **C#**，模板选择 **控制台应用 / Console App**；不要再选类库或带“.NET Framework”的旧模板。
4. 名称填 `G104.Sandbox`，位置填 `E:\game_study\games104\g104engine\samples`，框架选 `.NET 10.0`，其余先保持默认。不再另建解决方案或 Git 仓库。目标文件为 `samples/G104.Sandbox/G104.Sandbox.csproj`。
5. 正确模板会生成 `Program.cs`。默认的一行 `Console.WriteLine("Hello, World!");` 顶层入口也是正常的；本次先保留模板内容，不额外重复添加下面 B 路线的入口示例。
6. 重新为新 Sandbox 添加 **Sandbox → Engine** 项目引用，并将新 Sandbox 设为启动项目；旧项目里的引用和启动设置需要重新核对。
7. 继续第 2.2.2 节的 Debug/Release x64 配置，核对新项目的平台映射；新项目可能重新采用 Any CPU 默认值。完成后生成并运行模板，再交给助手检查。

这里的删除和重建均由用户在界面完成。若以后项目已有业务代码、自定义配置或资源，不默认删重建，应先保留工作并评估迁移。参考 [VS 创建和移除项目说明](https://learn.microsoft.com/en-us/visualstudio/ide/creating-solutions-and-projects?view=visualstudio)。

#### B：备用，在现有项目中修改输出类型与入口

仅在选择保留现有项目时执行；不要与 A 路线同时进行：

1. 右键 **G104.Sandbox → 属性**，找到“应用程序 → 常规”中的“输出类型”，选择 **控制台应用程序 / Console Application**。其项目属性应对应 `OutputType=Exe`；`G104.Engine` 继续保持类库。不同语言或版本的属性页名称可能略有不同，找不到时让助手核对实际界面。
2. 在 Sandbox 项目中“添加 → 新建项”，添加 `Program.cs`，在 VS 编辑器中把该文件全部内容替换为下方的最小入口。原来的空 `Class1.cs` 可以暂时保留。
3. 保存，继续下方的 x64 配置，再生成、运行。若只改输出类型而没有入口，控制台项目还不能完成运行验收。

```csharp
namespace G104.Sandbox
{
    internal static class Program
    {
        private static void Main()
        {
            System.Console.WriteLine("G104.Sandbox 启动成功");
            System.Console.WriteLine($"64 位进程：{System.Environment.Is64BitProcess}");
        }
    }
}
```

这是供用户在 VS 中输入的验证示例，助手未创建或修改源码。运行位数输出用于核对实际进程，不能替代工程平台配置检查。

### 2.2.2 x64 平台设置

在截图所示的“配置管理器”中操作：

1. 点击**右上角“活动解决方案平台”**的下拉框；有 `x64` 时直接选，没有则选“<新建…>”。
2. 新平台选择 `x64`，“从此处复制设置”选择 `Any CPU`；如果显示“创建新项目平台”，勾选它，再确定。
3. 回到表格，确认 `G104.Engine` 和 `G104.Sandbox` 两行的平台均为 `x64`，“生成”保持勾选。不能只改上方方案平台、却让表内仍映射到 `Any CPU`。若某行没有 x64，在该行的平台下拉中创建/选择 x64。
4. 左上角先检查 `Debug`，再切换到 `Release`，都核对 **方案 x64 → 两个项目 x64** 的映射。
5. 开发时切回 `Debug | x64`，关闭配置管理器并保存。当前两个项目不需要勾选“部署”。

若平台名称已为 x64，但项目属性页仍显示 CPU 目标为 Any CPU，在项目“生成”相关属性页核对“平台目标 / Target CPU”，为对应的 Debug/Release x64 配置选择 x64；不需要为这个 C# 设置安装 C++ 工具链。之后由助手核对实际求值的 `PlatformTarget` 与方案映射。

`Any CPU` 并不等于强制 32 位；本阶段设置 x64 是为了明确目标架构，便于后续核对原生图形依赖，不表示 x64 自动使代码更快。参考 [VS 平台设置](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-configure-projects-to-target-platforms?view=visualstudio) 与 [C# PlatformTarget / OutputType](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/output)。

### 助手随后验证

读取解决方案及两个 `.csproj`，检查文件层级、`net10.0`、项目引用、实际 `OutputType`、入口和平台；区分方案平台标签、项目平台映射与求值后的 `PlatformTarget`。启动项目设置和断点行为在用户实际运行时确认。属性读取不等于构建或运行通过，模板控制台也不会自动成为 OpenGL 程序。

**本机验收记录（2026-10-02）：** 上述配置正确；Debug/Release x64 使用 `--no-restore` 构建均为 0 警告、0 错误，Debug 输出 `Hello, World!` 并正常退出。用户在 VS 中也已复现 Release 加载 DLL 失败（`0x800711C7`）。CodeIntegrity 3033/3077、3099 和 SAC 状态值 `1` 确认来源为 Windows 智能应用控制，而非项目模板错误。完整证据见 [status.md](../status.md)。

**后续状态：** 安装第 3 节的包后，Debug 也曾被相同 SAC 策略拦截。用户随后自行在 Windows 安全中心关闭 SAC，并反馈两种配置均恢复输出；助手确认状态值为 `0`。Release 的“仅我的代码”提示是另外的调试体验警告，见第 2.2.4 节。

<a id="223-本机-sac-拦截的处理说明仅供了解未执行关闭"></a>
### 2.2.3 本机 SAC 拦截的处理记录

用户已自行通过界面关闭 SAC，助手只读确认 `VerifiedAndReputablePolicyState=0`。以下保留为本机问题处理参考，不要求其他设备关闭保护；先前“只了解、不更改”的阶段已由用户实际操作更新。

**当前已确认决定（2026-10-02）：本机开发阶段暂时保持 SAC 关闭。** 本机自编译的 Debug/Release 均曾被拦截，关闭后已验证正常运行。这是用户了解影响后的本机选择，不是引擎依赖或跨设备复现的必需配置；以后重新开启或换设备时重新验证。建议保持 Defender 实时保护、SmartScreen 和防火墙开启，不添加整个工程目录的杀毒排除；这些保护的实际状态本轮未核验，也未修改。

SAC 检查应用及二进制文件的信任信息与签名；没有针对本工程的单独放行开关。关闭影响整台电脑，移除 SAC 对不受信任程序的这一层拦截；Defender 实时防病毒和其他独立保护仍是各自的设置，不需要一并关闭。参见 [微软 SAC FAQ](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions)。

界面步骤：

1. 在开始菜单搜索并打开“Windows 安全中心”。
2. 进入“应用和浏览器控制 → 智能应用控制设置（Smart App Control settings）”。
3. 在变更前查看当前状态及关闭、重新开启的提示。微软当前 FAQ 说明近期 Windows 更新支持重新开启；本机截图中“打开”可见，但重新开启尚未实测，不能把可见选项等同于已经恢复验证。
4. 本机用户已经选择该页面的“关闭”。助手没有改注册表、Defender、SmartScreen 或杀毒排除项，也不从 SAC 关闭推断其他保护的实际状态。
5. 实际设置变更后再核验状态、Debug/Release 构建与运行。若仍失败，检查最新 CodeIntegrity 事件，不假定关闭一个开关就已完成所有验收。

这属于开发电脑的本地安全取舍，不是引擎项目依赖或新设备的必做步骤。公司/学校管理设备应由管理员决定。

### 2.2.4 Release 的“仅我的代码”警告

**本机已完成（2026-10-02，用户反馈）：** 已重新勾选“启用仅我的代码”，Debug 断点测试通过。以下说明保留供日后调试参考，不要求重复测试。

Release 默认启用优化；.NET 的“仅我的代码”会把优化后的模块视为非用户代码，因此用 F5 调试 Release 可能出现图示警告。它不代表编译错误或 SAC 拦截，禁用此选项也不会关闭编译优化、改变安全策略或修复代码。根据用户点击“禁用仅我的代码并继续”的操作，后续不再弹出同样提示符合选项改变后的行为；助手未直接读取 VS 当前选项。

- 日常调试：使用 `Debug | x64`，按 F5，在 `Program.cs` 输出语句设置断点验证命中。
- 建议恢复“仅我的代码”：停止调试，打开“工具 → 选项 →（所有设置）→ 调试 → 常规”，勾选“启用仅我的代码”。也可在选项搜索框搜索该名称。此项是 VS 全局调试偏好，不是项目代码配置。
- 检查 Release 的普通运行：选择 `Release | x64`，按 Ctrl+F5（开始执行，不调试）。此方式不附加调试器，不触发该调试体验警告。
- 确需调试只在 Release 出现的问题时，可以临时关闭“仅我的代码”后使用 F5；优化仍可能影响断点、变量查看及单步行为。完成后可恢复偏好，无需为了消除提示而关闭 Release 优化。

依据：[微软 Just My Code 文档](https://learn.microsoft.com/en-us/visualstudio/debugger/just-my-code?view=visualstudio)、[优化与调试说明](https://learn.microsoft.com/en-us/visualstudio/debugger/jit-optimization-and-debugging?view=visualstudio)。

## 3. 用户通过 NuGet 安装指定组件

**本机已安装并核对：** 以下三个直接包引用均位于 Engine，版本正确；两个项目的还原记录无报错，包目录存在。用户关闭 SAC 后反馈两种配置恢复运行；最新助手验证结果见 `status.md`。无需重复安装，以下步骤保留用于复现。

在 **`G104.Engine` 项目**上右键“管理 NuGet 程序包”：

1. 选择“浏览”，包源选 `nuget.org`，关闭“包括预发行版”。
2. 逐个搜索下列准确包名，在版本列表明确选择 **4.9.4**，点击“安装”，核对预览中的项目和包版本。

| 包 | 作用 | 版本 |
|---|---|---|
| [OpenTK.Graphics](https://www.nuget.org/packages/OpenTK.Graphics/4.9.4) | OpenGL 函数与类型绑定 | 4.9.4 |
| [OpenTK.Windowing.Desktop](https://www.nuget.org/packages/OpenTK.Windowing.Desktop/4.9.4) | 桌面窗口、输入与循环基础 | 4.9.4 |
| [OpenTK.Mathematics](https://www.nuget.org/packages/OpenTK.Mathematics/4.9.4) | 向量、矩阵等数学类型 | 4.9.4 |

3. 允许 NuGet 解析并还原它们的传递依赖。不要另外下载 DLL 并手工复制，也不额外安装 OpenTK 总包或预览版。
4. 在“已安装”和“依赖项 → 包”中核对结果，重新通过 VS 生成解决方案，再交给助手检查。

在本环境验证示例中，组件暂放在 `G104.Engine` 类库，`G104.Sandbox` 通过项目引用验证依赖关系；这不决定未来平台、渲染、数学等能力的程序集归属。正式软件架构与包边界在后续阶段讨论。`System.Text.Json`、`Console`、`Trace` 使用 .NET 自带功能，本阶段无需给它们添加额外包。

NuGet 界面会同时安装依赖并写入项目引用，官方操作说明见 [使用 VS NuGet 包管理器](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio)。

### 助手随后验证

读取包引用及还原产物，核对三个直接依赖和实际解析版本。用户已在 VS 完成还原后，助手使用 `dotnet build --no-restore` 验证 Debug、Release；运行已有结果时使用 `dotnet run --no-build`。这些检查可能产生构建产物，但不借验证自动添加包或下载依赖。若缺少还原产物，由用户返回 VS 完成还原。

