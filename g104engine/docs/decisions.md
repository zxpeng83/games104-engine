# 决策、理由与演变索引

本页保留已确认选择与取代关系；用户最新明确指令优先。D01–D54沿用原交接编号，原0001/0002保留来源标识，不因整理把候选改称批准。
选择、实现、行为验证、人工体验和学习掌握分别成立；决定本身不是完成证据。当前操作政策只在 [agent-workflow.md](agent-workflow.md#current-policy) 维护，本页解释当时为何如此选择。
初次搭建、V1收敛、修复与本次文档改造的时间关系可选查 [development-history.md](development-history.md#timeline)。

<a id="decision-index"></a>
## D01–D54：既有决定及承接

按编号或主题查阅，不要求每次逐条读完。D18的完整渲染目标经D46收敛V1、IBL后移；D23经D47保留有限坡台、平台推箱后移；D13与原M1候选细节不能越过D44/D52后续收敛。
D04/D45/D51定义设计与授权，D54记录实际完整V1批准，不使环境/Git/新专题自动获准。D53的模型要求保留实际使用需如实记录的边界。

| 编号 | 已确认决定／后续承接 | 来源与详细内容 |
|---|---|---|
| <a id="d01"></a>D01 | 短期 Gameplay/客户端求职，长期独立 3D 引擎；渲染、物理、动画、Gameplay、AI、工具等所有模块均需不同程度工程实践 | [plan.md](plan.md)、[learning-map.md](learning-map.md) |
| <a id="d02"></a>D02 | 按掌握程度推进；旧 2–4 周、每周 10–15 小时不是当前总期限；未设新硬期限 | [plan.md](plan.md) |
| <a id="d03"></a>D03 | 关键原理自研配合成熟库；说明自研、集成及简化边界；以笔记和固定 Piccolo 参考实现自己的引擎 | [plan.md](plan.md)、[design-reference-checks-2026-10-03.md](reviews/design-reference-checks-2026-10-03.md) |
| <a id="d04"></a>D04 | 助手主要编码，用户学习/运行/调试/验收/讨论；完整V1按D54连续实施，普通内部检查不逐项等待，重大取舍先讨论 | [plan.md](plan.md)、[AGENTS.md](../../AGENTS.md) |
| <a id="d05"></a>D05 | 综合第三人称训练场：移动、跳跃、交互/机关、NPC；基础场景当前不含战斗 | [plan.md](plan.md) |
| <a id="d06"></a>D06 | 运行窗口内有限调试编辑面板已采用ImGui.NET实现；查看状态、调参和设计保存重载已落地 | [plan.md](plan.md) |
| <a id="d07"></a>D07 | 借鉴 Piccolo 的对象组件组织，明确系统更新阶段；起步不采用完整 ECS | [architecture.md](architecture.md) |
| <a id="d08"></a>D08 | 固定模拟60Hz、绘制独立；V1已实现五补步上限、丢时、焦点/暂停清理、输入边沿消费和显示插值，具体合同见实际指南 | [architecture.md](architecture.md) |
| <a id="d09"></a>D09 | 控制器驱动世界位移，基础动画 in-place；相对镜头移动；首块控制器先验收平地/墙面/滑动/跳跃，复杂地形后续 | [architecture.md](architecture.md) |
| <a id="d10"></a>D10 | 世界 Y-up 右手；默认局部前 -Z、右 +X；米/秒；CPU 沿用 OpenTK 原生行向量、GLSL 列向量，集中记录映射 | [architecture.md](architecture.md) |
| <a id="d11"></a>D11 | 关卡资源共享至场景卸载；新场景准备成功后才替换旧场景 | [architecture.md](architecture.md) |
| <a id="d12"></a>D12 | 基础阶段保留 Engine/Sandbox 两项目：通用能力与演示/玩法分工；后续按实际需求再评估拆分 | [plan.md](plan.md)、[architecture.md](architecture.md) |
| <a id="d13"></a>D13 | 早期绘制采用环绕观察相机，合入 V1 后仍保留；不是角色相机，也不追认原 M1 的所有细节 | [m1-minimal-3d-draft-2026-10-03.md](archive/m1-minimal-3d-draft-2026-10-03.md) |
| <a id="d14"></a>D14 | 网络、动态 GI、GPU 几何后续开专题分支研究；先保留稳定基线，允许有理由且可验收的局部重构 | [plan.md](plan.md)、[architecture.md](architecture.md) |
| <a id="d15"></a>D15 | 保留可学习的基线/改进对照；V1已提供编辑态选择Forward/Deferred、下次Play生效，后续专题的即时切换/重载另定 | [architecture.md](architecture.md) |
| <a id="d16"></a>D16 | 架构文档/图、中文注释、代码—笔记—固定参考—简化方案—证据随进展维护；新对话依靠文件接续 | [learning-map.md](learning-map.md)、[AGENTS.md](../../AGENTS.md) |
| <a id="d17"></a>D17 | 正式光照先 Forward，再实际实现 Deferred 对比；基础绘制在 V1 内验证，原独立 M1 流程已被 D44 合并 | [rendering-roadmap.md](plans/rendering-roadmap.md) |
| <a id="d18"></a>D18 | 此前已选渲染模块目标含 PBR、基础阴影、IBL、天空盒、HDR/色调映射/FXAA；当前 V1 逐项归属以范围草案/明确决定为准，不要求全部渲染完成才接入玩法 | [rendering-roadmap.md](plans/rendering-roadmap.md) |
| <a id="d19"></a>D19 | 地形、天空/云、AO、雾等扩展专题，每类先做代表性实验，再按需整合进训练场 | [rendering-roadmap.md](plans/rendering-roadmap.md) |
| <a id="d20"></a>D20 | 主要输入采用glTF/GLB，SharpGLTF及受控支持子集已实现并部署素材；当前不读FBX，后续外部转换工具按素材任务选定 | [assets-scene-roadmap.md](plans/assets-scene-roadmap.md) |
| <a id="d21"></a>D21 | 基础版先保存设计场景；运行状态存档另设阶段，不将全部 Runtime 内存直接写回场景 | [assets-scene-roadmap.md](plans/assets-scene-roadmap.md) |
| <a id="d22"></a>D22 | 单层模板与明确实例覆盖已实现；嵌套/变体/完整Apply-Revert不在V1，后续再细化 | [assets-scene-roadmap.md](plans/assets-scene-roadmap.md) |
| <a id="d23"></a>D23 | 角色/物理长期含坡台、平台、推箱；D47收敛的V1平地/墙滑/跳跃/有限坡台已实现，平台/推箱后移 | [physics-character-roadmap.md](plans/physics-character-roadmap.md) |
| <a id="d24"></a>D24 | 自研角色规则配合JoltPhysicsSharp后端查询/刚体适配，已完成本机原生调用与行为验证 | [physics-character-roadmap.md](plans/physics-character-roadmap.md) |
| <a id="d25"></a>D25 | 布娃娃、布料/PBD/XPBD、破坏、车辆等进阶物理先独立代表实验，再按需整合 | [physics-character-roadmap.md](plans/physics-character-roadmap.md) |
| <a id="d26"></a>D26 | 基础动画终点为 Idle/Walk/Run 速度混合、跳跃状态机、平滑过渡和基础事件；Mask/Additive/脚部 IK 后续实验 | [animation-roadmap.md](plans/animation-roadmap.md) |
| <a id="d27"></a>D27 | 动画采用小型可配置状态机与混合运行时，先有状态/权重调试显示，不同时制作完整节点编辑器 | [animation-roadmap.md](plans/animation-roadmap.md) |
| <a id="d28"></a>D28 | V1已采用同一骨架相容动作，65关节模型及43个LINEAR clips已核验；重定向另做专题 | [animation-roadmap.md](plans/animation-roadmap.md) |
| <a id="d29"></a>D29 | 基础玩法先 C# 规则＋数据配置，Lua/可视化脚本留后续专题；数据重载不等于代码热更新 | [gameplay-ai-roadmap.md](plans/gameplay-ai-roadmap.md) |
| <a id="d30"></a>D30 | 基础 AI 先 FSM，再小型 BT 对照同一巡逻/跟随/搜索场景；与动画 FSM 职责分离 | [gameplay-ai-roadmap.md](plans/gameplay-ai-roadmap.md) |
| <a id="d31"></a>D31 | 自研受控平面网格A*与NPC控制器跟随已实现；NavMesh对照的库/范围仍待后续专题 | [gameplay-ai-roadmap.md](plans/gameplay-ai-roadmap.md) |
| <a id="d32"></a>D32 | 粒子先 CPU 生命周期/Billboard，再 GPU Compute 实际对比；不等于提前启动 Nanite/GPU 几何专题 | [particles-audio-roadmap.md](plans/particles-audio-roadmap.md) |
| <a id="d33"></a>D33 | 声音基础版做到 2D/3D 播放、方位距离、一次性/循环事件及基本播放管理；遮挡/混响等后续实验 | [particles-audio-roadmap.md](plans/particles-audio-roadmap.md) |
| <a id="d34"></a>D34 | V1 Listener随相机位置/朝向，OpenTK OpenAL＋OpenAL Soft后端已接入；后续可按试听问题评估调整 | [particles-audio-roadmap.md](plans/particles-audio-roadmap.md) |
| <a id="d35"></a>D35 | ImGui.NET有限场景编辑、约定Undo/Redo和PlayStop保护已实现；通用编辑器/Gizmo/视口拾取等另行分期 | [tools-debug-roadmap.md](plans/tools-debug-roadmap.md) |
| <a id="d36"></a>D36 | Play 使用当前内存设计数据，Stop 恢复设计预览并保留未保存编辑；运行变化不自动写回设计，也不等于运行存档 | [tools-debug-roadmap.md](plans/tools-debug-roadmap.md) |
| <a id="d37"></a>D37 | 用户关注初版成本后确认：初版仅 FPS/帧耗时，CPU/GPU 详细计时与相关计数按问题或对比实验需要加入；不作为初版验收要求，模块调试显示仍保留 | [tools-debug-roadmap.md](plans/tools-debug-roadmap.md) |
| <a id="d38"></a>D38 | 基础主线程按明确阶段更新与提交图形，同步准备场景；允许加载短暂停顿，不代表整个进程只有一条线程 | [core-architecture-roadmap.md](plans/core-architecture-roadmap.md)、[architecture.md](architecture.md) |
| <a id="d39"></a>D39 | 后续数据布局对比＋小型 ECS 实验，再按需局部接入；不预定主引擎整体迁移 | [core-architecture-roadmap.md](plans/core-architecture-roadmap.md) |
| <a id="d40"></a>D40 | 先用 .NET 并行库，再自研有限任务调度原型，先用成熟同步原语；Fiber/复杂无锁等后续细化，详细计时按实验需要加入 | [core-architecture-roadmap.md](plans/core-architecture-roadmap.md) |
| <a id="d41"></a>D41 | Deferred已纳入并实现V1，与Forward共用场景/资产；G-buffer/格式/切换合同见实际指南，其他渲染扩展后移 | [v1-baseline.md](plans/v1-baseline.md)、[rendering-roadmap.md](plans/rendering-roadmap.md) |
| <a id="d42"></a>D42 | V1有限设计操作Undo/Redo已实现，含拖动事务/草稿提交及真实保存基准；运行状态不纳入撤销 | [v1-baseline.md](plans/v1-baseline.md)、[tools-debug-roadmap.md](plans/tools-debug-roadmap.md) |
| <a id="d43"></a>D43 | 统一以“基础综合训练场 V1”为主名称，注明“用于面试展示”；旧面试 Demo/面试 V1 指同一版本；M1 后续按 D44 合入，不因名称统一批准其余范围 | [v1-baseline.md](plans/v1-baseline.md)、[plan.md](plan.md) |
| <a id="d44"></a>D44 | 最小 3D 里程碑正式合入基础综合训练场 V1，必要绘制/变换/资源验证保留为内部工作；旧草案归档，不再独立交付或审批，原参数不自动获批 | [v1-baseline.md](plans/v1-baseline.md#m1-integration-proposal) |
| <a id="d45"></a>D45 | 开始写代码前必须弹窗展示具体实施范围，得到用户明确同意后再开始；普通范围/选型回答和“推进下一步”不代替开工确认 | [AGENTS.md](../../AGENTS.md)、[v1-baseline.md](plans/v1-baseline.md) |
| <a id="d46"></a>D46 | V1 渲染含 Forward/Deferred、PBR、基础阴影、天空盒、HDR/色调映射/FXAA；IBL 后移，该次选择时具体参数待实施方案，后续已由V1实施与专题指南落实 | [v1-baseline.md](plans/v1-baseline.md) |
| <a id="d47"></a>D47 | V1 角色/物理含平地、墙滑、跳跃与有限坡台；平移平台/受控推箱后移，自研角色规则分工保持 | [v1-baseline.md](plans/v1-baseline.md) |
| <a id="d48"></a>D48 | V1有限创建/删除/变换/参数编辑、Undo/Redo、Play/Stop已落地并保留未保存设计，字段/事务以实际指南为准 | [v1-baseline.md](plans/v1-baseline.md) |
| <a id="d49"></a>D49 | V1实际采用SharpGLTF、StbImageSharp、JoltPhysicsSharp/Native、ImGui.NET、OpenTK OpenAL及OpenAL Soft；依赖/素材部署、功能验证与正式授权均已完成 | [dependencies.md](guides/dependencies.md) |
| <a id="d50"></a>D50 | Kenney Ogg保持离线转PCM16路线，7个选定音效已转换，另有1个生成循环测试音；不新增运行时Ogg解码库 | [v1-input-archives-check-2026-10-03.md](reviews/v1-input-archives-check-2026-10-03.md)、[dependencies.md](guides/dependencies.md) |
| <a id="d51"></a>D51 | 必要设计需完整连贯，普通细节由助手按授权决定，重大取舍集中询问；完整V1必要设计与D54开工授权已完成，新范围沿用此规则 | [AGENTS.md](../../AGENTS.md)、[v1-baseline.md](plans/v1-baseline.md) |
| <a id="d52"></a>D52 | 已实现受控父子层级、挂接/保存/Undo；当前Player/Npc为根单位缩放、父组限正统一缩放，角色/移动门下仅纯显示后代。设计期活动相机/动态刚体组件限制属扩展约束，当前DTO未提供这两类组件 | [v1-baseline.md](plans/v1-baseline.md) |
| <a id="d53"></a>D53 | GPT-6.1 Sol Ultra主实施与实施子任务，GPT-6 Astra Ultra关键评审/独立核查；两者均Ultra，质量优先，不为节省自行降档；主对话由界面选择，子任务显式指定 | [v1-progress.md](execution/v1-progress.md)、[v1-start-checkpoint-2026-10-03.md](reviews/v1-start-checkpoint-2026-10-03.md) |
| <a id="d54"></a>D54 | 正式弹窗明确同意完整V1连续实施；代码/Shader/设计数据/必要复制配置、既有素材/OpenAL/便携FFmpeg校验转换、构建运行修复与文档/快照已授权，同范围恢复不重问 | [v1-progress.md](execution/v1-progress.md)、[v1-implementation-review-2026-10-03.md](reviews/v1-implementation-review-2026-10-03.md) |

<a id="legacy-0001"></a>
## 原0001：开发基础、命名与分工

来源日期2026-10-01，命名/私有资料/Git分工于2026-10-02补充，2026-10-04核对阶段状态。以下保留当时背景和理由；基础选择仍有效，但实际参数及现行操作政策由相应正文维护。
原0002当时延后架构的阶段已结束，后续架构及V1接续它；基础选型不替代模块设计及运行证据。

### 背景

用户熟悉 C#/Unity，希望把 GAMES104 理论转化为独立引擎实践。短期服务 Gameplay 客户端求职，长期持续发展；初版预算有限，同时要求其他引擎模块有明确学习路线。

### 已确认的选择与理由

| 选择 | 理由及边界 |
| --- | --- |
| C# 独立引擎 | 利用现有语言基础；引擎不依赖 Unity 提供场景或运行循环。不能据此宣称具有 C++ 工程熟练度。 |
| .NET10 LTS + VS2026 | 用户在讨论支持周期和 IDE 兼容后确认；采用正式稳定版本。安装和具体补丁号需要实际核验。 |
| OpenGL + OpenTK 4.9.4 | OpenTK 提供窗口、输入与 API 绑定；渲染器、GLSL、Pass、资源组织仍由项目实现，不代替渲染流程学习。 |
| OpenGL 4.3 Core | 给后续 Compute Shader/SSBO 实验留下能力，首版从基础绘制学习；硬件/驱动需实际验证。 |
| 最小依赖 | 数学采用配套 OpenTK.Mathematics；JSON、初期输出使用 .NET 自带能力；其他库按模块引入。 |
| 单仓、分目录 | 一次提交可以对应代码、笔记修订和学习映射；仅同步约定的十份主笔记及配图，具体清单以计划为准。 |
| Piccolo main 固定快照 | 参考提交 `f5053707fed4d3f94d270a436fb0d3a8ae54e3e5` 的架构、算法和调用链；不作为本工程编译依赖。旧作业案例保留各自版本依据。 |
| 全模块学习、分阶段实现 | 原理理解、源码研读、工程实践分别记录；第一版不承诺实现课程全部系统。 |

### 命名补充（2026-10-02）

用户确认工程目录与解决方案文件基名统一为全小写 `g104engine`，展示名称仍为 G104Engine。命名讨论时曾以 `games104/g104engine/g104engine.sln` 为候选，约定若 VS 生成 `.slnx` 就保留真实格式并记录实际文件名。

同日落实结果：用户实际生成 `games104/g104engine/g104engine.slnx`，XML 与 .NET CLI 的空方案读取检查通过；后续使用该实际文件名，先前 `.sln` 仅是候选格式，不再作为创建目标。

这只是目录和文件命名调整，不确定软件实现架构，也不更改 Git 根 `games104`、仓库名 `games104-engine` 或起步项目 `G104.Engine` / `G104.Sandbox`。原架构归档保持原文。

### 本地个人记录范围（2026-10-02）

用户新增的 `games104/操作流程/` 仅在本地保存操作截图，不属于工程文档、学习映射或仓库资源。明确忽略该目录，不上传、引用或复制其中内容；其他设备复现工程不依赖这些截图。

### 环境与Git协作分工（已确认，持续有效）

用户希望亲自学习环境相关流程，因此把以下操作明确分配给用户：软件下载与安装、VS 解决方案和项目创建、NuGet 操作、GitHub 网页建仓、本地 Git 初始化和远程关联。

助手负责目录、文档、指引、检查与问题定位。用户完成一个阶段后，助手核对结果再推进下一阶段。环境修正先给可视化步骤；未经新委托不代为安装、生成工程或改包版本。

后续代码分工已补充为助手主要实现、用户学习/运行/调试/验收/讨论；2026-10-03完整V1正式开工授权已取得且实施完成，同范围恢复/修复继续有效。此代码授权不扩大环境安装、改包或Git写操作权限；当前首轮人工验收初步通过，学习尚未完成，见 [status.md](status.md#resume)。

当时约定每次Git提交前提供清单并交互确认，由用户执行、助手核对；以后即使委托助手也默认保留批次确认。将来若用户明确改变执行者或确认粒度，以协作流程中的新政策为准，并保留这里的历史依据。

阶段复查补充：用户曾报告确认框不可见，并在聊天中明确确认首次提交及推送；现行规则允许这种针对同一批范围的明确文字确认。不是取消提交确认，也不能把“未回复”当作批准。当前阅读顺序与执行边界以根 AGENTS.md、status.md 为准。

2026-10-02 分工补充：用户已创建公开空仓库 `zxpeng83/games104-engine`，本地 Git 改用用户自己的 Git Bash／Git GUI，暂不使用 VS Git 界面。工程编辑、NuGet 还原和调试继续使用 VS；Git 工具调整不改变发布范围或提交前确认规则。

### 对旧方案的处理

聊天早期的 C++17/CMake/GLFW/GLAD 组合、代码与笔记双仓、助手自动安装/初始化方案均已被上述选择替代。不要根据早期摘要恢复它们。

已有 VS2022、.NET9 或 Piccolo 的构建产物可以保留，不在本任务中清理。未创建的项目和未执行的检查不写成完成。

### 后续决策触发点

- 新增第三方库或更改技术基线时，说明收益、学习成本、兼容性和迁移影响。
- 所选 OpenGL 上下文无法创建时，先诊断驱动/会话/设备条件，再讨论替代方案；不静默降低版本。
- 目录、发布范围或执行分工改变时，更新相应主要维护位置并修正相关引用，避免多处复制政策。

<a id="legacy-0002"></a>
## 原0002：为何曾撤回前瞻架构

来源日期2026-10-01。以下“当前阶段”“尚无实现/提交”“架构占位”均描述当时状态。
该延后阶段已由后来架构收敛、D41–D54及完整V1正式实施接续。它用于解释撤回前瞻图的理由，不撤销后来的授权，也不要求同范围任务重新开工。

### 2026-10-01的决定（历史）

当前阶段的约束是已确认的环境与依赖、仓库/目录、协作和提交规则，以及最小环境验证。模块划分、类接口、数据模型、资源管理、帧更新顺序、线程调度和具体渲染组织均未定案。

`architecture.md` 改为占位与待讨论事项。原有前瞻模块图和帧流程保存为 [architecture-draft-2026-10-01.md](archive/architecture-draft-2026-10-01.md)，不作为后续执行依据。

基础搭建中拟使用的两个项目及 NuGet 包放置方式，仅服务可视化工程创建、引用关系和探针验证；不把它们冻结为长期软件架构。后续可按确认的设计调整。

### 原因

当前没有引擎实现，把草案画得过细容易使后续对话误认为设计已经确认。用户希望先完成可复现的开发基础，再围绕实际功能讨论架构。

### 当时的推进方式

基础阶段验收后，先确认下一个功能目标，再讨论该目标所需的最小设计、接口/数据流、取舍与验收方法；确认后实施。架构文档和图表随后逐步补充，并分别标识候选、已确认和已实现状态。

本决定不撤销 C#/.NET10/VS2026/OpenGL4.3/OpenTK4.9.4 技术基线、单仓范围、全模块学习目标、用户可视化环境操作分工，以及每次提交前的交互确认要求。

### 记录保留

由于尚无本项目 Git 提交历史，本次在改写架构正文前先保存完整原文，采用带“非执行依据”说明的文本归档，并记录原文件 SHA256。此归档是历史材料，不是新增架构方案。

<a id="document-redesign"></a>
## D55–D60：本次用户明确决定（2026-10-04）

这些决定来自本聊天的明确答复和最终完整实施指令；不把助手建议伪装成用户逐项选择。

| 编号 | 用户决定 | 实施与边界 |
|---|---|---|
| <a id="d55"></a>D55 | 让AI长期稳定维护现有引擎，整理规则、知识、任务、评审与交接 | 本批重构文档，不另建自主Agent服务 |
| <a id="d56"></a>D56 | 保全信息及学习意义，允许重写/合并，不硬性逐字保留 | 原信息到新位置逐项核对；决策、计划、下一步、取舍、失败和纠正不能因精简丢失 |
| <a id="d57"></a>D57 | 按专题组织，附开发时间索引 | 当前机制与设计演进分开；时间索引链接专题及原证据 |
| <a id="d58"></a>D58 | 后续可能委托Git、接CI、Skills及其他未知能力，要可扩展 | 当前操作政策集中维护；候选/可用/已授权分别判断，本轮不启用新权限 |
| <a id="d59"></a>D59 | 合并文件减少总数，并减少循环文档跳转 | 49份正式工程MD变43份，14旧页撤下、新增8份；任务主页面完整，来源可选 |
| <a id="d60"></a>D60 | 用户发送“PLEASE IMPLEMENT THIS PLAN”并附完整最终计划 | 执行模式下授权文档重构、快照和只读验收；不Git写、不改资料/代码/配置/依赖资产，不恢复D5 |

两次导航是本项目的可读性目标，不是Codex官方目录标准。D59取代讨论中“全部路径保留、另增6份到55份”的候选：旧页删除后修正当前引用，旧路径在迁移表可追溯。
用户明确保护整个资料目录；既有archive正文/快照不改，仅必要围栏外导航更新。此前Plan和只读讨论阶段均未修改；D60开启本轮实际执行。
本次实际进展、49份信息去向和验收结果在 [document-restructure.md](execution/document-restructure.md)，批准方案不等于验收已通过。
