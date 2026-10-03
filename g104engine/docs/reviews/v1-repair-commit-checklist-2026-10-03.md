# V1评审与角色转向修复：提交清单

日期：2026-10-03。用户要求把当前修复保存到GitHub；助手已只读核对范围与既有实际验证，没有执行Git暂存、提交或推送。用户仍使用Git Bash/Git GUI操作。

**实际结果已核实：** 用户提交推送`d2e8d40691dd76a4637e35ed2905a2ccfaefe3d2`，`fix:修复 V1 评审问题及角色转向退出`，2026-10-03 23:00:25 +08:00；实际38文件与下列清单一致，HEAD/main/origin/main及实时GitHub main一致、核对开始工作区干净。下方“准备/尚未提交”语句保留准备当时状态，不覆盖本结果；随后本轮只更新同步/新聊天交接文档，未再次提交，不重跑未变化代码。

## 本次保存节点与范围

基线为`b91dfe4137686d9a5f962e40e10a3afe939cfe4c`，本地main/origin/main及本次实时GitHub main一致；暂存区为空。共38路径：修改30、新增8，包含本清单及publishing当前节点纠正。旧[首次V1清单](v1-commit-checklist-2026-10-03.md)的108文件是已保存历史，不是本次数量。

| 类别 | 文件数 |
| --- | ---: |
| 入口与协作规则 | 2 |
| GLSL | 1 |
| 架构、学习、进度、评审及发布文档 | 13 |
| Sandbox源码与验证 | 10 |
| Engine源码与验证 | 12 |

本次包括独立评审的编辑/导航/渲染/资产合同修复，以及近180°转身SceneValidationException修复、38例CPU与800帧窗口回归、注释/学习文档及中断恢复规则。没有删除文件、资产新增、SDK/NuGet/锁文件/csproj/slnx变更；原笔记和发布资料范围保持。

当前未跟踪文件都是源码/评审文档，要一并提交；bin/obj/.cache（FFmpeg/ZIP/日志/截图/本地快照）、私有资料与Piccolo已忽略，没有这些路径已被跟踪。核对实际范围后可全选当前Git GUI变化，或按下方仓库入口/g104engine路径暂存；不要用-f强制加入缓存。原第三方DLL/许可/同版本源码配套已在b91dfe4，不用另行重复加入。

## 验证证据

- Debug/Release x64最新构建均0警告0错误，--verify完整行为和原生Jolt/动画检查通过；38/38路线及11项原生UI各通过。
- 两配置新增800帧九条真实窗口路线全部PASS；原240帧的Forward/Deferred、2jump/110moving、保存/Undo/PlayStop及失败清理均通过，无GL错误。
- 先前独立GPU十项与独立double参考通过；本批Source与最终133文件快照逐SHA匹配，只有新建本清单及publishing文字纠正，不重复功能测试。
- 根因/独立复核：[评审记录](v1-independent-review-2026-10-03.md)、[转身退出修复](v1-contact-exit-fix-2026-10-03.md)。用户实际VS重建后原路线复试及整体视听/手感验收尚未确认，不能以保存节点代替验收。
- Git diff --check、SDK/项目/锁文件/原笔记保护比较通过。暂存后用户再核对暂存清单，不能单靠.gitignore判断所有文件。

## 身份、提交说明与用户操作

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

完成后助手只读核对实际HEAD、工作区和GitHub远端；本文件目前是提交准备记录，不表示已提交/推送。若工作区又新增或变化，重新核对真实暂存范围，不以38当固定要求。

## 实际路径清单

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
