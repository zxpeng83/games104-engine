# 依赖、版本锁定与素材合同

本页维护精确依赖、部署来源、选型理由及受控升级方法。当前任务/同步状态查 [status.md](../status.md)，安装/还原及变更权限见 [agent-workflow.md](../agent-workflow.md)；这里的操作说明不会自动启动升级。历史准备步骤已完成，按页末日期索引追溯，不需重新添加包。

<a id="dependency-locks"></a>
## 已采用版本与分工

global.json选择SDK10.0.401、rollForward=disable、allowPrerelease=false；两项目目标net10.0、Windows x64，RestorePackagesWithLockFile和RestoreLockedMode均为true。Sandbox只引用Engine，不重复装直接包。Engine九个直接包及两份各13解析包的精确依赖图以项目/锁文件为机器可读来源，本文解释作用和取舍。

| Engine直接包 | 版本 | 作用/边界 |
| --- | --- | --- |
| OpenTK.Graphics | 4.9.4 | GL绑定，不是显卡驱动 |
| OpenTK.Windowing.Desktop | 4.9.4 | GLFW桌面窗口/输入 |
| OpenTK.Mathematics | 4.9.4 | 数学类型，项目解释矩阵边界 |
| OpenTK.Audio.OpenAL | 4.9.4 | netcoreapp3.1托管绑定，不解码Ogg；Core/Mathematics要求>=4.9.4且<4.10 |
| ImGui.NET | 1.91.6.1 | net8/net6/netstandard2.0及win-x64 cimgui；自研OpenTK输入/渲染后端和场景命令 |
| JoltPhysicsSharp | 2.22.0 | net9/net10托管包装，依赖Native>=1.1.0 |
| JoltPhysics.Native | 1.1.0 | 显式锁win-x64 joltc，真实初始化与查询另验证 |
| SharpGLTF.Core | 1.0.7 | 含net10目标且该目标无额外依赖；只用Core，不加Toolkit/Runtime |
| StbImageSharp | 2.30.16 | net8/netstandard2.0 C#图片解码，无独立解码DLL |

上述六个后续新增包的公开许可资料为MIT（StbImageSharp为MIT或Unlicense），接口资料按固定版本使用，不混入ImGui1.92 API。依赖出处：[Engine项目](../../src/G104.Engine/G104.Engine.csproj)、[Sandbox项目](../../samples/G104.Sandbox/G104.Sandbox.csproj)、[Engine锁文件](../../src/G104.Engine/packages.lock.json)、[Sandbox锁文件](../../samples/G104.Sandbox/packages.lock.json)、[global.json](../../global.json)。

OpenAL Soft1.25.2独立于NuGet包，LGPL-2.0-or-later；采用官方包bin/Win64/soft_oal.dll改名OpenAL32.dll，部署到third_party后由Sandbox复制应用根并由AudioSystem显式加载。router/Win64/OpenAL32.dll未采用，64位文件名含32不代表32位。许可/readme与同版本完整源码归档及SHA见 [SOURCE.md](../../third_party/openal-soft/1.25.2/SOURCE.md)。PE资源1.25.1与嵌入ALSOFT1.25.2是已核输入的元数据差异，实际后端曾报告1.25.2，不据它自动重下。

<a id="openal-deployment"></a>
### OpenAL Soft为什么单独部署DLL

2026-10-05补充说明：解释当前获取与部署方式及其取舍，不新增依赖或改变已采用版本。

OpenAL Soft既可以由原生NuGet包分发，也可以直接使用官方预编译DLL。当前工程采用“NuGet提供C#绑定、官方DLL提供实际音频实现”：OpenTK.Audio.OpenAL 4.9.4是托管绑定，本机已还原包中没有OpenAL32.dll原生实现；只有绑定并不足以完成后端播放。绑定定位见 [OpenTK.Audio.OpenAL 4.9.4](https://www.nuget.org/packages/OpenTK.Audio.OpenAL/4.9.4)。

当前官方DLL方案的实际考虑和收益是：

- 来源与版本明确：直接采用已核验的OpenAL Soft 1.25.2 Windows x64二进制，保存来源、哈希、许可及对应源码；所采用文件和实际后端版本可直接核对。
- 单一平台部署直接：V1限定Windows x64，音频实现DLL随应用输出，OpenAL无需用户另装系统级运行库。官方 [readme.txt](../../third_party/openal-soft/1.25.2/readme.txt)支持不使用router时将soft_oal.dll改名为OpenAL32.dll；此方式固定使用该实现，不经router选择其他系统安装实现。
- 构建自动复制：DLL保存在third_party的正式版本目录，[G104.Sandbox.csproj](../../samples/G104.Sandbox/G104.Sandbox.csproj)的Content/CopyToOutputDirectory负责复制到Debug/Release输出；[AudioSystem.cs](../../src/G104.Engine/Audio/AudioSystem.cs)从AppContext.BaseDirectory加载。它不是每次手工放进bin，缓存ZIP也不是长期部署源。

NuGet能分发原生库，通常通过runtimes/{rid}/native管理不同平台并复制运行所需文件；本工程的JoltPhysics.Native已采用这类获取方式。机制见 [NuGet原生库打包说明](https://learn.microsoft.com/en-us/nuget/create-packages/native-files-in-net-packages)。OpenAL Soft也有原生包，例如 [Silk.NET.OpenAL.Soft.Native 1.23.1](https://www.nuget.org/packages/Silk.NET.OpenAL.Soft.Native/1.23.1)；这里仅举已核对的固定版本，不是最新版本承诺、穷尽候选清单或迁移推荐。

采用合适的原生包，原则上可继续使用OpenTK绑定；不必因包名包含Silk.NET就推定要替换整个图形/窗口框架。实际适配仍需核对包内OpenAL Soft版本、win-x64文件、DLL名称、构建/发布落点、加载规则及许可/源码来源，再做原生播放与清理验证。包可还原或目标框架兼容，不单独证明这条运行链已通过。

直接部署DLL的代价是自行管理二进制更新与各平台文件；采用维护状态、版本及平台均合适的原生包，可以减少复制配置和更新工作。两种获取方式不直接决定音频性能或效果，真正运行的实现与版本才是比较基础。

**选型记录的限制：** 当时记录能证明官方DLL方案已核对、部署并有实际运行证据，没有充分记录对原生NuGet候选的系统比较。因此不能断言NuGet不适用，也不能把当前方案称为优于所有NuGet方案；上述部署收益是本次补充解释，不补造当时用户逐项选择或候选已被验证淘汰的历史。后续是否改变获取方式属于另行收敛的依赖变更，本说明不启动安装、换包或升级。

## 物理与解码方案为什么这样选

已比较 [BepuPhysics 2.4.0](https://www.nuget.org/packages/BepuPhysics/2.4.0)＋BepuUtilities 2.4.0：纯 C#，net6.0，Apache-2.0；适合托管源码学习。该稳定版精确初始重叠处理需额外适配，包围盒候选不能直接当精确接触，sweep 在零时刻命中也不能假定带有效法线，见 [v2.4.0 查询](https://github.com/bepu/bepuphysics2/blob/v2.4.0/BepuPhysics/Simulation_Queries.cs) 与 [接触查询示例](https://github.com/bepu/bepuphysics2/blob/v2.4.0/Demos/Demos/CollisionQueryDemo.cs)。用户在2026-10-03选择推荐的 Jolt 组合，不同时引入两个物理后端。

绑定已调用CastRay/CastShape/CollideShape等查询；原准备链接 [NarrowPhaseQuery](https://github.com/amerkoleci/JoltPhysicsSharp/blob/main/src/JoltPhysicsSharp/NarrowPhaseQuery.cs)是当时主线，当前精确2.22.0源码与查询矩阵边界见 [physics-and-gameplay.md](physics-and-gameplay.md#固定jolt版本与矩阵边界)。沿墙滑动、坡台、接地、跳跃/去穿透规则自研，不改用CharacterVirtual冒称自研。Numerics/OpenTK/Jolt转换已在边界落实。

Jolt提供精确ray/sweep/overlap和刚体，角色去穿透/墙滑/接地/跳跃/坡台规则自研；当前包装层查询矩阵在PhysicsWorld.QueryTransform单次预转置。Body位置/四元数接口不同，转换不能散落或二次叠加。纯托管备选的可学习性与查询适配成本都保留为取舍，不同时接入两后端。

SharpGLTF仅读取格式，默认节点/采样/混合/蒙皮由项目求值；StbImageSharp仅解码，颜色空间、GPU上传/释放仍由引擎负责。ImGui.NET提供原生控件，不能替代编辑事务/Undo。OpenTK音频绑定＋OpenAL Soft播放PCM，不再引入运行时Ogg解码库；用已选声音离线转换缩小运行依赖。

<a id="41-固定-sdk版本选择"></a>
### 4.1 固定SDK：含义与生效范围

SDK是编译/构建/还原工具，net10.0是API目标，运行时执行已编译程序，IDE用于编辑/调试，NuGet包提供库。global.json只选择SDK，不下载它、不锁运行时补丁/VS/包，也不改变其他目录项目默认值。

| 字段 | 值 | 效果/取舍 |
| --- | --- | --- |
| sdk.version | 10.0.401 | 选择完整验证版本 |
| sdk.rollForward | disable | 缺准确版本报错，即使更高版本已安装也不自动采用 |
| sdk.allowPrerelease | false | 不选择预览SDK |

VS从解决方案目录、dotnet从当前目录向上解析global.json，所以命令从g104engine或其子目录执行；仅在上层传方案路径不保证采用此文件。它放在slnx旁，不需C#加载、项目资源项或复制到输出。将来升级需先确认目标SDK并同步配置/环境记录，不能浮动latest。依据：[global.json](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)。

确认选择时结合dotnet --version、dotnet --info里的实际global.json路径及MSBuild NETCoreSdkVersion/MSBuildSDKsPath，而非只看机器恰好默认同版。2026-10-02创建及验证原文属于 [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#foundation-20261002)。

<a id="42-nuget-依赖锁文件第-41-节核验后继续"></a>
<a id="package-locks"></a>
### 4.2 锁文件、还原与严格模式

| 文件 | 用途 | 谁读取 |
| --- | --- | --- |
| `global.json` | 选择 SDK 构建工具版本，如 `10.0.401` | .NET/VS 的 SDK 解析器 |
| `.csproj` 的 `PackageReference` | 声明项目直接依赖的包及版本要求，如 OpenTK.Graphics `4.9.4` | NuGet 还原过程 |
| `packages.lock.json` | 保存还原得到的完整包依赖结果，包括实际版本、直接/间接关系及包内容哈希 | 后续 NuGet 还原与锁定校验 |

最初虽只手动安装三个OpenTK组件，它们还引入OpenTK.Core、Windowing.Common、Windowing.GraphicsLibraryFramework `4.9.4` 和OpenTK.redist.glfw `3.4.0.44` 等“传递依赖”。当前九个直接包的完整图仍由两项目锁文件记录，最初三包的教学例子不是当前完整依赖清单。

项目文件记录“我要求什么”，锁文件记录“最终解析到了什么”。给直接引用写了版本号，不代表已经把完整传递依赖结果作为可审查的文件保存下来；这也不意味着 NuGet 平时会随意升级所有包。保存锁文件是为了在换设备和 CI 中复用、校验已确认的结果，并能在 Git 中看清依赖变化。

锁文件是清单，不包含 DLL，也不是游戏启动时读取的配置。它不会让引擎功能变多，不会自动修复代码或 SAC 拦截；版本锁定也不等于已完成安全审计。

#### 学习说明：“还原”是什么意思

“还原 NuGet 程序包”是根据项目依赖要求准备构建所需的包：有本地缓存时可以复用，缺少时从配置的包源获取，并产生供构建使用的 `obj/project.assets.json`。启用锁文件后，还原过程会同时生成或使用 `packages.lock.json`。它不是把源码恢复到旧版本，也不需要先卸载已经安装的三个包。


Sandbox虽无直接OpenTK引用，也经Engine解析运行所需图；运行应用整体结果看Sandbox锁文件。Engine锁文件方便本项目单独核对，不能强制未来所有类库消费者使用相同图。以后独立类库发布再评估策略，不手工复制两个锁文件。

#### 学习说明：两个 true 为什么不冲突

它们不是互相覆盖的“更新方向”，而是功能开关与约束条件：

| 属性 | 正确含义 | 不代表什么 |
| --- | --- | --- |
| `RestorePackagesWithLockFile=true` | 启用锁文件机制；在非严格模式下可生成或随依赖要求变化更新清单 | 不是每次都强制覆盖已有锁文件，也不是扫描本机 DLL 后反向填写依赖 |
| `RestoreLockedMode=true` | 还原必须遵守已确认的清单；项目依赖声明与清单不一致时报错 | 不是根据本机已安装的库更新锁文件，也不会自动把 `.csproj` 改成锁文件中的要求 |

**同时启用表示：使用锁文件，并禁止还原过程擅自改写依赖清单。** 先核对 `.csproj` 的要求与锁文件是否一致；一致时按清单准备依赖，缓存缺少的包可以下载，已经齐全时无需重复下载；不一致时失败，不靠自动修改项目声明或锁文件来“调和”。实际获取包与允许改写清单是两件事。

例如，当前项目声明 OpenTK.Graphics `4.9.4`，锁文件也记录对应请求及解析版本，还原成功。若以后主动把声明改成另一版本但未更新清单，严格模式会拒绝还原；它既不会把声明改回旧值，也不会自动把锁文件改成新值。这个错误是在提醒我们按升级流程审查变化。

没有启用严格模式时，依赖声明没变也会复用现有清单；依赖声明变了才可能重新解析并更新。因此两个属性一起启用不会导致“先更新锁文件，再反向更新项目”的循环。2026-10-02历史截图中的“所有程序包都已安装，没有要还原的内容”属于正常的无需重复工作的结果；它本身不展示 Debug/Release 编译结果，也不是故意制造不一致的反向测试。

还原成功、锁文件一致、构建成功、原生加载、行为正确与用户体验分别成立。obj/project.assets.json供构建消费、bin/obj是缓存；packages.lock.json是需审查和提交的文本，不含DLL，不是游戏运行配置，不证明依赖安全审计。dotnet build --no-restore禁止该次还原，不等于测试锁定还原。首次A/B步骤的原文与当时证据可选查基础历史。

<a id="dependency-upgrade"></a>
### 4.3 已明确决定升级后的受控操作

`packages.lock.json` 是NuGet根据项目声明计算出的结果，**不手工修改**。此节仅供未来已明确决定升级时使用，是否启动升级按具体任务和协作流程决定。用户决定新的直接依赖版本并修改项目，NuGet重新计算直接/传递依赖和内容哈希；以下以OpenTK组件为例，现有四个OpenTK直接引用的兼容关系应一起核对：

1. 开始前确认当前工作区中没有混入其他未处理的项目或锁文件修改，并记录准备升级的包、原版本和目标版本。一次尽量处理一组相关包；目前Graphics、Windowing.Desktop、Mathematics、Audio.OpenAL四个直接组件均为4.9.4，目标版本是否继续统一需按对应版本兼容关系核对。
2. 在 **Engine 和 Sandbox 两个项目**中暂时将严格模式改为：

```xml
<RestorePackagesWithLockFile>true</RestorePackagesWithLockFile>
<RestoreLockedMode>false</RestoreLockedMode>
```

保留第一个属性，让 NuGet 继续使用并更新锁文件；暂时关闭第二个属性，允许这次经过计划的依赖变化写入新清单。不要删除两个 `packages.lock.json`。
3. 在 VS 中右键 **G104.Engine → 管理 NuGet 程序包 → 更新**，选择明确的目标版本并核对安装预览。不要直接在锁文件里替换版本或哈希。Sandbox 没有直接的 OpenTK 包引用，不在 Sandbox 中重复安装；它会通过 Engine 的项目引用获得新的依赖图。
4. 右键解决方案 →“还原 NuGet 程序包”。NuGet 会根据更新后的 `.csproj` 重新计算依赖图，并自动更新 Engine 和 Sandbox 各自的 `packages.lock.json`。如果只更新了一个锁文件、出现 `NU****` 错误或还原失败，保留输出并交给助手检查，不通过手工复制另一个锁文件解决。
5. 审查这次变化：直接包版本是否是目标值、传递依赖为何变化、是否出现意外新增/删除包、内容哈希是否由NuGet正常生成。现有Git仓库使用差异视图同时检查 `.csproj` 和两个锁文件，不为了升级重新建仓。
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

## 素材来源、许可与离线转换

<a id="assets-and-conversion"></a>

| 用途 | 当前采用/部署 | 已知输入与验证边界 |
| --- | --- | --- |
| 角色与动作 | assets/models/UAL1_Standard.glb，Quaternius Standard非_RM、CC0；许可/README已保存 | 1skin/65joints/43LINEAR clips/四正权重；128容量UBO完整处理65骨骼，保留根/实例校正；Run默认Jog_Fwd_Loop，配置可换Sprint，实际采样/混合已有证据 |
| 场景与材质 | 项目自有primitive/28对象seed，material-probe.glb及checker.png/flat normal.png | 实际引用/UV/覆盖拒绝已有证据；flat normal探针不证明任意复杂法线图全部正确；未搬Piccolo素材 |
| 天空盒 | renderer创建项目自有固定cubemap背景 | V1仅背景，无IBL/预计算环境照明；外部六面图片更换不是已实施步骤 |
| 短音效 | 两包230个Ogg仅选七段，经便携FFmpeg9.0.2转mono/44100Hz/PCM16：step0、step1、door、jump、click、complete、switch | 原包编码/声道保留历史；正式七个WAV逐chunk/哈希/实际OpenAL有历史验证，人工听辨另取证；没有运行时Ogg库或系统安装 |
| 循环声/测试音 | 自有110Hz loop-test.wav，与七个转换文件共八个WAV | loop pause/resume/stop/cleanup已有证据；测试信号不当成最终环境声品质 |

当前PcmWave已限定RIFF/WAVE、整数PCM16、1/2声道并从文件读取采样率，逐chunk处理对齐，不假设44字节头；拒绝float/extensible/压缩。3D点声源需mono，2D提示不随场景方向/距离变化。OpenAL只播放PCM、不解码任意文件，原机制参考见 [OpenTK AL API](https://opentk.net/api/OpenTK.Audio.OpenAL.AL.html) 和 [openal-1.1-specification.pdf](https://www.openal.org/documentation/openal-1.1-specification.pdf)，其他格式明确报错。


采用素材的作者/来源/许可、选中条目、转换参数与逐项SHA由 [asset-manifest.json](../../assets/licenses/asset-manifest.json) 保存。原始四ZIP是忽略的本机来源缓存，正式采用子集在assets/third_party；不用包宣传页推定免费Standard包含所有动作、PCM编码或相容skin，不扩大 [git-and-publishing.md](git-and-publishing.md)。

Quaternius Standard来源：[作者页](https://quaternius.itch.io/universal-animation-library)，采用Unreal-Godot/UAL1_Standard.glb非_RM版本；[作者更新](https://quaternius.itch.io/universal-animation-library/devlog/1555893/added-root-motion-and-other-fixes)区分RM。实际65关节/43LINEAR clips保根姿态，128矩阵UBO不截64，Run默认Jog；GLB无TANGENT/图片/纹理，用自有material-probe/checker/flat normal补贴图证据，未购买额外角色。flat normal不证明复杂法线图全部方向/滤波。

Kenney来源：[Impact Sounds](https://kenney.nl/assets/impact-sounds)、[Interface Sounds](https://kenney.nl/assets/interface-sounds)，均CC0；230个Ogg仅选七段，不整体发布。PcmWave要求RIFF/WAVE整数PCM16、1/2声道并读采样率，逐chunk对齐，不假设44字节头；float/extensible/压缩拒绝。3D需mono，2D提示不受场景方位距离。OpenAL API与 [openal-1.1-specification.pdf](https://www.openal.org/documentation/openal-1.1-specification.pdf)只支持已准备PCM播放，不自动解码任意格式。

### 便携工具的适用范围

按授权普通细节已采用Gyan release essentials便携Windows FFmpeg9.0.2，仅转换已选声音。原准备来源为 [FFmpeg官方Windows提供方](https://ffmpeg.org/download.html) → [Gyan发行构建](https://www.gyan.dev/ffmpeg/builds/)，实际文件ffmpeg-9.0.2-essentials_build.zip；历史核对见 [版本记录](https://www.gyan.dev/ffmpeg/builds/release-version) 与 [对应SHA-256](https://www.gyan.dev/ffmpeg/builds/packages/ffmpeg-9.0.2-essentials_build.zip.sha256)。版本记录是浮动网页，本轮没有联网重新核其最新值，当前采用版本由已校验文件/台账固定。

已获取ZIP的SHA-256与预期`60f467265b1e312373dbcd92200c2618a74850f98d3d078e94296bb3fa2047ba`一致，正式授权后已运行转换七段声音；工具/日志保留忽略缓存。未系统安装/改PATH，不属于引擎运行依赖或Git发布包；将来如需重获仍固定版本/哈希，不能浮动latest升级。

实际七段均输出mono、44100Hz、pcm_s16le，原条目/绝对转换命令与输出哈希已入manifest。生成文件、WAV解析、OpenAL运行和用户听感分别有证据；文档改造不重转，历史听感反馈不等于全部声学/设备组合。

工具只负责已选声音离线转换，未系统安装/改PATH，不是应用运行或Git发布依赖。tools/prepare_assets.py负责已核输入提取、程序测试资产及离线转换；重获原始输入、重转或升级工具需要具体任务，阅读本页不触发执行。解析、输出哈希、原生播放与人工听感分别取证。

## 可选版本资料与准备历史

- 2026-10-02：SDK/三OpenTK包/锁文件A-B、窗口与smoke、Git基础原文，见 [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#foundation-20261002)。
- 2026-10-03：六包准备顺序、13包文件/PE/哈希核对、四ZIP与正式部署演变，见 [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#v1-preparation-20261003)；原ZIP静态核对见 [v1-input-archives-check-2026-10-03.md](../reviews/v1-input-archives-check-2026-10-03.md)。
- [SharpGLTF1.0.7](https://www.nuget.org/packages/SharpGLTF.Core/1.0.7)、[StbImageSharp2.30.16](https://www.nuget.org/packages/StbImageSharp/2.30.16)、[Jolt2.22.0](https://www.nuget.org/packages/JoltPhysicsSharp/2.22.0)、[Native1.1.0](https://www.nuget.org/packages/JoltPhysics.Native/1.1.0)、[ImGui1.91.6.1](https://www.nuget.org/packages/ImGui.NET/1.91.6.1)、[OpenTK OpenAL4.9.4](https://www.nuget.org/packages/OpenTK.Audio.OpenAL/4.9.4)为版本依据；[BACKENDS.md](https://github.com/ocornut/imgui/blob/v1.91.6/docs/BACKENDS.md)说明自研适配职责。
- [OpenAL Soft1.25.2](https://github.com/kcat/openal-soft/releases/tag/1.25.2)、[官方Windows包](https://openal-soft.org/openal-binaries/openal-soft-1.25.2-bin.zip)、[维护者DLL命名说明](https://openal.org/pipermail/openal/2016-September/000532.html)、[OpenTK AL API](https://opentk.net/api/OpenTK.Audio.OpenAL.AL.html)补充运行实现依据。
- [NuGet锁模式](https://learn.microsoft.com/en-us/nuget/consume-packages/package-references-in-project-files#locking-dependencies)、[VS还原安装](https://learn.microsoft.com/en-us/nuget/consume-packages/install-use-packages-visual-studio)是操作机制说明。版本资料的历史核查不等于本轮重新联网、还原或运行。
