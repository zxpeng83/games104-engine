# G104Engine 实现与 GAMES104 笔记映射

本页把十份GAMES104主笔记与实际代码、固定参考及简化边界对应起来，并提供可独立阅读的源码学习单元。项目结构查 [architecture.md](architecture.md#overview)，当前进展查 [status.md](status.md)；历史测试是可选证据，不是阅读本页的前置。

理论学习、参考源码核查、本工程实现、用户体验与源码掌握分别记录。原最小3D工作已并入V1；后续模块目标保留，不能把当前有限子集当成完整课程工程目标。

<a id="first-input-lesson"></a>
## 第一单元：启动、一帧与一次W/Space输入

目标是追踪一次输入怎样变成角色状态、显示姿态与画面，并能在VS解释一个零步/多步例子。本单元给出全部必要背景；无需先拼读其他三份指南。普通运行从Edit开始，Play/F5建立独立运行世界，Stop保留内存设计；第一次学习先用现有场景，不改保存文件。工程使用现有Debug/x64输出与已锁依赖，不为阅读重新安装或还原；操作政策见 [agent-workflow.md](agent-workflow.md)。

### 最少前提

- Engine提供可复用的时钟、输入、场景、控制器、动画及渲染；Sandbox的Program/TrainingWindow把它们组装成训练场。
- 一次显示更新可以含零到五个模拟步。模拟dt恒为1/60秒；真实帧时间进入累计器，余量产生alpha，完整积压超过上限会丢弃。
- 按住W是持续Move，Space刚按下是Jump边沿；请求跳跃仍由控制器检查是否接地，不等于每次按键必然起跳。
- Look来自按住鼠标右键时的MouseState.Delta；未按右键时为零，UI捕获鼠标时InputBuffer清零该增量。每显示帧只ConsumeLook一次来更新相机yaw/pitch，本帧所有补步共用这个水平参考，不重复累加鼠标位移。
- 世界Y向上，默认前-Z，单位米/秒。角色Position是feet；胶囊查询中心比脚底高Height/2。动画in-place读取实际速度，不推动角色。
- 逻辑状态给碰撞/Gameplay；显示把previous/current局部姿态按同一alpha插值。绘制和重复插值不推进模拟或重复发布Jump事件。
- UI捕获键盘时角色不收该设备输入；失焦/暂停切换清旧输入与时钟。点击画面空白处再操作，避免在文本字段中测试W/Space。

### 调用与数据顺序

| 步骤 | 实际入口 | 观察什么、为什么 |
| --- | --- | --- |
| 1 启动 | [Program](../samples/G104.Sandbox/Program.cs)、[LaunchOptions](../samples/G104.Sandbox/LaunchOptions.cs) | 无参数选择TrainingWindow；验证参数分支与异常退出码属于启动组装 |
| 2 资源就绪 | [TrainingWindow.OnLoad](../samples/G104.Sandbox/TrainingWindow.cs) | 设计Graph、renderer/UI/audio准备；Play从内存设计Clone并准备独立Simulation/PhysicsWorld，全部成功才提交 |
| 3 本帧采集 | TrainingWindow.OnUpdateFrame | ProcessCommands安全点先处理UI请求；W/S生成Move.Y，Space生成jump，焦点/捕获决定能否进入角色输入 |
| 4 缓存 | [InputBuffer.Push/Consume](../src/G104.Engine/Core/InputBuffer.cs) | Move/Sprint更新按住态；Jump/Interact以OR积累待消费边沿；Look每显示帧另ConsumeLook一次来更新yaw |
| 5 固定步 | [FixedStepClock.Advance](../src/G104.Engine/Core/FixedStepClock.cs) | 每步先SceneGraph.CapturePrevious，再Consume；零步不会调用Consume，多步只首步获得旧Jump边沿 |
| 6 玩法与控制器 | [TrainingSimulation.Tick/SubmitCharacter](../samples/G104.Sandbox/Gameplay/TrainingSimulation.cs)、[KinematicCharacter.Tick](../src/G104.Engine/Physics/KinematicCharacter.cs) | Move经相机yaw映射为水平意图/速度，控制器ray/sweep/overlap决定合法位移、重力、接地与Jumped/Landed；SubmitCharacter只写SceneGraph的feet/旋转；随后BuildObjects通过Simulation.AnimationFor从CharacterState派生AnimationInputs |
| 7 表现 | TrainingWindow.Feedback、renderer.UpdateAnimations | Simulation每Tick仅发当步事实；声音/粒子消费一次；窗口BuildObjects取得AnimationFor派生输入，UpdateAnimations据实际Speed/Grounded/VerticalVelocity求Pose，Render不重发 |
| 8 显示 | [SceneGraph.InterpolatedWorldMatrix](../src/G104.Engine/Scene/SceneGraph.cs)、TrainingWindow.BuildObjects/OnRenderFrame | 返回同alpha显示变换与模型骨骼，渲染只读RenderObject；逻辑位置与屏幕位置允许因插值有一个步间差 |

建议先在OnUpdateFrame的采集后、InputBuffer.Consume、Simulation.Tick、KinematicCharacter.Tick、SubmitCharacter、BuildObjects各设一个断点，先逐个启用。观察Move、JumpPressed、step、Steps/Alpha、CharacterState.Position/Velocity/Grounded/Jumped，而不是同时展开所有GPU/原生对象。停在断点会改变真实帧时间，恢复后出现丢时/补步属于观测副作用，不能据此判断正常运行性能。

### 三个小例子

| 条件 | 预期消费与结果 |
| --- | --- |
| 零步：累计时间不足1/60秒时按下Space | Push记录Jump边沿，Advance不执行步，下一有效步才Consume；绘制可以发生，但没有新的逻辑Jump事实 |
| 多步：一显示更新执行三个固定步 | Move/Sprint三个步保持按住，待消费Jump只在第一步为true；控制器若接地可Jumped一次，之后读实际腾空状态；Look不乘三 |
| 失焦：按键或鼠标旧量尚未消费便切出窗口 | Clear/Reset清边沿/Look/累计时间，恢复丢弃旧delta并重置显示历史，Voice暂停/恢复；不会补算失焦时间或触发旧Jump |

时钟最多五补步、输入frameSeconds上限0.25秒；不足一步余量保留，超出时间及五步后完整积压记丢弃。窗口也限制显示delta并在切换/同步加载后丢旧帧，两层分工不同，不能把Clock统计当作所有系统耗时。键盘按住态仍由每帧采集；UI鼠标/文字/焦点的事件队列是另一条生产输入链。

### 本单元自测与下一次交互

1. 画Program→Window→InputBuffer/Clock→Simulation→Character→SceneGraph→RenderObject的简图，说出每份数据的写入者。
2. 解释W/Space/Look三种消费为何不同；预测零步和三步时Jump次数，并定位相应方法。
3. 用现有Debug运行和逐个断点观察一次平地移动/跳跃，解释feet、速度、Grounded与显示alpha，Stop后确认运行位置未写回设计。
4. 说明一次体验通过不能证明自己已能讲解源码；记录实际能解释的部分、仍不清楚的调用及观察结果。

完成后再跟一次E：输入Interact→Simulation查Button/TargetId→距离/视线/门洞占用校验→门世界姿态/Body开关/Nav Revision→一次事实→Feedback音效/粒子。失败关闭不提交关闭事实；Goal完成锁存一次。这段链与W/Space共用固定步和事实消费，不必先学通用事件总线。

可选深入：[architecture.md](architecture.md#frame-flow)、[scene-and-editor.md](guides/scene-and-editor.md#时钟与输入)、[physics-and-gameplay.md](guides/physics-and-gameplay.md#角色算法与公开查询)。对应笔记是第20节时钟/架构、第10–11节步长/角色控制、第15节3C/Event；下面保留所有模块的精确映射。

<a id="learning-progress"></a>
## 1. 学习与工程进度分别记录

用户已学完 GAMES104 并整理十份笔记，这是学习背景，不自动代表已经结合本工程解释原理、独立定位代码或通过面试自测。三个维度持续分开：

- **理论：** 已学习/有笔记 → 按实际任务复习 → 能解释、画数据流并完成自测。
- **参考源码：** 局部核查 → 能说明固定版本具体路径和简化边界；不从文件名猜功能，不从课程范围猜 Piccolo 全部实现。
- **本工程：** 已有代码 → 具体 CPU/原生/GL 行为验证 → 用户实际验收；分别标自研、第三方集成和仅研究。

章节导航仍采用主笔记链接和精确大章/章节标题，不批量改原笔记或增加锚点。各笔记完整大章仍是长期学习目标，当前V1只实现各模块约定子集。本文记录已实现的学习入口；后续专题保留工程实践目标，“仅研究”不算替代实现。详细算法/API不重复粘贴方案，阅读对应实际指南和源文件。

<a id="module-scope"></a>
## 2. 全模块实践与基线边界

| 模块 | 理论/固定参考边界 | 实际代码与有限子集 |
| --- | --- | --- |
| 渲染 | Forward/Deferred/PBR/阴影/后处理局部核查，Vulkan需转换 | TrainingRenderer/GpuResources/GLSL；固定Pass、无IBL或完整可见性/LOD |
| 动画 | 采样/层级/palette及首Clip固定权重差异 | GltfModel/AnimationController；同骨架65关节、速度混合与跳跃有限FSM |
| 物理 | Jolt查询/过滤/寿命及包装矩阵边界 | PhysicsWorld/KinematicCharacter；后端查询＋自研有限墙滑/跳跃/坡台 |
| 粒子 | Compute池/Billboard/同步限制局部核查 | 自研CPU ParticleSystem/Billboard，512容量，GPU专题未开始 |
| 声音 | 固定Piccolo未见完整音频链 | PcmWave/AudioSystem/OpenAL，7转码＋1测试WAV，非完整传播/混音 |
| 工具链与资产 | ID/实例/编辑消费者/模式重载差异 | 严格JSON/路径、单层模板/受控父子树/有限编辑/100历史 |
| Gameplay/3C | 输入/Motor/Camera/有限脚本局部核查 | TrainingWindow/Simulation；镜头相对移动、按钮门目标、一次事实 |
| AI | 所查参考无通用FSM/BT/感知/A*完整链 | 自研Patrol/Follow/Search/Return FSM和平面A*实际控制器跟随 |
| 网络 | 所查参考未见完整链 | 未实施；传输/复制/预测/校正待专题选型，保留延迟/丢包实验 |
| 核心架构 | 串行主帧/双缓冲/组件/同步加载及后端线程局部核查 | FixedStepClock/InputBuffer/SceneGraph/Window；布局/ECS/Job/Fiber待专题 |
| 动态GI/Lumen | 所查参考未见完整Lumen链 | 未实施，具体GI方法待定，不承诺复刻 |
| GPU几何/Nanite | 参考主链CPU裁剪/直接提交 | 未实施，筛选/LOD/驻留方法待定，不承诺复刻 |

求职方向只影响优先级。模块能运行观察、能定位入口、能解释数据/寿命/简化、能指出失败条件，才形成可用学习成果；源码复习、独立调试和面试自测以实际讲解/操作为依据，体验验收不能替代。

<a id="notes-by-module"></a>
## 3. 十份笔记与实际代码

### 3.1 渲染：从场景数据到像素

来源：[GAMES104_第4-7节_渲染部分知识体系总结.md][render]；采用/格式/Pass细节见 [rendering-and-animation.md](guides/rendering-and-animation.md)。

- 实际入口：[TrainingRenderer](../src/G104.Engine/Rendering/TrainingRenderer.cs)、[GpuResources](../src/G104.Engine/Rendering/GpuResources.cs)、[PrimitiveMeshes](../src/G104.Engine/Rendering/PrimitiveMeshes.cs)、[RenderContracts](../src/G104.Engine/Rendering/RenderContracts.cs)、[GLSL目录](../assets/shaders)。`Program`默认进入正式训练场，`--smoke`只保留环境信息用途。
- 第一章“渲染基础设施—GPU、网格与裁剪”：对应primitive/index/VAO/VBO、GPU所有权与相机/模型变换；“1.5 可渲染物体的数据组织”对应RenderObject与材质输入。当前未实现完整可见性/遮挡/LOD系统，“1.6 可见性裁剪”仍可作为后续测量实验入口。
- “第四章：PBR材质系统 — 物理为王”：对应GGX、Schlick、Smith、导入baseColor/MR与线性/sRGB边界；弱固定环境项与天空背景不能当成IBL完成。
- “第六章：阴影技术演进 — 从Shadow Map到Virtual Shadow Map”：对应单盏方向光2048 Shadow Map、3×3 PCF和偏移；级联、点光阴影、虚拟阴影未实现。
- “第十四章：后处理 — “美颜相机””：对应HDR、曝光、Reinhard、一次Gamma和FXAA；“15.2 Forward Rendering”“15.7 串联渲染流程”对应实际两条管线/固定Pass。Render Graph/Frame Graph章节用于后续组织对比，当前没有通用Render Graph。
- 性质：OpenTK绑定/窗口和StbImageSharp解码为第三方集成，Pass/PBR/目标管理/颜色与矩阵适配自研。固定Piccolo路径参见第4节及来源核查；不照搬Vulkan投影或资源布局。
- 实践与下一实验：后续IBL、地形、天空/云、AO/雾等代表实验继续见 [rendering-roadmap.md](plans/rendering-roadmap.md)。

### 3.2 动画：Clip、Pose、蒙皮与控制

来源：[GAMES104第08-09节_游戏动画系统完整梳理.md][animation]；空间/格式/事件详见 [rendering-and-animation.md](guides/rendering-and-animation.md)。

- 实际入口：[GltfModel](../src/G104.Engine/Assets/GltfModel.cs)、[GltfModelLoader](../src/G104.Engine/Assets/GltfModelLoader.cs)、[AnimationController](../src/G104.Engine/Animation/AnimationController.cs)、[AnimationVerification](../src/G104.Engine/Animation/AnimationVerification.cs)。
- “9. Local、Model/Component 与 World Space”“18. Skinning Matrix Palette 与 GPU Skinning”“19. 一个最小可验证的蒙皮求值流程”：对应完整默认节点TRS、local×parent、mesh相对palette和最终drawModel；65关节进入128矩阵std140 UBO，不能默认截到64。
- “20. Clip Sampling：渲染帧不等于动画关键帧”“22. 基础动画运行时管线”：对应二分查找、STEP/LINEAR、短弧归一化和未动画节点默认值；当前不支持CUBICSPLINE，显式拒绝。
- “36. 一维 Blend Space”“45. 动画树：Pose 的表达式系统”“48. Gameplay 与动画的职责边界”：对应Idle/Walk/Jog速度混合、同步步态相位、JumpStart/Loop/Land和平滑过渡。当前是有限配置/FSM，不是通用动画树编辑器；基础in-place不写角色世界位移。
- 性质：SharpGLTF读取格式，采样/层级/混合/FSM/事件与上传自研；实际素材67节点/65关节/43个LINEAR clips，Run映射Jog。Piccolo首Clip固定权重与本工程混合不同，不据其数据结构推断功能完成。
- 实践与下一实验：Two-Bone IK、Mask/Additive、重定向、表示/压缩/性能见 [animation-roadmap.md](plans/animation-roadmap.md)，尚未实现。

### 3.3 物理：表示、检测、响应与游戏控制

来源：[GAMES104_第10-11节_物理系统详细总结.md][physics]；算法/固定绑定矩阵与测试见 [physics-and-gameplay.md](guides/physics-and-gameplay.md)。

- 实际入口：[PhysicsWorld](../src/G104.Engine/Physics/PhysicsWorld.cs)、[KinematicCharacter](../src/G104.Engine/Physics/KinematicCharacter.cs)、[PhysicsVerification](../src/G104.Engine/Physics/PhysicsVerification.cs)。
- “1. 物理世界的数据表示：Actor 与 Shape”“2. 力、冲量与数值积分：把连续运动塞进离散帧”：对应System/Body/Shape所有权、动态箱重力/睡眠/释放和固定步。角色重力/跳跃自研，不能混写为动画或动态刚体驱动。
- “4. Broad Phase”“5. Narrow Phase”“6. 从检测到碰撞到让世界稳定”：Jolt提供后端碰撞、ray/shape cast/overlap，项目适配GUID/过滤/尺寸/空间与寿命，没有自研完整物理解算器。
- “8. Character Controller：用反物理换取好操作”：对应feet胶囊、穿透恢复、有限墙滑、接地/撞顶、可走坡和有限台阶。父组变换/中心偏移只应用一次；当前包装层查询矩阵在QueryTransform边界单次转置。
- 性质：JoltPhysicsSharp/JoltPhysics.Native集成；角色规则与后端查询适配自研，未以Jolt现成完整角色控制器代替。固定Piccolo Controller目标overlap不是本工程扫掠墙滑的完整参考实现。
- 实践与下一实验：第7章运行保障与第9–13章布娃娃、车辆、PBD/XPBD、破坏，以及平台/推箱各自继续见 [physics-character-roadmap.md](plans/physics-character-roadmap.md)。

### 3.4 粒子与声音：生命周期、预算和事件

来源：[GAMES104_第12节_粒子与声音系统详细总结.md][effects]。

- 实际入口：[ParticleSystem](../src/G104.Engine/Effects/ParticleSystem.cs)、[PcmWave](../src/G104.Engine/Audio/PcmWave.cs)、[AudioSystem](../src/G104.Engine/Audio/AudioSystem.cs)、[TrainingWindow.Feedback](../samples/G104.Sandbox/TrainingWindow.cs)；Billboard合成位于TrainingRenderer。
- 粒子“2. Particle、Emitter 与 System”“3. Spawn、Simulation 与 Lifetime”：对应Burst、速度/重力、Life/死亡回收、512容量、按剩余寿命淡出和场景清理。“6. GPU粒子系统：Pool、Alive/Dead List”保留下一阶段CPU→Compute对比，没有将当前List实现冒称GPU粒子。
- 声音“14. 三维声场的基本输入：Source 与 Listener”“16. Attenuation”“22. Voice Budget、延迟加载和关卡差量”：对应相机Listener、相对2D/世界3D、mono点声源、InverseDistanceClamped、共享Buffer/独立Voice和32源上限。当前启动枚举并同步载入assets/audio的8个WAV（7转码＋1生成测试音），没有完整延迟加载/差量音频流。
- 性质：CPU粒子与事实映射自研；OpenTK OpenAL/OpenAL Soft后端集成，RIFF分块PCM16读取/Voice管理自研。Kenney Ogg经已授权便携FFmpeg离线转换，FFmpeg不是运行时依赖；不新增运行时Ogg解码。
- 实践与下一实验：传播/遮挡/混响、GPU池/排序/深度碰撞见 [particles-audio-roadmap.md](plans/particles-audio-roadmap.md)。不把画面近似碰撞写回权威物理，不把玩家混音当AI听觉。

### 3.5 工具链与资产：让数据能够生产和复现

来源：[GAMES104_第13-14节_引擎工具链详细梳理.md][tools]；实际接口见 [scene-and-editor.md](guides/scene-and-editor.md) 和 [rendering-and-animation.md](guides/rendering-and-animation.md)。

- 实际入口：[SceneData](../src/G104.Engine/Scene/SceneData.cs)、[SceneSerializer](../src/G104.Engine/Scene/SceneSerializer.cs)、[SceneGraph](../src/G104.Engine/Scene/SceneGraph.cs)、[TemplateCatalog](../src/G104.Engine/Scene/TemplateCatalog.cs)、[SceneEditor](../src/G104.Engine/Editor/SceneEditor.cs)、[EditorHistory](../src/G104.Engine/Editor/EditorHistory.cs)、[SceneEditorPanel](../samples/G104.Sandbox/Tools/SceneEditorPanel.cs)、[ImGuiController](../src/G104.Engine/Tools/ImGuiController.cs)。
- “13.3 数据保存/加载”“13.4 资产引用、实例与变种”“13.7 Schema”：对应schemaVersion=1、稳定GUID/引用校验、规范相对路径、原子保存/损坏保留、单层模板与白名单显式覆盖。设计与运行状态分离，当前只保存设计，无运行存档、资产数据库/版本迁移或嵌套变体。
- “13.6 Command/Undo”“13.8 In-game Tool/PIE”“14.4 属性编辑”：对应命令安全点、100条快照历史、拖动事务/Escape取消、删除外部引用拒绝、保持世界重挂接、dirty/Redo分支及Play克隆/Stop保留未保存设计。
- “14.1 交换/运行格式”“14.9 Reflection”：glTF读取与引擎CPU模型分层，ImGui控件使用有限显式字段命令；当前不是C++代码生成反射或通用C#反射编辑器。标准交换格式、Schema、实际运行消费者均需理解，不能用一次JSON库调用代替。
- 性质：格式解码与ImGui.NET为第三方，设计DTO/路径/场景图/命令/输入渲染后端为自研。Piccolo模式切换/磁盘重载与本工程保留内存设计不同；原homework01案例按其版本解释，不改原笔记。
- 实践与下一实验：完整工具、通用反射/导入管线和详细Profiler继续见 [assets-scene-roadmap.md](plans/assets-scene-roadmap.md)、[tools-debug-roadmap.md](plans/tools-debug-roadmap.md)。

### 3.6 Gameplay 与 AI：意图、执行和一次反馈

来源：[GAMES104_第15-17节_Gameplay玩法系统完整梳理.md][gameplay]；现有流程与限制见 [physics-and-gameplay.md](guides/physics-and-gameplay.md)。

- 实际入口：[TrainingSimulation](../samples/G104.Sandbox/Gameplay/TrainingSimulation.cs)、[TrainingWindow](../samples/G104.Sandbox/TrainingWindow.cs)、[NavigationGrid](../src/G104.Engine/Navigation/NavigationGrid.cs)、[GameplayVerification](../samples/G104.Sandbox/Gameplay/GameplayVerification.cs)、[NavigationVerification](../src/G104.Engine/Navigation/NavigationVerification.cs)。
- 第15节“3. Event：横向解耦对象与系统”“6. 3C”：对应GameInput、镜头相对方向、查询自研角色、显示相机、按钮请求→校验→门姿态/Body/网格→一次反馈。事件每Tick发布/消费，Render不重发；完成目标锁存一次。
- 第16节“7. A*”“13. FSM与HFSM”：对应平面八邻域Octile、角点/端点/路径简化净空、预算和Revision；NPC巡逻/跟随/搜索/返回、视距/FOV/视线/记忆、上一决定下一步执行。导航结果和角色受阻不同，FSM不直接绕过控制器写世界位置。
- 当前自研C#规则/数据配置、FSM/A*与感知适配，没有通用AI框架、Lua/可视化脚本或完整NavMesh。角色胶囊不注册互推Body，NPC受控平面区域与玩家坡台区分开。
- 实践与下一实验：机关/NPC的完整分支与源码解释需结合实际学习，不由体验反馈推定已掌握。
- 后续第15节“4. Script：纵向开放规则、迭代与热更新”、第16节“14. Behavior Tree：把条件、计划片段与中断组织成树”、第17节“4. HTN：用领域知识把高层任务分解为可执行计划”“5. GOAP：把显式目标回归成低成本动作计划”“13. 离线训练环与运行时推理环必须分开”均保留代表实验与BT对比/NavMesh扩展，见 [gameplay-ai-roadmap.md](plans/gameplay-ai-roadmap.md)，不由本FSM宣称全部AI掌握或实现。

### 3.7 网络：多份世界与一致性

来源：[GAMES104_第18-19节_网络游戏架构完整学习讲义.md][network]。

- “4. TCP、UDP与消息交付语义”“9. 快照、确定性锁步与状态同步”“10. 客户端预测/服务器校正/输入历史”“11. 插值、外推、Dead Reckoning、PVB与移动碰撞”“12. 命中检测与Lag Compensation”仍为下一专题入口。
- 代码尚未开始，传输库/网络模型/服务端架构未选择；当前离线固定步、显示插值和GUID不能被写成网络同步能力。
- 后续需可运行的延迟/丢包、权威状态、预测校正/远端插值实验，保留简单基线解释响应/误差/带宽。完整MMO分布式服务不列入当前V1承诺；Piccolo所查路径未提供完整链，另补固定参考。

### 3.8 核心架构：数据、时钟、安全点与寿命

来源：[GAMES104_第20节_现代游戏引擎架构_详细总结.md][architecture]；实际主流程见 [architecture.md](architecture.md#frame-flow)。

- 实际入口：[FixedStepClock](../src/G104.Engine/Core/FixedStepClock.cs)、[InputBuffer](../src/G104.Engine/Core/InputBuffer.cs)、[SceneGraph](../src/G104.Engine/Scene/SceneGraph.cs)、[CoreSelfChecks](../src/G104.Engine/Core/CoreSelfChecks.cs)、[TrainingWindow](../samples/G104.Sandbox/TrainingWindow.cs)。
- “1. 性能目标与并发基础”“2. 引擎并行架构演进”：当前主线程明确阶段、同步资源准备、对象/后端所有权和图形上下文约束；Jolt内部JobSystem不是输入/Gameplay/动画/渲染的通用调度器。
- 当前自研固定累计器/输入边沿/显示插值/设计事务配合OpenTK回调；CPU检查30/60/144Hz、五补步/丢时、零步边沿/多步去重、场景层级/历史/回滚通过，默认场景原生集成与自动窗口链也通过。
- “4. 数据为什么成为性能瓶颈”“5. DOP与ECS”：后续AoS/SoA、4.8 False Sharing、5.1–5.5身份/查询/Chunk/结构变化、5.8–5.9范式边界仍未实现，需要实际测量后按需局部接入，不整体重写当前对象主线。
- “3. Fiber与Job System”、6.3–6.7分批/依赖/稳定交付/测量：先.NET并行库、再有限自研调度；Fiber对应3.3.6及3.4–3.12，不能用async/yield冒称有栈切换。初版只有FPS/帧间隔，不把它当GPU时间或已证明性能改进；路线见 [core-architecture-roadmap.md](plans/core-architecture-roadmap.md)。

### 3.9 动态 GI 与 Lumen：后续机制实践

来源：[GAMES104_第21节_动态全局光照和Lumen知识体系总结.md][lumen]。

- 入口：“3. RSM：把第一次照亮的表面变成次级光源”“5. 屏幕空间方法”“6. Lumen阶段A：SDF”“7. Lumen阶段B：Surface Cache”“13. 从光源到像素”。
- 当前无GI实现；固定弱环境项和天空不是动态间接光，具体机制/参考尚需独立选定，不承诺复刻Lumen。
- 后续用可运行简化场景比较直接/间接光、历史失效、离屏信息、动态更新和成本；图和源码研读不能替代用户要求的工程实践。

### 3.10 GPU 驱动几何与 Nanite：后续机制实践

来源：[GAMES104_第22节_GPU驱动的几何管线与Nanite_知识体系总结.md][nanite]。

- 入口：“1. 从CPU逐个提交，到GPU自己生成绘制工作”“2. Cluster与可见性：先把不必画的几何去掉”“3. Visibility Buffer：先决定看见谁，再计算它的材质”“4. Nanite离线构建：如何得到能混合细节的几何”“5. 运行时LOD选择：DAG、误差、BVH与并行遍历”“6. 光栅化与材质：如何高效地产生最终表面信息”“7. Virtual Shadow Map：海量几何如何投下精细阴影”“8. Streaming与压缩：让完整世界不必同时驻留”。其中8.4/8.5分别对应GPU量化和磁盘压缩。
- 当前为有限primitive/模型直接提交，没有GPU裁剪/多级LOD/流式几何；UBO蒙皮和GPU粒子目标均不能等同Nanite。
- 后续比较CPU/GPU筛选、精度/LOD、全部驻留/按需加载，观察场景规模、质量、带宽、驻留和成本；具体方法未选，不承诺复刻Nanite。

### 3.11 跨模块复习顺序

| 知识点 | 实际入口 | 笔记与已知边界 |
| --- | --- | --- |
| 一次输入如何变成画面 | TrainingWindow→InputBuffer→TrainingSimulation→KinematicCharacter→SceneGraph→RenderObject | 物理2.4/第8章、Gameplay 3C、动画in-place；CPU/原生链与源码讲解自测分别取证 |
| 三棵树与两类姿态 | 设计SceneGraph、glTF节点、skin关节；逻辑/显示TRS | 动画9/18/19、工具13.4；默认节点/root/mesh相对palette不可重复应用 |
| 行列向量与三种边界 | TransformMath、GpuResources、PhysicsWorld.QueryTransform | 动画9.1/9.3/11.8、渲染基础；GL上传与Jolt包装层适配不同，不能二次转置 |
| 请求、提交和一次事实 | Interact、World.SetEnabled、NavigationGrid.Rebuild、Events、Feedback | Gameplay第15节Event；关闭占用拒绝、一次目标事件已有行为检查 |
| 设计与运行如何隔离 | SceneSerializer.Clone、Play/Stop、EditorHistory、PrepareChange | 工具13.3/13.6/13.8；不是序列化句柄或从磁盘覆盖未保存设计 |
| 谁释放哪些资源 | Renderer.Prepare/Resize/Dispose、PhysicsWorld.Dispose、AudioSystem.Dispose、外层finally | 工具实例/共享资产和架构所有权；本机检查不覆盖全部失败注入或第二设备 |
| 后续布局/并行/网络/GI/GPU实验 | 当前基线及模块路线 | 架构第2–6章、网络第9–11章、GI第3/5–7/13章、GPU几何第1/2/5/8章；目标保留，代码未开始 |

<a id="fixed-references"></a>
## 4. Piccolo 固定版本与差异

主要参考：[固定提交 f5053707fed4d3f94d270a436fb0d3a8ae54e3e5](https://github.com/BoomingTech/Piccolo/tree/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5)。本地独立参考库不随新工程发布、未修改/构建；精确路径/行号与已有源码核查集中见 [design-reference-checks-2026-10-03.md](reviews/design-reference-checks-2026-10-03.md)。

| 固定参考入口 | 借鉴/核查内容 | 与本工程的区别 |
| --- | --- | --- |
| [engine.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/engine.cpp)、[level.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/level/level.cpp) | 串行主帧、对象/角色/物理职责 | 所查链无本工程固定累计补步器或完整ECS/Job；我们明确时序和输入去重 |
| [render_system.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/render_system.cpp)及passes | 渲染交换、Forward/Deferred、共享网格/材质和Pass | 本工程GL4.3、固定Pass和确定性Dispose；不照搬Vulkan投影、缓存键或空清理 |
| [skeleton.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/animation/skeleton.cpp) | 层级/palette、首Clip固定权重 | 现有数据结构不证明完整混合；本工程STEP/LINEAR、默认节点、mesh相对palette/FSM自研 |
| [physics_scene.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/physics/physics_scene.cpp)、[character_controller.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/controller/character_controller.cpp) | Jolt Update配置步长、Static创建与目标overlap | 不证明主循环累加器/完整动态玩法/扫掠墙滑；本工程查询与规则行为另验证 |
| [particle_pass.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/render/passes/particle_pass.cpp) | 已有Compute模拟/池/Billboard及同步限制 | 不能沿用“只有billboard”的旧判断；本工程现阶段仍为CPU粒子 |
| [editor_ui.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/editor/source/editor_ui.cpp)、[world_manager.cpp](https://github.com/BoomingTech/Piccolo/blob/f5053707fed4d3f94d270a436fb0d3a8ae54e3e5/engine/source/runtime/function/framework/world/world_manager.cpp) | 属性消费者、有限Lua/反射、模式切换和磁盘重载 | 不等于本工程100条事务Undo或保留未保存设计的Play/Stop |
| 所查音频/AI/网络/GI/GPU几何边界 | 未发现这些完整自有运行链 | 另选官方/论文/独立固定参考，不能拿Piccolo覆盖全部课程作为实现证据 |

部分笔记绑定homework01，保留原版上下文，引用时记录原版和main对应关系/缺失。课程、参考与本工程三个进度不能合并；原笔记不批量增加锚点，所查版本与自研实现不声称逐行复刻。

<a id="recording-template"></a>
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

调用关系变化时更新 [architecture.md](architecture.md) 和具体指南；当前执行/接续事实归 [v1-progress.md](execution/v1-progress.md) 与 [status.md](status.md)。新对话按任务读取实际模块和相关笔记，不默认通读十份笔记。D5独立克隆、CI、第二设备仍暂缓，本机PASS不代替它们。

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

<a id="learning-evidence"></a>
## 可选：2026-10-04迁移前的模块学习/验证证据基线

下表和各项摘录记录迁移前已保存的覆盖与缺口，不是现行任务播报；未来验回/体验更新只维护status和相应任务证据。精确日期、命令、日志在reviews/执行台账，阅读学习单元无需先打开它们。

<details>
<summary>历史模块表与各专题证据边界</summary>

| 模块 | 理论/参考当前边界 | 本工程实际入口与证据 |
| --- | --- | --- |
| 渲染 | 已有笔记；固定 Piccolo Forward/Deferred/PBR/阴影/后处理局部核查，Vulkan细节需转换 | `TrainingRenderer/GpuResources` 与GLSL已实现；自动GL运行通过，首轮画面检查初步通过；专项质量/性能未全面验收，IBL等后移 |
| 动画 | 已有笔记；采样/层级/palette及Piccolo首Clip固定权重差异已核查 | `GltfModel/AnimationController`、65关节素材与材质探针CPU检查通过；首轮人工验收总体通过，GPU蒙皮/脚滑/过渡观感未逐专项验收 |
| 物理 | 已有笔记；Jolt查询/过滤/生命周期及固定包装层矩阵边界已核查 | `PhysicsWorld/KinematicCharacter`原生射线/扫掠/重叠、墙滑/跳跃/坡台/刚体与双世界清理通过；首轮人工操控初步通过，极端复杂接触未验收 |
| 粒子 | 已有笔记；Piccolo Compute池/Billboard与同步限制局部核查 | `ParticleSystem`自研CPU生成/运动/死亡/512容量，renderer透明Billboard已有代码；首轮人工验收总体通过，效果/遮挡未逐专项验收，GPU Compute未开始 |
| 声音 | 已有笔记；所查Piccolo未见完整音频链，使用OpenAL独立依据 | `PcmWave/AudioSystem`与8个运行WAV（7个Kenney转码音效＋1个生成循环测试音）已部署；真实OpenAL检查与首轮试听初步通过，方位听感/混音未逐专项验收 |
| 工具链与资产 | 已有笔记；固定Piccolo资产/ID/编辑消费者与模式/重载差异已核查 | SharpGLTF导入、严格JSON/资产路径、单层模板、受控父子树、有限编辑/100条历史已有代码；CPU历史/保存检查与自动GL编辑路径通过，首轮人工编辑/保存流程初步通过，全部控件/失败分支未逐项验收 |
| Gameplay/3C | 已有笔记；Piccolo输入/Motor/Camera及有限脚本路径局部核查 | `TrainingWindow/TrainingSimulation`镜头相对移动、按钮门目标、一次事实已实现并检查通过；首轮镜头/角色操作初步通过，极端组合未穷尽 |
| AI | 已有笔记；所查Piccolo无通用FSM/BT/感知/A*完整链 | `TrainingSimulation`巡逻/跟随/搜索/返回FSM与`NavigationGrid`平面A*实际跟随通过；BT/NavMesh/高级规划后续 |
| 网络 | 已有笔记；Piccolo所查路径未见完整链路，另补固定参考 | 未开始；传输/复制/预测/校正模型尚未选，保留独立延迟/丢包实验 |
| 核心架构 | 已有笔记；Piccolo串行主帧/双缓冲/对象组件/同步加载及后端线程局部核查 | `FixedStepClock/InputBuffer/SceneGraph/TrainingWindow`明确阶段已落地，30/60/144Hz及输入/层级/回滚CPU检查通过；布局/ECS/Job/Fiber专题未开始 |
| 动态GI/Lumen | 已有笔记；所查Piccolo未见完整Lumen链，另补参考 | 后续机制实践未开始，具体GI方法待定，不承诺复刻Lumen |
| GPU几何/Nanite | 已有笔记；Piccolo主几何链是CPU裁剪/直接提交，不是完整Nanite | 后续机制实践未开始，具体筛选/LOD/驻留方法待定，不承诺复刻Nanite |

- 证据与学习验收：实际GL已完成240帧exercise、管线和调试入口；首轮画面检查初步通过，阴影/深度/法线/贴图/粒子/蒙皮仍可结合源码作专项学习核对，性能比较不由“无GL错误”证明。后续IBL、地形、天空/云、AO/雾等代表实验继续见 [rendering-roadmap.md](plans/rendering-roadmap.md)。

- 证据与学习验收：CPU真实素材、贴图探针、STEP/LINEAR/短弧、非单位mesh空间、palette和事件通过；GPU代码与自动绘制存在，历史首轮人工验收总体通过，绑定姿态画面、脚滑、过渡和跳跃节奏未逐专项验收。Two-Bone IK、Mask/Additive、重定向、表示/压缩/性能见 [animation-roadmap.md](plans/animation-roadmap.md)，尚未实现。

- 证据与学习验收：真实原生查询、高速墙阻挡/墙滑、跳跃落地/撞顶、初始重叠、30°坡/坡度拒绝、合法/超高台、动态箱、双世界、层级碰撞通过；首轮人工操控初步通过，极端复杂接触仍未验收。第7章运行保障与第9–13章布娃娃、车辆、PBD/XPBD、破坏，以及平台/推箱各自继续见 [physics-character-roadmap.md](plans/physics-character-roadmap.md)。

- 证据与学习验收：WAV解析及实际OpenAL context/buffer/2D/3D/暂停恢复/清理通过；粒子渲染链已有并参与GL运行，首轮视听体验初步通过，粒子排序/遮挡和声音远近左右听感未逐专项验收。传播/遮挡/混响、GPU池/排序/深度碰撞见 [particles-audio-roadmap.md](plans/particles-audio-roadmap.md)。不把画面近似碰撞写回权威物理，不把玩家混音当AI听觉。

- 证据与学习验收：CPU层级/非法字段/剪切/模板/历史/损坏保存检查通过，自动GL保存重载/UndoRedo路径通过；首轮人工编辑/保存/重启恢复流程初步通过，全部控件及损坏数据等失败分支未穷尽。完整工具、通用反射/导入管线和详细Profiler继续见 [assets-scene-roadmap.md](plans/assets-scene-roadmap.md)、[tools-debug-roadmap.md](plans/tools-debug-roadmap.md)。

- 证据与学习验收：机关画面数据/碰撞/网格同步、关闭占用拒绝、目标一次完成、NPC实际路径/控制器跟随通过；首轮人工游玩和镜头体验初步通过，未逐项验证全部机关/NPC分支，也不代表已掌握实现原理。

动画配置/显示收尾的历史记录：有限四状态动画定义已在assets/config/character-animation.json落实，实际非默认行为经AnimationVerification验证；上一/当前局部Pose的显示插值与SceneGraph共用alpha，mesh/palette/skeleton一致，显示不推进逻辑或事件。暂停/恢复仅ResetDisplayHistory，场景成功切换ResetAnimations。Debug/Release最终--verify及delivery图形回归通过，详细结果与剩余限制见实施复查/执行台账；声音听感和真实操控的首轮初步通过来自用户确认，不由自动日志代替。

</details>
