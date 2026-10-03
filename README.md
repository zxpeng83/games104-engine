# G104Engine：GAMES104 学习与引擎实践

C# / .NET 10 / Windows x64 / OpenGL 4.3 Core / OpenTK 4.9.4。
目标是结合 GAMES104 做可解释、可扩展的独立引擎，服务 Gameplay 客户端求职和长期学习。

当前代码仍只有开发基础及 OpenGL/Smoke 环境探针；基础架构的部分约定已确认并保存，正式引擎功能尚未实现。
首个目标版本统一称为 **基础综合训练场 V1（用于面试展示）**，范围见 [V1 草案](g104engine/docs/plans/basic-training-ground-v1-draft.md)。原最小 3D 里程碑已合入 V1 的渲染/场景基础工作，不再独立交付或审批；后续扩展与专题另行分期。
V1范围/依赖已确认，NuGet及双配置输出核对通过；四个外部ZIP已到齐并完成静态检查，OpenAL实现库/角色基础动作已识别，声音按已选路线待离线转PCM16。当前入口为 [实施草案](g104engine/docs/plans/v1-implementation-draft.md) 与 [准备清单](g104engine/docs/plans/v1-dependencies-and-assets.md)。持久部署和功能验证未完成，代码开始前必须弹窗明确同意。
D0–D4 已完成；D5（独立克隆复现、CI、第二台设备）按用户要求全部暂缓、未验证。

## 从这里开始
- 新对话先读 [AGENTS.md](AGENTS.md) 和 [status.md](g104engine/docs/status.md)，按任务继续查阅。
- 新对话交接、决策索引与继续讨论入口：[handoff.md](g104engine/docs/handoff.md)。
- 长任务执行接续点与中断恢复：[V1执行台账](g104engine/docs/execution/v1-progress.md)；当前仅已建立规则，尚未批准代码开工。
- 目标、基线、分工与全模块覆盖：[plan.md](g104engine/docs/plan.md)。
- 已确认架构与未决设计：[architecture.md](g104engine/docs/architecture.md)；学习对应：[learning-map.md](g104engine/docs/learning-map.md)。
- V1 内部基础工作见范围草案；追溯：[原 M1 草案归档](g104engine/docs/archive/m1-minimal-3d-draft-2026-10-03.md)；证据：[设计参考核查](g104engine/docs/reviews/design-reference-checks-2026-10-03.md)。
- 环境搭建与学习资料：[setup.md](g104engine/docs/setup.md)，按主题进入指南，不需要每轮全读。
- 本阶段 review：[复查记录](g104engine/docs/reviews/foundation-review-2026-10-02.md)。
- 发布范围：[publishing.md](g104engine/docs/publishing.md)。

## 已有运行入口
在 g104engine 目录打开 g104engine.slnx，使用 Debug/Release x64。
不带参数运行 Sandbox 创建图形窗口；传入 --smoke 仅打印托管环境及程序集信息，不能代替 GPU 或 CI 全面验证。
可用命令及边界见 [探针指南](g104engine/docs/guides/probes.md)。

## 同步与资料
公开仓库：[zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)。
本地已提交基线为 3f98d1b（开发基础文档与阶段交接）；3ea7515 是首次提交。
2026-10-03 本轮文档与用户新增 NuGet 配置均保存在本机，尚未暂存、提交或推送；实时状态以 Git 为准。
Git 根是 games104，main 跟踪 origin/main；用户负责 Git Bash／Git GUI 操作。
笔记保留原路径，26 处非发布本地引用按用户决定不处理；个人操作截图、Piccolo 和构建缓存不上传。
详细操作和历史均保留在指南/归档中，不以旧记录覆盖当前决定。
