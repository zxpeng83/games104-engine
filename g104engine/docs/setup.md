# 环境与工程搭建：可视化操作指南

**当前协作方式：用户操作安装器、Visual Studio、GitHub 网页及 Git Bash／Git GUI；助手编写说明、检查结果并定位问题。Git 操作按最新选择暂不使用 VS 界面。每次只完成当前步骤，检查通过后再继续。**

本指南同时用于分步操作与换设备复现，完成情况以 [status.md](status.md) 的实际核验为准，范围与分工见 [plan.md](plan.md)。安装器和菜单名称可能随版本、语言变化；以下路径安排已经确定，VS 项目创建等界面流程尚未逐项实测，遇到与说明不同的界面先核对。

**本指南只处理开发基础。** 下文两个项目、项目引用和包归属用于学习工程操作与环境验证，不代表具体软件架构已经确定；后续设计可以调整这些起步安排。模块、接口、数据模型和帧流程留到基础阶段后的计划中讨论，见 [架构占位与议题](architecture.md)。

## 1. 安装 VS2026 与 .NET 10（本机已通过核验）

2026-10-02 核验通过：本机已安装 **VS2026 Community 18.10.3（Stable）**、**.NET 10 SDK 10.0.401（x64）**，VS 的“.NET 桌面开发”、SDK、C# 编译器、MSBuild 和 NuGet 组件均已检出；.NET 与桌面运行时为 10.0.12。第 2 节项目配置、第 3 节三个包的引用和还原结果已核对。用户随后关闭本机 SAC，反馈 Debug/Release 均恢复 Hello World 输出；最新复核结果见 [进度](status.md)。已完成步骤保留供复现，不必重复安装或创建工程。

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

**本机验收记录（2026-10-02）：** 上述配置正确；Debug/Release x64 使用 `--no-restore` 构建均为 0 警告、0 错误，Debug 输出 `Hello, World!` 并正常退出。用户在 VS 中也已复现 Release 加载 DLL 失败（`0x800711C7`）。CodeIntegrity 3033/3077、3099 和 SAC 状态值 `1` 确认来源为 Windows 智能应用控制，而非项目模板错误。完整证据见 [status.md](status.md)。

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

## 4. 固定版本与 OpenGL 验证

### 4.1 固定 SDK：版本选择

**本机已完成：用户明确委托助手执行本节，助手已创建并验证 `global.json`。** 已验证 SDK 为 `10.0.401`；从工程目录执行 `dotnet --version` 及 MSBuild SDK 属性求值均选中该版本，`dotnet --info` 显示本文件路径。下面保留可视化创建方法供学习，不需要重复创建；其他安装、NuGet 与 Git 操作分工不变。

1. 在 VS 中选择“文件 → 新建 → 文件”，创建 JSON 文件；若没有此模板，选择文本文件即可。不创建新的项目。
2. 输入以下完整内容，再“另存为” `E:\game_study\games104\g104engine\global.json`。如使用文本模板，将保存类型设为“所有文件”，避免自动追加 `.txt`。

```json
{
  "sdk": {
    "version": "10.0.401",
    "rollForward": "disable",
    "allowPrerelease": false
  }
}
```

3. 在资源管理器核对 `global.json` 与 `g104engine.slnx` 位于同一目录，名称不是 `global.json.txt`，也没有放进 Engine 或 Sandbox 子目录。它不需要作为项目资源加入 `.csproj`。
4. 保存后告诉助手“第 4.1 节完成”，由助手读取并核对实际 SDK 选择；此时不安装新 SDK，也不开始 OpenGL 探针。

#### 学习说明：为什么创建 global.json

SDK 是编译、构建和还原 .NET 工程所用的开发工具集合。`global.json` 是供这些工具读取的版本选择配置，回答的是“这个工程使用哪一版 SDK”。它是 .NET 工具约定的文件名，不是我们自己编写的引擎配置格式。

| 字段 | 本工程的值 | 含义 |
| --- | --- | --- |
| `sdk.version` | `10.0.401` | 选择已经实际验证的完整 SDK 版本 |
| `sdk.rollForward` | `disable` | 必须匹配该版本；缺少时报错，不自动改用其他 SDK |
| `sdk.allowPrerelease` | `false` | 不把预览 SDK 作为候选 |

例如，今天使用 SDK `10.0.401` 构建成功，后来升级 VS 或在另一台电脑安装了更新的 SDK。没有工程级版本约束时，工具可能选择不同 SDK，带来编译器、分析器或构建行为差异；当前配置让这种变化显式发生，而不是随环境更新悄悄发生。

代价是另一设备必须安装准确版本：即使装了更高版本，也不满足当前 `disable` 策略。以后需要升级时，先安装并验证目标 SDK，再同步修改 `global.json`、环境记录和相关 CI 配置；不是永久停留在 `10.0.401`。文件本身不会下载 SDK，也不会卸载或改动机器上的其他 SDK。依据：[微软 global.json 说明](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)。

#### 学习说明：谁读取、何时生效

这个文件由 .NET 的 SDK 解析机制读取，不需要在 C# 中加载，也不需要添加到 `.csproj`。它解决的是“用哪一套编译和构建工具”：没有它时，安装了多个 SDK 的机器通常会选择较新的 SDK；有了它，未来升级 VS 或换电脑时不会无意切换 SDK。它不保证整个环境完全相同，VS、运行时与 NuGet 依赖仍有各自的配置和验证。

- **VS 构建**：SDK 解析器从解决方案所在目录开始向上查找，因此文件放在 `.slnx` 旁。
- **命令行**：`dotnet` 从当前工作目录向上查找；后续命令应从 `g104engine` 或其子目录执行，不要以为在上层目录传入方案路径就一定使用了此文件。
- **未来 CI/另一设备**：同步该文件、安装对应 SDK，并将构建工作目录设为 `g104engine`。文件本身不负责下载 SDK，缺少准确版本会报错。

这里的“global”不代表修改整台电脑的全局默认版本。它通过目录查找规则约束本工程；其他目录下的项目有各自的查找结果。将文件保存在 `.slnx` 旁边即可，不需要手动写一段代码去加载，也无需作为运行时资源复制进 `bin`。

#### 学习说明：不要混淆几种版本

| 项目 | 本机/本工程示例 | 负责什么 |
| --- | --- | --- |
| .NET SDK | `10.0.401` | 使用哪套开发工具构建；由本节 `global.json` 选择 |
| 目标框架 | `.csproj` 的 `net10.0` | 工程面向的 .NET API/框架目标；不是准确 SDK 补丁版本 |
| .NET 运行时 | 本机已检出的 `10.0.12` | 执行编译后的程序；不是由本文件直接固定其补丁版本 |
| NuGet 包 | OpenTK 组件 `4.9.4` | 工程依赖的第三方库；由包引用和后续锁文件管理 |
| IDE | VS2026 `18.10.3` | 编辑与调试环境；本文件不锁定 VS 的版本 |

因此，写了 `net10.0` 并不等于固定 SDK `10.0.401`；创建 `global.json` 也不等于锁定了 OpenTK。SDK 固定只是跨设备复现的一部分，下一节继续处理依赖。

#### 本次怎样确认配置生效

以下是从 `g104engine` 工作目录执行过的核验，保留作为学习记录，不需要重复运行：

| 核验 | 实际结果与作用 |
| --- | --- |
| 解析 `global.json` | JSON 格式合法，字段和值与上面的示例一致 |
| `dotnet --version` | 返回 `10.0.401`，确认当前命令选择的 SDK |
| `dotnet --info` | 显示本工程 `global.json` 的完整路径，确认实际发现了这个文件 |
| MSBuild 的 `NETCoreSdkVersion` 与 `MSBuildSDKsPath` 属性 | 分别返回 `10.0.401` 和对应 SDK 目录，确认项目求值使用的工具位置 |

仅看到 `dotnet --version` 与预期相同，还可能是机器恰好默认选择了同一版本；结合 `--info` 中的配置路径，证据更完整。

本节只验证了配置解析与 SDK 选择，没有重新构建或运行程序。NuGet 锁文件仍待第 4.2 节处理。

### 4.2 NuGet 依赖锁文件（第 4.1 节核验后继续）

**当前状态：第 4.1 节和第 4.2 节已完成。** 两个项目均已启用锁文件及严格模式；助手核对 MSBuild 属性、还原记录中的严格模式及成功标记，锁文件与 A 阶段的 SHA256 一致。用户反馈 Debug/Release 生成成功；本轮助手未重复还原或构建。下方步骤保留供学习复现，不需要重复执行。

#### 学习说明：锁定什么，为什么需要

| 文件 | 用途 | 谁读取 |
| --- | --- | --- |
| `global.json` | 选择 SDK 构建工具版本，如 `10.0.401` | .NET/VS 的 SDK 解析器 |
| `.csproj` 的 `PackageReference` | 声明项目直接依赖的包及版本要求，如 OpenTK.Graphics `4.9.4` | NuGet 还原过程 |
| `packages.lock.json` | 保存还原得到的完整包依赖结果，包括实际版本、直接/间接关系及包内容哈希 | 后续 NuGet 还原与锁定校验 |

我们虽然只手动安装三个 OpenTK 组件，但这些组件还会依赖其他包。前轮实际解析结果还包含 OpenTK.Core、Windowing.Common、Windowing.GraphicsLibraryFramework `4.9.4` 和 OpenTK.redist.glfw `3.4.0.44`。这类由依赖继续引入的包叫“传递依赖”。

项目文件记录“我要求什么”，锁文件记录“最终解析到了什么”。给直接引用写了版本号，不代表已经把完整传递依赖结果作为可审查的文件保存下来；这也不意味着 NuGet 平时会随意升级所有包。保存锁文件是为了在换设备和 CI 中复用、校验已确认的结果，并能在 Git 中看清依赖变化。

锁文件是清单，不包含 DLL，也不是游戏启动时读取的配置。它不会让引擎功能变多，不会自动修复代码或 SAC 拦截；版本锁定也不等于已完成安全审计。

#### 学习说明：“还原”是什么意思

“还原 NuGet 程序包”是根据项目依赖要求准备构建所需的包：有本地缓存时可以复用，缺少时从配置的包源获取，并产生供构建使用的 `obj/project.assets.json`。启用锁文件后，还原过程会同时生成或使用 `packages.lock.json`。它不是把源码恢复到旧版本，也不需要先卸载已经安装的三个包。

#### 操作 A：首次生成清单（用户已完成并通过核对）

1. 停止 VS 调试。在解决方案资源管理器右键 `G104.Engine`，选择“编辑项目文件”；找不到时，可双击 SDK 风格的项目节点打开 `.csproj`。
2. 在现有 `<PropertyGroup>` 中加入下面这一行，保留现有框架、平台等设置，保存：

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
```

3. 对 `G104.Sandbox` 的 `.csproj` 同样加入该属性并保存。这里是项目属性，不放进 `<ItemGroup>`、`Program.cs` 或 `global.json`，也不要手工编写锁文件里的版本和哈希。
4. 在 VS 中右键**解决方案**，选择“还原 NuGet 程序包”，等待完成。保存项目时也可能已经触发自动还原。已有包会尽量复用缓存；本节不要求点击“更新”或改变三个直接包的 `4.9.4` 版本。若没有还原菜单，先核对实际界面和 NuGet 还原选项，不通过卸载重装解决。
5. 核对项目文件旁边是否生成以下两个文件，再告诉助手“4.2 A 已完成”。若未显示，可用资源管理器查看；未生成或报错时保留输出信息，不手工补一个空文件。

```text
g104engine/
├─ src/G104.Engine/packages.lock.json
└─ samples/G104.Sandbox/packages.lock.json
```

Sandbox 虽然没有直接添加 OpenTK 包，但通过项目引用依赖 Engine，因此也有需要解析的包。**运行程序整体依赖结果要看 Sandbox 的锁文件**；Engine 的锁文件不能强制未来其他引用者采用完全相同的包版本。本阶段沿用两个验证项目各自生成记录的安排，Engine 的记录便于单独还原/构建时核对；以后设计独立类库发布时，再评估其锁文件管理方式。微软建议应用入口提交锁文件，并说明公共类库锁文件不能约束消费方，见下方官方依据。

#### 操作 B：严格校验（先完成 A 并由助手核对）

**本机已完成并核对（2026-10-02）：** 两个属性均为 `true`，两个项目的还原记录均包含 `restoreLockedMode=true`，还原缓存标记成功，锁文件内容保持不变。用户已反馈两个配置生成成功。

`RestorePackagesWithLockFile` 启用锁文件，但**不是禁止依赖变化的开关**。默认情况下，如果修改了项目依赖，普通还原可以更新锁文件。

严格还原使用另一个属性：

```xml
<RestoreLockedMode>true</RestoreLockedMode>
```

启用后，依赖声明与锁文件不一致时，还原会失败并要求处理，而不是悄悄改写锁文件。A 已通过，接下来由用户在 VS 操作：

1. 分别打开 Engine 和 Sandbox 的“编辑项目文件”，在现有 `<PropertyGroup>` 中加入 `RestoreLockedMode`，保留 A 中添加的属性。两个项目都应包含以下两行，其余内容不改：

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
<RestoreLockedMode>true</RestoreLockedMode>
```

2. 保存，右键解决方案 →“还原 NuGet 程序包”，等待成功。打开“视图 → 输出”，按实际界面选择“程序包管理器”或相关还原输出，保留失败时的完整错误；保存项目时可能已自动还原。
3. 通过 VS 分别生成 `Debug | x64` 和 `Release | x64`，确认没有还原或构建错误。不删除或手工编辑锁文件，也不升级包；若提示清单不一致，把错误交给助手核对。
4. 告知“4.2B 已完成”及还原/生成结果。助手核对配置、还原产物与 A 的锁文件哈希，再完成必要的构建运行检查。

此时两个开关分别负责“使用锁文件”和“按锁文件严格校验”。后续 CI 也应使用锁定还原；`dotnet build --no-restore` 本身不是锁定还原验证。

#### 学习说明：两个 true 为什么不冲突

它们不是互相覆盖的“更新方向”，而是功能开关与约束条件：

| 属性 | 正确含义 | 不代表什么 |
| --- | --- | --- |
| `RestorePackagesWithLockFile=true` | 启用锁文件机制；在非严格模式下可生成或随依赖要求变化更新清单 | 不是每次都强制覆盖已有锁文件，也不是扫描本机 DLL 后反向填写依赖 |
| `RestoreLockedMode=true` | 还原必须遵守已确认的清单；项目依赖声明与清单不一致时报错 | 不是根据本机已安装的库更新锁文件，也不会自动把 `.csproj` 改成锁文件中的要求 |

**同时启用表示：使用锁文件，并禁止还原过程擅自改写依赖清单。** 先核对 `.csproj` 的要求与锁文件是否一致；一致时按清单准备依赖，缓存缺少的包可以下载，已经齐全时无需重复下载；不一致时失败，不靠自动修改项目声明或锁文件来“调和”。实际获取包与允许改写清单是两件事。

例如，当前项目声明 OpenTK.Graphics `4.9.4`，锁文件也记录对应请求及解析版本，还原成功。若以后主动把声明改成另一版本但未更新清单，严格模式会拒绝还原；它既不会把声明改回旧值，也不会自动把锁文件改成新值。这个错误是在提醒我们按升级流程审查变化。

没有启用严格模式时，依赖声明没变也会复用现有清单；依赖声明变了才可能重新解析并更新。因此两个属性一起启用不会导致“先更新锁文件，再反向更新项目”的循环。截图中的“所有程序包都已安装，没有要还原的内容”属于正常的无需重复工作的结果；它本身不展示 Debug/Release 编译结果，也不是故意制造不一致的反向测试。

#### 操作 C：以后主动升级 NuGet 包时如何更新锁文件

`packages.lock.json` 是 NuGet 根据项目声明计算出的结果，**不手工修改**。需要升级时，由用户决定新的直接依赖版本并修改项目；NuGet 负责重新计算直接依赖、传递依赖和内容哈希。以以后将某个 OpenTK 组件升级为例，按下面流程操作：

1. 开始前确认当前工作区中没有混入其他未处理的项目或锁文件修改，并记录准备升级的包、原版本和目标版本。一次尽量处理一组相关包；OpenTK 的三个直接组件通常保持同一版本，具体仍按该版本的兼容关系核对。
2. 在 **Engine 和 Sandbox 两个项目**中暂时将严格模式改为：

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
<RestoreLockedMode>false</RestoreLockedMode>
```

保留第一个属性，让 NuGet 继续使用并更新锁文件；暂时关闭第二个属性，允许这次经过计划的依赖变化写入新清单。不要删除两个 `packages.lock.json`。
3. 在 VS 中右键 **G104.Engine → 管理 NuGet 程序包 → 更新**，选择明确的目标版本并核对安装预览。不要直接在锁文件里替换版本或哈希。Sandbox 没有直接的 OpenTK 包引用，不在 Sandbox 中重复安装；它会通过 Engine 的项目引用获得新的依赖图。
4. 右键解决方案 →“还原 NuGet 程序包”。NuGet 会根据更新后的 `.csproj` 重新计算依赖图，并自动更新 Engine 和 Sandbox 各自的 `packages.lock.json`。如果只更新了一个锁文件、出现 `NU****` 错误或还原失败，保留输出并交给助手检查，不通过手工复制另一个锁文件解决。
5. 审查这次变化：直接包版本是否是目标值、传递依赖为何变化、是否出现意外新增/删除包、内容哈希是否由 NuGet 正常生成。建立 Git 后使用差异视图同时检查 `.csproj` 和两个锁文件；没有 Git 时先让助手读取核对。
6. 在允许更新的状态下，生成并运行 `Debug | x64`、`Release | x64`，再执行该包相关的功能验证。包能还原、工程能编译，不代表升级后的行为已经正确。
7. 验证通过后，将两个项目重新设置为：

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
<RestoreLockedMode>true</RestoreLockedMode>
```

8. 再次还原解决方案，并生成两个配置。此时严格还原必须成功，锁文件不应再次发生意外变化。最后把直接依赖声明、两个锁文件和相关代码/文档作为同一次升级变更审查；提交仍遵守本项目的提交前确认规则。

如果升级验证失败，先保留错误和差异，再决定修复代码、选择其他版本或回退这次升级；不要只修改锁文件来掩盖 `.csproj` 与依赖图不一致。`RestoreLockedMode=false` 只在明确更新窗口中临时使用，完成或放弃升级后都应恢复为 `true`。

这一流程的方向是：

```text
用户选择目标版本
    → VS/NuGet 修改 .csproj 的直接依赖要求
    → NuGet 重新解析并生成 packages.lock.json
    → 人工审查、构建与功能验证
    → 恢复严格模式，按新清单复验
```

项目声明是升级意图的来源，锁文件是 NuGet 计算并供后续严格还原使用的结果。锁文件不会反向改写 `.csproj`，也不应成为人工指定新版本的入口。

#### 助手验证与完成标准

- 核对两个项目属性、实际生成文件及直接/传递依赖版本，确认与现有已验证依赖相符。
- 区分“清单已生成”“严格还原通过”“构建运行通过”，分别记录；清单与严格还原记录核对已通过，设置后的两个配置生成成功来自用户反馈。此前含依赖版本的模板运行已经通过；本轮没有重复构建运行或制造不一致测试。
- 锁文件属于需要审查、纳入计划发布范围的文本记录；`obj/project.assets.json` 和 `bin`/`obj` 仍是生成缓存，不上传。首次发布时再核对实际文件清单，不在本节自动提交。

官方依据：[NuGet 锁文件与锁定模式](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#locking-dependencies)、[VS 包还原操作](https://learn.microsoft.com/en-us/nuget/consume-packages/package-restore#restore-packages-in-visual-studio)。

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

<a id="5-用户通过网页和-vs-建仓关联与同步"></a>
## 5. 用户通过 GitHub 网页及 Git Bash／Git GUI 建仓、关联与同步

**开始本节前，助手先检查发布清单、根目录的 `.gitignore` 和 `.gitattributes`。同步范围以 [plan.md](plan.md) 为准。** 本地 Git 根为 `E:\game_study\games104`；现有 Piccolo 独立仓库不作子模块上传。

`games104/操作流程/` 是用户个人截图目录，已用根规则 `/操作流程/` 显式忽略。不要暂存、强制添加、上传其中内容，也不把它加入解决方案项、项目资源或工程文档。提交前助手核对忽略状态和实际文件范围，不需要读取截图。

### 5.1 GitHub 网页建立空仓库

**本机对应远程已完成：** 用户创建了 [zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)，HTTPS 地址为 `https://github.com/zxpeng83/games104-engine.git`。2026-10-02 API 查询为 public、size=0、默认分支名 main，`git ls-remote` 成功且未返回任何引用，确认当时无已有提交。下方保留建仓过程供复现，不要重复创建。当前发布候选为 31 个文件，原笔记引用保持原样。

1. 登录自己的 GitHub 账号，检查是否已有 `games104-engine`。同名仓库存在时先让助手核对，不重建或覆盖。
2. 网页“+ → New repository”，Owner 选本人账号，名称填 `games104-engine`，可见性选 **Public**。
3. 不套用模板，不生成 README、`.gitignore` 或 License，创建一个空仓库。本地文档已存在；此处额外初始化会增加需要处理的远程提交。
4. 创建后保留页面中的 **HTTPS 地址**，告知助手仓库地址；不把访问令牌或密码写进聊天和项目文件。

GitHub 官方说明支持网页建仓，并建议导入已有内容时不要额外预填文件，见 [创建仓库](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository)。

### 5.2 用户自行初始化并关联远程

当前用户选择使用 Git Bash／Git GUI，暂不使用 VS Git。**最新核验：Git 根、origin、main 分支和提交姓名/邮箱均已配置正确，第 5.2 节完成。** 以下初始化命令和本机收尾过程保留供复现，不需要重复执行。

需要达成的结果：本地根为 **`E:\game_study\games104`**，初始分支名 `main`，远程名 `origin`，地址 `https://github.com/zxpeng83/games104-engine.git`。保留已有 `.gitignore`、`.gitattributes` 和 README。

用户如选择 Git Bash，可按以下顺序操作（助手不代执行）：

```bash
cd /e/game_study/games104
pwd
git init -b main
git remote -v
```

确认 `pwd` 是目标目录。新仓库的 `git remote -v` 通常无输出；确认没有 `origin` 后再添加：

```bash
git remote add origin https://github.com/zxpeng83/games104-engine.git
git rev-parse --show-toplevel
git branch --show-current
git remote -v
```

若用户选择 Git GUI，使用客户端的创建本地仓库及添加远程功能，核对上面相同的目录、分支和 URL；具体菜单因客户端而异。若客户端自动提交，应停在提交动作前，遵守原有确认规则。单独 `git init` 和 `git remote add` 不提交或上传文件。

不要把空远程克隆到已有工程内形成第二层目录；不要在 `g104engine` 或 `资料` 中初始化。若发现仓库或 `origin` 已存在，先查看并核对，不能重复添加或直接覆盖。完成后告知助手“本地初始化与关联完成”，先验收再提交。

#### 本机收尾：分支名与提交身份

2026-10-02 早先核验发现 HEAD 为 `master` 且尚无提交，提交身份未配置；用户随后已完成更名和身份设置，最新检查为 `main`、姓名/邮箱可读取。以下为当时的处理步骤，保留供学习：

```bash
cd /e/game_study/games104
git branch -m main
```

接着配置**当前仓库**的提交身份。以下引号内是说明性占位文字，先替换成你实际选择的值再执行：

```bash
git config --local user.name "你希望提交记录显示的姓名"
git config --local user.email "你的提交邮箱"
```

姓名不强制与 GitHub 用户名相同；邮箱选择 GitHub 已验证邮箱或账户“Settings → Emails”页面给出的 noreply 地址。公开提交中的姓名和邮箱属于提交元数据；这里不填写账号密码或访问令牌。仓库级 `--local` 配置只影响当前仓库。依据：[Git 分支更名](https://git-scm.com/docs/git-branch)、[GitHub 提交邮箱说明](https://docs.github.com/en/account-and-profile/how-tos/email-preferences/setting-your-commit-email-address)。

完成后可用以下只读命令自查并通知助手，无须先提交：

```bash
git branch --show-current
git config --get user.name
git config --get user.email
git remote -v
```

预期分支为 `main`，身份为你选择的值，origin 地址不变。目前没有 upstream 是正常的，首次推送时再建立跟踪关系；不要为了消除这一状态执行一次未经确认的提交或推送。

### 5.3 首次提交与推送

当前本地为尚无提交的 main，索引为空；全部 31 个候选文件已核对。流程为：用户按下方清单暂存 → 助手核对实际暂存内容 → 展示身份、清单与提交说明并交互确认 → 用户提交/推送 → 助手核验。暂存只是准备提交内容，不会创建提交或上传。

<a id="initial-submit-manifest"></a>
#### 首次提交清单（31 个文件）

以下路径均相对仓库根 `E:\game_study\games104`，每行一个文件；首次提交为一次完整的开发基础快照。

```text
.gitattributes
.gitignore
AGENTS.md
README.md
g104engine/docs/architecture.md
g104engine/docs/archive/architecture-draft-2026-10-01.md
g104engine/docs/decisions/0001-foundation.md
g104engine/docs/decisions/0002-defer-implementation-architecture.md
g104engine/docs/learning-map.md
g104engine/docs/plan.md
g104engine/docs/setup.md
g104engine/docs/status.md
g104engine/g104engine.slnx
g104engine/global.json
g104engine/samples/G104.Sandbox/G104.Sandbox.csproj
g104engine/samples/G104.Sandbox/Program.cs
g104engine/samples/G104.Sandbox/packages.lock.json
g104engine/src/G104.Engine/Class1.cs
g104engine/src/G104.Engine/G104.Engine.csproj
g104engine/src/G104.Engine/packages.lock.json
资料/笔记博客汇总/第04-07节_渲染部分总结/AI梳理2/GAMES104_第4-7节_渲染部分知识体系总结.md
资料/笔记博客汇总/第08-09节_动画部分总结/GAMES104第08-09节_游戏动画系统完整梳理.md
资料/笔记博客汇总/第10-11节_物理系统部分总结/GAMES104_第10-11节_物理系统详细总结.md
资料/笔记博客汇总/第12节_粒子和声效部分总结/GAMES104_第12节_粒子与声音系统详细总结.md
资料/笔记博客汇总/第13-14节_引擎工具链/GAMES104_第13-14节_引擎工具链详细梳理.md
资料/笔记博客汇总/第15-17节_Gameplay玩法总结/GAMES104_第15-17节_Gameplay玩法系统完整梳理.md
资料/笔记博客汇总/第15-17节_Gameplay玩法总结/image/GAMES104_第15-17节_Gameplay玩法系统完整梳理/1789309063459.png
资料/笔记博客汇总/第18-19节_网络游戏部分总结/GAMES104_第18-19节_网络游戏架构完整学习讲义.md
资料/笔记博客汇总/第20节_现代游戏引擎架构总结/GAMES104_第20节_现代游戏引擎架构_详细总结.md
资料/笔记博客汇总/第21节_动态全局光照和Lumen总结/GAMES104_第21节_动态全局光照和Lumen知识体系总结.md
资料/笔记博客汇总/第22节_GPU驱动的几何管线-nanite总结/GAMES104_第22节_GPU驱动的几何管线与Nanite_知识体系总结.md
```

分组为根配置与入口 4 个、工程/源码/SDK/锁文件 8 个、工程文档 8 个、笔记 10 个及配图 1 张。`Class1.cs` 仍是类库模板，本次如实纳入，不将其记为引擎功能；历史架构草案也有明确的非执行依据标记。笔记中的非发布引用按用户决定保留。

排除 `操作流程/`、Piccolo、其他课程资料、备份、`.vs`、`bin`、`obj`、下载缓存。Git 元数据 `.git` 由 Git 自行管理，不作为普通文件提交。两个锁文件纳入清单，`obj/project.assets.json` 不纳入。

#### 当前用户操作：只暂存并交回核对

在 Git Bash 中执行：

```bash
cd /e/game_study/games104
git add --all
git -c core.quotepath=false diff --cached --name-status
git diff --cached --stat
git diff --cached --check
```

本次可以使用 `git add --all`，因为当前 31 个未跟踪文件及生效的忽略规则已核对，且没有其他已跟踪改动；它不会忽略 `.gitignore` 的排除规则。不要加 `-f`。这一命令不是以后每次都可以不经检查全量暂存的规则。

预期暂存清单恰好对应上面 31 个文件，均为 `A`（新增）。`--check` 无输出表示未发现它检查的空白错误，不是全部代码测试。若数量或内容不同、出现被排除目录，先保留状态交给助手，暂不提交。完成后告知“已暂存，请核对”，不必在聊天里重复抄写长清单。

#### 提交说明与确认后的操作

建议首次提交说明：

```text
chore: initialize G104Engine development foundation
```

摘要：建立 .NET 10/OpenTK 4.9.4 验证工程，固定 SDK 和 NuGet 依赖，加入 OpenGL/Smoke 探针、维护文档、十份课程笔记与配图。验证依据为本机双配置构建、用户可见 OpenGL 4.3 Core 运行和助手复核的双配置 smoke；CI、其他设备及引擎功能仍待完成。

目标为公开仓库 `zxpeng83/games104-engine` 的 `main`，执行人为用户。提交身份按实际 Git 配置在确认框中展示；不在指南重复保存私人邮箱。提交/推送范围必须按 [协作规则](../../AGENTS.md) 经用户明确确认。

**以下仅供了解；实际暂存清单核对并确认后才执行：**

```bash
git commit -m "chore: initialize G104Engine development foundation"
git push -u origin main
```

第一条创建本地提交，第二条将 main 推送到 origin 并建立上游跟踪；若本次只确认本地提交，不执行第二条。认证由用户完成，遇到拒绝或远程已有历史时先检查，不使用强制推送。

### 助手随后验证

只读检查 Git 根、分支、提交、远程地址与同步状态，核对 GitHub 文件树和发布清单。文件保存、本地提交、成功推送是三个独立状态，分别记录。当前没有必要安装 GitHub CLI，也不由助手代为初始化、关联、提交或推送。

## 6. 第二台设备如何接续

仓库首次发布并固定 SDK 后，另一台 Windows 设备由用户通过 Git Bash／Git GUI 克隆到新的本地目录，在 VS2026 打开仓库内的 `g104engine/g104engine.slnx`。助手先读取规则与当前状态，再检查缺失环境；用户按本指南安装对应 SDK、通过 VS 还原依赖，助手随后验证。本地个人截图不属于复现要求。

不要在另一台设备重新创建同名工程或从头添加 NuGet 包；这些应来自已经同步的项目文件。换机前确认本机改动已经按协作规则提交并推送，换机后先核对当地工作区再拉取。尚未真正完成的第二台设备验证，记录为“待验证”。

## 7. 每个步骤的交接记录

用户完成一步后，可以直接说：“已完成第 2.2 节的项目创建和引用，请验证。”助手负责检查并更新 [status.md](status.md)，记录实际路径、版本、验证结果、未解决问题和下一步。

**当前交接点：D0–D3 和第 5.2 节已完成，main 分支与提交身份已核对。用户按第 5.3 节的 31 文件清单暂存，助手检查实际暂存内容后发起提交/推送确认，再由用户操作。原笔记引用按用户决定保持原样，正式软件架构留到基础阶段完成后讨论。**
