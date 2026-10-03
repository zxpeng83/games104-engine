# 基础综合训练场 V1：依赖与素材准备清单

更新：2026-10-03。状态：**NuGet与双配置输出已核对；四个外部ZIP已下载并完成静态内容检查，OpenAL Win64库及同骨架角色/基础动作已识别。** 声音实际为Ogg，用户已选离线转PCM16 WAV；工具、转换及持久部署待落实。没有新增库功能验证，代码开工仍需弹窗明确同意。

## 1. 当前基线与推荐版本

两项目保持 net10.0、SDK 10.0.401、Windows x64。Engine 现有原三个 OpenTK 4.9.4 包和下表六个新增 NuGet 直接引用；Sandbox 仍只引用 Engine。两项目 RestorePackagesWithLockFile/RestoreLockedMode 均为 true，原七个包的解析版本和哈希未变化。

用户已选择并按清单配置 SharpGLTF.Core、StbImageSharp、JoltPhysicsSharp、ImGui.NET、OpenTK.Audio.OpenAL；JoltPhysics.Native 也已显式引用，原有三个 OpenTK 包未升级。OpenAL Soft 1.25.2 仍为待部署的原生运行库，不是此次六个 NuGet 包之一。

| 依赖 | 已解析 NuGet 版本 / 原生库待用版本 | 官方核对与职责 | 运行准备 |
| --- | --- | --- | --- |
| [SharpGLTF.Core](https://www.nuget.org/packages/SharpGLTF.Core/1.0.7) | 1.0.7 | 含 net10.0 目标；该目标无额外包依赖；MIT。读取 glTF/GLB 及访问节点/skin/animation 数据 | 纯托管；先不加入 Toolkit/Runtime，我们实现姿态求值、混合与蒙皮 |
| [StbImageSharp](https://www.nuget.org/packages/StbImageSharp/2.30.16) | 2.30.16 | 含 net8.0/netstandard2.0；MIT 或 Unlicense；解码 PNG/JPEG 等图片 | C# 移植，无独立图片解码 DLL；颜色空间、纹理上传/释放由引擎负责 |
| [JoltPhysicsSharp](https://www.nuget.org/packages/JoltPhysicsSharp) | 2.22.0 | 官方页实际含 net9.0/net10.0，依赖 Native >=1.1.0；MIT | 与下行原生包一起锁定；精确查询/刚体能力由库提供，CCT 移动规则自研 |
| [JoltPhysics.Native](https://www.nuget.org/packages/JoltPhysics.Native/1.1.0) | 1.1.0 | 建议显式约束版本，提供 win-x64 的 joltc.dll；MIT | 核对输出目录、CPU/精度模式、加载与退出；不能以 NuGet 安装成功代替原生运行测试 |
| [ImGui.NET](https://www.nuget.org/packages/ImGui.NET/1.91.6.1) | 1.91.6.1 | 含 net8.0/net6.0/netstandard2.0；MIT；包含 win-x64 cimgui.dll | 自行适配 OpenTK 输入、字体/顶点上传、裁剪和 GL 状态；库不提供场景编辑/Undo 语义 |
| [OpenTK.Audio.OpenAL](https://www.nuget.org/packages/OpenTK.Audio.OpenAL/4.9.4) | 4.9.4 | 实际目标 netcoreapp3.1；Core/Mathematics 范围 >=4.9.4 且 <4.10；MIT | 托管绑定；需要下行原生实现；NuGet 标示可用于 .NET 10，本机组合未验证 |
| [OpenAL Soft](https://github.com/kcat/openal-soft/releases/tag/1.25.2) | 1.25.2 | 独立原生运行库，LGPL-2.0-or-later；[官方 Windows 二进制目录](https://openal-soft.org/openal-binaries/) | 使用 Win64 版本，随应用部署并保留许可证/对应版本资料；不是新增图形 SDK |

OpenAL Soft 候选部署是把官方 Win64 的 soft_oal.dll 以 OpenAL32.dll 名称随应用提供，再由选定绑定加载；64 位 DLL 使用该文件名并不表示 32 位。[维护者说明](https://openal.org/pipermail/openal/2016-September/000532.html) 必须核验实际加载行为，不能从第三方 DLL 网站补文件。当前 OpenAL Soft 尚未部署；Jolt/cimgui 随 NuGet 的 Debug/Release 输出已按 3.2 核对。

NuGet 实际目标框架、计算兼容标记、包还原/文件部署和功能运行是不同证据；本批只读核对不代表整套组合的功能已验证。ImGui.NET 稳定包较旧，[官方仓库](https://github.com/ImGuiNET/ImGui.NET)记录维护交接；使用与 1.91.6 匹配的后端接口，不混入 1.92 字体/纹理 API。参考 [对应后端说明](https://github.com/ocornut/imgui/blob/v1.91.6/docs/BACKENDS.md)。

## 2. 已比较的物理备选与自研边界

已比较 [BepuPhysics 2.4.0](https://www.nuget.org/packages/BepuPhysics/2.4.0)＋BepuUtilities 2.4.0：纯 C#，net6.0，Apache-2.0；适合托管源码学习。该稳定版精确初始重叠处理需额外适配，包围盒候选不能直接当精确接触，sweep 在零时刻命中也不能假定带有效法线，见 [v2.4.0 查询](https://github.com/bepu/bepuphysics2/blob/v2.4.0/BepuPhysics/Simulation_Queries.cs) 与 [接触查询示例](https://github.com/bepu/bepuphysics2/blob/v2.4.0/Demos/Demos/CollisionQueryDemo.cs)。用户本次选择推荐的 Jolt 组合，不同时引入两个物理后端。

Jolt 绑定当前提供 CastRay、CastShape、CollideShape 等查询，见 [NarrowPhaseQuery](https://github.com/amerkoleci/JoltPhysicsSharp/blob/main/src/JoltPhysicsSharp/NarrowPhaseQuery.cs)。该源码链接为核查时主线，实施时还需核对安装版本 API。沿墙滑动、坡台、接地、跳跃和去穿透仍由项目定义；不改用完整 CharacterVirtual 后声称自研。物理与资产等库常用 System.Numerics，需在边界集中转换至既定 OpenTK 数学约定。

## 3. 用户在 VS 的准备步骤与核对结果

本批 NuGet 配置已由用户完成，助手没有代为安装；下面保留操作步骤，实际核对结果见 3.2。原生 OpenAL Soft 与素材准备仍未完成。

1. 用 VS 打开现有 g104engine.slnx，保持两个项目、net10.0、SDK 锁定与 x64 配置。先记录当前项目/锁文件状态，保留现有文档修改。
2. **先处理锁定模式，再添加依赖。** 两项目当前 RestoreLockedMode=true；本准备批次临时将两项目该属性设为 false，保留 RestorePackagesWithLockFile=true，允许一次受控依赖图更新。依据 [NuGet 锁文件说明](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#locking-dependencies)。
3. 通过 NuGet 管理界面为 G104.Engine 添加上表六个托管/原生包引用，指定列出的版本，不启用预发布、不批量升级全部包。Sandbox 保留项目引用，不复制一套重复直接引用；完成两项目还原。
4. 核对 Engine 与 Sandbox 两份锁文件实际解析版本、传递依赖和原生资产，然后恢复两项目 RestoreLockedMode=true 并再次锁定还原；不长期关闭锁定、不手改或删除锁文件。出现非预期版本变化/还原错误时先定位原因。
5. 准备 OpenAL Soft 官方 Win64 运行库和对应许可材料。原生/Shader/资产复制到最终 Sandbox 输出目录的规则由准备清单与后续获准配置共同落实；没有本地准备与明确规则前，不能标为“运行就绪”。

用户本批操作改变了 Engine.csproj 及两份锁文件；Sandbox.csproj 最终配置与原基线一致。助手仅只读核对，未执行安装/还原/构建。现有 D0–D4 构建记录和原探针可运行不能替代新增库功能验证；D5 仍暂缓。

<a id="nuget-vs-batch"></a>
### 3.1 本批操作说明：在 VS 添加六个包（已完成）

操作前的只读快照：Engine 只有三个 OpenTK 4.9.4 直接引用；两份锁文件为原七个包（Sandbox 另含项目引用），GLFW redist 3.4.0.44；两项目 RestoreLockedMode 都为 true。以下为当时给出的操作顺序，不覆盖 3.2 的最新结果。

本批只做 NuGet 准备，不编写引擎代码；操作由用户在 VS 完成：

1. 打开现有 [g104engine.slnx](../../g104engine.slnx)。在解决方案资源管理器分别右键 G104.Engine、G104.Sandbox，选择“编辑项目文件”；把两个项目的 RestoreLockedMode 从 true 改为 false 并全部保存，RestorePackagesWithLockFile 保持 true。
2. 右键 **G104.Engine** →“管理 NuGet 程序包”→“浏览”，包源选择 nuget.org，不勾选预发布。在详情中选定本页表格版本，依次安装 SharpGLTF.Core、StbImageSharp、JoltPhysicsSharp、JoltPhysics.Native、ImGui.NET、OpenTK.Audio.OpenAL。每次包安装都可能触发还原，因此第一步必须先完成。界面操作参考 [Microsoft NuGet 管理说明](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio)。
3. 全部安装后，右键解决方案执行“还原 NuGet 程序包”，等待成功；确保 Engine 和 Sandbox 的锁文件均已更新。
4. 将两个项目的 RestoreLockedMode 恢复为 true、全部保存，再还原整个解决方案。若有报错，记录“输出”窗口中 NuGet 的具体错误；不要用删除锁文件或升级全部包来跳过问题。
5. 完成后报告结果，由助手只读核对两个 csproj、两份锁文件和还原资产记录；预期 Engine 有原三包＋新增六包的直接引用，Sandbox 仍用项目引用。确认后继续 OpenAL Soft 原生库与素材准备；是否实际复制到运行输出仍需后续构建/加载验证。

本批允许产生 NuGet 还原缓存与 obj 资产记录；当前助手没有执行安装、还原或构建。不得把已有项目构建成功误报为新音频/物理/UI 运行通过，代码开工弹窗尚未发起。

### 3.2 已核对：NuGet 配置及 Debug/Release 输出通过

用户先反馈安装无报错、原项目可运行，随后补充“release也运行了，一切正常”。助手在 2026-10-03 只读检查项目、锁文件、obj 还原记录、两配置 bin 输出及 Git 差异，未运行 restore/build/应用，也未加载任何新原生库。

| 检查项 | 实际结果 |
| --- | --- |
| [Engine 项目](../../src/G104.Engine/G104.Engine.csproj) | 9 个直接引用；新增六包的版本全部与本页一致，三个原 OpenTK 包仍为 4.9.4 |
| [Sandbox 项目](../../samples/G104.Sandbox/G104.Sandbox.csproj) | 0 个直接包引用、1 个 Engine 项目引用；两项目 net10.0 与两个锁定属性均正确 |
| [Engine 锁文件](../../src/G104.Engine/packages.lock.json)、[Sandbox 锁文件](../../samples/G104.Sandbox/packages.lock.json) | 各含 13 个包，版本及 contentHash 逐项一致，Sandbox 另含项目节点；原七包没有升级或哈希变化 |
| 两项目还原记录 | project.nuget.cache 的 success 均为 true，无缓存错误日志；project.assets.json 目标为 net10.0，记录 restoreLockedMode=true，包图与锁文件一致 |
| Debug x64 输出 | deps.json 包含全部 13 个包；JoltPhysicsSharp、ImGui.NET、SharpGLTF.Core、StbImageSharp、OpenTK.Audio.OpenAL 托管 DLL 均存在 |
| Debug win-x64 原生文件 | runtimes/win-x64/native 下的 joltc.dll、joltc_double.dll、cimgui.dll 存在；PE Machine 为 0x8664，SHA-256 均与对应 NuGet 缓存文件一致；只查文件，不等于已成功初始化 |
| Release x64 输出 | 用户补生成/运行后再次核对：deps.json 全部13包与Debug一致；五个新增托管DLL、三份win-x64原生DLL齐全，与Debug及NuGet缓存SHA-256一致；原生文件为AMD64。原探针运行反馈由用户提供，不等于新增功能调用通过 |
| OpenAL Soft | 在 Sandbox/bin 范围未找到 OpenAL32.dll 或 soft_oal.dll，仍需单独部署；不能据此推断系统其他位置是否安装 OpenAL |
| 非文档 Git 差异 | 仅 Engine.csproj 和两份锁文件；原 Class1.cs、Program.cs、global.json 与 slnx 未改变，未暂存/提交/推送 |

结论：本批 **NuGet 引用、锁定还原记录和 Debug/Release 文件部署核对通过**。用户反馈两个配置的原窗口/清屏探针正常；它没有调用新物理、UI 或音频接口，因此新增模块初始化、功能和退出清理仍待获准实施后验证。下一步准备 OpenAL Soft 原生库与素材，不再重复要求补 Release 输出。

<a id="external-input-batch"></a>
### 3.3 当前操作批次：集中下载外部原始文件

为减少反复准备，用户可一次下载下面四个原始压缩包，暂存到 **E:\game_study\games104\g104engine\.cache\v1-preparation\**。该目录尚待用户创建；已用 git check-ignore 只读确认现有 `**/.cache/` 规则覆盖此处，无需修改 .gitignore。这里仅为本机准备缓存，不是正式发布目录。

| 下载项 | 官方入口与选择 | 本批操作 |
| --- | --- | --- |
| OpenAL Soft 1.25.2 Windows 二进制包 | [官方 ZIP](https://openal-soft.org/openal-binaries/openal-soft-1.25.2-bin.zip)，文件名 openal-soft-1.25.2-bin.zip；[目录核对](https://openal-soft.org/openal-binaries/) | 保留完整ZIP，避免误选源码包；不运行系统安装程序、不先复制DLL到系统目录 |
| Quaternius Universal Animation Library | [作者下载页](https://quaternius.itch.io/universal-animation-library)，选择免费 Universal Animation Library[Standard].zip，页面显示约15MB | 只需Standard，不必购买Pro/Source；如出现付费选择，使用页面的免费获取入口 |
| Kenney Impact Sounds | [作者页面](https://kenney.nl/assets/impact-sounds)，选择下载资产包 | 保留原文件名和ZIP；用于候选脚步/机关反馈 |
| Kenney Interface Sounds | [作者页面](https://kenney.nl/assets/interface-sounds)，选择下载资产包 | 保留原文件名和ZIP；用于候选提示反馈 |

当前先保存原始压缩包即可，不必手工重命名其中模型、合并骨架、转换声音或修改项目。下载后由助手只读核对压缩包清单、原生DLL架构及许可材料、GLB节点/skin/clip和声音头信息，再选实际采用子集并明确持久部署/复制规则；原始压缩包本身不自动加入Git发布范围。

作者页标注动画库提供GLB/CC0；[作者更新说明](https://quaternius.itch.io/universal-animation-library/devlog/1555893/added-root-motion-and-other-fixes)区分带 `_RM` 后缀与关闭Root Motion的版本，V1优先后者。但页面不足以逐项证明Standard包含所需Idle/Walk/Run/Jump及相容skin，必须检查实际包。Kenney两页标注CC0，未明确文件编码/位深/声道；不能预先声称都是所需PCM16 WAV。若格式不符，再讨论转换或替代来源，不自动装额外工具或新增解码依赖。

用户现已将四包放入上述目录，助手只读检查ZIP内存流，未解压落盘、部署、转换或执行内容。当前结果见 [外部输入核对记录](../reviews/v1-input-archives-check-2026-10-03.md)；缓存仍受Git忽略。文件检查不等于开始编码，开工弹窗要求继续有效。

## 4. 素材准备：来源候选与验收条件

原始四包已存在缓存且完成静态检查，尚未挑出部署到工程的正式资产。下表给出已核实的输入和剩余工作；实际图形/音频效果需获准实施后验证，不能把元数据检查当作最终演示验收。

| 用途 | 推荐准备方式 | 必须核对 |
| --- | --- | --- |
| 角色与动作 | 已核到 Quaternius Standard 包的 Unreal-Godot/UAL1_Standard.glb，CC0、非_RM版本；完整条目/哈希见核对记录 | 1skin/65joints、43clips、LINEAR、最多4正权重；Idle/Walk/Jog/Sprint及Jump_Start/Loop/Land齐全。需支持65骨骼、适配+Z前向并保留根变换；Run状态映射至Jog/Sprint待视觉/速度验证 |
| 场景与材质 | 地面/墙/坡台/按钮/门先使用本项目简单几何；加入少量有明确来源的未压缩 glTF/GLB 和 PNG/JPEG 作为导入验收 | 资产引用可解析、法线/UV 正确；只复制约定子集，记录来源和版本，不搬运整个 Piccolo 素材库 |
| 天空盒 | 一个固定六面环境背景，先可用自有程序生成色块/渐变，后换已审来源的六面图片 | 面方向、接缝、颜色空间一致；天空盒在 V1 仅为背景，不宣称已经实现 IBL |
| 短音效 | Kenney两包内共230个Ogg Vorbis文件，包内许可CC0；已选保持WAV、离线转换少量条目 | Impact130个均stereo/44100Hz；Interface100个为77mono/23stereo、44100Hz，WAV为0。3D候选需转单声道PCM16并试听；尚未解码/转换，当前PATH未找到ffmpeg/ffprobe，不自动安装工具 |
| 循环声/测试音 | 准备一个短循环 PCM WAV；许可明确的素材或本项目测试信号 | 循环接缝、停止与重载清理；测试信号只作功能验证，成品表现再单独试听 |

音频首版建议限定 RIFF/WAVE、整数 PCM16、1/2 声道，采样率从文件读取；逐 chunk 解析并处理对齐，不假设固定 44 字节头。首版拒绝 float/extensible/压缩格式；3D 点声源素材要求 mono，2D 提示音不随场景旋转/距离变化。OpenAL 负责播放 PCM，并不自动解码任意音频文件，见 [OpenTK AL API](https://opentk.net/api/OpenTK.Audio.OpenAL.AL.html) 和 [OpenAL 规范](https://www.openal.org/documentation/openal-1.1-specification.pdf)。其他格式给出明确错误，不静默播放错误字节。

对每个实际纳入资产记录作者、来源、下载版本/日期、许可证、所用文件及转换步骤。加入公共仓库前按 [发布范围](../publishing.md) 核对实际子集，当前不扩充发布清单、不下载或上传资源。不得假设一个包的整套宣传动画全部包含在免费子集中。

### 4.1 本批已知的部署/实现注意点

- OpenAL采用 `openal-soft-1.25.2-bin/bin/Win64/soft_oal.dll`，后续按包内说明以OpenAL32.dll随应用部署；`router/Win64/OpenAL32.dll`是路由器，不能仅凭名字拿错文件。保留COPYING、LICENSE-pffft和readme.txt，持久源目录/项目复制规则仍待落实。
- 该DLL的PE资源版本为1.25.1，嵌入字符串为ALSOFT 1.25.2；这是实测元数据差异，记录在核对报告，不据此要求重下或声称已运行验证。
- 角色GLB没有TANGENT/图片/纹理，贴图与法线贴图验收需另一个小型输入；可在获准实施时准备本项目自有测试资产，不必另购角色包。
- 声音离线转换路线已获选；候选条目及原声道见核对报告，未试听不能认定名称对应最终用途。实际转换工具及部署动作需要明确后执行，不自动新增解码库或安装工具。

### 4.2 助手定案：便携离线转换工具

按用户授权助手决定普通细节，选用FFmpeg的Gyan release essentials便携Windows构建，仅处理已选声音。来源是 [FFmpeg官方列出的Windows提供方](https://ffmpeg.org/download.html) → [Gyan发行构建](https://www.gyan.dev/ffmpeg/builds/)。本次核对版本9.0.2，候选文件为ffmpeg-9.0.2-essentials_build.zip；[提供方版本记录](https://www.gyan.dev/ffmpeg/builds/release-version)及[对应SHA-256](https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-9.0.2-essentials_build.zip.sha256)已读取。

预期ZIP SHA-256：`60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba`。后续获取时再次核对版本/哈希，不以浮动latest链接静默升级；尚未下载或运行。工具保留在被忽略的缓存中，不系统安装、不修改PATH、不加入引擎运行时依赖或Git发布包。实际获取/使用及素材提取、项目复制规则将明确列入最终开工范围，未获同意前不执行。

转换只选核对报告中的少量UI/脚步/机关候选，输出PCM16 WAV、44100Hz；3D点声源输出mono，UI按实际需求保留声道。保留原ZIP和输入文件，记录工具版本、条目、转换参数及输出哈希；按WAV chunk格式检查后再试听，不将文件生成直接标为声音效果通过。

## 5. 准备与开工状态

| 项目 | 当前状态 |
| --- | --- |
| 依赖组合方向 | 用户已选择推荐组合 |
| 精确版本与资料 | 六包已按列出版本解析；尚无新增库功能调用验证 |
| NuGet/项目/两份锁文件 | 用户已配置/还原，助手只读核对通过；锁定模式已恢复 |
| 原生运行库 | Jolt/cimgui两配置输出已核对；OpenAL Win64实现库已在ZIP内核对，尚未部署/加载 |
| 模型/同骨架动作/图片/声音 | 角色/基础动作静态核对通过；纹理/循环测试输入需补，Ogg→PCM16离线转换已选但未执行；效果仍待验收 |
| 完整实施方案 | 见 [V1 实施草案](v1-implementation-draft.md)，仍待整体审阅 |
| 开工弹窗 | **尚未发起、尚未获准，不写代码** |

外部原始文件已到齐；后续落实离线转换工具/条目与持久部署规则。获准实施后再验证原生初始化/退出、GLB/图片读取、角色查询、UI 输入/缩放和基本声像；文件与元数据检查通过不等于正式模块完成。
