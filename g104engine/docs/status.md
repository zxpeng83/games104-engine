# 当前状态与交接

更新：2026-10-04。用户确认运行指南第1–5项初步验收无问题，V1首轮人工验收通过（初步、非穷尽），后续Bug继续反馈修复。当前转入源码学习与调试练习；HEAD仍4be648e，本次修复/文档未提交。

## 当前事实

- 完整V1已获正式弹窗明确同意（call_gsRE59tQ8TeA5eqCjywBzR1M）；同范围继续修复/恢复不再询问开工。
- 长期Sol Ultra主实施、Astra Ultra评审；转向修复批按用户要求主要由Astra实施。当前UI修复由Sol实施、root验证、Astra有限复核，未发现阻塞P1/P2。
- UI主因是按下控件后误清ActiveId，现按WantCaptureMouse判断点击归属；同时逐事件接收鼠标，修复帧内短点击丢失。Debug/Release均0warn0err、新输入13PASS、旧UI11PASS、完整verify及240帧通过；见 [专项记录](reviews/v1-ui-mouse-fix-2026-10-04.md)。
- Engine/Sandbox模块、GLSL、28对象种子场景、模板、配置化动画/骨骼显示插值、素材/OpenAL与许可已落地；架构图、关键中文注释和代码—十份笔记映射已保存。
- 本轮修复文本草稿/拖动Undo/真实保存基准、导航范围及浮点尾格/NPC实际朝向/半径合同、GGX及half数值/HDR动态范围、粒子排序、UV覆盖及共享skin/rigid引用；见 [独立评审](reviews/v1-independent-review-2026-10-03.md)。
- 最新退出源于近180°矩阵往返误拒合法TRS；位置/旋转字段更新保存其余TRS，分解稳定提取且不放宽校验。六球无Collider，Physics生产未改；见 [专项修复](reviews/v1-contact-exit-fix-2026-10-03.md)。
- 最新Debug/Release各38/38专项、完整--verify和11项UI通过；各800帧九路实测六球经过、原Ramp上下/侧挡，668moving、无GLerror；原240帧仍通过。旧10GPU专项属于上一批shader证据。
- 最终Debug/Release x64均0警告0错误；两配置--verify全部PASS、7个具名玩法/导航入口PASS、11项原生UI输入及10项真实GPU检查PASS。
- 两配置全新隔离数据各240帧：Forward→Deferred、2jump/110moving、SaveLoad/UndoRedo、失败Play清理、未保存设计跨PlayStop、慢准备丢时和同GUID静态骨架空间通过，无GL错误。
- 本次冻结帧双管线RGB MAE.0621/255、最大22/255；独立double BRDF/source-over参考也通过，不以两管线相互接近替代正确性或性能测量。
- HDR颜色附件RGBA32F以保存正确高光，较原16F多8bytes/pixel；G-buffer仍原格式，反射颜色[0,1]与每灯HDR radiance≤1e12分域校验。
- FFmpeg9.0.2哈希及7段mono PCM16素材已验证；真实OpenAL Soft1.25.2后端/播放管理已有证据，声音体验随第5项初步验收通过，未推定全部声学/设备组合已覆盖。
- 初始/首轮/本批快照与日志精确路径见 [执行台账](execution/v1-progress.md)，未跟踪源码也备份；快照不是异地备份。
- 用户已确认指南1–5项整体初步体验通过，涵盖操控/玩法、渲染、编辑/保存、声音及窗口操作；不要求穷尽分支才记录通过。极端输入/专项组合、跨设备与学习掌握未由此证明；PCF弱斜面细斑及有限子集仍保留。

## 下一步

以当前已验收版本进入学习与调试：先理解Engine/Sandbox分工、启动和帧循环，再跟踪一次W输入与E交互，结合笔记解释空间/时间/生命周期。用户后续反馈Bug时按现有V1范围修复；本轮仅同步验收/接续文档，不新增功能或重跑既有测试。

1. 按既有Git流程由用户保存本轮UI修复与验收文档；尚未暂存/提交，不把本地快照当远程同步，学习可先继续。
2. 从 [实际架构/学习入口](guides/v1-architecture-and-learning.md) 的首个单元开始，结合 [learning-map](learning-map.md) 读源码、调参、画数据流；学习自测尚未完成。
3. 中断/新对话先读规则、状态、[交接](handoff.md) 与执行台账，核对实际Git/文件/日志/快照；本批实施代理已结束写入，构建/测试均已退出。

## 边界与同步

- d2e8d40为前次源码修复节点；本地HEAD/main/origin/main均为4be648ea2923c62ccd561dfc0fc3cc4c771f2265，含前次六份交接文档，接续开始工作区干净。本次实时远端连接失败未核实；新UI修复/验证/文档仅本机未提交，助手无Git写操作。
- SDK10.0.401、现有NuGet/锁文件及slnx未变；不系统安装/改PATH。测试全部使用工作区隔离数据，不修改真实用户设计。
- 用户打断后曾有旧代理interrupted未被send_message唤醒，已明确恢复并核对running后完成；新恢复规则已写入AGENTS/台账，不以消息投递代替执行。
- D5独立克隆/CI/第二设备仍暂缓；IBL、平台推箱、BTNavMesh、GPU粒子、网络/GI/GPU几何等后移专题不自动扩入V1。
- 原笔记/26处非发布链接、Piccolo、私有截图和上层Git未改；发布资产许可与OpenAL对应源码已在b91dfe4保存。
- 首轮追溯：[实施复查](reviews/v1-implementation-review-2026-10-03.md)；当前证据：[独立评审](reviews/v1-independent-review-2026-10-03.md)。
