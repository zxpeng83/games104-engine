# V1 独立验收评审与修复

日期：2026-10-03。用户提出希望使用Astra Ultra进一步review并修正；既有完整V1明确授权覆盖同范围修复。本轮已完成核查、失败复现、修复、双配置回归与最终变更复核，用户体验仍待验收。

## 保存基线与分工

用户自行提交推送`b91dfe4137686d9a5f962e40e10a3afe939cfe4c`：`feat: 实现基础综合训练场 V1（待验收）`，2026-10-03 19:41:08 +08:00。本轮起始只读核实HEAD/main/origin/main/GitHub main一致、工作区干净；随后修复尚未提交推送。

三个没有本轮实施历史上下文的GPT-6 Astra Ultra评审者分别核查运行/编辑/数据保护、渲染/资产/动画、物理/导航/Gameplay；GPT-6.1 Sol Ultra分域复现与修复，root统一构建/运行/文档。Astra进行最终有限变更复核及渲染数值独立核查，没有自行运行GPU；本页运行证据由root实际执行。未因额度剩余新增工业功能或无目标重构。

## 核实问题、修复与证据

| 问题 | 实际修复 | 失败及后验覆盖 |
| --- | --- | --- |
| 更换树选择丢失Name/模型路径/贴图路径草稿 | 按对象保存草稿和提交动作，更换选择、折叠或隐藏旧控件前提交；Escape取消草稿 | 4类真实cimgui输入失败→PASS |
| 失败拖动帧取消History事务，而Panel继续沿用owner | 只恢复失败帧，保留整段事务起点；继续拖动仍一条Undo，Escape恢复起点且不在同一按住段重开 | 2种真实拖动失败→PASS，Core合同同步覆盖 |
| Reset seed把种子当成磁盘Saved基准 | 编辑器保存基准来自实际有效存档；无有效存档为null；Reset保持原基准，实际Save才更新 | 真实隔离Save/Reset/Undo/Redo/PlayStop和11项UI检查PASS |
| ceil导航网格接受实际范围外起终点 | 先检查真实[min,max)，尾格取裁剪格心；格心舍入保护 | [0,2.1)中2.8拒绝，真实Jolt末格连接PASS |
| 浮点格界留下空尾格，Rebuild调用其CellCenter抛错 | 空尾格标不可行走并跳过物理采样；浮点归格按同一格下界校正，保留较小导入数组覆盖边界 | [-12,7.5)/.65构造、BitDecrement(1)内点、小导入数组检查先FAIL后PASS |
| float总跨度吞掉仍有合法位置的非零1ULP末格 | 容量用double相减/除法/ceil；实际位置与格界仍遵循float合同 | [-12,BitIncrement(1))/1双轴真实物理构造：14格、内点1/格心路径及exactMax拒绝，先FAIL后PASS |
| NPC FOV使用默认/目标Yaw而不是实际逻辑朝向 | 从当前SceneGraph逻辑world提取前向，XZ投影归一化 | 初始Yaw180与转向Slerp过程实际失败→PASS |
| 编辑接受radius=.02，Play固定Skin=.025拒绝 | 场景参数合同要求radius大于默认Skin，提前明确拒绝 | Player/Npc小于/等于/刚大于Skin，设计校验及实际准备PASS |
| GGX固定分母偏置压暗低roughness峰值 | 单位normal/half的cross恒等式避免相消，取消改变能量的固定偏置；保持roughness下限.045 | 独立double公式与双管线真实HDR读回，正对/斜光PASS |
| 正确GGX强光超过half最大65504 | HDR颜色附件改RGBA32F，G-buffer原格式保持 | HDR194061.797及LDR1有限；增加8bytes/pixel（1440×900约9.9MiB） |
| BaseColor>1在Forward保留、RGBA8 G-buffer截断 | 最终反射颜色[0,1]明确校验；灯光radiance另域，可为HDR；double预乘、有限非负且每灯RGB≤1e12 | BaseColor2先失败；模型factor.5×tint2合法、factor1×tint2拒绝；HDR灯色不改设计；极值拒绝/支持上限有限 |
| G-buffer半精度把roughness.045向下量化 | 共同BRDF入口保留.045下限；独立参考读实际G-buffer值 | 储存.0449829102仍输出正确.045峰值PASS |
| 近相反V/L的half被epsilon缩短，不是单位向量 | max绝对分量缩放后真正单位化；完全零sum输出0 | epsilon1e-8/1e-12/1e-20/0的生产BRDF探针与独立参考PASS |
| 粒子欧氏距离排序导致透明覆盖反转 | Billboard按view-space Z从远到近排序 | 两管线、两种枚举次序与独立source-over像素PASS |
| 无UV0模型的设计贴图覆盖绕过导入前置校验 | 保留源primitive HasUv0，按最终有效贴图校验Prepare/Render，错误含模型/节点/primitive | Normal/Base覆盖先被误接受，修复后拒绝并保留旧图像、动画及未消费Fall事件 |
| 同mesh被有skin及无skin节点引用时误报关节越界 | 仅有实际node.skin的引用消费皮肤属性；rigid引用忽略未使用JOINTS/WEIGHTS | 真实导入先FAIL后PASS；Khronos该情况是Warning，未扩展glTF子集 |
| 文档误称编辑态已有物理预览重建 | 准确记录编辑Prepare只准备渲染；Play按设计创建PhysicsWorld | 源码核对后修正文档，没有新增预览物理世界 |

工具栏直接点击Save会丢文本的最初猜测已撤回：真实普通鼠标按下先完成失活提交，Button释放才执行；正向回归保留。没有依据静态调用顺序误修工具栏。

## 先失败、再修复的记录

所有日志位于忽略的`g104engine/.cache/execution/`，摘要与源码检查入口在本仓库保留；未触碰真实用户设计。

- `review-playability-before.log`四项FAIL，第一批`review-playability-after.log`四PASS。最终复核新增`review-navigation-tail-before.log`（空尾格/内点舍入FAIL）及`review-navigation-capacity-before.log`（非零1ULP漏格FAIL），再修后`review-playability-final-debug.log`和`review-playability-final-release.log`各七个具名入口PASS；合并入口内包含细分用例，七入口不代表七个互不重复的案例。
- `review-ui-before.log`原生10项为6FAIL/4PASS；最终`review-ui-final-debug.log`及`review-ui-final-release.log`各11PASS，新增真实存档基准检查。
- `review-render-before.log`六项FAIL；第一批`review-render-after.log`七PASS。`review-render-extra-before.log`再复现颜色合同及roughness量化两FAIL；`review-render-grazing-before.log`前九PASS、half归一化FAIL。最终`review-render-final-debug.log`与`review-render-final-release.log`各十项PASS，包括不依赖用户起跳阈值的Fall历史保留用例。
- 一次构建撞上Visual helper写入而失败，随后旧DLL的输出不算后验；最终记录均来自成功的新构建。隔离文件File.Replace在普通沙箱受限，获准环境重跑通过，保留正确原子保存语义。

数值代表证据：roughness.045正对HDR19406.1797，独立double19406.1780572；原值1.02441406。近反向epsilon1e-8原GPU6.50519562，修后1.04083097e-6，独立参考1.04083082919e-6。粒子R/B修后约.46279246/.43291038，独立source-over约.46283174/.43285507。两个管线相互接近不能替代这些独立正确性检查。

## 最终回归和复核边界

- `review-build-debug.log`及`review-build-release.log`：既有依赖、--no-restore、x64、0警告0错误。SDK、slnx、包/锁文件未改变。
- `review-verify-debug.log`及`review-verify-release.log`：Core保存/历史、导航、原生Jolt、Gameplay、7组动画、默认训练场和PCM16 WAV全部PASS。
- `review-graphics-final-debug.log`及`review-graphics-final-release.log`：全新隔离数据各240帧，实际Forward→Deferred Play、2jump/110moving、SaveLoad/UndoRedo、失败Play保留有效预览、未保存设计跨PlayStop、同GUID角色/静态骨架空间及慢准备丢时通过，无GL错误。120ms注入后下一raw帧分别140.93/139.08ms被丢弃、模拟步0。
- 同帧冻结数据双管线RGB MAE为.0621/255、最大22/255（本场景/相机/RTX5060Ti）；不声称像素完全一致或Deferred更快。
- `captures-review-final-debug/`及`captures-review-final-release/`保留截图；root已看本批落地和贴图探针，未见明显骨骼爆散，贴图可见，已有PCF弱斜面自阴影细斑限制仍保留。
- Editor/Visual及导航最后double容量的Astra Ultra有限变更复核均闭环，未发现本轮剩余阻塞P1/P2；不等于证明所有输入无缺陷或人工体验已通过。声音实现未改，首轮实际OpenAL证据仍有效，完整窗口回归也正常创建1.25.2后端；试听由用户进行。

## 下一步与恢复

用户按 [运行与验收](../guides/v1-run-and-review.md) 体验操控、镜头、脚滑、NPC/机关、声音和编辑；同范围问题按证据继续修复。架构、参数合同、中文注释及学习对应在 [架构](../architecture.md)、[场景/工具](../guides/scene-and-editor.md)、[物理/Gameplay](../guides/physics-and-gameplay.md)、[渲染/动画](../guides/rendering-and-animation.md) 已同步。

本批源码/文档只在本机，用户决定下一次Git保存；助手未暂存、提交、推送或发布。恢复先读 [status](../status.md)、[handoff](../handoff.md) 与 [执行台账](../execution/v1-progress.md)，核对真实Git/文件/日志/快照。D5及所有后移专题保持暂缓，没有安装/升级依赖或修改源资料。
