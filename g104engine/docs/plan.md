# G104Engine 目标与长期路线

本页维护稳定需求、已确认的推进方向和仍待收敛的阶段。V1的范围与设计取舍集中在 [v1-baseline.md](plans/v1-baseline.md#scope)；实现进展、验证限制和本次接续任务由 [status.md](status.md) 单独维护。文中的未来目标、已选路线与候选实验均不生成实施授权，也不证明功能或学习已经完成。

<a id="goals"></a>
## 项目目标

- 短期服务Gameplay/游戏客户端求职，长期持续打磨可扩展的独立3D引擎。用户学完GAMES104并有十份详细笔记，熟悉C#/Unity，C++经验较少；项目与学习安排以此为起点。
- 全模块都要有不同程度的工程实践与掌握：渲染、动画、物理、粒子、声音、工具链与资产、Gameplay、AI、网络、核心架构、动态GI与GPU几何。不同模块深度可以不同，不承诺复刻课程全部算法或商业系统。
- 关键原理自研并配合成熟库，分别说明自研、第三方集成和简化边界。原理理解、参考源码研读、本工程运行证据与用户掌握分别记录。
- 每块保留可运行、可观察的成果以及足以解释正确性和限制的证据；维护中文注释、架构图与“代码入口—笔记章节—固定参考版本—采用/简化方案—验证证据”。
- 按掌握程度推进。旧“2–4周、每周10–15小时”是历史估算，当前没有硬期限；不依据算法数量虚构工期或保证求职结果。

<a id="foundation"></a>
## 已确认的基础方向

| 主题 | 长期有效的方向与理由 |
|---|---|
| 工程与语言 | 展示名G104Engine，目录/解决方案基名g104engine；C#独立引擎、.NET10、VS2026、Windows x64，利用现有语言基础，不依赖Unity，也不等同于取得C++工程熟练度 |
| 图形与依赖 | OpenGL4.3 Core与OpenTK；为Compute/SSBO实验保留能力，渲染、Pass和资源组织仍由项目实现。精确SDK/包/原生版本与部署依据见依赖指南，不以计划更新包 |
| 工程职责 | 先保留G104.Engine与G104.Sandbox两项目；Engine按通用职责组织且不依赖Sandbox，训练场专属规则在Sandbox。长期拆分按实际需求评估 |
| 对象与更新 | 对象/组件、明确阶段，基础主线程组织模拟和图形提交、同步准备场景；组件组织不自动等于完整ECS或Job System |
| 时间与运动 | 固定60Hz模拟、绘制独立，控制器提交角色世界位移、基础动画in-place，镜头相对移动；状态写入者、资源所有者及安全点明确 |
| 资产与保存 | glTF2.0/GLB优先，运行时不直接读FBX；先保存设计场景，运行存档另设阶段。共享资产与独立实例分开，新场景准备成功后才替换旧场景 |
| 数学 | Y-up右手、默认局部前−Z/右+X、米和秒；CPU沿OpenTK行向量、GLSL列向量，转换和上传集中说明 |
| 示范内容 | 第三人称非战斗综合训练场：移动、跳跃、机关、NPC；窗口内有限调试编辑面板，避免另建重复演示工程 |
| 对照与演进 | 保留有教学价值的基线/改进对照，允许有理由且可验收的局部重构；不要求每个历史方案永久成为主引擎开关 |

基础选型与旧替代方案的理由保存在 [decisions.md](decisions.md)。固定Piccolo参考为 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`，旧homework案例保留各自版本；它不是编译依赖，也不为所有课程专题提供完整实现。其他模块按需补充固定版本参考。

<a id="roadmap"></a>
<a id="整体模块覆盖待讨论表"></a>
## 全模块覆盖与阶段路线

下表维护能力方向；V1列只标明已批准版本的边界，实际完成与证据不在本表同步。后续列含已选路线和候选实验，其算法、阶段、依赖、最低成果及验收需要在专题开始前收敛。模块R/P/G/T/C/J等编号用于讨论，不是发布版本或全局排期。

| 模块 | V1批准边界 | 后续实践方向 |
|---|---|---|
| 核心架构 | 明确主线程阶段、固定步/输入、显示插值、同步准备与失败保护 | 数据布局对照→小ECS实验→按需局部接入；先.NET并行库再有限自研调度，Fiber/无锁/异步加载另定，见[core-architecture-roadmap.md](plans/core-architecture-roadmap.md) |
| 渲染 | Forward→Deferred对比、基础PBR/阴影、天空背景、HDR/色调映射/FXAA；IBL后移 | IBL、预计算光照、更多阴影、地形/天空云/AO/雾、AA/后处理、裁剪/实例化与性能实验，见[rendering-roadmap.md](plans/rendering-roadmap.md) |
| 资产与场景 | glTF/GLB受控子集、设计JSON、对象身份、受控父子、单层模板和实例覆盖 | 新输入子集、稳定资产身份/依赖、迁移/Cook/重导入/异步加载/打包、模板扩展与运行存档，见[assets-scene-roadmap.md](plans/assets-scene-roadmap.md) |
| 工具与调试 | 有限场景编辑、Undo/Redo、Play/Stop保护未保存设计、模块调试、FPS/帧间隔 | Gizmo/拾取、单步、按需CPU/GPU测量、通用导入/反射/高级编辑与恢复工具，见[tools-debug-roadmap.md](plans/tools-debug-roadmap.md) |
| Gameplay | C#规则＋数据配置、镜头相对输入、按钮/门/目标及事实反馈；无战斗 | Lua/可视化规则、3C扩展及相应绑定/寿命/重载合同，见[gameplay-ai-roadmap.md](plans/gameplay-ai-roadmap.md) |
| AI | 感知/记忆、巡逻/跟随/搜索FSM、受控平面网格A*与真实控制器跟随 | 同场景FSM→小BT，网格A*→NavMesh对照；Steering/Crowd、HTN/GOAP/MCTS与学习型代表实验，见[gameplay-ai-roadmap.md](plans/gameplay-ai-roadmap.md) |
| 物理与角色 | 成熟后端查询/刚体适配、自研平地/墙滑/跳跃/有限坡台；平台/推箱后移 | 积分/转动/检测/接触约束原理实验、平台/受控推箱、Sleeping/CCD、Ragdoll/PBD/XPBD/Cloth/破坏/车辆，见[physics-character-roadmap.md](plans/physics-character-roadmap.md) |
| 动画 | 同骨架素材、采样/蒙皮、Idle/Walk/Run混合、跳跃状态/过渡/事件及有限配置 | Mask/Additive/Blend Space、IK、重定向/Root Motion、压缩/Morph/性能与物理动画，见[animation-roadmap.md](plans/animation-roadmap.md) |
| 粒子 | 有限CPU生命周期/Burst/Billboard/透明合成 | CPU→GPU Compute实际对比、池/同步/间接绘制、持续Emitter与Mesh/Ribbon/Flipbook/碰撞等表示，见[particles-audio-roadmap.md](plans/particles-audio-roadmap.md) |
| 声音 | 2D/3D一次性/循环、相机Listener和基本Voice管理；PCM输入/成熟设备后端 | 数字音频/声像、遮挡/混响/Doppler/HRTF/Ambisonics、流送/预算，见[particles-audio-roadmap.md](plans/particles-audio-roadmap.md) |
| 网络 | 独立后续专题 | 多份世界与权威规则、同步、预测/校正/插值及受控网络条件实验；模型和范围待选 |
| 动态GI/Lumen | 独立后续专题 | 可运行的直接/间接光对照、GI近似、动态更新与历史限制实验，不承诺复刻Lumen |
| GPU几何/Nanite | 独立后续专题 | 可运行的GPU裁剪、间接绘制、简化LOD/驻留及CPU/GPU工作量对照，不承诺复刻Nanite |

<a id="stages"></a>
## 如何推进及仍未定案的阶段

1. 从可重复运行的V1基线学习启动、帧循环、输入到运动及交互的链路，结合断点、调参、解释自测和独立小练习。体验通过与源码掌握是不同成果；具体练习/展示脚本按掌握程度细化。
2. 从稳定基线选择可观察、可验收的能力扩展或方案对比，渲染、角色、玩法与工具按依赖交错推进。先做代表实验，再按学习价值/维护成本决定是否接入训练场。
3. 网络、动态GI、GPU几何等分别形成专题；先明确假设、范围、参考版本、成果、测量和失败边界，再决定隔离方式与局部重构。专题分支的创建、合并/切换机制尚未定案。

整体分层为“开发基础→基础综合训练场V1→能力扩展/进阶专题”，不代表三个发布版本。原最小3D里程碑已合入V1内部；完整跨模块后续版本表、总数量、编号、期限和统一顺序均未冻结，不能把所有未来实验合成无边界的V2/V3。课程顺序、表格顺序、模块编号和Git分支也不决定实施顺序。

初版性能观察的投入限定为FPS/帧耗时；有模块调试显示不等于完整Profiler。对照实验需要时再加入相应CPU/GPU计时及计数，不能永远只用FPS，也不提前为未来专题建整套性能产品。

<a id="execution-boundaries"></a>
## 分工与执行前提

助手主要编码，用户学习、运行/调试、验收与讨论；具体权限、模型分工和持久化要求在 [agent-workflow.md](agent-workflow.md) 单点维护。完整V1曾取得正式范围授权，同范围恢复/修复不因换聊天重问；未来路线与新增专题不继承该授权。重大范围/架构/依赖/成本/体验变化先说明理由及影响。

D5的本机新目录克隆、CI、第二设备测试整体暂缓，未来专题规划和V1完成均不自动恢复它们。Git同步、文档保存与应用发布分别处理；发布白名单及个人资料边界见 [git-and-publishing.md](guides/git-and-publishing.md)。私有截图、原笔记和独立参考仓库不因为长期路线调整而纳入改造。

可选深入：[architecture.md](architecture.md)、[learning-map.md](learning-map.md)、[dependencies.md](guides/dependencies.md)、[v1-baseline.md](plans/v1-baseline.md)、[design-reference-checks-2026-10-03.md](reviews/design-reference-checks-2026-10-03.md)。这些链接补充主题依据，不要求读者重新回入口完成同一任务。
