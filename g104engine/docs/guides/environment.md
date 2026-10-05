# 环境使用、x64与故障定位

本页用于打开现有工程和诊断缺失环境，不是首次建工程待办。实际运行和验证命令在 [v1-run-and-review.md](v1-run-and-review.md#run-and-verify)；精确SDK/包/锁文件在 [dependencies.md](dependencies.md#dependency-locks)，安装/配置职责见 [agent-workflow.md](../agent-workflow.md)。只需按当前问题使用对应步骤，不先通读旧搭建记录。

<a id="existing-project"></a>
## 打开现有工程

打开E:\game_study\games104\g104engine\g104engine.slnx，启动G104.Sandbox，选Debug或Release/x64。方案内已有Engine/Sandbox项目及Sandbox→Engine引用；无参数是完整训练场Edit窗口。不要重复新建解决方案、同名项目、NuGet引用，不用Hello World/旧蓝色探针覆盖现有Program。

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

环境安装、新建和NuGet还原由用户在VS完成；旧三OpenTK包流程不是今天缺包清单。先区分缺SDK、缺还原资产、缺x64原生DLL、错误启动项目和驱动上下文失败，再选择动作。没有问题证据时不升级驱动/工具或重新创建工程。

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
