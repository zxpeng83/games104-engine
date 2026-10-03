# V1 GitHub保存提交清单

核查：2026-10-03。当前分支main，基线cbce581；暂存区为空。用户希望先把V1保存到GitHub，尚不表示用户体验已经验收。助手只核查/补对应源码材料和本清单，未执行暂存、提交、推送。

## 候选范围

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

## 本批特别核对

- .gitignore实际排除了bin/obj、.cache（FFmpeg/原始ZIP/日志/截图/快照）、私有操作截图和Piccolo；当前没有已跟踪的缓存/产物混入。
- 正式模型/声音/Shader、场景和动画JSON、许可及OpenAL32.dll需要提交，供恢复当前代码和资产；最大单文件UAL GLB约7.27MiB，OpenAL DLL约3.66MiB。
- OpenAL二进制发布源码配套此前缺失；现已补官方1.25.2源码包（约1.08MiB）与SOURCE.md，未解压执行/编译/安装。许可、来源、版本、SHA及说明在同目录，不把这些配套源码误排除为缓存。
- SDK、slnx、两NuGet锁文件与原笔记没有修改；.gitignore/.gitattributes本批也未修改。
- Debug/Release0警告0错误、CPU/原生/图形/音频记录见 [实施复查](v1-implementation-review-2026-10-03.md)；新增源码归档/说明不改变运行代码，本批不重复功能测试。

## 用户操作

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

## 实际文件清单

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
