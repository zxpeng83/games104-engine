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

SceneGraph持有逻辑姿态。角色控制器提交世界位置，基础in-place动画读取实际Speed/Grounded/VerticalVelocity。显示插值由场景/窗口负责，不回写碰撞世界。角色胶囊只作为查询形状，不注册为可互推的刚体，所以角色彼此不推挤。

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

A*使用八邻域和Octile启发式；对角连接要求两侧格均可走，连接与简化直线再做胶囊扫掠和格子检查。实际起点/目标还必须有胶囊净空，且能连到各自格心；同一采样格内存在细障碍时不能省略端点后仍报告成功。搜索有显式预算，返回Success/NoPath/InvalidStart/InvalidGoal/SearchBudgetExceeded、展开节点数和网格Revision。导航不支持多层、任意坡台、跳跃连接或移动平台；玩家坡台展示区与NPC平面区域分开。

TrainingSimulation的NPC FSM为Patrol→Follow→Search→Return。感知每0.2秒检查距离/水平视野/遮挡；只在看见玩家时更新最后观察位置，失去视线后按记忆时长进入搜索，搜索超时返回巡逻原点。Follow保持间隔，所有位移继续通过同一个KinematicCharacter；路径执行报告Following/Arrived/Blocked/NoPath。堵塞和网格Revision改变触发重规划，不把路径画线当作真实移动完成。

Tick先处理本步交互/物理，执行玩家意图与NPC上一固定步已提交的意图，再由更新后的世界状态生成NPC下一步意图。默认一个NPC，没有群体避让。NpcPath提供实际路径点供窗口绘制，NpcMode/NpcTask/NpcNavigationResult/NpcReason用于区分决策、搜索与碰撞结果。

## 机关事实与反馈

GameInput拆分Move/Sprint持续状态与Jump/Interact边沿，并接受本帧确定的CameraYaw。输入采集/多补步边沿消费属于窗口/Core；Simulation不重复生成键盘输入。

交互查找Button→TargetId对应Door，校验距离、视线和关闭占用。开门在同一模拟边界提交门的世界上移、碰撞退出和导航重建；关闭前暂启原位形状并检查玩家/NPC胶囊，门洞有人则恢复打开状态并反馈拒绝。V1门采用即时状态切换，没有渐进门动画；开门高度使用openHeight参数。

Events每Tick清空，随后只发布当步发生的事实。声音键为`footstep/jump/land/button/door/complete/denied`，每项含世界位置、颜色及是否产生粒子。表现系统在Tick后消费一次，不在每次Render重发。Goal支持Box触发体或水平radius/triggerRadius；有Door时要求门已经打开，完成状态锁存，完成反馈只产生一次。

## 真实验证与限制

2026-10-03 Debug x64 Engine/Sandbox `dotnet build --no-restore`成功；最新修复端点后的构建0错误、1项Rendering中GL.CullFace重载弃用警告（此前一轮0警告）。本模块没有引入新测试框架。以下独立方法经PowerShell加载当前构建DLL和对应Win-x64 Jolt原生DLL执行，均真实通过：

| 检查入口 | 实际覆盖 |
| --- | --- |
| [PhysicsVerification.Run](../../src/G104.Engine/Physics/PhysicsVerification.cs) | 原生地面ray/墙面高速shape cast/penetration；墙面阻挡与墙滑；接地跳跃与一次落地；0.30m台阶通过；初始穿透恢复；形状停启；动态箱重力/落地/休眠；双世界并存/清理；父旋转/统一缩放/center offset；撞顶；30°坡通过、15°上限拒绝同一坡、0.50m超高台拒绝 |
| [NavigationVerification.Run](../../src/G104.Engine/Navigation/NavigationVerification.cs) | 绕分隔墙、简化路径净空、断路报告、斜向角点拒绝、搜索预算报告；真实Jolt子格障碍导致实际起点/目标连接失败、目标自身穿透拒绝 |
| [GameplayVerification.Run](../../samples/G104.Sandbox/Gameplay/GameplayVerification.cs) | 开门画面/碰撞/网格同步、一次按钮事实、下一中性步无机关重发、实际穿门进入目标完成一次、NPC实际路径/角色状态、开门后走入门洞的关闭拒绝 |

另用倾斜30°Box射线核对世界Normal=(0,0.8660254,-0.50000006)、顶面Position.Y≈1.01547，未出现查询矩阵边界的二次转置。

这些是当前Windows x64/既有包的行为证据，不代表其他机器、跨平台逐位确定性、所有复杂网格、人工游玩或音频/图形效果已验收。整体`--verify --user-data-root .cache/v1-physics-gameplay-verification`在Core序列化File.Replace遇到沙箱拒绝；该阻塞和后续整体复验由主任务记录，不能据此把独立模块PASS说成整个程序已通过。

## 课程与参考映射

角色/查询对应第10–11节物理笔记的碰撞检测与角色控制；规则链、3C对应第15节；FSM/感知/A*对应第16节。现有完整索引见[learning-map](../learning-map.md)，保持自研规则、Jolt集成、后移专题的区别。

Piccolo固定参考为`f5053707fed4d3f94d270a436fb0d3a8ae54e3e5`的物理查询/角色输入组织，既有来源核查见[design-reference-checks](../reviews/design-reference-checks-2026-10-03.md)。本项目没有把Piccolo目标重叠式移动当作完整扫掠控制，也没有声称Piccolo提供此FSM/A*闭环。
