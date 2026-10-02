# G104Engine：GAMES104 学习与引擎实践

使用 **C#、.NET 10、Visual Studio 2026、OpenGL 4.3 Core、OpenTK 4.9.4**，逐步构建能够独立运行的轻量 3D 引擎。

本仓库范围包含引擎工程、项目文档和十份精选课程笔记。工程放在 `g104engine/`，笔记保留在 `资料/笔记博客汇总/` 原位置。Piccolo 仅作独立源码参考，不随本仓库发布。

> **当前阶段：D0–D3 已完成。** 安装、项目配置、依赖锁定、本机 OpenGL 4.3 Core 窗口探针及 Debug/Release 非交互检查均已通过；这仍是环境验证，尚未实现正式引擎功能。用户负责环境操作，Git 使用 GitHub 网页与 Git Bash／Git GUI；助手编写指引、检查结果并维护文档。实时状态以 [当前进度](g104engine/docs/status.md) 为准。

当前只搭建开发基础。具体软件实现架构与细节尚未确定，在后续功能计划中讨论；环境验证工程及历史示意不作为长期实现约束。

## 从这里开始

- [项目计划与责任分工](g104engine/docs/plan.md)：目标、范围、技术基线、发布清单与阶段验收。
- [当前进度与下一步](g104engine/docs/status.md)：已完成、待完成、等待谁操作、最近验证结果。
- [可视化环境搭建指引](g104engine/docs/setup.md)：用户操作步骤，以及完成后助手如何验证。
- [软件架构议题](g104engine/docs/architecture.md)：当前占位，记录后续待讨论的问题；具体设计尚未确定。
- [实现与笔记映射](g104engine/docs/learning-map.md)：学习模块、笔记入口、Piccolo 参考与验证路线。
- [技术与协作决策](g104engine/docs/decisions/0001-foundation.md)：已确认的选择、理由和适用边界。
- [阶段边界决策](g104engine/docs/decisions/0002-defer-implementation-architecture.md)：当前开发基础与后续软件设计的边界。
- [协作规则](AGENTS.md)：新对话的阅读顺序、环境分工、文档维护和提交确认规则。

## 当前下一步

本地 Git 根、main 分支、提交身份和 origin 均已核对。下一步用户按 [首次提交清单](g104engine/docs/setup.md#initial-submit-manifest)暂存 31 个文件，助手核对实际暂存内容并发起确认，再由用户提交与推送。原笔记引用按用户决定保持原样，证据见 [当前进度](g104engine/docs/status.md)。

项目展示名称保持 G104Engine，工程目录与解决方案文件基名统一为 `g104engine`。本项目使用 `g104engine/g104engine.slnx`；后续命令与 CI 从 `g104engine/` 开始。本机图形上下文和非交互检查分别记录，另一设备及 CI 仍待独立验证。

## 同步边界

- 已创建公开仓库：[zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)，本地已关联 origin；尚未首次提交或推送。
- 唯一新增 Git 根为 `games104`，不要在 `g104engine` 或笔记目录再次初始化仓库。
- 发布范围由 [计划中的精确清单](g104engine/docs/plan.md#发布文件范围) 和根目录忽略规则约束；提交前仍需核对实际文件。
- `操作流程/` 仅供用户在本地查看操作截图，不属于工程文档或发布资源；已显式忽略，不上传，也不作为工程或文档的依赖。
- 每次提交前，助手先展示改动与验证结果，并弹出交互确认框。用户确认后通过 Git Bash／Git GUI 提交、推送，助手验证结果。
- 部分笔记引用未发布的本地附件或参考源码；用户已决定按原样保留这些链接，不扩大上传范围。对应链接在公开仓库中可能无法访问，此限制不影响工程构建。

本阶段完成后，再共同确认下一阶段功能目标及必要的软件设计，然后拆分实现任务。最小 3D 场景、相机和输入交互仅为候选方向；所有模块保持学习追踪。
