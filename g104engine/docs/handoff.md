# G104Engine V1 新对话交接

更新：2026-10-03。完整V1已正式获准，首轮代码、架构/注释/笔记映射与本机验证已完成；当前进入用户体验验收和学习，同范围修复继续有效。

## 当前接续点

用户正式答复“同意，按以上完整范围正式开工”，已记录完整C#/GLSL/数据/必要配置、素材/OpenAL/便携FFmpeg、构建运行修复和文档/快照权限。Sol Ultra主实施、Astra Ultra评审/独立核查；同范围恢复不重复开工，Scope外变化才讨论。

本地与GitHub准备节点cbce581一致；随后实现只在本机，未提交推送。实际Core/Scene/Editor、Rendering/Assets/Animation、Physics/Navigation、Audio/Effects/ImGui及Sandbox规则/窗口已落地。素材和许可已部署；动画四状态/混合数据配置及逻辑/显示Pose分离已实现。

Debug/Release0警告0错误，--verify与实际OpenAL通过；两配置隐藏GL240帧无错误，同帧双管线MAE0.0651/255、最大22/255；失败Play/坏资源/损坏存档备份/未保存设计与Undo跨PlayStop、120ms加载丢时和同GUID静态骨架空间已有证据。视听、手感、真实键鼠/DPI/失焦组合仍待用户验收，单图PCF弱斜面acne限制保留。

从 [运行验收](guides/v1-run-and-review.md)、[实际架构/学习](guides/v1-architecture-and-learning.md) 与 [实施复查](reviews/v1-implementation-review-2026-10-03.md) 开始；恢复细节及最新快照见 [执行台账](execution/v1-progress.md)。不安装/升级SDK或NuGet、不系统安装改PATH、不Git提交推送发布，D5仍暂缓，不改Piccolo/原笔记/私有资料。

## 阅读顺序与状态含义

1. 根 [README](../../README.md)、[AGENTS](../../AGENTS.md)，然后 [status](status.md) 核对现实状态及限制。
2. [plan](plan.md)：稳定目标、全部模块覆盖、分工、阶段边界及待完善的总体路线。
3. [architecture](architecture.md)：已确认约定、理由、图和待细化设计；当前交付范围见 [V1 草案](plans/basic-training-ground-v1-draft.md)。[原 M1](archive/m1-minimal-3d-draft-2026-10-03.md)为历史追溯，非当前执行入口。
4. 具体问题再读 [learning-map](learning-map.md) 对应章节，以及 [本次来源核查](reviews/design-reference-checks-2026-10-03.md)。不默认加载全部笔记、指南或 archive。
5. 开工/恢复任务必读 [完整实施方案](plans/v1-implementation-draft.md)、[准备清单](plans/v1-dependencies-and-assets.md) 与 [执行台账](execution/v1-progress.md)。先核对当前单元/授权/实际文件，再开始修改；若已存在清晰且范围不变的授权，不因换对话重复索取。

“已确认”指用户明确选择的设计方向；“候选/待审阅”包含助手建议及 M1 未获批准的具体默认值；“核查事实”只证明所查版本的代码或文档行为；“已实现/已验证”必须有本工程证据。禁止相互替代。

## 已确认决策索引

以下是压缩索引；完整理由、边界和公式在链接文档中，不需要向用户从头重复提问。

| 编号 | 已确认决定 | 保存位置 |
| --- | --- | --- |
| D01 | 短期 Gameplay/客户端求职，长期独立 3D 引擎；渲染、物理、动画、Gameplay、AI、工具等所有模块均需不同程度工程实践 | [plan](plan.md)、[learning-map](learning-map.md) |
| D02 | 按掌握程度推进；旧 2–4 周、每周 10–15 小时不是当前总期限；未设新硬期限 | [plan](plan.md) |
| D03 | 关键原理自研配合成熟库；说明自研、集成及简化边界；以笔记和固定 Piccolo 参考实现自己的引擎 | [plan](plan.md)、[核查](reviews/design-reference-checks-2026-10-03.md) |
| D04 | 助手主要编码，用户学习/运行/调试/验收/讨论；各检查点停留反馈；重大问题先解释参考与取舍再问 | [plan](plan.md)、[AGENTS](../../AGENTS.md) |
| D05 | 综合第三人称训练场：移动、跳跃、交互/机关、NPC；基础场景当前不含战斗 | [plan](plan.md) |
| D06 | 运行窗口内调试编辑面板：查看状态、调参、保存重载；工具库后续按 D49 选 ImGui.NET，具体细节待审 | [plan](plan.md) |
| D07 | 借鉴 Piccolo 的对象组件组织，明确系统更新阶段；起步不采用完整 ECS | [architecture](architecture.md) |
| D08 | 固定模拟 60 Hz，绘制频率独立；补步上限、焦点/暂停、输入事件和插值细则仍须审阅 | [architecture](architecture.md) |
| D09 | 控制器驱动世界位移，基础动画 in-place；相对镜头移动；首块控制器先验收平地/墙面/滑动/跳跃，复杂地形后续 | [architecture](architecture.md) |
| D10 | 世界 Y-up 右手；默认局部前 -Z、右 +X；米/秒；CPU 沿用 OpenTK 原生行向量、GLSL 列向量，集中记录映射 | [architecture](architecture.md) |
| D11 | 关卡资源共享至场景卸载；新场景准备成功后才替换旧场景 | [architecture](architecture.md) |
| D12 | 基础阶段保留 Engine/Sandbox 两项目：通用能力与演示/玩法分工；后续按实际需求再评估拆分 | [plan](plan.md)、[architecture](architecture.md) |
| D13 | 早期绘制采用环绕观察相机，合入 V1 后仍保留；不是角色相机，也不追认原 M1 的所有细节 | [原 M1 归档](archive/m1-minimal-3d-draft-2026-10-03.md) |
| D14 | 网络、动态 GI、GPU 几何后续开专题分支研究；先保留稳定基线，允许有理由且可验收的局部重构 | [plan](plan.md)、[architecture](architecture.md) |
| D15 | 保留有学习价值的基线/改进实现进行比较；具体即时开关、重载模式及组合范围尚未选择 | [architecture](architecture.md) |
| D16 | 架构文档/图、中文注释、代码—笔记—固定参考—简化方案—证据随进展维护；新对话依靠文件接续 | [learning-map](learning-map.md)、[AGENTS](../../AGENTS.md) |
| D17 | 正式光照先 Forward，再实际实现 Deferred 对比；基础绘制在 V1 内验证，原独立 M1 流程已被 D44 合并 | [渲染路线](plans/rendering-roadmap.md) |
| D18 | 此前已选渲染模块目标含 PBR、基础阴影、IBL、天空盒、HDR/色调映射/FXAA；当前 V1 逐项归属以范围草案/明确决定为准，不要求全部渲染完成才接入玩法 | [渲染路线](plans/rendering-roadmap.md) |
| D19 | 地形、天空/云、AO、雾等扩展专题，每类先做代表性实验，再按需整合进训练场 | [渲染路线](plans/rendering-roadmap.md) |
| D20 | 主要模型输入采用 glTF 2.0/GLB，FBX 素材先通过外部工具转换；解析库后续按 D49 选 SharpGLTF，外部转换工具/支持子集待细化 | [资产/场景路线](plans/assets-scene-roadmap.md) |
| D21 | 基础版先保存设计场景；运行状态存档另设阶段，不将全部 Runtime 内存直接写回场景 | [资产/场景路线](plans/assets-scene-roadmap.md) |
| D22 | 单层对象模板与明确的实例覆盖；嵌套、变体、完整 Apply/Revert 不自动纳入，覆盖契约待细化 | [资产/场景路线](plans/assets-scene-roadmap.md) |
| D23 | 此前已选角色/物理目标含斜坡/台阶、平移平台及受控推箱，分别验收；当前 V1 归属待范围收敛，首块平地/墙面不变 | [物理/角色路线](plans/physics-character-roadmap.md) |
| D24 | 自研角色移动规则，物理后端提供查询与刚体能力；后续按 D49 选 JoltPhysicsSharp，包已准备、功能调用尚未验证 | [物理/角色路线](plans/physics-character-roadmap.md) |
| D25 | 布娃娃、布料/PBD/XPBD、破坏、车辆等进阶物理先独立代表实验，再按需整合 | [物理/角色路线](plans/physics-character-roadmap.md) |
| D26 | 基础动画终点为 Idle/Walk/Run 速度混合、跳跃状态机、平滑过渡和基础事件；Mask/Additive/脚部 IK 后续实验 | [动画路线](plans/animation-roadmap.md) |
| D27 | 动画采用小型可配置状态机与混合运行时，先有状态/权重调试显示，不同时制作完整节点编辑器 | [动画路线](plans/animation-roadmap.md) |
| D28 | 初期使用同一骨架的一组相容动作，重定向另做专题；解析库已按 D49 选择，实际素材待核验 | [动画路线](plans/animation-roadmap.md) |
| D29 | 基础玩法先 C# 规则＋数据配置，Lua/可视化脚本留后续专题；数据重载不等于代码热更新 | [Gameplay/AI 路线](plans/gameplay-ai-roadmap.md) |
| D30 | 基础 AI 先 FSM，再小型 BT 对照同一巡逻/跟随/搜索场景；与动画 FSM 职责分离 | [Gameplay/AI 路线](plans/gameplay-ai-roadmap.md) |
| D31 | 导航先自研网格 A* 并让 NPC 实际跟随，再加入 NavMesh 对比；具体库与可走范围未定 | [Gameplay/AI 路线](plans/gameplay-ai-roadmap.md) |
| D32 | 粒子先 CPU 生命周期/Billboard，再 GPU Compute 实际对比；不等于提前启动 Nanite/GPU 几何专题 | [粒子/声音路线](plans/particles-audio-roadmap.md) |
| D33 | 声音基础版做到 2D/3D 播放、方位距离、一次性/循环事件及基本播放管理；遮挡/混响等后续实验 | [粒子/声音路线](plans/particles-audio-roadmap.md) |
| D34 | 第三人称 Listener 起步跟随相机位置与朝向，可在后续试听实验中评估调整；后端后续按 D49 选择 | [粒子/声音路线](plans/particles-audio-roadmap.md) |
| D35 | 基础工具做到有限场景编辑＋约定操作的 Undo/Redo；ImGui.NET 已选，具体字段/命令与分期待定，完整工具不阻塞基础绘制 | [工具/调试路线](plans/tools-debug-roadmap.md) |
| D36 | Play 使用当前内存设计数据，Stop 恢复设计预览并保留未保存编辑；运行变化不自动写回设计，也不等于运行存档 | [工具/调试路线](plans/tools-debug-roadmap.md) |
| D37 | 用户关注初版成本后确认：初版仅 FPS/帧耗时，CPU/GPU 详细计时与相关计数按问题或对比实验需要加入；不作为初版验收要求，模块调试显示仍保留 | [工具/调试路线](plans/tools-debug-roadmap.md) |
| D38 | 基础主线程按明确阶段更新与提交图形，同步准备场景；允许加载短暂停顿，不代表整个进程只有一条线程 | [核心架构路线](plans/core-architecture-roadmap.md)、[architecture](architecture.md) |
| D39 | 后续数据布局对比＋小型 ECS 实验，再按需局部接入；不预定主引擎整体迁移 | [核心架构路线](plans/core-architecture-roadmap.md) |
| D40 | 先用 .NET 并行库，再自研有限任务调度原型，先用成熟同步原语；Fiber/复杂无锁等后续细化，详细计时按实验需要加入 | [核心架构路线](plans/core-architecture-roadmap.md) |
| D41 | Deferred 纳入基础综合训练场 V1，保留先 Forward 再实际 Deferred 对照；具体 G-buffer/光照/切换和验收细节待定，不自动纳入所有扩展渲染 | [V1 范围](plans/basic-training-ground-v1-draft.md)、[渲染路线](plans/rendering-roadmap.md) |
| D42 | Undo/Redo 纳入基础综合训练场 V1，沿用有限场景设计操作范围；具体字段/命令清单待定，运行状态不自动纳入撤销 | [V1 范围](plans/basic-training-ground-v1-draft.md)、[工具路线](plans/tools-debug-roadmap.md) |
| D43 | 统一以“基础综合训练场 V1”为主名称，注明“用于面试展示”；旧面试 Demo/面试 V1 指同一版本；M1 后续按 D44 合入，不因名称统一批准其余范围 | [V1 范围](plans/basic-training-ground-v1-draft.md)、[plan](plan.md) |
| D44 | 最小 3D 里程碑正式合入基础综合训练场 V1，必要绘制/变换/资源验证保留为内部工作；旧草案归档，不再独立交付或审批，原参数不自动获批 | [V1 合并说明](plans/basic-training-ground-v1-draft.md#m1-integration-proposal) |
| D45 | 开始写代码前必须弹窗展示具体实施范围，得到用户明确同意后再开始；普通范围/选型回答和“推进下一步”不代替开工确认 | [AGENTS](../../AGENTS.md)、[V1 范围](plans/basic-training-ground-v1-draft.md) |
| D46 | V1 渲染含 Forward/Deferred、PBR、基础阴影、天空盒、HDR/色调映射/FXAA；IBL 后移，具体算法参数待实施方案 | [V1 范围](plans/basic-training-ground-v1-draft.md) |
| D47 | V1 角色/物理含平地、墙滑、跳跃与有限坡台；平移平台/受控推箱后移，自研角色规则分工保持 | [V1 范围](plans/basic-training-ground-v1-draft.md) |
| D48 | V1 工具含有限创建/删除/变换/参数编辑、Undo/Redo、Play/Stop；保留未保存设计，具体字段/事务待定 | [V1 范围](plans/basic-training-ground-v1-draft.md) |
| D49 | V1 采用 SharpGLTF.Core、StbImageSharp、JoltPhysicsSharp、ImGui.NET、OpenTK.Audio.OpenAL＋OpenAL Soft；NuGet 后续已由用户完成，原生Soft/素材待准备，功能未验证，代码授权另行确认 | [依赖与素材准备](plans/v1-dependencies-and-assets.md) |
| D50 | Kenney实际输入为Ogg Vorbis，用户选择保持V1 PCM16 WAV子集、离线转换选定声音；不新增运行时Ogg解码库，工具/转换尚未执行 | [输入核对](reviews/v1-input-archives-check-2026-10-03.md)、[准备清单](plans/v1-dependencies-and-assets.md) |
| D51 | 必要开工前设计需完整且连贯；普通实现细节授权助手决定并记录，重大范围/架构/成本/体验取舍集中询问。节省token不意味着跳过前期设计，代码仍需最终弹窗确认 | [AGENTS](../../AGENTS.md)、[实施草案](plans/v1-implementation-draft.md) |
| D52 | V1实现受控基础父子层级：父子变换/挂接/保存及Undo；玩家/NPC/活动相机/动态刚体保持根、父组限正统一缩放，运行中不以父组运动驱动物理对象；模型/骨骼内部层级保留 | [实施草案9.1](plans/v1-implementation-draft.md) |
| D53 | GPT-6.1 Sol Ultra主实施与实施子任务，GPT-6 Astra Ultra关键评审/独立核查；两者均Ultra，质量优先，不为节省自行降档；主对话由界面选择，子任务显式指定 | [执行台账](execution/v1-progress.md)、[节点记录](reviews/v1-start-checkpoint-2026-10-03.md) |
| D54 | 正式弹窗明确同意完整V1连续实施；代码/Shader/设计数据/必要复制配置、既有素材/OpenAL/便携FFmpeg校验转换、构建运行修复与文档/快照已授权，同范围恢复不重问 | [执行台账](execution/v1-progress.md)、[实施复查](reviews/v1-implementation-review-2026-10-03.md) |

## 当前需要用户验收与后续讨论

- 用户按运行指南验收走跑跳/坡台/镜头、NPC与机关、管线/阴影/粒子、声音与场景编辑。助手已完成可独立验证的部分，不把人工体验写为已通过。
- 有反馈直接在已授权V1范围修复；先定位实际证据，不回到开工前重新选库或再批准同范围实现。
- 长期网络/GI/GPU几何及其他模块不同深度实践保留；IBL/平台推箱/BTNavMesh/GPU粒子等后移，新专题再决定具体范围。
- 后续Git同步仍由用户按确认流程操作，缓存/快照不是远程备份；D5不因V1完成自动恢复。

## 现实工程、参考与执行边界

- 当前已有完整V1模块，已有双配置构建和CPU/原生/图形证据，范围与限制见status/实施复查；旧探针不是当前功能入口。
- C#/.NET 10、SDK 10.0.401、OpenTK 三个直接包 4.9.4、Windows x64、OpenGL 至少 4.3 Core 保持。
- Piccolo 固定 f5053707fed4d3f94d270a436fb0d3a8ae54e3e5；其已有行为和局限见来源核查，不从课名推断参考引擎完整实现了对应专题。
- D5 克隆复现、CI、第二台设备测试仍全部暂缓。网络等后续模块的规划不恢复 D5。
- 环境安装、工程创建/NuGet 和 Git 操作继续由用户在既有工具完成；编码分工不扩大这些授权。
- 用户已提交推送cbce581，本轮只读核实本地与GitHub main一致，见 [开工前节点记录](reviews/v1-start-checkpoint-2026-10-03.md)。本轮追加的模型/节点记录仅在本机，未自动提交推送；输入ZIP仍为忽略缓存。
- 不进入私有截图，不修改 Piccolo、原笔记和 26 处非发布链接，不清理上层仓库或扩大发布范围。

## 可复制的新对话提示

~~~text
继续G104Engine，现有目录E:\game_study\games104。
先按README读取AGENTS、status、handoff与execution/v1-progress.md，再读运行验收、实际架构/学习与实施复查，核对Git/当前源码/相关日志。
完整基础综合训练场V1已获正式弹窗同意并完成首轮实现；同范围验收与修复不重新询问开工。Sol Ultra主实施、Astra Ultra评审；架构/配置/依赖方向不重复选型。
当前需要用户体验验收与学习；按反馈定位修复，保留未保存设计/Undo与源资料。若是意外中断，先检查实际文件和台账最新单元再恢复。
不安装或升级SDK/NuGet，不系统安装改PATH，不提交推送/发布，D5仍暂缓；长期专题不自动扩入V1。
每批保存进度/验证/下一步，维护实际架构、注释与代码—笔记关系。
~~~

## 后续怎样维护

按根 [AGENTS 的跨对话持久化长期规则](../../AGENTS.md#cross-chat-persistence) 主动维护，不等待用户逐次提醒。重要决定、用户纠正、关键发现、阶段完成/中断及换对话前及时保存；记录决定与理由、实际进展、验证及限制、未决问题和下一步，只更新受影响文档。

所有接续本项目的新对话先读规则、status 和本文件，再按需读计划/架构并核对实际工程；已确认选择不从头重复询问。只读/Plan 模式下明确未落盘内容，提供可复制摘要和待更新文档，不越过模式写入或声称已经保存。文件中的旧授权边界服从用户新的明确指令。
