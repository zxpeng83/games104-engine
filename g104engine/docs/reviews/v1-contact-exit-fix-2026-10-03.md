# V1 用户反馈：靠近球/斜坡时退出

<a id="historical-evidence"></a>

日期：2026-10-03。本批已完成复现、生产修复、双配置专项/集成验证及Astra Ultra有限独立复核；本记录形成时仍待用户在VS重新构建后亲自复试原路线。

**后续反馈与保存（2026-10-04补记）：** 用户已明确反馈“复试转身、球区移动、上下坡 我已经验过了，没问题”，本问题人工复试通过；随后鼠标修复也已确认，并确认运行指南第1–5项V1首轮人工验收通过（初步、非穷尽）。本批与前期评审修复已保存为d2e8d40，最新UI与验收批保存为38cb85f。下文“尚待确认”“正式验收尚未进行”等保留当时证据，不再是当前待办；未穷尽专项与学习掌握仍需按实际进展记录。当前接续见 [status.md](../status.md)。

## 用户反馈与保存基线

用户随手运行时，角色往南侧PBR球或右侧斜坡移动，接近时必现退出；朝墙走可正常碰撞。用户已撤回原退出码0截图，新图显示`G104.Engine.Scene.SceneValidationException`、进程退出1。正式验收尚未进行。

Git HEAD保持b91dfe4，上一轮独立评审30个改动/未跟踪路径全部保留。本批修改前核对上一批快照`g104engine/.cache/execution/snapshots/20261003-212054-v1-independent-review`中130文件与工作区SHA一致。测试全部使用内存克隆/工作区隔离目录，未改真实用户设计或原种子。

完整V1同范围修复授权继续有效。用户本批要求相关工作尽量使用GPT-6 Astra Ultra，因此复现、生产修复、真实窗口回归和有限独立复核均由该模型/Ultra执行；root统一集成与实测。本批分工不静默改写长期D53模型约定。

## 根因与先失败证据

六个`PBR sample`球没有Collider，只有材质展示几何；靠近它们不代表触发球碰撞。Ramp是原有旋转Box碰撞体。异常实际来自角色转身/移动后的场景变换提交。

旧`SceneGraph.SetWorldPosition/SetWorldRotation`将已存TRS反复矩阵化、分解再重构，一方面改变不应动的Scale字段，另一方面在接近180°旋转时矩阵→四元数数值误差被重建校验判为非TRS。直接异常是“Transform contains shear, reflection or a non-TRS projection”。最后合法Scale约(1.0000029,1,1.0000029)，尚在unitScale容差1e-4内，不能把根因写成缩放超过单位阈值。

有效Debug新构建0warn0err后，`contact-approaches-before.log`38案例为4PASS/34FAIL：12次真实SceneValidationException、22项仅精确Scale字段保持断言失败。纯SceneGraph南向更新第35次(frame34)就抛出同类异常，不创建PhysicsWorld；实际种子球区域、上坡和NPC转向也复现。原240帧路线主要初始朝向前进，未覆盖这些方向，原PASS不代表此用户路线已通过。

辅助PowerShell探针曾用错误小数类型转换得到无效正向结论，已撤回，不纳入上述证据；更正后±PI也复现异常。生产诊断与最终验回以真实C#入口和有效新构建日志为准。

## 实际修复与文件责任

| 内容 | 实际实现 | 负责者 |
| --- | --- | --- |
| 世界位置单字段更新 | 克隆原TRS，仅写Position；有父级用父world逆矩阵换算，Rotation/Scale原样保留 | Astra runtime修复 |
| 世界旋转单字段更新 | 根节点直接保存归一化输入；父级用inverse(parentWorldRotation)×desired，保持Position/Scale；Rebuild失败回滚 | Astra runtime修复 |
| 通用矩阵分解 | 去行缩放，double中间量，以trace/最大对角元素的稳定四分支提取；保留原1e-4重建校验，拒绝剪切/反射/非有限及提取溢出Scale | Astra runtime修复 |
| 角色转向输入 | Player/Npc已被强制为根节点，直接读存储的Quaternion，避免每步从world矩阵反求近180°旋转 | Astra runtime修复 |
| 有限Core回归 | 根/两层父链、非交换旋转与非统一叶缩放、字段保持、主轴/混合轴±180°、无效矩阵及拒绝后旧姿态保持 | Astra runtime修复 |
| 38例独立复现 | 只新增ContactApproachVerification；纯TRS、六球经过、Ramp上下/斜坡/侧挡、南北墙及600步多方向走跑 | Astra regression |
| 真实窗口回归 | 独立--exercise-contacts，默认/最少800帧及显式隔离数据；6球+Ramp上/下/侧九路，经原InputBuffer→Tick→动画/相机/音频/GL链 | Astra窗口任务 |
| 构建运行、状态/学习/快照 | root统一串行构建、隔离实测、结果核对；没有两个代理写同一生产文件 | root |

入口：[SceneGraph](../../src/G104.Engine/Scene/SceneGraph.cs)、[TransformMath](../../src/G104.Engine/Scene/TransformMath.cs)、[TrainingSimulation](../../samples/G104.Sandbox/Gameplay/TrainingSimulation.cs)、[Core自检](../../src/G104.Engine/Core/CoreSelfChecks.cs)、[CPU路线](../../samples/G104.Sandbox/Gameplay/ContactApproachVerification.cs)、[窗口路线](../../samples/G104.Sandbox/Tools/ContactWindowExercise.cs)。PhysicsWorld/Jolt查询与KinematicCharacter接触算法未修改；没有给展示球新增碰撞体或吞异常。

## 最终真实验证

日志位于忽略的`g104engine/.cache/execution/`，测试成功还要求实际运动与行为断言，而不是只没退出。

| 证据 | Debug/Release实际结果 |
| --- | --- |
| contact-build-debug/release.log | x64、既有依赖、--no-restore，0警告0错误 |
| contact-approaches-debug/release.log | 各38/38 PASS，failures=0；源seed未改、native worlds归还；22个Scale合同失败也闭环 |
| contact-verify-debug/release.log | 完整Core/导航/原生Jolt/Gameplay/动画/默认场景/WAV全部PASS，含本批数学/父链/拒绝回滚回归 |
| contact-ui-debug/release.log | 原生UI各11PASS，编辑/Undo/Saved保护保持 |
| contact-window-debug/release.log | 每配置800帧、九路PASS、668个移动步、无GL错误；此路线没有跳跃，不能把jump=0当作失败或已验证跳跃 |
| contact-legacy-debug/release.log | 原240帧保持，2jump/110moving、实际Forward→Deferred、同帧MAE.0621/255 max22、SaveLoad/UndoRedo/PlayStop/失败准备/同GUID静态骨架空间通过，无GL错误 |

两配置窗口路线：每个球最小水平距离.013m，实际穿过显示位置且未产生球接触；上坡终Y1.3475875、支撑96步，下坡回地面并支撑96步；高侧终X7.125、被挡115步。每段均走/跑且实际提交/绘制，Player/Npc Scale与各自作者值精确相同。旧240帧的120ms准备后下一raw帧分别141.31/139.99ms被丢弃、补步0。

独立Astra Ultra最后只读复核确认变换顺序与校验保持、38例失败会汇总抛出退出1、新窗口flag默认关闭并隔离旧测试；未发现本批有证据的剩余P1/P2。该评审读源码及root的Debug日志，没有自行构建/运行；Release由root随后实际执行。root已查看旧240帧最新Release落地截图，未见明显骨骼爆散。800帧contact路线只记录绘制计数，不输出旧截图序列，传capture-root不代表有contact截图。

## 本批协作状态纠正

用户打断后，两个旧代理处于interrupted。root误用send_message继续投递，以为任务仍运行，造成无效等待；它只投递消息，不会唤醒中断代理。当时只有打断后创建的回归代理与root真实完成失败复现，生产未修。用户指出后，root明确followup_task恢复两任务、list_agents核实running，然后才有上述生产代码与窗口回归落盘。恢复规程已写入AGENTS/执行台账。

`contact_approach_regression_astra`首先完成的是测试，不是生产修复；`contact_exit_runtime_astra`修生产Scene/Core/Simulation；`contact_failure_physics_astra`补窗口测试并交叉检查；回归代理再只读最终delta。代理完成、测试通过、生产落盘和最终验回分别记录，不能混称“已修复”。

## 修复收尾时的下一步与边界（历史）

用户在VS重新构建Debug/x64后，复试S向球区、D向坡侧、从坡低端上/下坡及转身走跑；无需重置或覆盖已有设计。本批自动验回已通过，实际用户路线尚待确认。球是展示对象，可以穿过；有限坡侧/台阶行为按原V1范围验收。

规则/架构/学习/交接与本地快照随本批收尾更新；新对话从status/handoff/execution台账核对实际Git、文件和日志。助手未暂存/提交/推送/发布、不安装/升级SDK或NuGet，不恢复D5/后移专题，不改原笔记/Piccolo/私有目录。
