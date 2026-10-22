# 环境准备、x64与故障定位

新电脑先按下面准备系统、开发工具和驱动，再沿 [reproduction.md](reproduction.md#new-machine-setup) 获取当前工程、检查素材和首次还原；已有环境直接从“打开现有工程”继续。这里安装的是开发环境，工程本身已有代码和配置。实际运行和验证在 [v1-run-and-review.md](v1-run-and-review.md#run-and-verify)；精确SDK、包与部署在 [dependencies.md](dependencies.md#dependency-locks)，协作职责见 [agent-workflow.md](../agent-workflow.md)。

<a id="new-machine-environment"></a>
## 新电脑：准备系统、工具和驱动

这条路线面向当前V1的Windows x64开发环境。先在Windows“设置 → 系统 → 系统信息”确认64位操作系统和基于x64的处理器；新机采用仍在微软支持期内的Windows 11 x64。下面使用的VS2026支持Windows 11，其他系统是否符合要求查 [VS2026系统要求](https://learn.microsoft.com/en-us/visualstudio/releases/2026/vs-system-requirements)。本项目尚未完成第二设备验证，ARM64、Linux、macOS不在当前复现路线内。

### 1. 确认显卡、声音和下载条件

| 要准备什么 | 去哪里、怎样确认 |
| --- | --- |
| OpenGL 4.3 Core显卡与驱动 | 在“设备管理器 → 显示适配器”确认显卡型号。缺少合适驱动时，按型号从电脑厂商支持页，或 [NVIDIA驱动](https://www.nvidia.com/en-us/drivers/)、[AMD驱动](https://www.amd.com/en/support/download/drivers.html)、[Intel驱动](https://www.intel.com/content/www/us/en/download-center/home.html) 获取对应Windows驱动。显卡硬件和当前驱动必须能创建OpenGL 4.3 Core上下文；首次运行还要检查控制台打印的GPU/GL版本。OpenTK包不能提供驱动，单装名为“OpenGL”的SDK也不能补足不支持的硬件。 |
| 声音输出 | 要听声音，连接扬声器或耳机，在“设置 → 系统 → 声音”选择可用输出并测试。音频设备初始化失败时普通窗口仍会继续，声音状态显示Unavailable；声音验收和`--verify-audio`需要可用设备。OpenAL实现随工程部署，获取方式见 [dependencies.md](dependencies.md#openal-deployment)。 |
| 网络与安装权限 | 浏览器须能访问下面的官方下载页，以及首次获取工程和NuGet包所用网络。安装VS和系统级.NET SDK需要管理员权限；安装位置可按磁盘空间选择，工程不要求放在E盘。 |

不需要为新机预先关闭SAC、Defender、SmartScreen或防火墙；发生实际拦截时按本页“SAC拦截”保留错误并诊断。

### 2. 安装Git和Visual Studio

1. 从 [Git for Windows官方下载页](https://git-scm.com/install/windows) 选择 **Git for Windows/x64 Setup** 并完成安装。它用于获取和保存源码；下载页版本会更新，Git版本不由引擎锁文件固定。后续可用Git Bash或Git GUI获取仓库，具体路径见 [reproduction.md](reproduction.md#new-machine-setup)。
2. 从 [Visual Studio官方下载页](https://visualstudio.microsoft.com/downloads/) 选择 **Visual Studio 2026 Community → Free download**，使用Stable稳定通道；个人学习可用Community，其他用途按其许可选版本。当前项目使用`net10.0`和`.slnx`，新机使用支持它们的VS2026；微软 [.NET SDK与VS兼容表](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs) 列明.NET10/10.0.4xx的支持关系。VS2022的旧安装选择仅保留为历史，不作为这条新机路线。
3. 运行Visual Studio Installer，在“工作负载”勾选 **“.NET桌面开发”**。保留它的必需组件；在右侧安装详细信息中确认 **“.NET SDK”**，并保留 **“NuGet包管理器”**。已有VS2026时，在Installer对应实例选择“修改”进入同一界面。图形化步骤见 [VS官方安装](https://learn.microsoft.com/en-us/visualstudio/install/install-visual-studio?view=visualstudio)，组件对应见 [组件清单](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-community?view=visualstudio)。
4. 完成安装后启动VS，在“帮助 → 关于Microsoft Visual Studio”确认版本；后续用“文件 → 打开 → 项目/解决方案”选择已有`g104engine.slnx`。当前工程使用预编译原生库，普通C#开发不需要额外勾选“使用C++的桌面开发”、Unity、CMake或游戏引擎工作负载。

2026-10-22核查时官网Stable为VS2026 18.11.1；这是官方下载入口当日提供的版本，不是项目新增的实测结果。Community当前只支持Stable最新版本，见 [VS发布历史](https://learn.microsoft.com/en-us/visualstudio/releases/2026/release-history)；不要求找到历史18.10.3安装包。VS随带的SDK可能已经更新，下一步仍须单独核对本项目的精确SDK。

### 3. 安装准确的.NET SDK 10.0.401 x64

1. 打开微软 [.NET10下载页](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)，找到 **SDK 10.0.401 → Windows → Installers → x64**；也可直接进入 [.NET SDK10.0.401 Windows x64安装入口](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-10.0.401-windows-x64-installer)。确认页面版本和文件名为`dotnet-sdk-10.0.401-win-x64.exe`，下载并运行安装程序。
2. 选择 **SDK**，不要只下载Runtime/Desktop Runtime，不选x86、Arm64、预览版或页面最上方其他SDK。SDK已经包含运行时；准确版本由工程`global.json`指定，`rollForward=disable`不会接受只有10.0.402等其他SDK的机器。可以并存其他SDK，不需要为此卸载它们。
3. 安装后重新打开VS及终端。默认x64 SDK通常位于`C:\Program Files\dotnet\sdk\10.0.401\`；该路径用于理解和诊断，工程中没有要求手工填写这个绝对路径。安装器路线无需为项目另建环境变量或手动设置`DOTNET_ROOT`。
4. 获取完整工程后，在VS通过“视图 → 终端”打开PowerShell，将当前目录切到方案所在的 **`g104engine`目录**，再检查：

```powershell
dotnet --list-sdks
dotnet --version
dotnet --info
```

`--list-sdks`须包含`10.0.401`；方案目录内`--version`须返回`10.0.401`；`--info`用于核对x64及实际`global.json`路径。运行这些检查不需要先构建工程。若没有识别到SDK，先重开应用，按 [Windows安装与路径排查](https://learn.microsoft.com/en-us/dotnet/core/install/windows#no-net-sdk-was-found) 检查是否误用了x86的`dotnet`；不要通过改`global.json`来绕过缺失。

2026-10-22官网仍列出该准确版本和x64安装入口；本次只核查网页，没有下载或安装文件，也没有在新设备验证。将来若该版本入口失效，先核查官方下载记录，不用“最新SDK”替换锁定版本。

工具安装完成但还没有工程时，按 [reproduction.md](reproduction.md#new-machine-setup) 获取仓库；已经取得工程并完成目录内SDK核对时，直接按 [dependencies.md](dependencies.md#first-restore) 完成首次NuGet还原。模型、Shader、声音以及OpenAL随仓库提供的文件按依赖页核对，不需要重新获取仓库或重做历史空模板探针。

<a id="existing-project"></a>
## 打开现有工程

在VS打开仓库中的`g104engine/g104engine.slnx`（当前机器示例为`E:\game_study\games104\g104engine\g104engine.slnx`），将G104.Sandbox设为启动项目，选Debug或Release/x64。新机可使用自己的目录和盘符，保持仓库相对结构。方案内已有Engine/Sandbox项目及Sandbox→Engine引用；无参数是完整训练场Edit窗口。不要重复新建解决方案、同名项目、NuGet引用，不用Hello World/旧蓝色探针覆盖现有Program。

| 项目 | 含义及核对位置 |
| --- | --- |
| SDK与目标框架 | global.json选构建SDK；两个csproj是net10.0。Runtime不等于SDK，文件本身不安装工具 |
| .slnx | 实际XML方案格式，由VS/dotnet sln支持；不只改后缀成.sln |
| 平台 | 方案x64和两个项目Debug/Release平台映射、实际PlatformTarget需同时正确 |
| 包与原生后端 | 双锁文件/还原产物与输出的x64 Jolt/cimgui/OpenAL各层分别核对 |
| 应用资产 | 模型/Shader/动画JSON/WAV及许可按Sandbox复制到输出assets；无需运行时FFmpeg/Python |
| 项目Git根 | games104而非g104engine；工程调试不自动创建、关联或发布仓库 |

<a id="222-x64-平台设置"></a>
## x64平台设置与核对

现有平台映射已经建立；仅在发现实际缺失时按下面核对，不按旧截图重做。

1. 点击**右上角“活动解决方案平台”**的下拉框；有 `x64` 时直接选，没有则选“<新建…>”。
2. 新平台选择 `x64`，“从此处复制设置”选择 `Any CPU`；如果显示“创建新项目平台”，勾选它，再确定。
3. 回到表格，确认 `G104.Engine` 和 `G104.Sandbox` 两行的平台均为 `x64`，“生成”保持勾选。不能只改上方方案平台、却让表内仍映射到 `Any CPU`。若某行没有 x64，在该行的平台下拉中创建/选择 x64。
4. 左上角先检查 `Debug`，再切换到 `Release`，都核对 **方案 x64 → 两个项目 x64** 的映射。
5. 开发时切回 `Debug | x64`，关闭配置管理器并保存。当前两个项目不需要勾选“部署”。

若平台名称已为 x64，但项目属性页仍显示 CPU 目标为 Any CPU，在项目“生成”相关属性页核对“平台目标 / Target CPU”，为对应的 Debug/Release x64 配置选择 x64；不需要为这个 C# 设置安装 C++ 工具链。之后由助手核对实际求值的 `PlatformTarget` 与方案映射。

`Any CPU` 并不等于强制 32 位；本阶段设置 x64 是为了明确目标架构，便于后续核对原生图形依赖，不表示 x64 自动使代码更快。参考 [VS 平台设置](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-configure-projects-to-target-platforms?view=visualstudio) 与 [C# PlatformTarget / OutputType](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/output)。

Any CPU并不强制32位，x64也不使代码自动更快；该设置明确原生依赖目标。C#平台配置不要求C++工具链。区分方案标签、项目映射和MSBuild求值后的PlatformTarget；只看到一个下拉框不够，运行时ProcessArchitecture用于另行核对。

<a id="debugging"></a>
## Debug与Release调试

- 日常源码学习用Debug/x64、F5，在Program/TrainingWindow.OnUpdateFrame逐个断点观察。断点暂停会改变真实delta，恢复后的丢时/补步不能直接作为正常性能结果。
- Release/x64用Ctrl+F5普通运行，不附加调试器。Release默认优化，断点/变量/单步可能与Debug不同。
- “仅我的代码”是VS全局调试偏好；优化模块可能被视为非用户代码，出现警告不等于编译/原生加载/SAC失败。
- 需要时停止调试，在工具→选项→调试→常规核对“启用仅我的代码”；可搜索名称。为调查只在Release出现的问题可临时关闭后F5，结束恢复偏好，无需关闭优化。

2026-10-02用户反馈重新勾选仅我的代码、Debug断点通过，属于历史实际反馈，不代表本轮读过VS设置。依据：[Just My Code](https://learn.microsoft.com/en-us/visualstudio/debugger/just-my-code?view=visualstudio)、[优化与调试](https://learn.microsoft.com/en-us/visualstudio/debugger/jit-optimization-and-debugging?view=visualstudio)。

<a id="missing-environment"></a>
## 将来确有环境缺失时

新电脑的完整准备顺序见本页“新电脑：准备系统、工具和驱动”；这里用于已有环境的故障定位。环境安装和NuGet还原由用户在VS等安装界面完成；旧三OpenTK包流程不是今天缺包清单。先区分缺SDK、缺还原资产、缺x64原生DLL、错误启动项目和驱动上下文失败，再选择动作。没有问题证据时不升级驱动/工具或重新创建工程。

1. 在VS Installer核对“.NET桌面开发”及其必需.NET SDK组件（Microsoft.NetCore.Component.SDK）；Runtime条目、dotnet可选工作负载列表不能代替VS组件核对。本机历史dotnet工作负载列表为空，VS组件核对却已通过，不为这个提示加额外工作负载。
2. 用dotnet --list-sdks / --info查看完整版本、x64和路径；如果新安装未被当前进程识别，先重启相关应用/终端。精确版本要求按global.json，SDK已含相应Runtime，单装Runtime不够。
3. 若确需安装VS，参考 [VS官方安装](https://learn.microsoft.com/en-us/visualstudio/install/install-visual-studio?view=visualstudio) 与 [组件清单](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-community?view=visualstudio)。个人学习的Community/保留既有VS2022选择属于2026-10-02决定；未来版本/适用许可需按当时环境核对，不把旧安装号当新推荐。
4. 若准确SDK缺失，由用户通过 [.NET10下载SDK区](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) / [Windows说明](https://learn.microsoft.com/en-us/dotnet/core/install/windows)处理。NuGet则按现有csproj/双锁文件在VS还原，不重新逐个安装库。
5. 若新环境确需创建独立验证项目，先确认任务；现有仓库直接开方案。已有业务代码不删Sandbox重建，旧空模板删除A/OutputType替代B只保存在历史快照。

<a id="223-本机-sac-拦截的处理说明仅供了解未执行关闭"></a>
<a id="sac-diagnosis"></a>
## SAC拦截：历史取舍与诊断

2026-10-02模板Release及后来Debug曾报0x800711C7，CodeIntegrity3033/3077、3099和状态值1定位为Smart App Control；用户自行在安全中心关闭后两配置恢复运行，助手当时只读核对VerifiedAndReputablePolicyState=0。用户明确选择本机开发阶段暂保持SAC关闭，这不是引擎依赖、其他设备必做或助手可自动更改的设置。

SAC检查信任/签名，无针对本项目的单独放行开关；关闭影响整机这一层拦截。Defender、SmartScreen、防火墙是独立设置，不能从SAC值推断它们状态。既有建议保留这些保护、不添加整个工程杀毒排除；本轮没有读取或改变它们。管理设备由管理员决定。

需要诊断时保留最新CodeIntegrity事件、异常/退出码和实际状态；用户实际调整后再按获授权范围验证，不能假设关一个选项完成全部验收。重新开启/换机需按具体Windows版本和 [微软SAC FAQ](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions)核对；2026-10-02页面/界面有重新开启提示，本机未实测重新开启，本轮未联网复查。

## 可选：首次搭建原文与历史结果

2026-10-02实际检出VS2026 Community18.10.3 Stable、SDK10.0.401 x64、Runtime/Desktop10.0.12；两项目/空模板重建、x64映射、三OpenTK包、SAC与调试反馈已完成。本轮未重新查询IDE/系统Runtime，也不将Hello World构建成功当V1验证。

完整可视化安装、空解决方案、Sandbox空模板A/B代码、x64、NuGet及探针原文保留在 [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#foundation-20261002)。历史步骤不得覆盖现有V1；同设备新聊天直接使用同工作区，跨设备D5恢复条件见status及 [reproduction.md](reproduction.md)。

来源补充：[创建/移除项目](https://learn.microsoft.com/en-us/visualstudio/ide/creating-solutions-and-projects?view=visualstudio)、[dotnet sln](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln)、[平台设置](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-configure-projects-to-target-platforms?view=visualstudio)、[C# PlatformTarget/OutputType](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/output)。它们是可选机制说明，不是回到首次创建的指令。
