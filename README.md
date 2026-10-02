# G104Engine：GAMES104 学习与引擎实践

C# / .NET 10 / Windows x64 / OpenGL 4.3 Core / OpenTK 4.9.4。
目标是结合 GAMES104 做可解释、可扩展的独立引擎，服务 Gameplay 客户端求职和长期学习。

当前仅有开发基础及 OpenGL/Smoke 环境探针，正式引擎功能和软件架构尚未定案。
D0–D4 已完成；D5（独立克隆复现、CI、第二台设备）按用户要求全部暂缓、未验证。

## 从这里开始
- 新对话先读 [AGENTS.md](AGENTS.md) 和 [status.md](g104engine/docs/status.md)，按任务继续查阅。
- 架构与详细设计交接：[handoff.md](g104engine/docs/handoff.md)。
- 目标、基线、分工：[plan.md](g104engine/docs/plan.md)。
- 软件设计议题：[architecture.md](g104engine/docs/architecture.md)；学习对应：[learning-map.md](g104engine/docs/learning-map.md)。
- 环境搭建与学习资料：[setup.md](g104engine/docs/setup.md)，按主题进入指南，不需要每轮全读。
- 本阶段 review：[复查记录](g104engine/docs/reviews/foundation-review-2026-10-02.md)。
- 发布范围：[publishing.md](g104engine/docs/publishing.md)。

## 已有运行入口
在 g104engine 目录打开 g104engine.slnx，使用 Debug/Release x64。
不带参数运行 Sandbox 创建图形窗口；传入 --smoke 仅打印托管环境及程序集信息，不能代替 GPU 或 CI 全面验证。
可用命令及边界见 [探针指南](g104engine/docs/guides/probes.md)。

## 同步与资料
公开仓库：[zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)。
首次提交 3ea7515 已同步；当前文档整理仍在本机，未自动提交或推送，实时状态以 Git 为准。
Git 根是 games104，main 跟踪 origin/main；用户负责 Git Bash／Git GUI 操作。
笔记保留原路径，26 处非发布本地引用按用户决定不处理；个人操作截图、Piccolo 和构建缓存不上传。
详细操作和历史均保留在指南/归档中，不以旧记录覆盖当前决定。
