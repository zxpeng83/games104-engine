# 新对话交接：收敛基础综合训练场 V1 与开发节奏

更新：2026-10-03。本文件是接续本次长对话的入口；已保存讨论，不等于批准全部草案或开始实现。

## 先知道当前停在哪里

最新状态：D51/D52及普通细节已收敛，重新发起正式开工弹窗后，用户明确回复“稍等，我先看看”。**暂停推进，等待审阅后的明确指示；尚未获开工授权。** 待审范围包括C#/GLSL/场景数据、必要编译/资源复制配置、素材/OpenAL提取、FFmpeg9.0.2便携离线转换、验证和文档；不含系统安装/PATH/NuGet升级/SDK变化/Git发布。当前没有写代码、提取部署素材或获取/运行转换工具；不要把弹窗默认选项或用户暂缓答复当作同意。

随后用户提出连续完成V1、模型选择、剩余上下文与中断恢复问题。本轮只完善文档和 [执行台账/恢复规程](execution/v1-progress.md)，不代表开工授权。执行偏好为批准后连续实施、内部验证和留存预览，用户回来统一验收/学习，重大取舍或实际权限/阻塞再讨论；不保证断网/额度耗尽/进程退出后自动运行。建议新对话使用同一个现有本地目录；尚未提交的计划与忽略缓存不会自动出现在新克隆/工作树中。

最新纠正：用户报告本对话已自动压缩，决定暂不换对话，继续在本对话准备开工。当前正在解释Sol主实施、Astra子任务评审的可选机制；该模型分工尚未定案或启动，不把产品能力说明当作代码开工批准。以后确有中断/换对话再使用恢复规程。

用户希望各模块都不同程度地实现与掌握，已逐步确认基础架构和主要模块路线。早期只细化 M1 的计划已被后续版本规划取代，M1 现已合入 V1。**当前集中收敛 V1 的范围、依赖/素材、关键契约、完整实施方案和反馈节奏。** 长期模块目标保留，未来专题的精细设计不作为 V1 启动前置。

用户要求持久化全部关键决定与进度；助手此前负责方案/文档，当前已由用户在 VS 完成新增依赖配置，助手只读核对并记录。没有开始功能代码，助手没有安装/还原/构建/运行或 Git 写操作。

续接讨论已确定渲染、资产/场景、物理/角色、动画、Gameplay/AI、粒子/声音、工具/调试与核心架构实践的主要方向（D17–D40），见下方索引。后续尚需讨论网络/GI/GPU几何专题边界并汇总完整路线；模块内部分块仍是候选，不要求先做完全部渲染才开始玩法。

最新接续：NuGet与双配置输出已核对；四份外部ZIP已由用户放入g104engine/.cache/v1-preparation并完成只读内存检查，见 [输入核对](reviews/v1-input-archives-check-2026-10-03.md)。可用OpenAL Win64实现库和同骨架GLB/基础动作已识别；声音实际全为Ogg，用户已选保持PCM16 WAV、离线转换（D50）。工具/条目与部署规则见实施方案第9节，尚未解压/转换/导入/加载。**代码前必须弹窗明确同意，已询问但尚未获准**；执行范围仍见 [V1草案](plans/v1-implementation-draft.md)。

原最小 3D 工作已并入 V1，[旧 M1](archive/m1-minimal-3d-draft-2026-10-03.md)已归档。实施草案建议连续开发和非强制停工预览，节奏随开工方案确认。用户的原探针运行反馈与新增库功能验证分开记录；后者尚未进行。

本批核对：Engine9直接包，Sandbox仅项目引用；两锁各13包一致、原七包未升级，锁定/还原成功。Debug/Release deps包图一致；五个新增托管DLL和三份win-x64原生DLL与NuGet缓存SHA-256一致，原生PE为AMD64。Sandbox/bin未找到OpenAL Soft，新增功能未调用验证。非文档差异仍仅Engine.csproj与两锁文件，原C#源码/SDK/slnx未变，保留用户修改。

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

## 不能遗漏的未决事项

- **第一优先：准备与实施方案审阅。** 三组主要范围和依赖组合已确认，不重复提问；当前核对精确版本、本机原生部署、同骨架素材与实施契约/验收。网络/GI/GPU几何精细讨论后置，长期目标保留。
- **开工与节奏。** D45 要求代码开始前弹窗明确同意，当前未获准。完整方案建议连续开发、中途可运行预览不强制停工，该节奏随开工方案整体审阅；真实体验反馈仍须用户验证，不能自动标为通过。
- 原M1参数不因归档自动成为依据；其中已在实施方案第3/9节重新明确的最大5补步/0.25秒上限、输入/暂停规则与新增 --verify 属助手按D51定案，代码仍待授权。相机微调/键位等普通细节可在既定范围内处理，不重新开一轮选型。
- 固定步输入边沿、模拟外编辑安全点、UI/失焦/暂停、追赶与重置已有第3/9节契约；实际插值/事件/脚滑效果尚未实现验证，不要把设计定案写成已验证。
- 原始ZIP均已核对，下一步不是重新下载：OpenAL选bin/Win64/soft_oal.dll（非router）；GLB选Unreal-Godot/UAL1_Standard.glb，65骨骼、43全LINEAR动作/4影响，Run起步映射Jog_Fwd_Loop，Sprint留作后续调参。角色无纹理，另需小型贴图输入；音频共230个Ogg，按D50离线转PCM16，当前PATH未发现ffmpeg/ffprobe。便携FFmpeg选型/校验来源已记录，部署/转换/试听/导入与新增库功能均未执行。
- 核心架构实验的数据布局/查询/结构变化、任务依赖/完成/取消/失败/寿命与具体分期待定；D39/D40 保留自研实践，不等于把全部引擎改成 ECS，也不等于自研原生线程、锁、无锁队列或栈切换的范围已批准。
- 工具的有限命令、事务、100条可调Undo历史、只读运行状态、Play/Stop保留设计已见实施方案第7/9节；具体界面微调按D51处理，运行时写回设计不在基础范围。D37已取代“初版同时加入CPU/GPU细计时”的原建议，不取消其他模块必要调试显示。
- 后续专题保留工程实践：网络是多份世界与一致性能力，GI 是间接光能力，GPU 几何涉及筛选/表示/流送；不能统称简单提速，也不能声称小实验复刻 Lumen/Nanite。
- 用户曾对“热切换/重置切换”不理解，转而提出分支开发；**没有选择“必须全部运行中无缝切换”**。后续应结合实验解释再决定。

## 现实工程、参考与执行边界

- 当前仍只有 Engine 空类模板和 Sandbox OpenGL/Smoke 探针；本次没有构建或运行。旧截图与构建证据及局限见 status。
- C#/.NET 10、SDK 10.0.401、OpenTK 三个直接包 4.9.4、Windows x64、OpenGL 至少 4.3 Core 保持。
- Piccolo 固定 f5053707fed4d3f94d270a436fb0d3a8ae54e3e5；其已有行为和局限见来源核查，不从课名推断参考引擎完整实现了对应专题。
- D5 克隆复现、CI、第二台设备测试仍全部暂缓。网络等后续模块的规划不恢复 D5。
- 环境安装、工程创建/NuGet 和 Git 操作继续由用户在既有工具完成；编码分工不扩大这些授权。
- 本地基线 3f98d1b 已含上轮文档整理。本批文档已保存但未暂存/提交/推送，同机可读，换设备需按确认流程同步。
- 不进入私有截图，不修改 Piccolo、原笔记和 26 处非发布链接，不清理上层仓库或扩大发布范围。

## 可复制的新对话提示

~~~text
继续 G104Engine，项目根目录 E:\game_study\games104。
使用这个现有本地目录；不要另建克隆或工作树，以免遗漏未提交计划和忽略缓存。
先只读 README.md → AGENTS.md → g104engine/docs/status.md、handoff.md，再读 execution/v1-progress.md、plans/v1-implementation-draft.md 和 plans/v1-dependencies-and-assets.md；按需读 architecture/plan，并核对真实Git及当前代码。
目标是完整“基础综合训练场 V1（用于面试展示）”。M1已合入；范围与D01-D52及助手定案均已保存，不重复询问已确认选择，不扩大后续专题范围。
NuGet/双配置原探针/四ZIP静态核对已完成；新增库API、素材部署和正式功能仍未实施验证，不能把原探针正常当作完整V1正常。
上次正式开工弹窗答复为“稍等，我先看看”，尚未批准。恢复状态并确认方案后，请弹窗列明实施范围，得到我明确同意再开始代码/素材部署/转换。
获准后尽量连续完成V1，内部增量验证、预览不强制停工；普通细节与修复自主处理，重大范围/架构变化、新权限或真实阻塞再讨论，我回来统一验收与学习。
依执行台账每批保存进度、验证、修改文件与下一步；维护架构图、关键注释、代码—笔记映射。意外中断按真实工作区接续，已有清晰且范围不变的授权不反复确认。
不系统安装/改PATH，不新增升级NuGet、不改SDK，不暂存/提交/推送或发布，D5继续暂缓；保留用户改动和私有资料边界。
~~~

## 后续怎样维护

按根 [AGENTS 的跨对话持久化长期规则](../../AGENTS.md#cross-chat-persistence) 主动维护，不等待用户逐次提醒。重要决定、用户纠正、关键发现、阶段完成/中断及换对话前及时保存；记录决定与理由、实际进展、验证及限制、未决问题和下一步，只更新受影响文档。

所有接续本项目的新对话先读规则、status 和本文件，再按需读计划/架构并核对实际工程；已确认选择不从头重复询问。只读/Plan 模式下明确未落盘内容，提供可复制摘要和待更新文档，不越过模式写入或声称已经保存。文件中的旧授权边界服从用户新的明确指令。
