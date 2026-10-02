# 当前进度与交接

最近环境与文档核查日期：2026-10-02（Asia/Shanghai）。本文件记录真实完成情况，不把已选型当作已安装，不把文档当作实现。

## 当前任务

**D0–D3、GitHub 建仓、本地初始化/关联及 main 分支与提交身份配置已完成。** 首次提交的 31 文件清单与操作步骤已写入 setup 第 5.3 节。当前索引为空、尚无提交；等待用户按清单暂存，再由助手核对实际内容并发起提交/推送确认。原笔记非发布链接按用户决定保持原样。

本轮只读核对 main、身份、origin、31 个未跟踪文件和排除规则，并编写具体提交清单。助手未暂存、提交、推送、修改 Git 配置或重跑构建；此前程序验证仍为有效的历史证据，远程公开空仓库状态来自前轮查询。

## 已确认的选择

- C#、.NET 10 LTS、Visual Studio 2026、Windows x64。
- OpenGL 4.3 Core、OpenTK 图形/窗口/数学组件 4.9.4；JSON 和初期诊断用 .NET 自带能力。
- 公开仓库已创建：[zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)，HTTPS 地址 `https://github.com/zxpeng83/games104-engine.git`；Git 根为 `games104`，工程与解决方案基名为 `g104engine`，展示名称 G104Engine。
- 实际解决方案文件为 `g104engine/g104engine.slnx`；不再以此前候选 `.sln` 文件名指导创建或后续命令。
- 笔记保留原位置，仅发布十份指定正文与引用图片，不扩充整套资料。
- `操作流程/` 仅供用户在本地查看操作截图，不属于工程文档或发布资源；显式忽略，不上传、不引用其中截图。
- 环境下载/安装、工程创建与 NuGet 仍由用户可视化操作；GitHub 建仓用网页，本地 Git 初始化、关联、提交与推送改由用户通过 Git Bash／Git GUI 操作，暂不使用 VS Git。助手编写文档、指导、核对。
- 第 4.1 节为用户明确委托的例外：由助手创建并验证 `global.json`；其他环境操作分工不变。
- 每次提交前由助手弹出交互确认框；明确确认后再由用户通过 Git Bash／Git GUI 提交/推送，助手验证。
- 当前只搭建开发基础，具体软件架构和细节留到后续计划讨论；历史图示不构成实现约束，验证用项目容器也不冻结长期结构。
- 2026-10-02 用户明确确认：本机开发阶段暂时保持 SAC 关闭；仅适用于本机，不作为工程依赖、其他设备或未来使用者的要求。Defender 实时保护、SmartScreen、防火墙保持开启是建议配置，其实际状态本轮未核验。

## 实际环境核查

IDE/SDK 信息来自 2026-10-02 的 D1 检查；本轮核对项目配置、引用、构建与运行。原 VS2022 和 SDK 9.0.304 保留，本轮不调整环境或项目文件。

| 项目 | 最近观察 | 状态 |
| --- | --- | --- |
| VS2026 | Community / Stable，显示版本 `18.10.3`，安装版本 `18.10.12224.181`；`D:\Program Files\Microsoft Visual Studio\18\Community` | 安装完整、可启动、非预览；安装器未要求重启 |
| VS 开发组件 | `.NET 桌面开发`、`.NET SDK`、`.NET 10 Runtime`、C# 编译器、MSBuild、NuGet | 均由 VS2026 实例组件查询确认存在 |
| .NET SDK | `10.0.401`；`C:\Program Files\dotnet\sdk\10.0.401` | x64，当前命令行可识别；开发文件及 net10.0 引用程序集存在 |
| .NET 运行时 | `Microsoft.NETCore.App` / `Microsoft.WindowsDesktop.App` 为 `10.0.12` | 已安装；SDK 自带 MSBuild 为 `18.9.11+e34a38d2a` |
| OS / dotnet 路径 | Windows `10.0.26200`，`win-x64`；`C:\Program Files\dotnet\dotnet.exe` | 当前主机与 SDK 为目标 x64 架构 |
| SDK / 依赖锁定 | SDK `10.0.401`；两个项目的 `RestorePackagesWithLockFile`、`RestoreLockedMode` 均为 `true` | 两份还原记录均为严格模式、缓存成功；锁文件与 A 阶段一致，4.2 完成 |
| 目标 Git 根 | `E:/game_study/games104`，Git 元数据位于其 `.git`；`g104engine/.git` 不存在 | 初始化位置正确 |
| 当前分支与提交 | `main`，`No commits yet`；31 个未跟踪文件、索引为空 | 分支名正确；等待按清单暂存 |
| 远程关联 | `origin` 的 fetch/push URL 均为 `https://github.com/zxpeng83/games104-engine.git` | 本地配置正确；关联不等于推送 |
| 提交身份 | 当前有效 Git 姓名为 `zxpeng83`，邮箱已配置并读取核对 | 已设置；确认提交时展示完整身份，文档不重复保存邮箱 |
| GitHub 远程 | `zxpeng83/games104-engine`，public，默认分支名 `main`，size=0；`ls-remote` 无引用 | 空仓库已核验；默认分支名不代表已有 main 提交 |
| VS 解决方案 | `g104engine/g104engine.slnx` 已包含 Engine、Sandbox 两个项目 | 位置和项目路径正确 |
| Engine 项目 | `src/G104.Engine/G104.Engine.csproj`，`net10.0` 类库；已添加三个 OpenTK 直接依赖 | 含依赖的 Debug/Release x64 构建通过 |
| Sandbox 项目 | `samples/G104.Sandbox/G104.Sandbox.csproj`，`net10.0`、`OutputType=Exe`；入口为临时 OpenGL/Smoke 探针 | 两配置可见窗口通过；两配置非交互检查均退出 0 |
| 本机 SAC | 前轮只读查询 `VerifiedAndReputablePolicyState=0`；用户现确认暂时保持关闭 | 原运行阻断已解除；本机开发阶段的取舍，不是工程依赖 |
| OpenTK 依赖 | Engine 直接引用 Graphics、Windowing.Desktop、Mathematics，均为 `4.9.4`；两个项目的 assets 解析一致 | 包目录存在、还原日志无报错；不等于图形运行通过 |
| 项目引用与调试 | Sandbox → Engine 引用已核验；用户确认重新启用“仅我的代码”、Debug 断点测试通过 | IDE 操作结果以用户反馈为依据，助手未直接操作界面 |
| CPU 配置 | 方案 Debug/Release x64 均映射两个项目的 x64；四组属性求值 `PlatformTarget=x64`；两个配置的 Sandbox EXE 均为 AMD64 | 配置正确，保留 Any CPU 选项不影响 x64 配置 |
| 用户个人截图 | `操作流程/` 已存在，根忽略规则明确排除该目录 | 仅检查目录存在和排除规则，不读取或纳入截图内容 |
| OpenGL 4.3 运行能力 | NVIDIA GeForce RTX 5060 Ti，驱动字符串 `591.86`；OpenGL `4.3.0`，GLSL `4.30`，Core Profile | 本机可见窗口、缩放、Escape/关闭退出通过；截图与源码已核对 |
| Piccolo 参考仓库 | `main`，提交 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，工作区干净 | 只读确认，未改动 |

## 初始文档交付记录（历史）

以下保留当时的目录名称 `engine`。该目录已于 2026-10-02 改为 `g104engine`，历史路径不作为当前操作入口。

- 已建立 `engine/docs/decisions`、`engine/src`、`engine/samples` 等目录。
- 已建立 README、协作规则、计划、状态、搭建指引、架构、学习映射和决策记录，共 8 份 Markdown 文档。
- 已准备根目录 Git 忽略规则及文本规范，供用户以后在界面初始化 Git；这不等于已经建仓。
- 已确认十份主笔记路径存在；正文图片语法扫描找到一张 Gameplay 相对路径图片。
- 已检查新建文档的 UTF-8、代码围栏、36 个本地链接与 1 个本地锚点，未发现问题；图表源已写入，尚未在图形渲染器中实测。
- 已只读验证忽略规则：检查 3,253 个现有路径和 10 个模拟路径，当前允许发布的实际文件正好 21 个（10 个新建文档/配置、10 份原笔记、1 张配图）。这是发布候选范围，不表示文件已加入 Git。
- 忽略规则验证借用现有 Git 元数据并显式指定待检查工作目录，未初始化 Git、未写入索引、未产生提交；已复核 Piccolo 仍为干净的 `main` 工作区。
- **未执行**软件下载安装、VS 工程创建、NuGet 还原、构建、图形运行、Git 初始化、提交、推送或 GitHub 建仓。未修改原十份笔记或 Piccolo 源码。

## 2026-10-01：延后具体软件架构（历史）

- 根据用户确认，当前只搭建开发基础，具体软件实现架构与细节留到后续计划讨论。
- `architecture.md` 已改为占位、待讨论事项及后续设计流程，撤下了当前正文中的具体模块图和帧流程。
- 原文完整保存在 [历史架构草案](archive/architecture-draft-2026-10-01.md)，带有非执行依据说明和原文件 SHA256；新增 [阶段边界决策 0002](decisions/0002-defer-implementation-architecture.md)。
- 同步调整协作规则、计划、搭建指引、学习映射、入口及基础决策记录；两个项目和包归属明确仅是环境验证的起步容器，不约束未来软件架构。
- 已确认的技术基线、单仓范围、用户可视化操作分工和提交前交互确认规则保持有效。下一步仍是用户完成 D1 安装。
- 本轮仅修改项目文档并保存历史草案，没有修改原笔记、引擎源码、项目文件或环境，也没有 Git 提交/推送。
- 本轮已检查 10 份 Markdown 的 UTF-8、代码围栏、51 个本地链接与 1 个本地锚点；未发现问题。当前架构正文不再含具体模块/帧流程图，原稿归档恢复出的 SHA256 与保存前一致。
- 发布边界只读复核通过：3,255 个现有路径中正好允许 23 个文件（含新增的历史归档和决策记录）；目标 Git 根仍未初始化，工程源码与解决方案仍未创建。

## 2026-10-02：目录与解决方案命名统一

- 用户通过资源管理器完成 `engine` → `g104engine` 目录改名；助手此前的原地改名命令被 Windows 拒绝，没有强制修改权限或关闭用户程序。
- 助手已核对旧目录不存在、新目录实际名称为全小写 `g104engine`，并同步入口、规则、计划、搭建指引和发布白名单。
- 解决方案计划文件名统一为 `g104engine.sln`；VS 若生成 `g104engine.slnx`，保留其真实格式。取消旧指引中先用临时名、再改成 `G104Engine.sln` 的步骤。
- 项目展示名称 G104Engine、起步项目 `G104.Engine` / `G104.Sandbox`、Git 根 `games104` 和仓库名 `games104-engine` 保持不变。
- 原十份笔记、Piccolo 引用地址和历史架构草案原文未修改；没有创建解决方案、项目、安装依赖、初始化 Git、提交或推送。
- 本轮核验通过：10 份 Markdown 的 51 个本地链接和 1 个锚点有效；3,255 个现有路径中允许发布的文件仍为 23 个，10 个模拟忽略规则用例均符合预期。旧目录已不存在，新目录大小写正确。
- 架构占位、学习映射、历史草案和决策 0002 这 4 份文档的字节哈希保持不变；归档原文校验一致。Piccolo 仍为干净的 `main` 工作区，新项目 Git 根及解决方案仍未创建。

## 2026-10-02：D1 安装核验通过

- 用户截图中的 VS2026 Community 18.10.3，与安装器实例查询结果一致。
- 使用 `vswhere` 限定 VS 18 实例，并分别核对 `Microsoft.VisualStudio.Workload.ManagedDesktop`、`Microsoft.NetCore.Component.SDK`、`.NET10 Runtime`、Roslyn 编译器、MSBuild 和 NuGet；六项均已安装。
- `dotnet --list-sdks` 包含 `10.0.401`；`dotnet --info` 确认 SDK 10.0.401、x64、Windows 10.0.26200；`dotnet --list-runtimes` 确认 .NET 与桌面运行时 10.0.12。
- 核查 SDK 的 `dotnet.dll`、`MSBuild.dll`、`Roslyn/bincore/csc.dll` 及 .NET/WindowsDesktop 的 net10.0 引用程序集目录，均存在。
- SDK 在安装器中使用通用“.NET SDK”名称；本机确已安装 SDK，无需因未看到“.NET 10 SDK”同名复选项而重复下载安装。已修正搭建指南说明。
- 此次为安装和可用工具检查，没有创建/编译测试项目、还原 NuGet、安装软件或修改环境配置；没有验证 OpenGL，也没有 Git 提交/推送。项目运行和断点调试留到 D2，图形能力留到 D3 验证。
- 已同步进度、搭建指南、入口、计划和规则中的状态说明；10 份 Markdown 的 54 个本地链接、3 个本地锚点及代码围栏检查通过。

## 2026-10-02：第 2.1 节与本地截图边界核验

- 实际路径为 `E:\game_study\games104\g104engine\g104engine.slnx`，未多出同名嵌套目录。
- XML 根元素为 `Solution`，没有项目节点；`dotnet sln g104engine.slnx list` 成功返回“未在解决方案中找到项目。”，符合空方案预期。
- 读取前后 SHA256 相同：`5FA615953B29289F51796E2CA86E8C511397CC1B7997A8CBB7823DBBB9F6A571`。未修改解决方案内容，也未生成、添加项目。
- 首次 CLI 列表检查因用户目录下的 .NET 首次使用缓存权限而失败；按授权重试后成功。关闭了该进程的证书生成和全局工具 PATH 添加；只发生 SDK 首次使用缓存初始化，没有安装依赖或修改项目配置。
- 用户新增的 `操作流程/` 仅本地使用。已在 `.gitignore` 添加 `/操作流程/`，并同步规则、计划和指南；未读取、整理或复制其中截图。
- 实际 `.slnx` 文件名已同步到后续说明；不用重新创建 `.sln` 或修改扩展名。Git 根仍未初始化；Piccolo 工作区保持干净。
- 本轮检查通过：10 份 Markdown 的 54 个本地链接、3 个锚点和代码围栏有效，解决方案内容哈希保持不变；当前发布候选为 24 个文件（较此前新增用户创建的 `.slnx`）。
- 7 个忽略规则用例通过；另以明确匹配规则核对 `/操作流程/` 排除目录及其嵌套文件，而 `.slnx` 保持可发布。本轮未遍历或读取截图目录的内容，仅使用目录存在性和模拟路径验证规则，未初始化 Git、提交或推送。

## 2026-10-02：第 2.2 节部分核验与配置问题

- 两个 `.csproj` 均在预期目录，目标框架为 `net10.0`；Sandbox → Engine 引用路径正确。
- 执行 `dotnet msbuild <项目路径> -getProperty:OutputType,TargetFramework,PlatformTarget,Platforms,Configuration,RuntimeIdentifier`，只评估属性，未构建或还原依赖。两个项目均返回 `Library`、`AnyCPU`，默认配置为 Debug。
- `G104.Sandbox` 目前没有 `Program.cs` 或入口；应由用户在 VS 中把输出类型改为控制台应用并按搭建指南加入最小入口。无需删除、重建项目或丢弃已添加的引用。
- 截图中的 `Any CPU` 是正常默认值；按项目的 Windows x64 约定，用户需创建/选择 x64 方案平台，并核对两个项目在 Debug、Release 下的 x64 映射及实际平台目标。
- 已在 `setup.md` 补充类型修正、入口示例和配置管理器逐步操作。助手只更新文档，不直接修改 `.slnx`、`.csproj` 或 `.cs`，也不把当前属性核查标为运行通过。
- 文档检查通过：10 份 Markdown 的 55 个本地链接、3 个锚点和代码围栏有效；本轮读取的解决方案、两个项目文件和两个模板源码文件共 5 个文件，前后哈希一致。

## 2026-10-02：补充控制台模板重建路线

- 用户确认创建 Sandbox 时选成类库，希望通过正确模板重新创建。只读复查磁盘中只有项目文件、空 `Class1.cs`、`bin` 和 `obj`，尚未发现业务实现或额外包引用。
- 本阶段采用重新创建模板可以恢复标准控制台配置和自动生成的入口；原地修改输出类型同样有效，现作为备用说明保留，不能让用户两条路线同时执行。
- 操作边界是先从解决方案移除 Sandbox，再由用户核对完整路径，仅删除 `samples/G104.Sandbox` 目录；保留 Engine、解决方案和所有其他目录。重新创建后需要重新添加项目引用、设置启动项目并核对平台。
- 助手仅编写指南和更新进度，没有删除、重建、修改项目或源码；重建完成情况等待用户反馈后核验。
- 本轮文档检查通过：10 份 Markdown 的 55 个本地链接、3 个锚点及代码围栏有效。

## 2026-10-02：第 2 节构建与运行验收

- 用户重建后的 Sandbox 已是控制台应用，入口与 Engine 引用正确；无需再次重建或修改输出类型。
- 两个项目在 Debug/Release、Platform=x64 下的 `OutputType`、`TargetFramework` 和 `PlatformTarget` 求值均符合约定；`.slnx` 中 `*|x64` 映射覆盖两个配置。
- 在 `g104engine` 目录执行 `dotnet build g104engine.slnx --configuration Debug -p:Platform=x64 --no-restore --nologo` 及对应 Release 命令，最终均成功，0 警告、0 错误。没有执行依赖还原。
- 首次受限环境内默认构建失败但未报告具体编译错误；单节点诊断构建成功，同时日志出现编译服务管道访问拒绝。经工具授权，在受限环境外使用原始默认构建命令验证，Debug/Release 均成功；未修改系统权限或项目配置。
- 直接运行 `samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.exe`，输出 `Hello, World!`，退出码 0。
- 对应 Release EXE 启动失败：加载 `G104.Sandbox.dll` 时出现 `FileLoadException`、`0x800711C7`，退出码 `-532462766`。经工具授权在受限环境外重试仍失败。Windows `Microsoft-Windows-CodeIntegrity/Operational` 事件 3033/3077 明确记录该 DLL 未满足签名级别或违反代码完整性策略；策略 ID 为 `{0283ac0f-fff1-49ae-ada1-8a933130cad6}`。具体策略来源尚未确定，不把它归因于项目模板或 x64 设置，也未关闭、绕过或修改系统防护。
- 两个配置的 Sandbox EXE 的 PE Machine 均为 `0x8664`（AMD64）。方案、两个项目文件及两个源码文件共 5 个输入文件在构建前后 SHA256 不变；构建产物位于 `bin`/`obj`，不纳入发布。
- 助手没有直接操作 VS，启动项目选择和断点行为仍需用户界面结果佐证。尚未验证 OpenGL，也未添加 NuGet 包、初始化 Git、提交或推送。

## 2026-10-02：VS 复现及 SAC 原因确认

- 用户反馈 VS Release/x64 同样出现 `FileLoadException`、`0x800711C7`，调用堆栈为空，并提供先前验收时的应用程序异常弹窗截图。仅查看本次明确提供的附件，未复制或发布截图。
- 最新 CodeIntegrity 3033/3077 事件再次指向 Release 的 `G104.Sandbox.dll`；3099 策略加载事件将同一策略 ID `{0283ac0f-fff1-49ae-ada1-8a933130cad6}` 标识为 `VerifiedAndReputableDesktop`。
- 只读查询 `HKLM\SYSTEM\CurrentControlSet\Control\CI\Policy` 的 `VerifiedAndReputablePolicyState` 为 `1`。根据 [微软 SAC 状态说明](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/test-your-app-with-smart-app-control)，表示开启/强制执行。结合事件记录，确认此次阻断来自 SAC；不是仅发生在助手执行环境内。
- Debug/Release 的 Sandbox DLL 的 Authenticode 状态均为 `NotSigned`。Debug 成功不代表所有未签名程序都被允许，具体两个产物的信任判定差异尚未确定。
- 根据加载失败的位置推断，入口程序集在执行用户代码前已被阻断，因此可能没有可显示的用户代码调用栈；不据此判断 VS 调试器、PDB 或工程损坏。
- [微软 SAC FAQ](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions) 说明没有单个应用的 SAC 例外开关；关闭是整机保护取舍。近期 Windows 更新支持重新开启，但本机界面与更新状态仍需核对，不能保证本机立即可恢复。
- 保留 SAC 的正式兼容方案可评估 [受信任提供方的代码签名](https://learn.microsoft.com/en-us/windows/apps/develop/smart-app-control/code-signing-for-smart-app-control)，不把自签名或 .NET 强名称等同此类签名；当前未引入证书、费用或签名流程。

## 2026-10-02：NuGet 核对与 Debug 再次受阻

- Engine 的三个 `PackageReference` 均为约定的 `4.9.4`。两个 `obj/project.assets.json` 均解析为 `net10.0`，包目录均存在，`logs` 为空；Debug 的 `G104.Sandbox.deps.json` 包含相应依赖。
- 传递依赖包括 OpenTK.Core、Windowing.Common、Windowing.GraphicsLibraryFramework `4.9.4`，以及 OpenTK.redist.glfw `3.4.0.44`。GLFW 包使用自己的版本号，不要求把它改成 4.9.4。
- 用户反馈安装包后 Debug/x64 也出现 `FileLoadException`、`0x800711C7`。2026-10-02 04:57:38、04:59:26 的 CodeIntegrity 3077 事件均指向 Debug 的 `G104.Sandbox.dll`，策略 ID 与前次 SAC 拦截一致；当前 `VerifiedAndReputablePolicyState=1`。
- 被拒绝的是本工程启动程序集；`Program.cs` 仍只有 `Console.WriteLine("Hello, World!");`，没有调用 OpenTK。现有证据不支持卸载包、改包版本或重建工程作为修复手段。
- SAC 根据文件信任信息和签名判定，不承诺对 Debug 放行。添加依赖后的重新构建可能改变二进制，但当前没有旧 Debug DLL 哈希可作比较，因此不把“哈希改变导致失去信任”当作已经证实的具体原因。
- 本轮没有再运行已知受阻程序或重新构建；不把旧的构建/运行结果冒充当前含包版本的验收。用户保留保护的选择继续有效。

## 2026-10-02：用户关闭 SAC 后运行复核通过

- 用户提供 SAC 关闭截图，并反馈 Debug 输出正常；Release 使用 F5 出现“仅我的代码”警告，选择禁用该选项后输出正常，再次运行未重复弹窗。
- 助手只读确认 `VerifiedAndReputablePolicyState=0`；用户已实际执行的关闭操作取代前轮“仅了解”的状态。助手未修改任何安全或 IDE 设置，未复制、发布本次截图。
- 在 `g104engine` 目录依次执行 `dotnet build g104engine.slnx -c Debug -p:Platform=x64 --no-restore --nologo -m:1` 及对应 Release 命令，两者均为 0 警告、0 错误。未还原、下载新依赖。
- 分别直接运行 `samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.exe` 和 Release 对应路径，两者均输出 `Hello, World!`，退出码 0。原 SAC 运行遗留项已解除；这尚不验证 OpenGL 上下文。
- 两个 `.csproj` 与 `Program.cs` 内容/哈希和前轮读取结果相同；构建仅产生或更新 `bin`、`obj` 产物，没有 Git 初始化、提交或推送。
- “仅我的代码”是 VS 的全局调试过滤偏好。Release 默认优化，.NET 调试器会把优化模块归为非用户代码，因此可能提示断点/单步体验受影响。根据用户选择推断，选项被禁用后不再出现同一警告符合预期；助手未直接核对该 IDE 选项是否已持久保存。
- 日常建议为 Debug/x64 + F5，并启用“仅我的代码”；普通 Release 运行使用 Ctrl+F5。恢复该选项不修改项目或编译优化，也不会重新开启 SAC。具体步骤与微软依据见 [第 2.2.4 节](setup.md#224-release-的仅我的代码警告)。

## 2026-10-02：确认本机暂时保持 SAC 关闭

- 用户在了解整机保护取舍后明确要求暂时保持关闭，并写入文档。原因是本机已实际发生 Debug/Release 产物被 SAC 拦截，关闭后两个配置的构建及运行均已通过。
- 该决定仅针对当前开发电脑，不要求其他设备关闭 SAC，不写入工程配置、构建脚本或 CI。以后重新开启或更换设备时，按当时状态重新核对运行结果，不假定所有环境都会拦截。
- 建议保持 Defender 实时保护、SmartScreen 和防火墙开启，不为此新增整个工程目录的杀毒排除；本轮没有验证或修改这些设置。
- 同步更新 `setup.md` 和 `plan.md`，保留先前排错历史。此次仅维护文档，没有执行构建、安装、系统设置变更、Git 提交或推送。

## 2026-10-02：用户确认调试设置与断点通过

- 用户明确反馈“启用仅我的代码”已重新勾选，Debug 断点测试正常；将前轮待补充的 GUI 验证项标为完成，证据来源为用户实际操作反馈。
- D2 的项目、引用、依赖、模板构建和运行验证已完成；不据此认定 OpenGL 环境或引擎功能已完成。
- 本轮检查 `global.json` 与两个项目的 `packages.lock.json` 均尚不存在。已在搭建指引补充第 4.1 节，交给用户创建，助手没有代为生成环境配置。

## 2026-10-02：第 4.1 节由助手执行并核验

- 用户明确将此步骤委托给助手。先确认文件尚不存在、本机 SDK 列表包含 `10.0.401`，再创建与 `.slnx` 同目录的 `global.json`，未覆盖已有配置。
- JSON 解析成功；从 `g104engine` 运行 `dotnet --version` 返回 `10.0.401`，`dotnet --info` 明确显示读取了本工程的 `global.json`。
- `dotnet msbuild src/G104.Engine/G104.Engine.csproj -getProperty:NETCoreSdkVersion,MSBuildSDKsPath` 返回 SDK `10.0.401` 和对应 `Sdks` 目录；只求值，没有构建或还原。这不等于本轮直接操作、验证了 VS 界面。
- 配置选择构建工具 SDK，不锁定运行时补丁、VS 版本或 NuGet 依赖，也不会安装缺失的 SDK。没有执行第 4.2 节或修改两个项目文件。
- 更新分工、计划、指引、入口和状态；尚未初始化 Git、提交或推送。

## 2026-10-02：补充 global.json 学习说明

- 前轮第 4.1 节已包含作用和目录查找规则；本轮进一步整理成字段表、使用场景、升级与缺失版本的例子，以及 SDK/目标框架/运行时/NuGet/IDE 的区别。
- 新增本次实际核验命令与结果的说明，明确配置由工具自动读取，不由游戏代码加载，也不是安装器或整机设置。
- 学习正文集中维护在 [setup 第 4.1 节](setup.md#41-固定-sdk版本选择)，本文件只记录进度，避免在多个文档重复维护同一份长解释。原配置与已验证结果保持不变，下一步仍为第 4.2 节。

## 2026-10-02：4.2A 锁文件生成验收通过

- 两个 `.csproj` 的现有属性组中均已添加 `RestorePackagesWithLockFile=true`，没有设置 `RestoreLockedMode`；MSBuild 属性求值与文件内容一致，SDK 为 `10.0.401`。
- 两个 `packages.lock.json` 位于各自项目文件旁，格式版本为 1、框架为 `net10.0`。Engine 包含 3 个 Direct 和 4 个 Transitive 包；Sandbox 包含 7 个 Transitive 包及 `g104.engine` 项目引用条目。
- 两份清单均为 6 个 OpenTK 组件 `4.9.4` 和 `OpenTK.redist.glfw` `3.4.0.44`，与前次确认的依赖一致；每个包的 `contentHash` 均与 `obj/project.assets.json` 和本机 `.nupkg.metadata` 中的内容哈希匹配，assets 未记录还原错误。这是记录一致性检查，不等于本轮重新执行严格还原或包签名验证。
- 核对时使用 NuGet 的内容哈希语义；带签名归档的 `.nupkg.sha512` 不必等于锁文件的 `contentHash`，不能据此误判包损坏。依据：[NuGet 元数据说明](https://github.com/NuGet/Home/wiki/Nupkg-Metadata-File)。
- 两个锁文件的 SHA256 分别为：Engine `7d63db9bd2e3908c62874618b35f03fbade1a2655ed30ad75245942f8324928a`；Sandbox `075ca324d4c16a8e0b8ba80bf44e10348a105755f3b803546da2315b541fcd19`，供操作 B 后比较是否保持一致。
- 首次 SDK 属性查询受用户目录 `.dotnet` 首次使用缓存权限限制；经工具授权在受限环境外重试成功，关闭了该检查进程的证书生成和全局工具 PATH 添加。没有还原、下载、构建或修改项目配置。
- 本轮只更新进度、指南、计划与入口，未初始化 Git、提交或推送。4.2A 通过不等于 4.2B 严格还原通过。

## 2026-10-02：4.2B 严格还原配置与记录核对通过

- 用户报告 Debug/Release 均生成成功；附件的输出来源是“程序包管理器”，显示程序包已齐全、无需还原。截图证明的是还原界面结果，不单独用作两个配置的编译证据。
- 两个 `.csproj` 均保留 `RestorePackagesWithLockFile=true` 并加入 `RestoreLockedMode=true`；MSBuild 属性求值确认两项均为 `true`，SDK 为 `10.0.401`。
- 两个 `obj/project.assets.json` 的 `project.restore.restoreLockProperties` 均记录 `restorePackagesWithLockFile="true"`、`restoreLockedMode=true`，无还原错误日志；各自的 `obj/project.nuget.cache` 均为 `success=true`。
- 两份锁文件的 SHA256 与 4.2A 记录完全相同，包条目的内容哈希与 assets 记录一致，未发生依赖清单改写。
- 本轮依据用户 GUI 操作反馈和本地还原产物完成核对，没有替用户执行还原、重复构建运行或通过故意改错项目来测试失败路径；未来新克隆/CI 仍需独立验证。
- 在 `setup.md` 补充“两属性并非相反的更新方向”的学习说明：使用锁文件与限制其更新可以同时启用；一致则还原，不一致则报错，均不会自动反向修改项目依赖声明。

## 2026-10-02：4.3 OpenGL 环境探针验收通过

- 实际 `Program.cs` 与指南示例一致：明确请求 OpenGL 4.3 Core、在 `OnLoad` 查询真实设备/版本、验证数值版本与 Profile、在缩放时更新 Viewport、Escape 关闭，并在每帧清屏和交换缓冲。
- 用户明确反馈 Debug/Release 均能运行并关闭；提供的窗口截图显示深蓝灰清屏窗口，控制台截图记录 NVIDIA Corporation、NVIDIA GeForce RTX 5060 Ti/PCIe/SSE2、OpenGL `4.3.0 NVIDIA 591.86`、GLSL `4.30 NVIDIA via Cg compiler`、数值版本 `4.3`、Core Profile `True`。
- 用户反馈覆盖两种配置的运行/关闭；截图本身不标识配置，因此记录时区分“截图可见内容”和“用户对两配置的操作反馈”。本轮只读取用户明确提供的两张附件，未读取、复制或发布个人 `操作流程/` 目录。
- 助手从 `g104engine` 使用 SDK `10.0.401` 和已有还原结果，分别执行 Debug/Release x64 的 `dotnet build ... --no-restore -m:1`；两者均成功，0 警告、0 错误。没有下载或更新依赖。
- 构建前读取的探针源码 SHA256 为 `FE3DA344D20DBA68D4F88BC4E4FBDB1F01C96EA37172E9502687BC70DFD6C4C2`；项目及两份锁文件未由助手修改。生成物位于 `bin`、`obj`，保持忽略。
- 此结果证明当前主机的 C# → OpenTK → GLFW → Windows/驱动 → OpenGL 上下文链路可用，不证明已经实现正式渲染器、资源系统或引擎主循环。

## 2026-10-02：4.4 非交互 smoke 检查通过

- 实际代码在传入 `--smoke` 时打印 .NET、OS、进程架构和三个 OpenTK 程序集信息后返回；不传参数时继续进入既有窗口探针。分支不初始化 GLFW 或 OpenGL，因此与可见窗口结果分开验收。
- 用户截图显示 Debug/Release 均输出 `.NET 10.0.12`、Windows `10.0.26200`、X64、OpenTK Graphics/Windowing/Math `4.9.4+8d1d462877936922a3989f56aac8c17add824919` 和 `Smoke result: PASS`，未出现窗口。
- 助手使用用户文档中的两条 `dotnet run ... --no-restore -- --smoke` 命令重复检查；输出与截图一致，两次退出码均为 0，没有还原或下载依赖。
- 源码结构核对确认默认无参数路径仍执行前轮已经通过的 `OpenGlProbeWindow`；本轮没有代用户再次启动 GUI。Smoke 只证明托管程序和程序集加载，不替代 OpenGL 4.3 Core 的用户可见验证。
- 本轮只读取用户明确提供的截图，不读取、复制或发布 `操作流程/` 目录。D3 本机环境验证完成；跨设备与 CI 结果仍必须单独记录。

## 2026-10-02：D4 首次发布范围只读复查

- `games104/.git` 与 `g104engine/.git` 均不存在；没有初始化、暂存、提交或远程关联。
- 按现有 `.gitignore` 枚举得到 31 个发布候选：根入口/规则 4 个、`g104engine` 工程与文档 16 个、指定笔记 10 个、实际引用图片 1 张。新加入范围包括 `global.json`、两个锁文件和探针源码；`bin`、`obj`、`.vs` 未进入候选。
- 使用 Piccolo 仓库的 Git 元数据和目标工作树执行 `check-ignore --no-index`，只读验证 README、规则、解决方案、配置、源码、锁文件和指定笔记可发布；模拟的 `操作流程/`、Piccolo、`bin`、`obj`、未指定资料和草稿均由预期规则排除。没有读取个人截图目录内容。
- 工程/维护文档未发现用户临时目录、截图路径、常见访问令牌格式或用户名绝对路径。10 份维护 Markdown 的本地链接和围栏检查通过。
- 十份公开笔记共有 31 个本地 Markdown 引用：5 个指向本次发布范围，25 个指向存在但被排除的本地文件，1 个指向本地已缺失的 Piccolo 资产。26 个非发布引用包括课程原始材料、MiniEcs 示例、整理记录和 Piccolo 本地源码；若原样发布，公开页面会出现无法访问的链接。
- 推荐保持已经确认的“十份正文＋一张配图”范围：Piccolo 源码引用按笔记对应的实际版本核对后转换为固定提交的 GitHub URL；课程原始材料、示例和整理记录保留原路径并标注“未随仓库发布”。引用的处理形式等待用户选择，不扩大上传范围。

## 2026-10-02：中断后接续与引用版本复核

- 接续先前的 D4 只读检查，没有重复构建、运行或重新初始化工程；目标 Git 根仍未创建。
- 26 指非发布的引用出现次数，不是文件数量；其中 8 处为 Piccolo 本地引用，其余 18 处为课程原始材料、MiniEcs 示例及整理记录。
- 只读核对 Piccolo `main=f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`、`homework01=ffa910856f9f9869d1d02bde7f22362d8a50b312`。当前 main 中不存在的 `engine/asset/objects/character/player/components/motor/player.motor.json` 在旧 homework01 提交中存在，因此不能把它当作完全无来源的资产，也不能将笔记链接一律改到 main；正文案例与版本对应仍需逐项核对。
- 清理 plan、setup 和 README 中“探针尚不存在”等过期描述；保留历史核验记录，当前源码、包及锁文件未修改。

## 2026-10-02：用户决定保留原笔记引用

- 用户明确答复“不处理”：十份笔记的原文、路径和链接全部保持原样，不替换 Piccolo 链接、不取消本地链接，也不在原笔记插入说明。
- 26 处非发布引用在 GitHub 上可能无法访问是已知限制，记录在维护文档，不再列为同步前必修问题。原始素材、Piccolo 及个人截图仍不上传。
- 再次枚举发布候选仍为 31 个文件，未扩大范围。本次不将建议的链接整理方案当作已执行，也不把“发布范围复查通过”等同于笔记所有链接均可在公开仓库访问。

## 2026-10-02：远程空仓库核验与 Git 工具调整

- 用户提供 `https://github.com/zxpeng83/games104-engine.git`，并明确自行使用 Git Bash／Git GUI 关联，暂不使用 VS Git。已同步规则、计划、指南和决策补充；此前 VS Git 操作流程不再作为当前交接方式。
- GitHub API 返回 `full_name=zxpeng83/games104-engine`、`visibility=public`、`private=false`、`default_branch=main`、`size=0`。`git ls-remote` 成功、退出码 0 且没有返回引用，核验远程为空。
- 初次网络检查受本机代理连接限制失败；经工具授权在受限环境外只读重试成功。查询未使用交互凭据、未克隆下载工程，也没有更改网络或 Git 配置。
- 本地 `games104/.git`、`g104engine/.git` 均不存在；`git -C games104 rev-parse --show-toplevel` 返回非 Git 仓库。下一步仍需由用户初始化正确目录并关联，不能把网页建仓标为本地同步完成。

## 2026-10-02：本地初始化与关联核对

- `rev-parse --show-toplevel --absolute-git-dir` 返回 `E:/game_study/games104` 和 `E:/game_study/games104/.git`；工程子目录没有第二个 `.git`，符合唯一新增仓库根的约定。
- `origin` 的获取/推送地址均正确。当前 HEAD 指向尚无提交的 `master`，不是已约定的 `main`；尚未设置上游分支，在空仓库阶段正常。
- `git ls-files` 为空，`git diff --cached --stat` 无输出；31 个候选均未跟踪，组成仍为根文件 4、工程/文档 16、十份笔记和一张配图。没有产生首次提交，`git log` 的“尚无提交”提示符合这一状态。
- 使用本仓库的实际忽略规则核对模拟截图/Piccolo 路径以及实际 `bin`、`obj` 路径，均命中预期排除规则；未读取个人截图内容。
- 当前有效 Git 配置没有 `user.name`、`user.email`；不替用户猜测公开的提交身份。姓名/邮箱配置与 GitHub 登录凭据是不同事项，待用户补充。

## 2026-10-02：分支与身份通过，首次提交清单已准备

- 用户已将分支改为 main 并配置姓名/邮箱；读取结果与反馈一致，origin 获取/推送地址正确，尚无提交且索引为空。
- 首次提交仍为 31 个文件，具体清单见 [setup 第 5.3 节](setup.md#initial-submit-manifest)。候选包含 4 个根文件、8 个工程源码/配置文件、8 个工程文档、10 份笔记和 1 张配图。
- 再次核对个人截图、Piccolo、`.vs`、bin/obj 的排除规则；两个 NuGet 锁文件 SHA256 与此前验收一致。没有新的源码改动需要重复程序测试，本轮仅更新维护文档。
- 建议提交说明为 `chore: initialize G104Engine development foundation`，目标公开仓库 `zxpeng83/games104-engine` 的 main；本次是否立即推送将在提交前确认，不以提供操作说明等同于已批准提交。

## 下一步：用户暂存，助手核对并发起提交确认

**执行人：用户。** 按 setup 第 5.3 节在正确根目录暂存这 31 个文件，检查清单后告知助手。暂存不会提交或上传，当前先不执行 commit/push。

**随后执行人：助手。** 检查实际索引文件和差异，再展示完整提交身份、清单、验证结果、说明、目标仓库/分支与推送选择，并通过交互确认后交给用户执行。需要更新已经暂存的文档时，应重新暂存并核对最终快照。

## 2026-10-02：补充依赖升级与锁文件更新流程

- 在 `setup.md` 第 4.2 节新增操作 C，明确锁文件不手工编辑；升级意图写入 `.csproj`，完整依赖图与内容哈希由 NuGet 重新计算。
- 流程包含：升级前核对范围、临时关闭严格模式、通过 Engine 的 NuGet 页面选择目标版本、解决方案级还原、审查两个锁文件、双配置构建/运行与功能验证、恢复严格模式后复验。
- Sandbox 通过项目引用获得 Engine 的依赖，不在 Sandbox 重复安装 OpenTK；但其应用入口锁文件也要随新的完整依赖图更新和审查。
- 本轮只补充学习与操作文档。当前 OpenTK 仍为 `4.9.4`，两个项目的严格模式继续为 `true`，现有锁文件未修改；没有实际升级任务。

## 后续待办

- SDK、依赖锁定及本机窗口/非交互探针均已通过；接下来完成 D4 建仓和同步。
- 原笔记引用检查已记录；按用户决定不处理、不扩充发布范围，不再作为同步阻碍。
- 远程建仓、本地初始化/关联、main 与身份已完成；接下来按 31 文件清单暂存、确认、提交并推送。
- 配置/验证 CI、新克隆和另一设备的复现；未测项保持未验证。

## Git 同步状态

当前代码和文档**未暂存、未提交、未推送**；本地 Git 已初始化并关联 origin，HEAD 为尚无提交的 main。远程前轮核验为空；本轮未重新查询。后续同步状态以实际提交核对为准，不能从 origin 存在推断工程已上传。
