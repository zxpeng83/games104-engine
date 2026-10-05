# 基础搭建、探针与V1准备：历史索引和补遗

这是按时间组织的历史过程，保留当时目标、操作、实际核验及后续替代关系。现行技术合同见专题指南，当前任务查 [status.md](../status.md)，操作政策查 [agent-workflow.md](../agent-workflow.md)；历史命令不会重新获得执行权限。

<a id="foundation-20261002"></a>
## 2026-10-02：首次环境、工程与探针

完整安装/空方案/两个项目、Sandbox空模板A/B、x64、SAC、仅我的代码、三OpenTK包、global.json/锁文件A-B-C、OpenGL代码与smoke分支已逐字保存在 [foundation-documents-2026-10-02.md](foundation-documents-2026-10-02.md) 的“Snapshot: g104engine/docs/setup.md”。这里不再复制相同代码，原快照仍可审阅；其旧“待开工/当前状态”属于当时上下文，不能覆盖现行规则和V1。

| 当时过程 | 实际结果与教学要点 | 后续适用边界 |
| --- | --- | --- |
| VS/SDK安装核查 | VS2026 Community18.10.3 Stable、SDK10.0.401 x64、Runtime/Desktop10.0.12；VS.NET桌面组件已检出，dotnet可选工作负载列表为空不等于缺VS组件 | 原版号是日期明确的本机结果，非本轮重新查询或跨设备要求 |
| .slnx与项目 | 先空Solution再Engine类库/Sandbox控制台、Sandbox→Engine；误建Sandbox类库时仅有Class1/csproj/bin/obj，用户采用A移除/回收空目录后重建 | B改OutputType＋最小入口未执行；现在有V1业务/资产，不能按A/B删重建或覆盖Program |
| x64/Hello World | 方案和两个项目Debug/Release映射/PlatformTarget核对，双配置构建0警告0错误；模板Debug输出Hello World | 仅证明当时模板/工具链，不能代替V1或GPU行为 |
| SAC故障与调试 | CodeIntegrity3033/3077、3099与状态1定位0x800711C7；用户关闭后Debug/Release恢复，助手核状态0；用户后确认JMC重勾、Debug断点通过 | 只影响本机当时选择，非其他设备必关；Defender/SmartScreen/防火墙当时未全部核，SAC重启未实测 |
| SDK/锁定 | 用户专项委托助手创建global.json并核dotnet --version/--info路径及MSBuild属性；用户VS完成锁文件A及严格模式B，B哈希与A一致 | 配置解析/还原/构建分别取证；A/B后来被9包依赖图替代，旧哈希不是现行锁文件不变的证据 |
| OpenGL临时窗口 | 用户可见Debug/Release窗口、resize、Escape/关闭，RTX5060Ti/OpenGL4.3/GLSL4.30/Core=True；助手双配置no-restore构建0警告0错误 | 无GPU对象的蓝色探针，不是正式引擎；当时图形反馈来自用户，不能声称助手代启动 |
| 非窗口smoke | 用户VS终端和助手no-restore dotnet run均X64/.NET10.0.12/三个OpenTK4.9.4、PASS、exit0 | 不初始化GLFW/GL；dotnet run仍可能构建bin/obj，今天运行已有DLL命令在现行指南 |

旧探针包链是C#→OpenTK→GLFW→Windows/显卡驱动→当前GL上下文；OpenTK包不等于驱动，不需独立“OpenGL4.3 SDK”。创建上下文后查询版本/Core/vendor/renderer才有效；不支持需保留异常/栈，不静默降级/自动升级驱动。将来正式GPU资源由有效上下文线程显式释放，不能从using窗口就推定所有GPU生命周期正确。

<a id="git-foundation"></a>
## 2026-10-02：Git初建、31文件首推与上游补设

完整Git Bash命令、GUI备选、精确31路径清单及首次提交摘要保存在同一 [foundation-documents-2026-10-02.md](foundation-documents-2026-10-02.md) 的setup第5节（“首次提交清单”）。现行操作与发布白名单在 [git-and-publishing.md](../guides/git-and-publishing.md)，历史不构成今天的全量暂存授权。

1. 用户网页创建公开zxpeng83/games104-engine。2026-10-02 API为public/size0/default main、ls-remote无引用，说明当时为空；不套模板/生成额外README或License，以免已有本地内容多出需处理的远程历史。
2. 用户选择Git Bash/Git GUI，暂不用VS Git；本地唯一工程仓库根是E:\game_study\games104，origin为HTTPS；没有在g104engine/资料初始化，没有把Piccolo作子模块，也未清上层Git。
3. 初查HEAD为master且未有提交、姓名/邮箱未设；用户更名main并按该仓库--local补身份。配置姓名可与GitHub名不同，邮箱可验证邮箱/noreply；未把密码/令牌写入文档。更名/remote add不等于提交上传。
4. 首次31文件由根入口/配置4、工程/SDK/源码/锁文件8、工程文档8、十笔记及一配图组成；Class1只是模板。原精确路径清单保持，排除截图/Piccolo/网页PDF其他资料/备份/.vs/bin/obj/缓存，不把.git当普通文件。
5. 当时恰31个未跟踪文件、忽略已核、没有其他跟踪修改，才能按获确认范围git add --all；之后实际cached name-status/stat/check核对均A。git diff --check只是空白检查，不是功能测试；这条历史不能授权今天全量暂存或强制加入忽略文件。
6. 经用户明确确认后由用户提交/推送，首次完整SHA为3ea75150225e6df5fad9c92a5d03f2b1ad087be7，本地及GitHub main与31文件树一致。说明为chore: initialize G104Engine development foundation；当时验证仅工具链/窗口/smoke，V1功能尚未实施。
7. 首推后本次用户在其GUI找不到上游入口，改用Git Bash branch --set-upstream-to=origin/main main成功；并非所有GUI都不支持。截图显示main跟踪origin/main，助手读取branch.main.remote=origin、branch.main.merge=refs/heads/main，领先/落后0 0。
8. 那次0 0只比本地提交与origin/main缓存，status还有M README.md，且未重查服务器，不能据此认定工作区干净或实时远端一致。建立upstream不创建提交/上传；后续节点与实际服务器核验另记在 [git-submission-history.md](../reviews/git-submission-history.md)。

用户先因没有第二电脑暂缓设备测试，随后明确把本机新目录克隆、CI、第二设备整个D5暂缓。26处非发布笔记链接按明确“不处理”保留，不添加标注/替换链接或扩大资料。私有截图不默认读取/引用/复制/上传；以上历史仅用于追溯，当前恢复查status。

<a id="v1-preparation-20261003"></a>
## 2026-10-03：六包NuGet准备与静态核验补遗

这部分发生在基础快照之后，原文来源是撤下的V1依赖素材清单第3节。目标是准备包和输入；最初OpenAL/素材尚未部署、正式弹窗未答复。随后取得完整范围明确授权并完成部署，不能把历史“待准备”重新当成当前缺口。

### 当时用户在VS添加六包的顺序

该批固定版本：SharpGLTF.Core1.0.7、StbImageSharp2.30.16、JoltPhysicsSharp2.22.0、JoltPhysics.Native1.1.0、ImGui.NET1.91.6.1、OpenTK.Audio.OpenAL4.9.4，保留原三OpenTK4.9.4与GLFW redist3.4.0.44。

操作前的只读快照：Engine 只有三个 OpenTK 4.9.4 直接引用；两份锁文件为原七个包（Sandbox 另含项目引用），GLFW redist 3.4.0.44；两项目 RestoreLockedMode 都为 true。以下为当时给出的操作顺序，不覆盖后文该批历史核验结果。

本批只做 NuGet 准备，不编写引擎代码；操作由用户在 VS 完成：

1. 打开现有 [g104engine.slnx](../../g104engine.slnx)。在解决方案资源管理器分别右键 G104.Engine、G104.Sandbox，选择“编辑项目文件”；把两个项目的 RestoreLockedMode 从 true 改为 false 并全部保存，RestorePackagesWithLockFile 保持 true。
2. 右键 **G104.Engine** →“管理 NuGet 程序包”→“浏览”，包源选择 nuget.org，不勾选预发布。在详情中选定该批固定版本，依次安装 SharpGLTF.Core、StbImageSharp、JoltPhysicsSharp、JoltPhysics.Native、ImGui.NET、OpenTK.Audio.OpenAL。每次包安装都可能触发还原，因此第一步必须先完成。界面操作参考 [Microsoft NuGet 管理说明](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio)。
3. 全部安装后，右键解决方案执行“还原 NuGet 程序包”，等待成功；确保 Engine 和 Sandbox 的锁文件均已更新。
4. 将两个项目的 RestoreLockedMode 恢复为 true、全部保存，再还原整个解决方案。若有报错，记录“输出”窗口中 NuGet 的具体错误；不要用删除锁文件或升级全部包来跳过问题。
5. 完成后报告结果，由助手只读核对两个 csproj、两份锁文件和还原资产记录；预期 Engine 有原三包＋新增六包的直接引用，Sandbox 仍用项目引用。确认后继续 OpenAL Soft 原生库与素材准备；是否实际复制到运行输出仍需后续构建/加载验证。

历史NuGet批允许生成还原缓存/obj；当时助手未安装/还原/构建，提前开工弹窗未获答复，因此仍须正式范围确认。后来2026-10-03已取得完整范围明确同意并执行；不能用这段历史重新否定授权，也不能用原探针代替后来原生功能证据。

### 当时文件、锁定与原探针核验

用户先反馈安装无报错、原项目可运行，随后补充“release也运行了，一切正常”。助手在 2026-10-03 只读检查项目、锁文件、obj 还原记录、两配置 bin 输出及 Git 差异，未运行 restore/build/应用，也未加载任何新原生库。

| 检查项 | 实际结果 |
| --- | --- |
| [Engine 项目](../../src/G104.Engine/G104.Engine.csproj) | 9 个直接引用；新增六包的版本全部与该批固定版本一致，三个原 OpenTK 包仍为 4.9.4 |
| [Sandbox 项目](../../samples/G104.Sandbox/G104.Sandbox.csproj) | 0 个直接包引用、1 个 Engine 项目引用；两项目 net10.0 与两个锁定属性均正确 |
| [Engine 锁文件](../../src/G104.Engine/packages.lock.json)、[Sandbox 锁文件](../../samples/G104.Sandbox/packages.lock.json) | 各含 13 个包，版本及 contentHash 逐项一致，Sandbox 另含项目节点；原七包没有升级或哈希变化 |
| 两项目还原记录 | project.nuget.cache 的 success 均为 true，无缓存错误日志；project.assets.json 目标为 net10.0，记录 restoreLockedMode=true，包图与锁文件一致 |
| Debug x64 输出 | deps.json 包含全部 13 个包；JoltPhysicsSharp、ImGui.NET、SharpGLTF.Core、StbImageSharp、OpenTK.Audio.OpenAL 托管 DLL 均存在 |
| Debug win-x64 原生文件 | runtimes/win-x64/native 下的 joltc.dll、joltc_double.dll、cimgui.dll 存在；PE Machine 为 0x8664，SHA-256 均与对应 NuGet 缓存文件一致；只查文件，不等于已成功初始化 |
| Release x64 输出 | 用户补生成/运行后再次核对：deps.json 全部13包与Debug一致；五个新增托管DLL、三份win-x64原生DLL齐全，与Debug及NuGet缓存SHA-256一致；原生文件为AMD64。原探针运行反馈由用户提供，不等于新增功能调用通过 |
| 当时OpenAL Soft | 原探针Sandbox/bin未找到实现DLL；随后正式实施已部署OpenAL32.dll并验证，不再是当前缺口 |
| 当时非文档Git差异 | 仅Engine.csproj和两份锁文件，原Class1.cs/Program.cs/SDK/slnx未变；这些准备改动现已由用户提交推送至cbce581，见节点记录 |

该批结论仅为NuGet引用/锁记录/双配置文件部署及用户原探针反馈通过，未调用新接口；当时下一步是OpenAL/素材准备。随后新增模块初始化/功能/清理已经正式实施并验证，见当前部署与台账，不重新要求Release输出或部署。

<a id="external-input-batch"></a>
### 集中保存四个原始ZIP

用户当时按下表下载四个原始ZIP至 **E:\game_study\games104\g104engine\.cache\v1-preparation\**，目录/文件已准备；原静态核对见输入报告。`**/.cache/`忽略规则覆盖此处，它是本机来源缓存，正式采用子集另存assets/third_party，不将整包自动纳入Git。

| 下载项 | 官方入口与选择 | 本批操作 |
| --- | --- | --- |
| OpenAL Soft 1.25.2 Windows 二进制包 | [官方 ZIP](https://openal-soft.org/openal-binaries/openal-soft-1.25.2-bin.zip)，文件名 openal-soft-1.25.2-bin.zip；[目录核对](https://openal-soft.org/openal-binaries/) | 保留完整ZIP，避免误选源码包；不运行系统安装程序、不先复制DLL到系统目录 |
| Quaternius Universal Animation Library | [作者下载页](https://quaternius.itch.io/universal-animation-library)，选择免费 Universal Animation Library[Standard].zip，页面显示约15MB | 只需Standard，不必购买Pro/Source；如出现付费选择，使用页面的免费获取入口 |
| Kenney Impact Sounds | [作者页面](https://kenney.nl/assets/impact-sounds)，选择下载资产包 | 保留原文件名和ZIP；用于候选脚步/机关反馈 |
| Kenney Interface Sounds | [作者页面](https://kenney.nl/assets/interface-sounds)，选择下载资产包 | 保留原文件名和ZIP；用于候选提示反馈 |

当时仅下载保存ZIP，助手随后只读核对DLL/许可、GLB/skin/clip与声音头，确认采用子集与复制规则；正式授权后才提取/转换/部署。原始四包未整体发布，正式采用子集见依赖素材合同。

作者页标注动画库提供GLB/CC0；[作者更新说明](https://quaternius.itch.io/universal-animation-library/devlog/1555893/added-root-motion-and-other-fixes)区分带 `_RM` 后缀与关闭Root Motion的版本，V1优先后者。但页面不足以逐项证明Standard包含所需Idle/Walk/Run/Jump及相容skin，必须检查实际包。Kenney两页标注CC0，未明确文件编码/位深/声道；不能预先声称都是所需PCM16 WAV。若格式不符，再讨论转换或替代来源，不自动装额外工具或新增解码依赖。

该输入批用户四包到齐，助手当时仅检查ZIP内存流，未提取/运行；证据见 [v1-input-archives-check-2026-10-03.md](../reviews/v1-input-archives-check-2026-10-03.md)。后续正式弹窗已批准提取/转换/部署并完成，缓存仍受Git忽略，历史静态检查不代替功能验证。

<a id="deployment-transition"></a>
## 2026-10-03至10-04：准备、正式授权与部署的不同证据

- NuGet准备批只改Engine.csproj和双锁文件，Sandbox当时与基线一致；项目/13解析包/contentHash/obj成功标记/双配置原生文件核对不调用新接口。准备后来保存于cbce581，精确开工前记录见 [v1-start-checkpoint-2026-10-03.md](../reviews/v1-start-checkpoint-2026-10-03.md)。
- 四ZIP先只读ZIP内存流，不提取/运行；原包230个声音实为Ogg，角色为1skin/65关节/43LINEAR，OpenAL PE资源1.25.1/嵌入1.25.2差异保留。实际目录和SHA见 [v1-input-archives-check-2026-10-03.md](../reviews/v1-input-archives-check-2026-10-03.md)。
- 提前弹窗未答复不算开工；2026-10-03用户正式“同意，按以上完整范围正式开工”后才进行代码、资源复制、OpenAL/许可/源码、便携FFmpeg校验提取转换和验证。同范围恢复与历史仅文档阶段区别见 [decisions.md](../decisions.md)。
- 选Gyan release essentials便携FFmpeg9.0.2，ZIP SHA为60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba；七段mono/44100/pcm_s16le输出及来源/绝对命令/哈希入manifest，工具缓存未装系统/改PATH、不随运行发布。
- 正式角色非_RM、Run→Jog、根姿态/实例校正与128palette；自有material-probe/checker/flat normal弥补无图片/TANGENT输入，未搬Piccolo素材。OpenAL采用bin/Win64 soft_oal→OpenAL32而非router，许可和同版本完整源码保存；这些当前合同集中在 [dependencies.md](../guides/dependencies.md#assets-and-conversion)。
- 文件部署、格式解析、原生初始化、行为验证、GL画面、声音听感、用户人工体验与源码掌握分别记录。首轮及后续专项有各自有效构建/日志，日期精确的原证据见 [v1-progress.md](../execution/v1-progress.md) 和 [v1-independent-review-2026-10-03.md](../reviews/v1-independent-review-2026-10-03.md)；文档改造没有重跑应用。
- 后续UI/导航/GGX/HDR/粒子/资产及近180°TRS修复的过程保留在专题页可选历史与reviews。准备方案/元数据不是功能通过，旧PASS不能代替改后新验回；D5未因V1交付自动恢复。

## 可选资料的定位方式

本页按日期说明历史意义；基础快照是原文证据，不是新的必读入口。SDK选择/锁文件/包许可与转换用 [dependencies.md](../guides/dependencies.md)，环境故障用 [environment.md](../guides/environment.md)，现行smoke/verify/GL/音频入口用 [v1-run-and-review.md](../guides/v1-run-and-review.md#run-and-verify)。已有业务源码始终以实际文件为准，不将旧代码覆盖回Program。
