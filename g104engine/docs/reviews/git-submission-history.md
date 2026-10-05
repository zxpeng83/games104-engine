# Git 提交与同步历史

合并日期：2026-10-04。本页按批次保存三份原提交清单的准备核查、用户操作约定、实际保存结果和后续反馈，原文件路径作为历史来源保留；后续批次另作带日期的补记。每节中的“当前”“本轮”“尚未提交”按该节原核查或补记日期理解；后来的保存/人工反馈不改成更早已通过。

现行Git操作与白名单查 [git-and-publishing.md](../guides/git-and-publishing.md#daily-git)，执行政策以 [agent-workflow.md](../agent-workflow.md) 为准。最新授权、差异与同步节点仅在 [status.md](../status.md) 维护；本页的旧文件数、宽目录暂存命令和推送意图不构成新批次授权。

<a id="batch-index"></a>
## 按批次查阅

| 批次 | 当时准备候选 | 用户要求／确认记录 | 实际保存 |
|---|---|---|---|
| [首次完整V1](#v1-initial)，2026-10-03 | cbce581后108文件，23修改＋85新增；推荐说明含“待用户验收” | 原记录目标为GitHub保存，暂存核对后由用户执行；未单列确认文本／时间 | b91dfe4，19:41:08 +08；实际说明为“待验收”，108文件 |
| [评审／转向修复](#v1-repair)，2026-10-03 | b91dfe4后38文件，30修改＋8新增；人工路线仍待复试 | 用户要求把当前修复保存到GitHub；命令要求暂存范围核对、确认后执行 | d2e8d40，23:00:25 +08；38文件与候选清单一致 |
| [UI／首轮验收](#ui-acceptance)，2026-10-04 | 4be648e后16文件，13修改＋3新增；用户初步体验已确认 | 用户明确要求本地提交后推送；原记录要求展示范围／身份／验证并获该批确认 | 38cb85f，17:06:43 +08；16文件与候选清单一致 |
| [文档重构及后续维护](#document-submission-20261005)，2026-10-05 | 38cb85f后56路径，33修改＋14删除＋9新增；全部为Markdown | 用户表达提交Git意图；具体清单和推送选择待确认，Git写入仍由用户执行 | 尚未暂存、提交或推送 |

完整V1实施授权、Git保存意图、暂存清单确认和实际commit/push是不同事实。这里保留原记录能支持的用户要求及确认流程，不补造原文未保存的确认框时间或逐项答复。下文实际结果是用户完成操作后的核查事实，不能用候选清单或计划命令替代。

2026-10-04上一轮文档审计曾实际只读查询GitHub main并与38cb85f核对一致。**本次文档重构只核对本地HEAD/main/origin/main，未重新查询实时远端**；原批次的实时查询仍是其历史证据。本地快照／日志不随Git同步。六份交接文档的4be648e是两次源码批之间的独立文档节点，不混入38或16文件清单。

<a id="v1-initial"></a>
## 首次完整V1：准备、实际保存与后续验收

原来源：`reviews/v1-commit-checklist-2026-10-03.md`。准备日期2026-10-03，实际结果于2026-10-04补记。

核查：2026-10-03。以下保存该轮提交前的核查、候选清单与用户操作命令；其中“当前分支/未提交/待验收”均指准备当时。助手只核查/补对应源码材料和本清单，未执行暂存、提交、推送。

**实际保存结果（2026-10-04补记）：** 用户已提交推送`b91dfe4137686d9a5f962e40e10a3afe939cfe4c`，提交说明`feat: 实现基础综合训练场 V1（待验收）`，2026-10-03 19:41:08 +08:00；本轮只读复核提交记录，实际108文件。本批以cbce581为准备基线，后续评审/转向与UI批已另行保存；用户2026-10-04确认指南第1–5项首轮人工验收初步通过。最新同步与学习接续见 [status.md](../status.md)。旧108文件和下方命令仅追溯该批，不用于后续提交范围。

### 提交前候选范围（历史）

本次共108文件：修改23、新增85；下方是核查时实际清单，不要求未来批次文件数固定。根README/AGENTS与g104engine范围内的一切当前未忽略变化均属V1，未见私有资料、构建产物或下载缓存混入。

| 类别 | 文件数 |
| --- | --- |
| 入口与协作规则 | 2 |
| 运行资产、Shader、配置与许可 | 32 |
| 计划、架构、学习、验收、交接文档 | 26 |
| Sandbox源码与项目配置 | 8 |
| Engine源码与项目配置 | 33 |
| OpenAL DLL、许可及对应源码 | 6 |
| 资产准备工具 | 1 |

### 本批特别核对

- .gitignore实际排除了bin/obj、.cache（FFmpeg/原始ZIP/日志/截图/快照）、私有操作截图和Piccolo；当前没有已跟踪的缓存/产物混入。
- 正式模型/声音/Shader、场景和动画JSON、许可及OpenAL32.dll需要提交，供恢复当前代码和资产；最大单文件UAL GLB约7.27MiB，OpenAL DLL约3.66MiB。
- OpenAL二进制发布源码配套此前缺失；现已补官方1.25.2源码包（约1.08MiB）与SOURCE.md，未解压执行/编译/安装。许可、来源、版本、SHA及说明在同目录，不把这些配套源码误排除为缓存。
- SDK、slnx、两NuGet锁文件与原笔记没有修改；.gitignore/.gitattributes本批也未修改。
- Debug/Release0警告0错误、CPU/原生/图形/音频记录见 [v1-implementation-review-2026-10-03.md](v1-implementation-review-2026-10-03.md)；新增源码归档/说明不改变运行代码，本批不重复功能测试。

<a id="v1-initial-commands"></a>
### 当时用户操作与建议命令（历史）

Git身份：zxpeng83 / 2118168362@qq.com；目标origin/main，远程https://github.com/zxpeng83/games104-engine.git。推荐提交说明：`feat: 实现基础综合训练场 V1（待用户验收）`。用户本次目标为GitHub保存，因此本地暂存后还需commit和push；仅stage不会上传到GitHub。

在Git Bash打开仓库根后可使用：

```bash
cd /e/game_study/games104
git add -- README.md AGENTS.md g104engine
git diff --cached --stat
git diff --cached --name-only
git diff --cached --check
```

检查暂存列表与此清单一致，再由用户执行：

```bash
git commit -m "feat: 实现基础综合训练场 V1（待用户验收）"
git push origin main
```

Git GUI也可全选当前Unstaged Changes进行Stage，检查Staged Changes后提交/推送。不用-f强制加入被忽略的文件；.gitignore不负责排除已跟踪文件，也不判断g104engine内每个新文件是否属于发布范围，所以每次仍检查清单。

<a id="v1-initial-files"></a>
### 实际文件清单

```text
AGENTS.md
README.md
g104engine/assets/audio/click.wav
g104engine/assets/audio/complete.wav
g104engine/assets/audio/door.wav
g104engine/assets/audio/jump.wav
g104engine/assets/audio/loop-test.wav
g104engine/assets/audio/step0.wav
g104engine/assets/audio/step1.wav
g104engine/assets/audio/switch.wav
g104engine/assets/config/character-animation.json
g104engine/assets/licenses/asset-manifest.json
g104engine/assets/licenses/kenney_impact-sounds-License.txt
g104engine/assets/licenses/kenney_interface-sounds-License.txt
g104engine/assets/licenses/quaternius-0-License.txt
g104engine/assets/licenses/quaternius-1-README.txt
g104engine/assets/models/UAL1_Standard.glb
g104engine/assets/models/material-probe.glb
g104engine/assets/scenes/training-ground.json
g104engine/assets/shaders/deferred.frag
g104engine/assets/shaders/forward.frag
g104engine/assets/shaders/fullscreen.vert
g104engine/assets/shaders/fxaa.frag
g104engine/assets/shaders/gbuffer.frag
g104engine/assets/shaders/lighting.glsl
g104engine/assets/shaders/material.glsl
g104engine/assets/shaders/mesh.vert
g104engine/assets/shaders/particle.frag
g104engine/assets/shaders/particle.vert
g104engine/assets/shaders/shadow.frag
g104engine/assets/shaders/sky.frag
g104engine/assets/shaders/tonemap.frag
g104engine/assets/tests/checker.png
g104engine/assets/tests/normal.png
g104engine/docs/architecture.md
g104engine/docs/execution/v1-progress.md
g104engine/docs/guides/physics-and-gameplay.md
g104engine/docs/guides/rendering-and-animation.md
g104engine/docs/guides/scene-and-editor.md
g104engine/docs/guides/v1-architecture-and-learning.md
g104engine/docs/guides/v1-run-and-review.md
g104engine/docs/handoff.md
g104engine/docs/learning-map.md
g104engine/docs/plan.md
g104engine/docs/plans/animation-roadmap.md
g104engine/docs/plans/assets-scene-roadmap.md
g104engine/docs/plans/basic-training-ground-v1-draft.md
g104engine/docs/plans/core-architecture-roadmap.md
g104engine/docs/plans/gameplay-ai-roadmap.md
g104engine/docs/plans/particles-audio-roadmap.md
g104engine/docs/plans/physics-character-roadmap.md
g104engine/docs/plans/rendering-roadmap.md
g104engine/docs/plans/tools-debug-roadmap.md
g104engine/docs/plans/v1-dependencies-and-assets.md
g104engine/docs/plans/v1-implementation-draft.md
g104engine/docs/publishing.md
g104engine/docs/reviews/v1-commit-checklist-2026-10-03.md
g104engine/docs/reviews/v1-implementation-review-2026-10-03.md
g104engine/docs/reviews/v1-start-checkpoint-2026-10-03.md
g104engine/docs/status.md
g104engine/samples/G104.Sandbox/G104.Sandbox.csproj
g104engine/samples/G104.Sandbox/Gameplay/GameplayVerification.cs
g104engine/samples/G104.Sandbox/Gameplay/TrainingSimulation.cs
g104engine/samples/G104.Sandbox/LaunchOptions.cs
g104engine/samples/G104.Sandbox/Program.cs
g104engine/samples/G104.Sandbox/Tools/FramebufferCapture.cs
g104engine/samples/G104.Sandbox/Tools/SceneEditorPanel.cs
g104engine/samples/G104.Sandbox/TrainingWindow.cs
g104engine/src/G104.Engine/Animation/AnimationController.cs
g104engine/src/G104.Engine/Animation/AnimationSettings.cs
g104engine/src/G104.Engine/Animation/AnimationVerification.cs
g104engine/src/G104.Engine/Assets/AssetRoot.cs
g104engine/src/G104.Engine/Assets/GltfModel.cs
g104engine/src/G104.Engine/Assets/GltfModelLoader.cs
g104engine/src/G104.Engine/Audio/AudioSystem.cs
g104engine/src/G104.Engine/Audio/PcmWave.cs
g104engine/src/G104.Engine/Core/CoreSelfChecks.cs
g104engine/src/G104.Engine/Core/FixedStepClock.cs
g104engine/src/G104.Engine/Core/InputBuffer.cs
g104engine/src/G104.Engine/Editor/EditorHistory.cs
g104engine/src/G104.Engine/Editor/SceneEditor.cs
g104engine/src/G104.Engine/Effects/ParticleSystem.cs
g104engine/src/G104.Engine/G104.Engine.csproj
g104engine/src/G104.Engine/Navigation/NavigationGrid.cs
g104engine/src/G104.Engine/Navigation/NavigationVerification.cs
g104engine/src/G104.Engine/Physics/KinematicCharacter.cs
g104engine/src/G104.Engine/Physics/PhysicsVerification.cs
g104engine/src/G104.Engine/Physics/PhysicsWorld.cs
g104engine/src/G104.Engine/Rendering/GpuResources.cs
g104engine/src/G104.Engine/Rendering/PrimitiveMeshes.cs
g104engine/src/G104.Engine/Rendering/RenderContracts.cs
g104engine/src/G104.Engine/Rendering/TrainingRenderer.cs
g104engine/src/G104.Engine/Scene/AssetPath.cs
g104engine/src/G104.Engine/Scene/SceneData.cs
g104engine/src/G104.Engine/Scene/SceneGraph.cs
g104engine/src/G104.Engine/Scene/SceneParameterRules.cs
g104engine/src/G104.Engine/Scene/SceneSerializer.cs
g104engine/src/G104.Engine/Scene/SceneValidator.cs
g104engine/src/G104.Engine/Scene/TemplateCatalog.cs
g104engine/src/G104.Engine/Scene/TransformMath.cs
g104engine/src/G104.Engine/Tools/ImGuiController.cs
g104engine/third_party/openal-soft/1.25.2/COPYING
g104engine/third_party/openal-soft/1.25.2/LICENSE-pffft
g104engine/third_party/openal-soft/1.25.2/OpenAL32.dll
g104engine/third_party/openal-soft/1.25.2/SOURCE.md
g104engine/third_party/openal-soft/1.25.2/openal-soft-1.25.2.tar.bz2
g104engine/third_party/openal-soft/1.25.2/readme.txt
g104engine/tools/prepare_assets.py
```

<a id="v1-repair"></a>
## 评审与角色转向修复：准备、保存与后续复试

原来源：`reviews/v1-repair-commit-checklist-2026-10-03.md`。准备／保存日期2026-10-03，后续反馈于2026-10-04补记。

日期：2026-10-03。用户要求把当前修复保存到GitHub；助手已只读核对范围与既有实际验证，没有执行Git暂存、提交或推送。用户仍使用Git Bash/Git GUI操作。

**该批实际结果已核实：** 用户提交推送`d2e8d40691dd76a4637e35ed2905a2ccfaefe3d2`，`fix:修复 V1 评审问题及角色转向退出`，2026-10-03 23:00:25 +08:00；实际38文件与下列清单一致，提交后核对时HEAD/main/origin/main及实时GitHub main一致、核对开始工作区干净。下方“准备/尚未提交”语句保留准备当时状态，不覆盖本结果；随后的六份交接文档保存为4be648e。

**最新接续（2026-10-04补记）：** 用户已确认转身/球区/上下坡及鼠标修复，运行指南第1–5项首轮人工验收初步通过，UI与验收批已保存为38cb85f，当前进入学习/调试。本页38路径是该历史批次，不覆盖UI批或本轮文档审计；最新状态及同步节点见 [status.md](../status.md)。

### 提交前核对的保存节点与范围（历史）

基线为`b91dfe4137686d9a5f962e40e10a3afe939cfe4c`，本地main/origin/main及本次实时GitHub main一致；暂存区为空。共38路径：修改30、新增8，包含本清单及publishing当前节点纠正。旧[git-submission-history.md](git-submission-history.md#v1-initial)的108文件是已保存历史，不是本次数量。

| 类别 | 文件数 |
| --- | ---: |
| 入口与协作规则 | 2 |
| GLSL | 1 |
| 架构、学习、进度、评审及发布文档 | 13 |
| Sandbox源码与验证 | 10 |
| Engine源码与验证 | 12 |

本次包括独立评审的编辑/导航/渲染/资产合同修复，以及近180°转身SceneValidationException修复、38例CPU与800帧窗口回归、注释/学习文档及中断恢复规则。没有删除文件、资产新增、SDK/NuGet/锁文件/csproj/slnx变更；原笔记和发布资料范围保持。

当前未跟踪文件都是源码/评审文档，要一并提交；bin/obj/.cache（FFmpeg/ZIP/日志/截图/本地快照）、私有资料与Piccolo已忽略，没有这些路径已被跟踪。核对实际范围后可全选当前Git GUI变化，或按下方仓库入口/g104engine路径暂存；不要用-f强制加入缓存。原第三方DLL/许可/同版本源码配套已在b91dfe4，不用另行重复加入。

### 验证证据

- Debug/Release x64最新构建均0警告0错误，--verify完整行为和原生Jolt/动画检查通过；38/38路线及11项原生UI各通过。
- 两配置新增800帧九条真实窗口路线全部PASS；原240帧的Forward/Deferred、2jump/110moving、保存/Undo/PlayStop及失败清理均通过，无GL错误。
- 先前独立GPU十项与独立double参考通过；本批Source与最终133文件快照逐SHA匹配，只有新建本清单及publishing文字纠正，不重复功能测试。
- 根因/独立复核：[v1-independent-review-2026-10-03.md](v1-independent-review-2026-10-03.md)、[v1-contact-exit-fix-2026-10-03.md](v1-contact-exit-fix-2026-10-03.md)。用户实际VS重建后原路线复试及整体视听/手感验收尚未确认，不能以保存节点代替验收。
- Git diff --check、SDK/项目/锁文件/原笔记保护比较通过。暂存后用户再核对暂存清单，不能单靠.gitignore判断所有文件。

<a id="v1-repair-commands"></a>
### 当时身份、提交说明与用户操作（历史）

Git身份：`zxpeng83 <2118168362@qq.com>`；仓库：[zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)，目标`origin/main`，HTTPS远程`https://github.com/zxpeng83/games104-engine.git`。本次目标是保存到GitHub，所以本地commit后还需push。

推荐提交说明：

```text
fix: 修复 V1 评审问题及角色转向退出
```

先在Git Bash检查暂存：

```bash
cd /e/game_study/games104
git add -- README.md AGENTS.md g104engine
git diff --cached --stat
git diff --cached --name-only
git diff --cached --check
```

暂存应与下面38路径一致，确认后由用户执行：

```bash
git commit -m "fix: 修复 V1 评审问题及角色转向退出"
git push origin main
```

当时安排由助手在操作完成后只读核对实际HEAD、工作区和GitHub远端；该批结果已记于本节开头。以下命令仅保留准备流程，后续新增或变化需重新核对真实暂存范围，不以38当固定要求。

<a id="v1-repair-files"></a>
### 实际路径清单

```text
AGENTS.md
README.md
g104engine/assets/shaders/lighting.glsl
g104engine/docs/architecture.md
g104engine/docs/execution/v1-progress.md
g104engine/docs/guides/physics-and-gameplay.md
g104engine/docs/guides/rendering-and-animation.md
g104engine/docs/guides/scene-and-editor.md
g104engine/docs/guides/v1-run-and-review.md
g104engine/docs/handoff.md
g104engine/docs/plans/v1-implementation-draft.md
g104engine/docs/publishing.md
g104engine/docs/reviews/v1-contact-exit-fix-2026-10-03.md
g104engine/docs/reviews/v1-independent-review-2026-10-03.md
g104engine/docs/reviews/v1-repair-commit-checklist-2026-10-03.md
g104engine/docs/status.md
g104engine/samples/G104.Sandbox/Gameplay/ContactApproachVerification.cs
g104engine/samples/G104.Sandbox/Gameplay/GameplayVerification.cs
g104engine/samples/G104.Sandbox/Gameplay/TrainingSimulation.cs
g104engine/samples/G104.Sandbox/LaunchOptions.cs
g104engine/samples/G104.Sandbox/Program.cs
g104engine/samples/G104.Sandbox/Tools/ContactWindowExercise.cs
g104engine/samples/G104.Sandbox/Tools/EditorUiVerification.cs
g104engine/samples/G104.Sandbox/Tools/RenderingVerificationWindow.cs
g104engine/samples/G104.Sandbox/Tools/SceneEditorPanel.cs
g104engine/samples/G104.Sandbox/TrainingWindow.cs
g104engine/src/G104.Engine/Assets/GltfModel.cs
g104engine/src/G104.Engine/Assets/GltfModelLoader.cs
g104engine/src/G104.Engine/Core/CoreSelfChecks.cs
g104engine/src/G104.Engine/Editor/EditorHistory.cs
g104engine/src/G104.Engine/Navigation/NavigationGrid.cs
g104engine/src/G104.Engine/Navigation/NavigationVerification.cs
g104engine/src/G104.Engine/Rendering/GpuResources.cs
g104engine/src/G104.Engine/Rendering/RenderingVerification.cs
g104engine/src/G104.Engine/Rendering/TrainingRenderer.cs
g104engine/src/G104.Engine/Scene/SceneGraph.cs
g104engine/src/G104.Engine/Scene/SceneParameterRules.cs
g104engine/src/G104.Engine/Scene/TransformMath.cs
```

<a id="ui-acceptance"></a>
## UI鼠标修复与首轮验收：准备及实际同步

原来源：`reviews/v1-ui-acceptance-commit-checklist-2026-10-04.md`。核查／保存日期2026-10-04，其后文档审计另属未提交批次。

日期：2026-10-04。状态：该UI修复与首轮验收批次已由用户本地提交并推送，助手已只读核实；下方保留提交前候选清单及命令作为历史。实际Git操作由用户在Git Bash/Git GUI完成。

**实际提交与同步结果：** `38cb85f2be6d86804edd3331a54467389b3af604`，`fix: 修复 UI 鼠标交互并记录 V1 首轮验收通过`，2026-10-04 17:06:43 +08:00。实际16文件（修改13、新增3）与下方清单一致；本轮审计开始时工作区干净，HEAD/main/本地origin/main与本轮实时只读查询的GitHub main一致。这证明该批已保存同步，不证明任何新增验收或部署发布。

**本轮后续文档审计：** 此页提交后补记及其他文档一致性修订属于新的本地文档批次，未随上述提交保存，需以本轮实际差异重新核对范围；旧16路径和命令不能强行涵盖新批次。当前状态与接续见 [status.md](../status.md)。

### 本批行为与验收

鼠标按下控件后被错误清焦，导致按钮/下拉/勾选在松开时无响应；现按WantCaptureMouse判断点击归属，并按窗口事件顺序传递鼠标输入，保留短点击、位置顺序和失焦释放。新增--verify-ui-input覆盖生产输入后端及共享清焦策略。

用户已确认鼠标点击修复，随后明确确认[v1-run-and-review.md](../guides/v1-run-and-review.md)第一轮验收第1–5项初步体验无问题，记为“V1首轮人工验收通过（初步、非穷尽）”，后续Bug继续反馈修复。当前进入源码学习/调试练习，具体后续版本排期未定，D5继续暂缓。

### 提交准备时的基线、身份与目标（历史）

- 准备当时本地HEAD/main/origin/main：`4be648ea2923c62ccd561dfc0fc3cc4c771f2265`。这是前次六份交接文档节点，d2e8d40为前次源码修复节点；该批实际提交为本节开头38cb85f。
- Git身份：`zxpeng83 <2118168362@qq.com>`。
- 目标：[zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)，本地`main`，普通推送`origin main`；不改历史或强制推送。
- 准备当时用户明确要求本地提交后推送，实时远端尚未重新核实；本地origin/main不等同实时查询结果。实际提交后本轮实时GitHub main已核实与38cb85f一致。

当时推荐、后来实际采用的提交说明：

```text
fix: 修复 UI 鼠标交互并记录 V1 首轮验收通过
```

<a id="ui-acceptance-files"></a>
### 提交前候选范围（已与实际16文件核对）

当时共16路径：修改13、新增3；5份源码和11份入口/说明/验收/发布文档，无删除。清单计入自身及publishing更新，已与实际提交路径核对一致；当时暂存仍须以真实index为准，不以数量替代路径核对。

```text
README.md
g104engine/docs/execution/v1-progress.md
g104engine/docs/guides/v1-architecture-and-learning.md
g104engine/docs/guides/v1-run-and-review.md
g104engine/docs/handoff.md
g104engine/docs/learning-map.md
g104engine/docs/plan.md
g104engine/docs/publishing.md
g104engine/docs/reviews/v1-ui-acceptance-commit-checklist-2026-10-04.md
g104engine/docs/reviews/v1-ui-mouse-fix-2026-10-04.md
g104engine/docs/status.md
g104engine/samples/G104.Sandbox/LaunchOptions.cs
g104engine/samples/G104.Sandbox/Program.cs
g104engine/samples/G104.Sandbox/Tools/UiInputVerificationWindow.cs
g104engine/samples/G104.Sandbox/TrainingWindow.cs
g104engine/src/G104.Engine/Tools/ImGuiController.cs
```

该批无SDK/NuGet/锁文件/csproj/slnx、Shader、资产或许可变化；原笔记、26处非发布引用、私有截图、Piccolo及上层Git保持。bin/obj/.cache和用户设计不纳入。新验证源码和两份评审/提交文档已纳入上述实际提交，原“不要漏掉未跟踪文件”要求保留为当时范围核对背景。

### 该批提交准备时已有的有效验证

- 5份源码SHA-256逐一匹配`20261004-161208-ui-mouse-fixed`修后快照；此后仅文档更新，未因提交准备重复构建。
- Debug/Release x64各0警告0错误；各13项窗口输入、11项旧UI和完整--verify通过；各240帧集成2jump/110moving，无GL错误。
- 用户鼠标点击复试及指南1–5首轮初步验收通过；Astra Ultra提交前只读复核未发现新阻塞P1/P2，补审了文字输入和第13项滚轮验证。未声称全部分支、DPI/设备组合或学习掌握已通过。
- 根因、修前失败、日志名及验证限制见[v1-ui-mouse-fix-2026-10-04.md](v1-ui-mouse-fix-2026-10-04.md)。日志/快照仍是本机缓存，不随提交上传。
- 当时工作区diff --check通过，暂存区开始为空；准备流程要求暂存后再核对cached路径及cached diff --check。提交后实际范围与同步核对见本节开头，不以准备要求替代已执行检查的证据。

<a id="ui-acceptance-commands"></a>
### 当时用户操作顺序与命令（历史）

以下是该批已完成的保存流程，保留供追溯，不能直接用于本轮后续文档审计。准备当时先在Git Bash暂存与检查，对应上述16路径；出现其他改动须重新核对，Git GUI也可按该清单选择相同文件。

```bash
cd /e/game_study/games104
git add -- README.md g104engine
git diff --cached --stat
git diff --cached --name-only
git diff --cached --check
```

按当时AGENTS的Git规则（现行政策见 [agent-workflow.md](../agent-workflow.md)），展示实际暂存范围、验证、身份、目标和推送意图并取得该批明确确认后，再由用户执行：

```bash
git commit -m "fix: 修复 UI 鼠标交互并记录 V1 首轮验收通过"
git push origin main
```

准备当时要求完成后由助手只读核对实际HEAD、工作区、提交文件范围和实时远端；该批核对结果已记于本节开头。本清单或推送意图本身均不是提交/同步证据；后续批次继续按既有Git规则独立核对，助手不执行Git写操作。

<a id="document-submission-20261005"></a>
## 2026-10-05：文档重构及后续维护提交准备

准备核查开始：2026-10-05 16:43:34 +08:00。用户本轮要求“我要提交git”。以下是本批准备事实，具体范围与是否推送尚待明确确认；不是已经提交或同步的结果。

### 本批范围和保存基线

- 仓库根为E:\game_study\games104，分支main，远程origin为https://github.com/zxpeng83/games104-engine.git，上游origin/main。
- 本地HEAD、main、origin/main均为38cb85f2be6d86804edd3331a54467389b3af604；本地提交差异为0/0，暂存区为空。本轮未查询实时远端，不用本地缓存证明服务器一致。
- 实际56路径：33修改、14删除、9未跟踪，全部为Markdown。9份新增包括8份重构文档和此前未跟踪的一致性复查记录；14份删除是已经完成信息合并的旧文件。
- 范围包含入口/规则、文档一致性修订、49→43重构、链接显示名统一、三份易读性改写、OpenAL部署解释及此前SOURCE.md的历史同步措辞纠正。
- SOURCE.md只把“准备保存”纠正为对应DLL、许可和源码已随b91dfe4保存；该节点的六份正式OpenAL文件已只读核对。没有库版本、二进制或许可变化。
- 当前差异未包含引擎源码、项目/SDK/锁文件、配置、资产、原课程资料、私有截图、Piccolo或上层Git；本机缓存不纳入本批。

准备前43份现存工程文档已逐字节快照，14份删除项的HEAD原文另存备查；本批实际路径清单为本机缓存中的`paths.txt`。位置：`g104engine/.cache/execution/git-docs-20261005-164334/`。该快照和清单不随Git同步，不能当作异地备份。

### 验证与评审边界

本批是文档保存，不运行引擎或重做人工体验。已有引擎验证及用户首轮体验结论仍按原日期和范围理解。

提交准备前现行43份文档的本地链接、章节锚点、原文件名显示和空白检查通过；README全部文档可达、最多两次导航。提交准备补记后的结构检查及实际结果在同一缓存另存，不覆盖原批次证据。

326条原信息去向记录和此前易读性逐段比较仍是既有证据。本轮Astra只读提交前复核确认14个删除项均有现存正文承接，并直接核对原提交清单及搭建/探针的历史代码块保留；这不宣称重做了所有文档的穷尽语义审计。

### 身份、拟用说明及待执行动作

当前Git身份为zxpeng83 <2118168362@qq.com>；助手只读核查，不重设身份或远程。

拟用提交说明：

```text
docs: 重构工程文档并完善 Agent 协作与学习入口

- 合并状态、决策、方案和提交历史，保留信息去向及开发证据，将正式文档从49份精简为43份
- 按任务组织学习和运行入口，明确协作政策、接续与未来扩展边界
- 统一跨文档链接原文件名，改善状态与协作说明，并补充OpenAL部署理由
```

沿用 [agent-workflow.md](../agent-workflow.md#current-policy)：用户在Git Bash/Git GUI办理暂存、提交和推送，助手准备并核对。先向用户展示本批候选清单；用户暂存后按真实index核对路径、内容和空白，再按既有每批确认要求明确提交与是否推送。有清单外变化时先保留现场并重新核对。提交和推送分别记实际结果，不能用本节候选说明代替。

下一步：本批准备检查和有限评审结束后，展示实际范围、验证、身份、仓库/分支、拟用说明和推送选择，按既有每批确认要求办理；实际Git写入与远端同步仍未执行。
