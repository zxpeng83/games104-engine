# G104Engine 实现与 GAMES104 笔记映射

更新：2026-10-03。基础综合训练场 V1 已正式获准实施，实际模块主体已落地；本文更新代码入口和学习映射，保留十份原笔记。整体结构见 [architecture](architecture.md)，逐文件导航/实际流程见 [V1 架构与学习指南](guides/v1-architecture-and-learning.md)，真实验证与接续点见 [执行台账](execution/v1-progress.md)。原最小 3D 工作已并入 V1，旧草案只供追溯。

Debug x64 构建当前 0 警告、0 错误；完整 `--verify` 的 Core/Nav/Physics/Gameplay/Animation/默认场景/WAV 全部通过；`--verify-audio` 实际 OpenAL 上下文/2D/3D/暂停清理通过。修复 ShaderSource UTF8 长度后，本机图形 exercise 完成 240 帧、两种管线和有限编辑/保存/重载路径且无 GL 错误。**这些证据分别证明所测行为，不等于最终视觉、声音听感、操控学习或用户验收完成。** IBL、平台/推箱以及网络/GI/GPU 几何等后续目标仍保留。

## 1. 学习与工程进度分别记录

用户已学完 GAMES104 并整理十份笔记，这是学习背景，不自动代表已经结合本工程解释原理、独立定位代码或通过面试自测。三个维度持续分开：

- **理论：** 已学习/有笔记 → 按实际任务复习 → 能解释、画数据流并完成自测。
- **参考源码：** 局部核查 → 能说明固定版本具体路径和简化边界；不从文件名猜功能，不从课程范围猜 Piccolo 全部实现。
- **本工程：** 已有代码 → 具体 CPU/原生/GL 行为验证 → 用户实际验收；分别标自研、第三方集成和仅研究。

章节导航仍采用主笔记链接和精确大章/章节标题，不批量改原笔记或增加锚点。各笔记完整大章仍是长期学习目标，当前V1只实现各模块约定子集。本文记录已实现的学习入口；后续专题保留工程实践目标，“仅研究”不算替代实现。详细算法/API不重复粘贴方案，阅读对应实际指南和源文件。

## 2. 全模块当前状态

| 模块 | 理论/参考当前边界 | 本工程实际入口与证据 |
| --- | --- | --- |
| 渲染 | 已有笔记；固定 Piccolo Forward/Deferred/PBR/阴影/后处理局部核查，Vulkan细节需转换 | `TrainingRenderer/GpuResources` 与GLSL已实现；自动GL运行通过，画面质量/性能结论与用户视觉待验收；IBL等后移 |
| 动画 | 已有笔记；采样/层级/palette及Piccolo首Clip固定权重差异已核查 | `GltfModel/AnimationController`、65关节素材与材质探针CPU检查通过；GPU蒙皮/脚滑/过渡观感待视觉与用户 |
| 物理 | 已有笔记；Jolt查询/过滤/生命周期及固定包装层矩阵边界已核查 | `PhysicsWorld/KinematicCharacter`原生射线/扫掠/重叠、墙滑/跳跃/坡台/刚体与双世界清理通过；任意复杂接触与人工手感未验收 |
| 粒子 | 已有笔记；Piccolo Compute池/Billboard与同步限制局部核查 | `ParticleSystem`自研CPU生成/运动/死亡/512容量，renderer透明Billboard已有代码；效果/遮挡视觉待验收，GPU Compute未开始 |
| 声音 | 已有笔记；所查Piccolo未见完整音频链，使用OpenAL独立依据 | `PcmWave/AudioSystem`与七段离线PCM16已部署；WAV和真实OpenAL 2D/3D/暂停清理通过，方位听感/混音待用户 |
| 工具链与资产 | 已有笔记；固定Piccolo资产/ID/编辑消费者与模式/重载差异已核查 | SharpGLTF导入、严格JSON/资产路径、单层模板、受控父子树、有限编辑/100条历史已有代码；CPU历史/保存检查与自动GL编辑路径通过，人工UI流程待用户 |
| Gameplay/3C | 已有笔记；Piccolo输入/Motor/Camera及有限脚本路径局部核查 | `TrainingWindow/TrainingSimulation`镜头相对移动、按钮门目标、一次事实已实现并检查通过；镜头/角色操作待用户 |
| AI | 已有笔记；所查Piccolo无通用FSM/BT/感知/A*完整链 | `TrainingSimulation`巡逻/跟随/搜索/返回FSM与`NavigationGrid`平面A*实际跟随通过；BT/NavMesh/高级规划后续 |
| 网络 | 已有笔记；Piccolo所查路径未见完整链路，另补固定参考 | 未开始；传输/复制/预测/校正模型尚未选，保留独立延迟/丢包实验 |
| 核心架构 | 已有笔记；Piccolo串行主帧/双缓冲/对象组件/同步加载及后端线程局部核查 | `FixedStepClock/InputBuffer/SceneGraph/TrainingWindow`明确阶段已落地，30/60/144Hz及输入/层级/回滚CPU检查通过；布局/ECS/Job/Fiber专题未开始 |
| 动态GI/Lumen | 已有笔记；所查Piccolo未见完整Lumen链，另补参考 | 后续机制实践未开始，具体GI方法待定，不承诺复刻Lumen |
| GPU几何/Nanite | 已有笔记；Piccolo主几何链是CPU裁剪/直接提交，不是完整Nanite | 后续机制实践未开始，具体筛选/LOD/驻留方法待定，不承诺复刻Nanite |

求职方向只影响优先级。模块能运行观察、能定位入口、能解释数据/寿命/简化、能指出失败条件，才形成可用学习成果；当前没有把用户未完成的复习、自测或体验写成已完成。

## 3. 十份笔记与实际代码

### 3.1 渲染：从场景数据到像素

来源：[第04–07节渲染主笔记][render]；采用/格式/Pass细节见 [渲染与动画指南](guides/rendering-and-animation.md)。

- 实际入口：[TrainingRenderer](../src/G104.Engine/Rendering/TrainingRenderer.cs)、[GpuResources](../src/G104.Engine/Rendering/GpuResources.cs)、[PrimitiveMeshes](../src/G104.Engine/Rendering/PrimitiveMeshes.cs)、[RenderContracts](../src/G104.Engine/Rendering/RenderContracts.cs)、[GLSL目录](../assets/shaders)。`Program`默认进入正式训练场，`--smoke`只保留环境信息用途。
- 第一章“渲染基础设施—GPU、网格与裁剪”：对应primitive/index/VAO/VBO、GPU所有权与相机/模型变换；“1.5 可渲染物体的数据组织”对应RenderObject与材质输入。当前未实现完整可见性/遮挡/LOD系统，“1.6 可见性裁剪”仍可作为后续测量实验入口。
- “第四章：PBR材质系统 — 物理为王”：对应GGX、Schlick、Smith、导入baseColor/MR与线性/sRGB边界；弱固定环境项与天空背景不能当成IBL完成。
- “第六章：阴影技术演进 — 从Shadow Map到Virtual Shadow Map”：对应单盏方向光2048 Shadow Map、3×3 PCF和偏移；级联、点光阴影、虚拟阴影未实现。
- “第十四章：后处理 — “美颜相机””：对应HDR、曝光、Reinhard、一次Gamma和FXAA；“15.2 Forward Rendering”“15.7 串联渲染流程”对应实际两条管线/固定Pass。Render Graph/Frame Graph章节用于后续组织对比，当前没有通用Render Graph。
- 性质：OpenTK绑定/窗口和StbImageSharp解码为第三方集成，Pass/PBR/目标管理/颜色与矩阵适配自研。固定Piccolo路径参见第4节及来源核查；不照搬Vulkan投影或资源布局。
- 证据与学习验收：实际GL已完成240帧exercise、管线和调试入口；尚须看图核对阴影/深度/法线/贴图/粒子/蒙皮，人工操作与性能比较不由“无GL错误”证明。后续IBL、地形、天空/云、AO/雾等代表实验继续见 [渲染路线](plans/rendering-roadmap.md)。

### 3.2 动画：Clip、Pose、蒙皮与控制

来源：[第08–09节动画主笔记][animation]；空间/格式/事件详见 [渲染与动画指南](guides/rendering-and-animation.md)。

- 实际入口：[GltfModel](../src/G104.Engine/Assets/GltfModel.cs)、[GltfModelLoader](../src/G104.Engine/Assets/GltfModelLoader.cs)、[AnimationController](../src/G104.Engine/Animation/AnimationController.cs)、[AnimationVerification](../src/G104.Engine/Animation/AnimationVerification.cs)。
- “9. Local、Model/Component 与 World Space”“18. Skinning Matrix Palette 与 GPU Skinning”“19. 一个最小可验证的蒙皮求值流程”：对应完整默认节点TRS、local×parent、mesh相对palette和最终drawModel；65关节进入128矩阵std140 UBO，不能默认截到64。
- “20. Clip Sampling：渲染帧不等于动画关键帧”“22. 基础动画运行时管线”：对应二分查找、STEP/LINEAR、短弧归一化和未动画节点默认值；当前不支持CUBICSPLINE，显式拒绝。
- “36. 一维 Blend Space”“45. 动画树：Pose 的表达式系统”“48. Gameplay 与动画的职责边界”：对应Idle/Walk/Jog速度混合、同步步态相位、JumpStart/Loop/Land和平滑过渡。当前是有限配置/FSM，不是通用动画树编辑器；基础in-place不写角色世界位移。
- 性质：SharpGLTF读取格式，采样/层级/混合/FSM/事件与上传自研；实际素材67节点/65关节/43个LINEAR clips，Run映射Jog。Piccolo首Clip固定权重与本工程混合不同，不据其数据结构推断功能完成。
- 证据与学习验收：CPU真实素材、贴图探针、STEP/LINEAR/短弧、非单位mesh空间、palette和事件通过；GPU代码与自动绘制存在，绑定姿态画面、脚滑、过渡和跳跃节奏仍须视觉/用户。Two-Bone IK、Mask/Additive、重定向、表示/压缩/性能见 [动画路线](plans/animation-roadmap.md)，尚未实现。

### 3.3 物理：表示、检测、响应与游戏控制

来源：[第10–11节物理主笔记][physics]；算法/固定绑定矩阵与测试见 [物理与Gameplay指南](guides/physics-and-gameplay.md)。

- 实际入口：[PhysicsWorld](../src/G104.Engine/Physics/PhysicsWorld.cs)、[KinematicCharacter](../src/G104.Engine/Physics/KinematicCharacter.cs)、[PhysicsVerification](../src/G104.Engine/Physics/PhysicsVerification.cs)。
- “1. 物理世界的数据表示：Actor 与 Shape”“2. 力、冲量与数值积分：把连续运动塞进离散帧”：对应System/Body/Shape所有权、动态箱重力/睡眠/释放和固定步。角色重力/跳跃自研，不能混写为动画或动态刚体驱动。
- “4. Broad Phase”“5. Narrow Phase”“6. 从检测到碰撞到让世界稳定”：Jolt提供后端碰撞、ray/shape cast/overlap，项目适配GUID/过滤/尺寸/空间与寿命，没有自研完整物理解算器。
- “8. Character Controller：用反物理换取好操作”：对应feet胶囊、穿透恢复、有限墙滑、接地/撞顶、可走坡和有限台阶。父组变换/中心偏移只应用一次；当前包装层查询矩阵在QueryTransform边界单次转置。
- 性质：JoltPhysicsSharp/JoltPhysics.Native集成；角色规则与后端查询适配自研，未以Jolt现成完整角色控制器代替。固定Piccolo Controller目标overlap不是本工程扫掠墙滑的完整参考实现。
- 证据与学习验收：真实原生查询、高速墙阻挡/墙滑、跳跃落地/撞顶、初始重叠、30°坡/坡度拒绝、合法/超高台、动态箱、双世界、层级碰撞通过；人工手感、极端复杂接触仍未验收。第7章运行保障与第9–13章布娃娃、车辆、PBD/XPBD、破坏，以及平台/推箱各自继续见 [物理路线](plans/physics-character-roadmap.md)。

### 3.4 粒子与声音：生命周期、预算和事件

来源：[第12节粒子与声音主笔记][effects]。

- 实际入口：[ParticleSystem](../src/G104.Engine/Effects/ParticleSystem.cs)、[PcmWave](../src/G104.Engine/Audio/PcmWave.cs)、[AudioSystem](../src/G104.Engine/Audio/AudioSystem.cs)、[TrainingWindow.Feedback](../samples/G104.Sandbox/TrainingWindow.cs)；Billboard合成位于TrainingRenderer。
- 粒子“2. Particle、Emitter 与 System”“3. Spawn、Simulation 与 Lifetime”：对应Burst、速度/重力、Life/死亡回收、512容量、按剩余寿命淡出和场景清理。“6. GPU粒子系统：Pool、Alive/Dead List”保留下一阶段CPU→Compute对比，没有将当前List实现冒称GPU粒子。
- 声音“14. 三维声场的基本输入：Source 与 Listener”“16. Attenuation”“22. Voice Budget、延迟加载和关卡差量”：对应相机Listener、相对2D/世界3D、mono点声源、InverseDistanceClamped、共享Buffer/独立Voice和32源上限。当前启动同步载入七段WAV，没有完整延迟加载/差量音频流。
- 性质：CPU粒子与事实映射自研；OpenTK OpenAL/OpenAL Soft后端集成，RIFF分块PCM16读取/Voice管理自研。Kenney Ogg经已授权便携FFmpeg离线转换，FFmpeg不是运行时依赖；不新增运行时Ogg解码。
- 证据与学习验收：WAV解析及实际OpenAL context/buffer/2D/3D/暂停恢复/清理通过；粒子渲染链已有并参与GL运行，粒子排序/遮挡和声音远近左右听感待验收。传播/遮挡/混响、GPU池/排序/深度碰撞见 [粒子声音路线](plans/particles-audio-roadmap.md)。不把画面近似碰撞写回权威物理，不把玩家混音当AI听觉。

### 3.5 工具链与资产：让数据能够生产和复现

来源：[第13–14节工具链主笔记][tools]；实际接口见 [场景与编辑指南](guides/scene-and-editor.md) 和 [渲染动画指南](guides/rendering-and-animation.md)。

- 实际入口：[SceneData](../src/G104.Engine/Scene/SceneData.cs)、[SceneSerializer](../src/G104.Engine/Scene/SceneSerializer.cs)、[SceneGraph](../src/G104.Engine/Scene/SceneGraph.cs)、[TemplateCatalog](../src/G104.Engine/Scene/TemplateCatalog.cs)、[SceneEditor](../src/G104.Engine/Editor/SceneEditor.cs)、[EditorHistory](../src/G104.Engine/Editor/EditorHistory.cs)、[SceneEditorPanel](../samples/G104.Sandbox/Tools/SceneEditorPanel.cs)、[ImGuiController](../src/G104.Engine/Tools/ImGuiController.cs)。
- “13.3 数据保存/加载”“13.4 资产引用、实例与变种”“13.7 Schema”：对应schemaVersion=1、稳定GUID/引用校验、规范相对路径、原子保存/损坏保留、单层模板与白名单显式覆盖。设计场景与运行状态分别保存，当前无运行存档、资产数据库/版本迁移或嵌套变体。
- “13.6 Command/Undo”“13.8 In-game Tool/PIE”“14.4 属性编辑”：对应命令安全点、100条快照历史、拖动事务/Escape取消、删除外部引用拒绝、保持世界重挂接、dirty/Redo分支及Play克隆/Stop保留未保存设计。
- “14.1 交换/运行格式”“14.9 Reflection”：glTF读取与引擎CPU模型分层，ImGui控件使用有限显式字段命令；当前不是C++代码生成反射或通用C#反射编辑器。标准交换格式、Schema、实际运行消费者均需理解，不能用一次JSON库调用代替。
- 性质：格式解码与ImGui.NET为第三方，设计DTO/路径/场景图/命令/输入渲染后端为自研。Piccolo模式切换/磁盘重载与本工程保留内存设计不同；原homework01案例按其版本解释，不改原笔记。
- 证据与学习验收：CPU层级/非法字段/剪切/模板/历史/损坏保存检查通过，自动GL保存重载/UndoRedo路径通过；真实人工拖动/父子编辑/数据重建后重启保留仍应操作验收。完整工具、通用反射/导入管线和详细Profiler继续见 [资产场景路线](plans/assets-scene-roadmap.md)、[工具调试路线](plans/tools-debug-roadmap.md)。

### 3.6 Gameplay 与 AI：意图、执行和一次反馈

来源：[第15–17节Gameplay主笔记][gameplay]；现有流程与限制见 [物理与Gameplay指南](guides/physics-and-gameplay.md)。

- 实际入口：[TrainingSimulation](../samples/G104.Sandbox/Gameplay/TrainingSimulation.cs)、[TrainingWindow](../samples/G104.Sandbox/TrainingWindow.cs)、[NavigationGrid](../src/G104.Engine/Navigation/NavigationGrid.cs)、[GameplayVerification](../samples/G104.Sandbox/Gameplay/GameplayVerification.cs)、[NavigationVerification](../src/G104.Engine/Navigation/NavigationVerification.cs)。
- 第15节“3. Event：横向解耦对象与系统”“6. 3C”：对应GameInput、镜头相对方向、查询自研角色、显示相机、按钮请求→校验→门姿态/Body/网格→一次反馈。事件每Tick发布/消费，Render不重发；完成目标锁存一次。
- 第16节“7. A*”“13. FSM与HFSM”：对应平面八邻域Octile、角点/端点/路径简化净空、预算和Revision；NPC巡逻/跟随/搜索/返回、视距/FOV/视线/记忆、上一决定下一步执行。导航结果和角色受阻不同，FSM不直接绕过控制器写世界位置。
- 当前自研C#规则/数据配置、FSM/A*与感知适配，没有通用AI框架、Lua/可视化脚本或完整NavMesh。角色胶囊不注册互推Body，NPC受控平面区域与玩家坡台区分开。
- 证据与学习验收：机关画面数据/碰撞/网格同步、关闭占用拒绝、目标一次完成、NPC实际路径/控制器跟随通过；窗口人工游玩和镜头舒适度待验收。
- 后续第15节“4. Script：纵向开放规则、迭代与热更新”、第16节“14. Behavior Tree：把条件、计划片段与中断组织成树”、第17节“4. HTN：用领域知识把高层任务分解为可执行计划”“5. GOAP：把显式目标回归成低成本动作计划”“13. 离线训练环与运行时推理环必须分开”均保留代表实验与BT对比/NavMesh扩展，见 [Gameplay/AI路线](plans/gameplay-ai-roadmap.md)，不由本FSM宣称全部AI掌握或实现。

### 3.7 网络：多份世界与一致性

来源：[第18–19节网络主笔记][network]。

- “4. TCP、UDP与消息交付语义”“9. 快照、确定性锁步与状态同步”“10. 客户端预测/服务器校正/输入历史”“11. 插值、外推、Dead Reckoning、PVB与移动碰撞”“12. 命中检测与Lag Compensation”仍为下一专题入口。
- 代码尚未开始，传输库/网络模型/服务端架构未选择；当前离线固定步、显示插值和GUID不能被写成网络同步能力。
- 后续需可运行的延迟/丢包、权威状态、预测校正/远端插值实验，保留简单基线解释响应/误差/带宽。完整MMO分布式服务不列入当前V1承诺；Piccolo所查路径未提供完整链，另补固定参考。

### 3.8 核心架构：数据、时钟、安全点与寿命

来源：[第20节现代引擎架构主笔记][architecture]；实际主流程见 [V1指南](guides/v1-architecture-and-learning.md)。

- 实际入口：[FixedStepClock](../src/G104.Engine/Core/FixedStepClock.cs)、[InputBuffer](../src/G104.Engine/Core/InputBuffer.cs)、[SceneGraph](../src/G104.Engine/Scene/SceneGraph.cs)、[CoreSelfChecks](../src/G104.Engine/Core/CoreSelfChecks.cs)、[TrainingWindow](../samples/G104.Sandbox/TrainingWindow.cs)。
- “1. 性能目标与并发基础”“2. 引擎并行架构演进”：当前主线程明确阶段、同步资源准备、对象/后端所有权和图形上下文约束；Jolt内部JobSystem不是输入/Gameplay/动画/渲染的通用调度器。
- 当前自研固定累计器/输入边沿/显示插值/设计事务配合OpenTK回调；CPU检查30/60/144Hz、五补步/丢时、零步边沿/多步去重、场景层级/历史/回滚通过，默认场景原生集成与自动窗口链也通过。
- “4. 数据为什么成为性能瓶颈”“5. DOP与ECS”：后续AoS/SoA、4.8 False Sharing、5.1–5.5身份/查询/Chunk/结构变化、5.8–5.9范式边界仍未实现，需要实际测量后按需局部接入，不整体重写当前对象主线。
- “3. Fiber与Job System”、6.3–6.7分批/依赖/稳定交付/测量：先.NET并行库、再有限自研调度；Fiber对应3.3.6及3.4–3.12，不能用async/yield冒称有栈切换。初版只有FPS/帧间隔，不把它当GPU时间或已证明性能改进；路线见 [核心架构计划](plans/core-architecture-roadmap.md)。

### 3.9 动态 GI 与 Lumen：后续机制实践

来源：[第21节Lumen主笔记][lumen]。

- 入口：“3. RSM：把第一次照亮的表面变成次级光源”“5. 屏幕空间方法”“6. Lumen阶段A：SDF”“7. Lumen阶段B：Surface Cache”“13. 从光源到像素”。
- 当前无GI实现；固定弱环境项和天空不是动态间接光，具体机制/参考尚需独立选定，不承诺复刻Lumen。
- 后续用可运行简化场景比较直接/间接光、历史失效、离屏信息、动态更新和成本；图和源码研读不能替代用户要求的工程实践。

### 3.10 GPU 驱动几何与 Nanite：后续机制实践

来源：[第22节Nanite主笔记][nanite]。

- 入口：“1. 从CPU逐个提交，到GPU自己生成绘制工作”“2. Cluster与可见性：先把不必画的几何去掉”“3. Visibility Buffer：先决定看见谁，再计算它的材质”“4. Nanite离线构建：如何得到能混合细节的几何”“5. 运行时LOD选择：DAG、误差、BVH与并行遍历”“6. 光栅化与材质：如何高效地产生最终表面信息”“7. Virtual Shadow Map：海量几何如何投下精细阴影”“8. Streaming与压缩：让完整世界不必同时驻留”。其中8.4/8.5分别对应GPU量化和磁盘压缩。
- 当前为有限primitive/模型直接提交，没有GPU裁剪/多级LOD/流式几何；UBO蒙皮和GPU粒子目标均不能等同Nanite。
- 后续比较CPU/GPU筛选、精度/LOD、全部驻留/按需加载，观察场景规模、质量、带宽、驻留和成本；具体方法未选，不承诺复刻Nanite。

### 3.11 跨模块复习顺序

| 知识点 | 实际入口 | 笔记与已知边界 |
| --- | --- | --- |
| 一次输入如何变成画面 | TrainingWindow→InputBuffer→TrainingSimulation→KinematicCharacter→SceneGraph→RenderObject | 物理2.4/第8章、Gameplay 3C、动画in-place；实际CPU/原生链通过，人工操控待验收 |
| 三棵树与两类姿态 | 设计SceneGraph、glTF节点、skin关节；逻辑/显示TRS | 动画9/18/19、工具13.4；默认节点/root/mesh相对palette不可重复应用 |
| 行列向量与三种边界 | TransformMath、GpuResources、PhysicsWorld.QueryTransform | 动画9.1/9.3/11.8、渲染基础；GL上传与Jolt包装层适配不同，不能二次转置 |
| 请求、提交和一次事实 | Interact、World.SetEnabled、NavigationGrid.Rebuild、Events、Feedback | Gameplay第15节Event；关闭占用拒绝、一次目标事件已有行为检查 |
| 设计与运行如何隔离 | SceneSerializer.Clone、Play/Stop、EditorHistory、PrepareChange | 工具13.3/13.6/13.8；不是序列化句柄或从磁盘覆盖未保存设计 |
| 谁释放哪些资源 | Renderer.Prepare/Resize/Dispose、PhysicsWorld.Dispose、AudioSystem.Dispose、外层finally | 工具实例/共享资产和架构所有权；本机检查不覆盖全部失败注入或第二设备 |
| 后续布局/并行/网络/GI/GPU实验 | 当前基线及模块路线 | 架构第2–6章、网络第9–11章、GI第3/5–7/13章、GPU几何第1/2/5/8章；目标保留，代码未开始 |

## 4. Piccolo 固定版本与差异

主要参考：[固定提交 f5053707fed4d3f94d270a436fb0d3a8ae54e3e5](https://github.com/BoomingTech/Piccolo/tree/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5)。本地独立参考库不随新工程发布、未修改/构建；精确路径/行号与已有源码核查集中见 [来源核查](reviews/design-reference-checks-2026-10-03.md)。

| 固定参考入口 | 借鉴/核查内容 | 与本工程的区别 |
| --- | --- | --- |
| [engine.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/engine.cpp)、[level.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/level/level.cpp) | 串行主帧、对象/角色/物理职责 | 所查链无本工程固定累计补步器或完整ECS/Job；我们明确时序和输入去重 |
| [render_system.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_system.cpp)及passes | 渲染交换、Forward/Deferred、共享网格/材质和Pass | 本工程GL4.3、固定Pass和确定性Dispose；不照搬Vulkan投影、缓存键或空清理 |
| [skeleton.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/skeleton.cpp) | 层级/palette、首Clip固定权重 | 现有数据结构不证明完整混合；本工程STEP/LINEAR、默认节点、mesh相对palette/FSM自研 |
| [physics_scene.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.cpp)、[character_controller.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/controller/character_controller.cpp) | Jolt Update配置步长、Static创建与目标overlap | 不证明主循环累加器/完整动态玩法/扫掠墙滑；本工程查询与规则行为另验证 |
| [particle_pass.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/particle_pass.cpp) | 已有Compute模拟/池/Billboard及同步限制 | 不能沿用“只有billboard”的旧判断；本工程现阶段仍为CPU粒子 |
| [editor_ui.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor_ui.cpp)、[world_manager.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/world/world_manager.cpp) | 属性消费者、有限Lua/反射、模式切换和磁盘重载 | 不等于本工程100条事务Undo或保留未保存设计的Play/Stop |
| 所查音频/AI/网络/GI/GPU几何边界 | 未发现这些完整自有运行链 | 另选官方/论文/独立固定参考，不能拿Piccolo覆盖全部课程作为实现证据 |

部分笔记绑定homework01，保留其原版上下文，实际引用时记录原版和main对应关系/缺失。课程、参考与本工程三个进度不能合并；本次未改原笔记，未构建Piccolo，也未声称全模块逐行复刻。

## 5. 以后每项实现的记录方式

| 字段 | 必须记录的内容 |
| --- | --- |
| 知识点/任务 | 要理解和验证的具体行为，避免只写完成百分比 |
| 设计/实施状态 | 用户确认、助手授权定案、已有代码、实际通过或待验收；日期/理由 |
| 实际代码 | 存在的文件/类/方法/消费者链接；后续没写就保留未开始 |
| 笔记依据 | 十份主笔记与大章/精确标题，不为填表改原笔记 |
| 固定参考 | 仓库/规范/论文版本及具体路径，引用历史例子注明原版 |
| 采用/简化 | 为什么采用、范围、拒绝项和可观察限制 |
| 实现性质 | 自研/第三方集成/仅研究分别写；混合逐部分解释 |
| 实际证据 | 命令、平台、样例、日志/截图；CPU/原生/GL/听感/用户自测分开 |
| 接续 | 真实缺口、下一步和长期目标，不写计划等于已完成 |

调用关系变化时更新 [architecture](architecture.md) 和具体指南；当前执行/接续事实归 [执行台账](execution/v1-progress.md) 与 [status](status.md)。新对话按任务读取实际模块和相关笔记，不默认通读十份笔记。D5独立克隆、CI、第二设备仍暂缓，本机PASS不代替它们。

[render]: ../../资料/笔记博客汇总/第04-07节_渲染部分总结/AI梳理2/GAMES104_第4-7节_渲染部分知识体系总结.md
[animation]: ../../资料/笔记博客汇总/第08-09节_动画部分总结/GAMES104第08-09节_游戏动画系统完整梳理.md
[physics]: ../../资料/笔记博客汇总/第10-11节_物理系统部分总结/GAMES104_第10-11节_物理系统详细总结.md
[effects]: ../../资料/笔记博客汇总/第12节_粒子和声效部分总结/GAMES104_第12节_粒子与声音系统详细总结.md
[tools]: ../../资料/笔记博客汇总/第13-14节_引擎工具链/GAMES104_第13-14节_引擎工具链详细梳理.md
[gameplay]: ../../资料/笔记博客汇总/第15-17节_Gameplay玩法总结/GAMES104_第15-17节_Gameplay玩法系统完整梳理.md
[network]: ../../资料/笔记博客汇总/第18-19节_网络游戏部分总结/GAMES104_第18-19节_网络游戏架构完整学习讲义.md
[architecture]: ../../资料/笔记博客汇总/第20节_现代游戏引擎架构总结/GAMES104_第20节_现代游戏引擎架构_详细总结.md
[lumen]: ../../资料/笔记博客汇总/第21节_动态全局光照和Lumen总结/GAMES104_第21节_动态全局光照和Lumen知识体系总结.md
[nanite]: ../../资料/笔记博客汇总/第22节_GPU驱动的几何管线-nanite总结/GAMES104_第22节_GPU驱动的几何管线与Nanite_知识体系总结.md

## 配置与显示收尾索引

有限四状态动画定义已在assets/config/character-animation.json落实，实际非默认行为经AnimationVerification验证；上一/当前局部Pose的显示插值与SceneGraph共用alpha，mesh/palette/skeleton一致，显示不推进逻辑或事件。暂停/恢复仅ResetDisplayHistory，场景成功切换ResetAnimations。Debug/Release最终--verify及delivery图形回归通过，详细结果与用户待验收项见实施复查/执行台账；声音听感和真实操控不由自动日志代替。
