# V1物理、角色、导航与玩法代码入口

更新：2026-10-03。本页描述已落地实现和实际自检；整体窗口效果、手动体验和其他模块验收由总体验收记录分别说明。

## 职责与数据

| 入口 | 当前职责 | 归属 |
| --- | --- | --- |
| [PhysicsWorld](../../src/G104.Engine/Physics/PhysicsWorld.cs) | Jolt系统、静态形状、射线/胶囊扫掠/重叠、碰撞停启、刚体烟测、释放 | 第三方后端集成与空间适配 |
| [KinematicCharacter](../../src/G104.Engine/Physics/KinematicCharacter.cs) | 去穿透、迭代墙滑、接地、跳跃、撞顶、有限坡度/台阶 | 自研角色规则 |
| [NavigationGrid](../../src/G104.Engine/Navigation/NavigationGrid.cs) | 地面/净空采样、网格A*、角点限制、路径简化、明确搜索结果 | 自研导航 |
| [TrainingSimulation](../../samples/G104.Sandbox/Gameplay/TrainingSimulation.cs) | 镜头相对输入、NPC FSM、按钮/门/目标、一次事实反馈 | 训练场专属玩法 |

世界采用Y-up右手坐标、默认前方-Z、米与秒。角色逻辑Position代表脚底；查询胶囊中心是脚底加Height/2。默认Radius=0.35m、Height=1.9m、StepHeight=0.35m、MaxSlopeDegrees=45°；角色尺寸由参数决定，玩家/NPC保持单位缩放和场景根节点。

V1设计参数未开放Skin字段，采用CharacterSettings的默认Skin=0.025m，故玩家/NPC必须满足`radius > 0.025m`且`height > 2 × radius`。SceneParameterRules从CharacterSettings读取同一默认Skin阈值；编辑候选、保存、重载及Play准备共用SceneValidator，越界半径在设计校验时明确拒绝。修复前`radius=0.02m`可通过设计校验，进入Play时被控制器明确拒绝；这是两个校验入口的契约不一致，不能记为已发生崩溃或坏存档。默认0.35m角色行为保持。

SceneGraph持有逻辑姿态。角色控制器提交世界位置，基础in-place动画读取实际Speed/Grounded/VerticalVelocity。显示插值由场景/窗口负责，不回写碰撞世界。角色胶囊只作为查询形状，不注册为可互推的刚体，所以角色彼此不推挤。

正式种子的六个`PBR sample`球只有渲染几何，没有Collider，角色可经过球的显示位置；不能把靠近它们时的异常直接归为球碰撞。右侧`Ramp`则保留旋转Box碰撞，低端朝-Z、高端朝+Z，可从低端上坡，高侧超过角色跨步上限时应阻挡。2026-10-03用户反馈的退出已在不创建PhysicsWorld的纯SceneGraph南向转身中复现：接近180°时旧矩阵分解误拒合法TRS，详见[本次修复记录](../reviews/v1-contact-exit-fix-2026-10-03.md)；本批没有修改Jolt查询边界或角色接触算法。

## 固定Jolt版本与矩阵边界

采用既有JoltPhysicsSharp 2.22.0和JoltPhysics.Native 1.1.0。NuGet元数据中的绑定源码提交为`77a5be2dd30d587c1981dfcaf15851f18041b39c`，原生joltc提交为`59f7d63ff7760981b771b6b161346fcc007f4dfd`。本次通过本机DLL反射核对实际公开签名，未新增包或升级SDK。

绑定的[Matrix4x4Extensions.ToJolt固定源码](https://github.com/amerkoleci/JoltPhysicsSharp/blob/77a5be2dd30d587c1981dfcaf15851f18041b39c/src/JoltPhysicsSharp/Matrix4x4Extensions.cs)内部会转置Matrix4x4；[NarrowPhaseQuery固定源码](https://github.com/amerkoleci/JoltPhysicsSharp/blob/77a5be2dd30d587c1981dfcaf15851f18041b39c/src/JoltPhysicsSharp/NarrowPhaseQuery.cs)的单精度形状查询调用它。该公开参数实际按列向量矩阵使用，不能直接传本项目行向量的Numerics.CreateTranslation结果。

PhysicsWorld.QueryTransform集中执行一次转置后再交给包装层。最初原生验收的墙面fraction和地面穿透深度异常暴露了这个问题；修正后原生射线、扫掠、重叠和角色检查通过。这是版本适配边界，不能随意删除或向SceneGraph增加第二次转置。BodyCreationSettings的位置/四元数参数不经过该矩阵路径。

静态形状先从SceneGraph.WorldMatrix分解位置/旋转/缩放：Collider.Size是局部几何尺寸，乘世界缩放一次；Collider.Center通过完整世界矩阵变换一次。Box使用半尺寸；旋转独立交给Jolt。静态Capsule要求X/Z直径相同且Height大于直径，不静默把椭圆尺寸当作圆胶囊。已有父组统一正缩放的限制由场景校验维护。

每个PhysicsWorld拥有自己的PhysicsSystem、Body、Shape、过滤器和JobSystem；全局Foundation以活动世界数计数。新旧场景可同时准备，释放一个世界不会提前关闭另一个世界的Jolt。Dispose明确注销/销毁刚体、释放查询与环境形状、系统/线程池/过滤器，最后在最后一个世界释放时Shutdown。

## 角色算法与公开查询

- `Raycast(origin,direction,maxDistance)`归一化direction，未命中返回null；命中包含ObjectId/Position/Normal/Distance。用于相机遮挡、NPC视线和地面采样。
- `SweepCapsule(feet,radius,height,displacement)`返回最早阻挡命中，剔除离开/平行接触；命中包含Fraction/Normal/Depth/ObjectId。
- `OverlapCapsule`暴露精确穿透深度；`CanOccupy`检查当前位置净空；`SetEnabled`让门的Body退出/重新进入同一个世界。
- KinematicCharacter.Tick每个固定步接受期望水平速度和一次Jump请求；CharacterState反馈实际位移速度、接地法线、Jumped/Landed/Blocked。

控制顺序：有限次重叠恢复→地面探测→接地跳跃或重力→水平扫掠/沿接触平面墙滑→可选跨步→垂直扫掠与撞顶→向下吸附/接地→再次穿透恢复。坡度用接触法线与Y轴的夹角限制；不可走坡当作水平墙处理，防止沿陡面投影获得向上速度。

跨步先确认上方胶囊通路，再尝试本步水平位移，前方一个半径探测可走支撑，限制抬升高度并检查最终净空。圆角尚未越过台阶棱时，当前位置下扫的边缘法线不能代表台阶顶面；前方探测解决这个有限场景。若探测开始已经穿透高台，则拒绝跨步，防止用零fraction把超高台误认为合法支撑。XZ位移始终只来自本步运动请求。

迭代次数、Skin和GroundSnap均有限；这不是任意复杂接触的工业级控制器。未使用Jolt CharacterVirtual/Character的完整控制器代替自研规则。V1没有移动平台携带、推箱、角色推挤或复杂挤压恢复；动态箱仅用于证明后端重力/接触/睡眠/清理能力，不作为训练场推箱玩法。

## 导航与NPC

NavigationGrid限定在一个XZ平面。默认范围[-12,12]、PlaneY=0、CellSize=0.65m，可由NPC的`navMinX/navMinZ/navMaxX/navMaxZ/navPlaneY/navCellSize`配置。地面射线检查平面高度和接近水平的法线，胶囊重叠检查半径/高度净空；没有用渲染AABB代替精确角色占用。

范围约束角色脚底的XZ位置，采用`Minimum ≤ position < Maximum`：最小边界包含、最大边界排除，整除和非整除尺寸遵守同一规则。网格维度仍向上取整，末格只覆盖实际剩余区间；例如`[0,2.1)`、CellSize=1的末格为`[2,2.1)`，格心是2.05，不是2.5。若末格只剩一个float ULP，中点可能舍入为最大边界，格心会限制到仍在区间内的可表示坐标。采样、可走格输出、邻居连接和路径端点均使用这个实际范围，2.1及2.8目标返回InvalidGoal，对应起点返回InvalidStart；直接CanTraverse也拒绝越界端点。

float除法向上取整还可能多分配零宽尾格：例如Minimum.X=-12、Maximum.X=7.5、CellSize=0.65，分配31列，但第30列的float下界已经等于7.5。Rebuild跳过这种没有实际区间的格子并置不可走，导入占用数据也作同样过滤。端点在真实范围内后，索引计算会按采样使用的同一float格下界校正，避免`BitDecrement(Maximum)`被减法/除法舍入到空尾格而误拒。小于配置范围的导入数组仍受自身覆盖上界限制，未提供的格子不会夹进最后一格。

物理网格容量的跨度与除法使用double后向上取整，实际格下界与查询坐标仍使用float。例如Minimum=-12、Maximum=BitIncrement(1)、CellSize=1时，float相减会把跨度舍入成13并遗漏非零末段`[1,BitIncrement(1))`；double容量计算分配14格，实际下界仍决定哪些格有可表示区间，与零宽尾格过滤共用一致范围。

A*使用八邻域和Octile启发式；对角连接要求两侧格均可走，连接与简化直线再做胶囊扫掠和格子检查。实际起点/目标还必须有胶囊净空，且能连到各自格心；同一采样格内存在细障碍时不能省略端点后仍报告成功。搜索有显式预算，返回Success/NoPath/InvalidStart/InvalidGoal/SearchBudgetExceeded、展开节点数和网格Revision。导航不支持多层、任意坡台、跳跃连接或移动平台；玩家坡台展示区与NPC平面区域分开。

TrainingSimulation的NPC FSM为Patrol→Follow→Search→Return。感知每0.2秒检查距离/水平视野/遮挡；只在看见玩家时更新最后观察位置，失去视线后按记忆时长进入搜索，搜索超时返回巡逻原点。Follow保持间隔，所有位移继续通过同一个KinematicCharacter；路径执行报告Following/Arrived/Blocked/NoPath。堵塞和网格Revision改变触发重规划，不把路径画线当作真实移动完成。

水平视野从SceneGraph当前逻辑世界矩阵变换局部`-Z`，投影XZ后归一化。静止NPC保留设计朝向，例如Yaw=180°朝向+Z；运动转向期间感知跟随当步已提交的Slerp旋转，不提前使用目标Yaw，也不读取显示插值。原有1.8m近距离免视角判断与距离/遮挡规则保留；远处若前方投影接近零，则没有可用水平视锥并拒绝该次可见性。

Tick先处理本步交互/物理，执行玩家意图与NPC上一固定步已提交的意图，再由更新后的世界状态生成NPC下一步意图。默认一个NPC，没有群体避让。NpcPath提供实际路径点供窗口绘制，NpcMode/NpcTask/NpcNavigationResult/NpcReason用于区分决策、搜索与碰撞结果。

## 机关事实与反馈

GameInput拆分Move/Sprint持续状态与Jump/Interact边沿，并接受本帧确定的CameraYaw。输入采集/多补步边沿消费属于窗口/Core；Simulation不重复生成键盘输入。

交互查找Button→TargetId对应Door，校验距离、视线和关闭占用。开门在同一模拟边界提交门的世界上移、碰撞退出和导航重建；关闭前暂启原位形状并检查玩家/NPC胶囊，门洞有人则恢复打开状态并反馈拒绝。V1门采用即时状态切换，没有渐进门动画；开门高度使用openHeight参数。

Events每Tick清空，随后只发布当步发生的事实。声音键为`footstep/jump/land/button/door/complete/denied`，每项含世界位置、颜色及是否产生粒子。表现系统在Tick后消费一次，不在每次Render重发。Goal支持Box触发体或水平radius/triggerRadius；有Door时要求门已经打开，完成状态锁存，完成反馈只产生一次。

## 真实验证与限制

本次接近球/坡回归另有[ContactApproachVerification](../../samples/G104.Sandbox/Gameplay/ContactApproachVerification.cs)的`--verify-contacts`纯CPU入口，以及[ContactWindowExercise](../../samples/G104.Sandbox/Tools/ContactWindowExercise.cs)的`--exercise-contacts --user-data-root <独立测试目录>`隐藏窗口入口。后者默认800帧，显式`--frames`不得低于800；与原`--exercise`240帧路线分开。窗口分九段正常Stop/Play，仅在原种子的内存副本中设置玩家起点/初始朝向，保留原NPC及全部几何/Collider，不保存路线设计。六球各70帧南向走/跑穿越，原Ramp上坡120帧、下坡120帧、高侧阻挡140帧；输入经过原InputBuffer、固定步、Simulation、动画、相机、声音与实际OpenGL绘制。断言包含实际越过球心、坡面支撑与高度、侧面阻挡、走跑步数、渲染帧及玩家/NPC每步精确单位缩放。源码与断言已经落地，实际执行结果以本批修复记录的对应新构建日志为准，不能用旧240帧通过替代。

2026-10-03本轮可玩性回归先加入检查、保留原实现，由主任务统一构建后分别运行四个入口。`.cache/execution/review-playability-before.log`记录Debug构建0警告0错误与四项真实FAIL：非整除尾格把2.8误当合法目标、设计Yaw=180°的静止NPC没有看见+Z玩家、转向途中提前用目标Yaw看见玩家、radius=0.02m设计校验未拒绝。随后才修改实现。

有限变更独立复核进一步指出首轮导航修复产生的两个float边界问题：普通[-12,7.5)/0.65配置的零宽尾格导致构造采样抛出异常；[-12,1)/0.65内的0.99999994被归入空尾格而误拒。第二轮仍先追加回归，再由主任务有效新Debug构建后运行，`.cache/execution/review-navigation-tail-before.log`记录导航汇总及两个专项入口均FAIL，NPC初始朝向、转向过渡、角色半径三个入口PASS。其后才修正零宽格采样和索引归格。

第二轮修复后，主任务统一Debug/Release构建均0警告0错误，当时六项`--review-baseline`与完整`--verify`均PASS，两配置各240帧集成exercise无GLerror，2jump/110moving。这个阶段尚未覆盖随后追加的容量案例。

最终有限复核确认仍有上述非零1ULP末格容量遗漏，第三轮先追加真实Jolt双轴回归后由主任务有效新Debug构建，`.cache/execution/review-navigation-capacity-before.log`记录导航汇总及容量专项FAIL，其余五项PASS；之后仅把物理构造的两轴容量计算改为double。同一Astra Ultra代理对这个容量差异作独立只读闭环，确认先前P2从源码关闭、与空尾格过滤及小导入数组规则兼容，有限变更未发现新增P1/P2；该结论与下述实际运行证据分别记录。

第三轮最终源码由主任务重新进行有效Debug/Release构建，均0警告0错误；两配置`--review-baseline`七项全部PASS，最新日志为`.cache/execution/review-playability-final-debug.log`、`review-playability-final-release.log`。两配置完整`--verify`全部PASS，日志为`review-verify-debug.log`、`review-verify-release.log`，覆盖Core、导航、物理、玩法、动画、默认场景集成及音频文件解析；两配置又各完成240帧集成exercise，GL错误为none、2jump/110moving，日志为`review-graphics-final-debug.log`、`review-graphics-final-release.log`。这些是容量修复后的新构建结果，前一轮六项PASS没有代替本轮验证。

本轮保持已选单层平面网格A*、有限坡台/Jolt分工、原NPC FSM与感知算法边界；没有加入NavMesh、多层路径、新AI算法、可调Skin或恢复D5。上述证据限本机已有依赖，人工手感与跨设备验收仍分别记录。

以下新增public入口均挂入原`--verify`，也可单独调用，避免一个失败遮住后续检查：

| 检查入口 | 本轮覆盖 |
| --- | --- |
| NavigationVerification.VerifyNonIntegralBounds | `[0,2.1)²`/CellSize=1的2.8越界目标/起点/直连拒绝；整除/非整除最大边界排除；2.05末格中心；仅一float ULP末格中点不会舍入到排除边界；所有格心/路径点在范围内；真实Jolt在有限地板上的末格采样与连通 |
| NavigationVerification.VerifyRoundedTailSampling | 真实Jolt有限地板、[-12,7.5)/0.65的构造与Rebuild跳过零宽尾格；BitDecrement(7.5)作实际起点/目标与直连仍合法 |
| NavigationVerification.VerifyInteriorMaximumRounding | [-12,1)/0.65的BitDecrement(1)双轴起点/目标与直连仍合法，exact Maximum排除；较小2×2导入数组不扩大到缺失格、其自身边界内最后可表示点仍接受 |
| NavigationVerification.VerifySingleUlpTailAllocation | 真实独立大地板、双轴[-12,BitIncrement(1))/1分配非零末格；内部点1与末格中心可作合法起点/目标并直连，exact Maximum仍拒绝 |
| GameplayVerification.VerifyNpcFacing | 静止Yaw=180°的+Z玩家进入Follow、-Z玩家保持Patrol；Yaw=0°的-Z对照；设计旋转与静止状态保持 |
| GameplayVerification.VerifyNpcFacingTransition | 窄视野下，启动巡逻后实际Slerp朝向尚未对准玩家时保持Patrol；之后进入当前视锥再Follow |
| GameplayVerification.VerifyCharacterRadiusContract | 玩家/NPC的0.02及恰等于默认Skin拒绝；刚超过阈值与默认0.35通过设计校验并实际准备Play |

以下保留V1首轮开发期间的独立模块证据。2026-10-03早期Debug x64 Engine/Sandbox `dotnet build --no-restore`成功；当时修复端点后的构建0错误、1项Rendering中GL.CullFace重载弃用警告（此前一轮0警告），后续首轮整体Debug/Release构建已达到0警告0错误。本模块没有引入新测试框架。以下独立方法经PowerShell加载当时构建DLL和对应Win-x64 Jolt原生DLL执行，均真实通过：

| 检查入口 | 实际覆盖 |
| --- | --- |
| [PhysicsVerification.Run](../../src/G104.Engine/Physics/PhysicsVerification.cs) | 原生地面ray/墙面高速shape cast/penetration；墙面阻挡与墙滑；接地跳跃与一次落地；0.30m台阶通过；初始穿透恢复；形状停启；动态箱重力/落地/休眠；双世界并存/清理；父旋转/统一缩放/center offset；撞顶；30°坡通过、15°上限拒绝同一坡、0.50m超高台拒绝 |
| [NavigationVerification.Run](../../src/G104.Engine/Navigation/NavigationVerification.cs) | 绕分隔墙、简化路径净空、断路报告、斜向角点拒绝、搜索预算报告；真实Jolt子格障碍导致实际起点/目标连接失败、目标自身穿透拒绝 |
| [GameplayVerification.Run](../../samples/G104.Sandbox/Gameplay/GameplayVerification.cs) | 开门画面/碰撞/网格同步、一次按钮事实、下一中性步无机关重发、实际穿门进入目标完成一次、NPC实际路径/角色状态、开门后走入门洞的关闭拒绝 |

另用倾斜30°Box射线核对世界Normal=(0,0.8660254,-0.50000006)、顶面Position.Y≈1.01547，未出现查询矩阵边界的二次转置。

这些是当前Windows x64/既有包的行为证据，不代表其他机器、跨平台逐位确定性、所有复杂网格、人工游玩或音频/图形效果已验收。早期整体`--verify --user-data-root .cache/v1-physics-gameplay-verification`曾在Core序列化File.Replace遇到沙箱拒绝；之后首轮Debug/Release整体自检已由主任务通过，见[实施复查](../reviews/v1-implementation-review-2026-10-03.md)。本轮新回归仍须对应新构建证据，旧首轮PASS不能代替修复后检查。

## 课程与参考映射

角色/查询对应第10–11节物理笔记的碰撞检测与角色控制；规则链、3C对应第15节；FSM/感知/A*对应第16节。现有完整索引见[learning-map](../learning-map.md)，保持自研规则、Jolt集成、后移专题的区别。

Piccolo固定参考为`f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`的物理查询/角色输入组织，既有来源核查见[design-reference-checks](../reviews/design-reference-checks-2026-10-03.md)。本项目没有把Piccolo目标重叠式移动当作完整扫掠控制，也没有声称Piccolo提供此FSM/A*闭环。
