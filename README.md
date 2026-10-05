# G104Engine：GAMES104 学习与引擎实践

以 C# 独立实现可解释、可扩展的3D引擎，用“基础综合训练场 V1（用于面试展示）”串联渲染、动画、物理、玩法、AI、粒子、声音和有限编辑工具。
当前采用 .NET 10、Windows x64、OpenGL 4.3 Core、OpenTK；引擎不依赖 Unity。具体依赖只在 [dependencies.md](g104engine/docs/guides/dependencies.md) 维护。
V1具备移动/跳跃、机关/NPC、Forward/Deferred、骨骼动画、声音/粒子和设计编辑；最新进展、验收边界及同步情况只查 [status.md](g104engine/docs/status.md#resume)。

<a id="start"></a>
## 按要做的事情进入

| 要做什么 | 直接进入的主页面 | 页面内能得到什么 |
|---|---|---|
| 打开训练场、操作、排错或验证 | [v1-run-and-review.md](g104engine/docs/guides/v1-run-and-review.md#run-and-verify) | 环境前提、操作、命令、隔离数据和结果边界 |
| 从启动、一帧、W/Space开始学源码 | [learning-map.md](g104engine/docs/learning-map.md#first-input-lesson) | 最少概念、调用链、断点、状态观察与自测 |
| 查看各模块和课程对应 | [learning-map.md](g104engine/docs/learning-map.md) | 全模块、原笔记、源码、固定参考和实践边界 |
| 了解架构、时序、矩阵与资源寿命 | [architecture.md](g104engine/docs/architecture.md#overview) | 结构图、数据写入者、时间/空间/生命周期约定 |
| 接续当前工作 | [status.md](g104engine/docs/status.md#resume) | 已做到哪里、当前任务、未决事项及有效证据 |
| 查看目标和后续版本方向 | [plan.md](g104engine/docs/plan.md#roadmap) | V1边界、全部长期模块及未定阶段 |
| 理解从0到1的决定和踩坑 | [development-history.md](g104engine/docs/development-history.md#timeline) | 阶段/日期→专题经验→原始证据 |
| 提交、推送或检查公开范围 | [git-and-publishing.md](g104engine/docs/guides/git-and-publishing.md#daily-git) | 命令环境、身份/清单核查、精确发布范围 |
| 调整AI分工、权限或后续自动化 | [agent-workflow.md](g104engine/docs/agent-workflow.md#current-policy) | 当前政策、任务模板、CI/Skills等启用条件 |

按本次任务选择一行即可，不需顺序读完整张表。来源、历史和更深算法是可选阅读；会影响正确操作的前提在任务主页面就近说明。
Agent进入一次 [AGENTS.md](AGENTS.md) 和当前状态后选择任务正文，不要求从专题返回本表重走入口。

## 打开现有工程

在VS打开 `g104engine/g104engine.slnx`，选择Sandbox及Debug/Release x64。不带参数启动进入Edit，Play/F5开始，Stop恢复当前内存设计。完整操作与检查直接看上表运行页面；无需重建工程或重做首次探针。

`G104.Engine` 提供通用能力，`G104.Sandbox` 负责窗口、训练场规则、工具面板与演示。Shader、种子场景和已部署素材在 `g104engine/assets/`，工程知识在 `g104engine/docs/`。
若问题属于场景/Undo，直接查 [scene-and-editor.md](g104engine/docs/guides/scene-and-editor.md)；角色/Jolt/NPC查 [physics-and-gameplay.md](g104engine/docs/guides/physics-and-gameplay.md)；glTF/蒙皮/两管线查 [rendering-and-animation.md](g104engine/docs/guides/rendering-and-animation.md)。

## 学习与资料

全模块都保留不同深度的工程实践目标。网络、动态GI和GPU几何为后续专题，当前玩法不含战斗；具体范围由目标与V1基线维护。
[environment.md](g104engine/docs/guides/environment.md)解释VS、x64与调试选项；[dependencies.md](g104engine/docs/guides/dependencies.md)解释SDK/运行时、锁文件和部署。历史示例仅供学习，不能覆盖现有Program。

公开仓库：[zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)。本地Git根是 `games104`；实际提交/同步状态见状态页，不以README推定已发布。
资料目录保持原样；十份精选笔记、必要图片和第三方来源的发布范围在Git与发布指南中。私有截图、Piccolo和缓存不随工程发布。
