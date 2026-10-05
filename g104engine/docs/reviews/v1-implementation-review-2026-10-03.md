# V1 首轮实现与本机验证记录

<a id="historical-evidence"></a>

日期：2026-10-03。用户已正式批准完整V1及Sol Ultra/Astra Ultra分工。此记录区分代码交付、助手本机验证和待用户验收；不是Git提交或发布证明。

**记录范围与后续（2026-10-04补记）：** 下文是首轮实现收尾时的证据与限制，当时用户体验尚未验收、HEAD仍cbce581，保持原记录。首轮实现随后保存为b91dfe4，评审及转向修复保存为d2e8d40，UI修复与首轮验收反馈保存为38cb85f。用户2026-10-04已确认运行指南第1–5项首轮人工验收通过（初步、非穷尽），当前转入学习/调试；未测专项、跨设备与学习掌握不由此证明。后续证据见 [v1-independent-review-2026-10-03.md](v1-independent-review-2026-10-03.md)、[v1-contact-exit-fix-2026-10-03.md](v1-contact-exit-fix-2026-10-03.md)、[v1-ui-mouse-fix-2026-10-04.md](v1-ui-mouse-fix-2026-10-04.md)，最新状态见 [status.md](../status.md)。

## 实际实现范围

Engine含固定时间步/输入、稳定GUID场景/父子TRS/保存、单层模板、有限编辑事务/UndoRedo、资源准备回滚；glTF/PNG导入、自研骨骼采样/混合/跳跃与事件、有限四状态JSON配置、独立上一/当前局部Pose显示插值与128容量UBO；Forward/Deferred、PBR/方向阴影/天空/HDR/Reinhard/FXAA；Jolt后端与自研角色移动、平面A*、CPU粒子、PCM16/OpenAL播放管理及ImGui/OpenTK适配。Sandbox组装训练场、NPC FSM、按钮—门—目标和窗口/相机/调试UI。

具体架构与文件入口见 [architecture.md](../architecture.md)，代码—笔记导航见 [learning-map.md](../learning-map.md)，用户运行与检查见 [v1-run-and-review.md](../guides/v1-run-and-review.md)。

## 本机证据

| 检查 | 结果与范围 |
| --- | --- |
| Debug/Release x64构建 | 现有依赖、`--no-restore --disable-build-servers -m:1`，两项目0警告0错误；SDK/依赖锁文件未改 |
| `--verify` | 固定步30/60/144Hz、输入边沿、TRS/层级/剪切拒绝、Undo身份/事务/失败准备、模板与原子保存；A*端点/断路、Jolt查询/墙滑/跳跃/坡台/去穿透/刚体清理、门/导航/目标事实通过 |
| 实际动画/资产 | 67节点/65关节/43动作、LINEAR/STEP/最短弧、非单位mesh空间palette、Jump/Fall/Land与事件去重；配置非默认映射/速度/时长/markers改变行为，共享定义防御复制、显示插值不推进逻辑/事件；PNG/normal/相对URI探针通过 |
| 默认场景集成 | 场景准备、玩家接地、NPC推进与场景清理通过；7转换音效及loop测试WAV为mono PCM16 |
| 实际音频 | Debug/Release OpenAL Soft实际1.25.2 device/context/buffer、2D/3D source、loop pause/resume/stop与cleanup通过；未声称最终听感验收 |
| 隐藏图形回归 | RTX5060Ti、OpenGL4.3.0 NVIDIA591.86；240帧真实Forward/Deferred/65骨骼走跑跳落地/粒子、1280×800 resize、保存/重载/UndoRedo、PlayStop；无GL错误，2jump/110moving（最终脚本批次） |
| 同帧双管线 | 冻结同一对象/骨骼/相机，UI前RGB读回；最终MAE0.0651/255、最大22/255（最后加入独立骨骼显示插值的批次）；有限G-buffer量化差异，不称完全一致或性能胜负 |
| 失败准备与设计保护 | 无效物理候选Play被拒，旧预览/设计保留、临时物理世界释放；坏资源编辑回滚；未保存设计与Undo历史跨PlayStop保持 |
| 慢准备时间 | 注入120ms准备，下一原始帧约139ms被丢弃、模拟步0，不补算加载时间 |
| 损坏JSON/模型引用 | 隔离JSON损坏和现存坏GLB两种启动回落种子；原JSON备份逐字节/SHA一致，新保存有效，并可继续编辑运行 |
| 图片检查 | 人工查看Forward/Deferred/Jump/Landed/Normals/Shadow/材质探针：完整角色、腾空落地、粒子/阴影、世界法线及checker可见；图像不替代体验验收 |

详细日志与PNG均在本机忽略的`g104engine/.cache/execution/`：build-debug/release、verify-debug/release、verify-audio/release、delivery-debug/release、graphics-configured-debug/release、corrupt-save-regression、bad-model-regression以及captures-final-release等。日志不是远程同步结果；证据以真实退出和文件为准。

## 实际修复与评审

Sol Ultra实施并用Astra Ultra做开工前、实码及最终独立复核。评审提出的资源编辑/Undo准备回滚、移动祖先碰撞后代、导航实际端点、动画重启、FXAA采样、坏存档备份和慢加载时间问题已修并做对应验证；最终聚焦审查未发现仍阻塞收尾的P1/P2。

本机运行另发现并修复：Jolt2.22包装查询矩阵内部转置需要边界适配；已选GLB的未消费UV1不应拒绝；OpenTK4.9.4单字符串ShaderSource使用UTF16长度会截断包含中文注释的UTF8源，改为显式UTF8字节长度；自有测试GLB的tangent手性按UV关系修正。初次失败保留在修复说明，不当作最终失败。

阴影开关同相机图确认重复细斑来自斜面PCF自阴影；PolygonOffset从1.5改为3后减轻，未见明显脚底整块脱影。仍保留弱斜面acne/有限bias取舍，详细见 [rendering-and-animation.md](../guides/rendering-and-animation.md)。

## 首轮实现结束时待用户验收与持续限制

- 当时待用户验收：真实键鼠手感、镜头、走跑跳/脚滑与动画过渡、2D/3D听感及声音用途/音量、编辑体验和面试展示。2026-10-04运行指南第1–5项对应的首轮体验已初步通过，未逐专项穷尽；面试展示脚本、项目讲解与学习自测仍待后续完成，具体范围见页首后续说明。
- 单张方向阴影和有限bias，弱斜面细斑仍可能出现；无CSM/点光阴影/IBL。现有flat normal素材不足以证明任意复杂法线图效果。
- 自研有限坡台控制、静态Box/Capsule后端与受控平面A*；无平台携带/推箱/角色互推、NavMesh/BT或工业级角色/导航保证。
- glTF仅约定未压缩TRS/skin与材质子集，不含morph/CUBICSPLINE/压缩或全部扩展；英文ImGui有限工具，不是完整商业编辑器。
- FPS/帧耗时是初期观察，隐藏脚本输入与截图不证明正式性能、全部DPI/失焦/最小化组合或真实人工输入完成验收。
- D5、CI/跨设备/独立克隆仍暂缓；本记录形成时未提交/推送/发布，也未新增升级NuGet/SDK或系统安装/PATH改动。后续Git保存不等于部署发布或D5完成。

开发与恢复状态见 [v1-progress.md](../execution/v1-progress.md)。本记录形成时安排继续验收/修复，当前接续为学习/调试及后续Bug反馈修复；同范围实施授权继续有效。

## 最后收尾核对

AN5四状态配置与独立骨骼显示插值加入后，Debug/Release重新构建0警告0错误，七项实际动画自检通过，包括自定义映射/速度响应/时长/事件标记、防御复制/双实例、Jump/Fall/Land、显示端点与重复显示不推进逻辑/事件。最后仅新增两个CPU自检案例后又重建并重跑两配置--verify通过，运行实现未改变，不重复无必要GPU回归。

最终delivery-debug/release图形各240帧均通过，同GUID角色/静态模型骨架端点与原始bind节点世界坐标核对通过；暂停恢复重置显示历史而不重置FSM/游标/待消费事件。两配置--smoke保留环境输出（.NET10.0.12/X64/OpenTK4.9.4），仍不代替GPU检查。

文档检查：41份Markdown、466条实际渲染本地链接与围栏/空白检查通过；历史快照四反引号围栏内部的原文链接不当作当前导航。资产manifest21条SHA-256逐项匹配。Git diff空白检查通过，SDK/slnx/两锁文件与原笔记未改，HEAD仍cbce581；当前实现未提交推送。
