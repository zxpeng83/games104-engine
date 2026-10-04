# V1 UI鼠标修复与首轮验收：提交清单

日期：2026-10-04。状态：提交准备，尚未暂存/提交/推送。用户已明确选择本地提交后推送GitHub main；实际Git操作仍由用户在Git Bash/Git GUI完成，助手只读核对。

## 本批行为与验收

鼠标按下控件后被错误清焦，导致按钮/下拉/勾选在松开时无响应；现按WantCaptureMouse判断点击归属，并按窗口事件顺序传递鼠标输入，保留短点击、位置顺序和失焦释放。新增--verify-ui-input覆盖生产输入后端及共享清焦策略。

用户已确认鼠标点击修复，随后明确确认[运行指南](../guides/v1-run-and-review.md)第一轮验收第1–5项初步体验无问题，记为“V1首轮人工验收通过（初步、非穷尽）”，后续Bug继续反馈修复。当前进入源码学习/调试练习，具体后续版本排期未定，D5继续暂缓。

## 基线、身份与目标

- 本地HEAD/main/origin/main：`4be648ea2923c62ccd561dfc0fc3cc4c771f2265`。这是前次六份交接文档节点，d2e8d40为前次源码修复节点。
- Git身份：`zxpeng83 <2118168362@qq.com>`。
- 目标：[zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)，本地`main`，普通推送`origin main`；不改历史或强制推送。
- 本次用户明确要求本地提交后推送。实时远端尚未重新核实，本地origin/main不等同实时查询结果。

推荐提交说明：

```text
fix: 修复 UI 鼠标交互并记录 V1 首轮验收通过
```

## 实际候选范围

共16路径：修改13、新增3；5份源码和11份入口/说明/验收/发布文档，无删除。此清单计入自身及publishing更新；暂存后必须以实际index为准，不以数量替代路径核对。

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

无SDK/NuGet/锁文件/csproj/slnx、Shader、资产或许可变化；原笔记、26处非发布引用、私有截图、Piccolo及上层Git保持。bin/obj/.cache和用户设计不纳入。新验证源码和两份评审/提交文档均需提交，不能漏掉未跟踪文件。

## 已有有效验证

- 5份源码SHA-256逐一匹配`20261004-161208-ui-mouse-fixed`修后快照；此后仅文档更新，未因提交准备重复构建。
- Debug/Release x64各0警告0错误；各13项窗口输入、11项旧UI和完整--verify通过；各240帧集成2jump/110moving，无GL错误。
- 用户鼠标点击复试及指南1–5首轮初步验收通过；Astra Ultra提交前只读复核未发现新阻塞P1/P2，补审了文字输入和第13项滚轮验证。未声称全部分支、DPI/设备组合或学习掌握已通过。
- 根因、修前失败、日志名及验证限制见[UI专项记录](v1-ui-mouse-fix-2026-10-04.md)。日志/快照仍是本机缓存，不随提交上传。
- 工作区diff --check通过，暂存区开始为空。暂存后还需核对cached路径及cached diff --check。

## 用户操作顺序

先在Git Bash暂存与检查；该范围对应当前16路径，若又出现其他改动，先重新核对。Git GUI也可以按清单选择相同文件。

```bash
cd /e/game_study/games104
git add -- README.md g104engine
git diff --cached --stat
git diff --cached --name-only
git diff --cached --check
```

按[AGENTS的Git规则](../../../AGENTS.md)，展示实际暂存范围、验证、身份、目标和推送意图并取得该批明确确认后，再由用户执行：

```bash
git commit -m "fix: 修复 UI 鼠标交互并记录 V1 首轮验收通过"
git push origin main
```

完成后助手只读核对实际HEAD、工作区、提交文件范围和实时远端；不能以本清单或用户已选择推送为已提交/已同步证据。
