# G104Engine：GAMES104 学习与引擎实践

C# / .NET 10 / Windows x64 / OpenGL 4.3 Core / OpenTK 4.9.4。
目标是结合 GAMES104 做可解释、可扩展的独立引擎，服务 Gameplay 客户端求职和长期学习。

2026-10-03基础综合训练场V1首轮完整实现及新增Astra Ultra独立评审修复已落地：渲染、角色/动画/物理、Gameplay/AI、粒子/声音及场景编辑工具。Debug/Release构建、本机行为/原生UI/GPU专项及240帧集成回归通过，用户体验待验收；详情见status和执行台账。
首个目标版本统一称为 **基础综合训练场 V1（用于面试展示）**，范围见 [V1 草案](g104engine/docs/plans/basic-training-ground-v1-draft.md)。原最小 3D 里程碑已合入 V1 的渲染/场景基础工作，不再独立交付或审批；后续扩展与专题另行分期。
正式V1已通过弹窗获准实施。素材/OpenAL已部署、声音离线PCM16转换完成；状态机/混合可配置，基础父子层级、Undo/Redo与PlayStop设计保护已接入。先读 [运行与验收](g104engine/docs/guides/v1-run-and-review.md)，再按 [实际架构与学习入口](g104engine/docs/guides/v1-architecture-and-learning.md) 阅读源码；方案与准备来源仍保留。
D0–D4 已完成；D5（独立克隆复现、CI、第二台设备）按用户要求全部暂缓、未验证。

## 从这里开始
- 新对话先读 [AGENTS.md](AGENTS.md) 和 [status.md](g104engine/docs/status.md)，按任务继续查阅。
- 新对话交接、决策索引与继续讨论入口：[handoff.md](g104engine/docs/handoff.md)。
- 长任务执行接续点与中断恢复：[V1执行台账](g104engine/docs/execution/v1-progress.md)；已获完整V1授权，持续保存实际进度与恢复步骤。
- 目标、基线、分工与全模块覆盖：[plan.md](g104engine/docs/plan.md)。
- 已确认架构与未决设计：[architecture.md](g104engine/docs/architecture.md)；学习对应：[learning-map.md](g104engine/docs/learning-map.md)。
- V1 内部基础工作见范围草案；追溯：[原 M1 草案归档](g104engine/docs/archive/m1-minimal-3d-draft-2026-10-03.md)；证据：[设计参考核查](g104engine/docs/reviews/design-reference-checks-2026-10-03.md)。
- 环境搭建与学习资料：[setup.md](g104engine/docs/setup.md)，按主题进入指南，不需要每轮全读。
- 本阶段 review：[复查记录](g104engine/docs/reviews/foundation-review-2026-10-02.md)。
- V1保存后新增的失败复现、修复与最终回归：[独立验收评审](g104engine/docs/reviews/v1-independent-review-2026-10-03.md)。
- 用户球/斜坡附近退出专项：[根因、修复与双配置回归](g104engine/docs/reviews/v1-contact-exit-fix-2026-10-03.md)。
- 发布范围：[publishing.md](g104engine/docs/publishing.md)。

## 已有运行入口
在 g104engine 目录打开 g104engine.slnx，使用 Debug/Release x64。
不带参数运行Sandbox进入训练场编辑预览，点击Play开始；--smoke保留托管环境信息，--verify进行行为自检，真实GL/音频分别验证，详见运行指南。
当前可用命令与验收见 [V1运行指南](g104engine/docs/guides/v1-run-and-review.md)；[探针指南](g104engine/docs/guides/probes.md)保留原基础阶段来源。

## 同步与资料
公开仓库：[zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)。
用户已自行提交推送 b91dfe4（完整V1，待用户验收），2026-10-03本轮起始只读核实本地与GitHub main一致；cbce581为方案/准备历史节点，见 [开工前记录](g104engine/docs/reviews/v1-start-checkpoint-2026-10-03.md)。
b91dfe4之后新增的独立评审修复和文档只在本机，未自动暂存/提交/推送；实时状态以Git为准。
Git 根是 games104，main 跟踪 origin/main；用户负责 Git Bash／Git GUI 操作。
笔记保留原路径，26 处非发布本地引用按用户决定不处理；个人操作截图、Piccolo 和构建缓存不上传。
详细操作和历史均保留在指南/归档中，不以旧记录覆盖当前决定。
