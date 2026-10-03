# 基础综合训练场 V1：执行台账与中断恢复

更新：2026-10-03。此页记录实际执行状态；设计以 [实施方案](../plans/v1-implementation-draft.md)、[范围表](../plans/basic-training-ground-v1-draft.md) 为准，不另建一套版本规划。

## 当前接续点

**最新单元已完成：用户靠近球/Ramp退出反馈。** 新截图SceneValidationException/exit1，旧exit0撤回。Astra Ultra复现/生产修复/窗口路线及有限独立复核，root统一实际验回；双配置38/38、11UI、完整verify及800/240帧通过，用户原路线待VS重建后复试。见 [专项记录](../reviews/v1-contact-exit-fix-2026-10-03.md)。此前130文件快照为本批前受保护基线，下方保留分批证据。

- 工作区：`E:\game_study\games104`；分支`main`；用户已提交推送修复节点`d2e8d40691dd76a4637e35ed2905a2ccfaefe3d2`，本轮实查HEAD/main/跟踪/实时GitHub main一致，开始工作区干净。之后仅本地同步/交接文档更新未再次提交；源码与两批修复已远程保存，恢复仍先重新核实。
- **代码开工已获明确同意。** 2026-10-03用户答复“同意，按以上完整范围正式开工”（call_gsRE59tQ8TeA5eqCjywBzR1M）。完整C#/GLSL/设计数据/必要复制配置、既有素材/OpenAL、便携FFmpeg校验转换、构建运行修复与文档/快照已授权；模型均Ultra，Git/SDK/NuGet/系统安装/D5边界保持。
- 实际Engine/Sandbox正式模块、GLSL、设计/模板/动画配置、素材和OpenAL已落地；首轮V1为b91dfe4，独立评审及转向退出修复已保存至d2e8d40。来源及分工见 [独立评审](../reviews/v1-independent-review-2026-10-03.md) 和专项记录。
- 本批修复及最终有限复核已闭环：Debug/Release最终构建0warn0err、完整行为自检全PASS、11项原生UI/10项GPU读回各PASS；空尾格/内点舍入/非零1ULP容量均先失败后修，七个具名玩法/导航入口PASS。
- root统一完成全新隔离设计各240帧集成，实际Forward→Deferred Play、2jump110move、无GLerror；MAE.0621/max22。全部实现代理结束写入，构建/测试已退出；若中断先核对实际源码与最新review日志，不重复启动写入者。
- 用户统一手感、试听、编辑体验和学习验收仍待进行。四份原输入 ZIP/FFmpeg及各批日志快照位于忽略缓存，新克隆不会自动获得；已发布运行素材不依赖这些工具。

## 连续执行约定

用户希望批准后尽量连续完成完整 V1，离开期间由助手处理普通实现选择与修复，回来后统一验收、学习代码和架构资料。该连续执行安排已随正式开工弹窗批准。

- 按依赖分批实现、验证和集成；可运行预览用于自查和留证，不自动成为等待用户回复的闸门。跨模块公共契约由主任务统一维护。
- 保持范围、关键接口和学习要求；不为赶进度默默删功能、替换技术路线，或把未运行内容称为通过。
- 重大范围/架构/依赖/成本变化、可能丢失用户数据的操作、新权限需求或确实无法推进的阻塞才请求用户处理。仅受阻部分暂停时，可继续不依赖它的已授权工作。
- 图像、声音、手感和学习材料是否达到用户要求仍需最终人工验收；能完成的自动与运行检查先完成，未能验证的逐项列明。
- 连续任务不保证单个回复完成，不保证断网/额度耗尽/进程关闭后自动继续。本地执行依赖机器、电源和会话可用；新对话恢复依靠已落盘文件及实际工程。

## 内部工作单元

下表不是额外版本或逐项审批；可以按依赖交错实施。状态只使用“未开始 / 进行中 / 已实现待验证 / 已验证 / 受阻”；人工体验另标“待用户验收”。“已验证”必须注明所验证的具体范围。

| 单元 | 当前状态 | 完成证据与限制 |
| --- | --- | --- |
| P0 输入准备与部署 | 已验证 | 既有依赖、素材/许可/OpenAL部署、FFmpeg哈希/PCM16及实际音频API通过；听感待用户 |
| P1 核心循环、资源、场景与层级 | 已验证 | 时步/输入/层级/模板/保存/失败回滚及GUI资源生命周期通过 |
| P2 资产、动画、渲染 | 已验证（本机自动/图像自查） | 65骨骼、四状态数据配置、局部Pose显示插值、双管线/阴影/后处理/PNG和GL回归通过；最终用户观感待验收 |
| P3 角色、物理与相机 | 已验证（查询/控制/集成） | 墙滑/跳跃/坡台/刚体清理与GPU控制链通过；真实键鼠/镜头手感待用户 |
| P4 Gameplay、AI、粒子与声音 | 已验证 | FSM/A*/门碰撞导航/目标一次事件、CPU粒子绘制/OpenAL通过；试听与整体玩法观感待用户 |
| P5 场景工具 | 已验证（代码/自动回归） | 层级/Undo/SaveLoad/PlayStop保留未保存设计及错误准备保护通过；人工编辑UX待用户 |
| P6 集成、交付与学习资料 | 首轮已完成 | 双配置0warn0err、自检/真实GL/音频/Astra复查、架构/运行/学习/限制资料；等待统一人工验收 |

## 开工后怎样保存进度

1. **先登记再动手。** 开始一个连贯的编辑/验证单元前，填写下方当前单元记录：目标、涉及文件、前置条件、验证方式。正式授权另记录用户明确答复的时间和范围；沉默/默认项/自动工具审批不是授权。
2. **每批实质修改或验证后更新。** 不等整个 V1 或整轮会话结束才写进度；记录完成/部分完成、修改文件、检查命令/配置/退出结果、失败和未验证项、紧接着可执行的下一步。重要架构/学习映射同步到对应文档，不在本页重复设计全文。
3. **长操作前后留证。** 操作前记录目的、命令、工作目录、预期输出和日志位置；结束后记录真实退出结果。输出缺失或只有旧日志时标“未确认”，不能推定命令已完成。恢复时先检查相关进程，避免重复启动构建、转换或应用。
4. **保护本地改动。** 获准实施后，首次修改实现前及通过重要集成检查时，在忽略的 `.cache/execution/` 保存有文件清单的源码/配置/文档快照或等效可恢复副本；覆盖未跟踪文件，不只保存 `git diff`。不重复备份 bin/obj 或大型输入包，输入包沿用来源/SHA记录；不得纳入私有截图、凭据或无关目录。快照必须实际创建并核对后才能登记“已备份”。这不是 Git 提交，也不是异地备份；不自动创建提交、reset、clean或覆盖恢复。
5. **并行工作记录所有权。** 若采用子任务，先约定文件/模块负责范围与公共接口；本页记录任务及结果，不把代理编号或聊天记忆作为唯一依据。恢复前检查仍在执行的写入任务，同一文件由一个执行者负责集成。
6. **暂停/额度接近耗尽时收尾。** 能落盘时立即补齐当前状态、最新有效验证和下一步；突然中断来不及更新时，下次以文件/差异/日志为证据重建，允许重做最后未确认的一小批验证。

### 当前单元记录（开工后替换为实际值）

| 字段 | 当前值 |
| --- | --- |
| 正式授权及边界 | 2026-10-03明确同意完整范围，均Ultra；不Git写操作/升级SDKNuGet/系统安装/D5 |
| 正在进行的实现单元/文件责任人 | 本批球/Ramp退出已修复并验证，无代理继续写入；Astra分别生产Scene/Core/Simulation、独立38例、Window/Options，root已统一集成 |
| 已改实现文件 | Engine Core/Scene/Editor/Assets/Animation/Rendering/Physics/Navigation/Audio/Effects/Tools；Sandbox窗口/Gameplay/Tools/入口；assets、third_party、必要csproj复制配置；锁文件/SDK未变 |
| 最近有效验证 | contact-build双配置0warn0err；contact-approaches各38PASS、完整verify/11UI PASS；contact-window各800frames九路668move无GLerror，contact-legacy各240frames2jump110move/mean.0621 max22；Astra有限复核闭环 |
| 操作/进程/日志/快照 | 日志在.cache/execution；开工/首轮/本批快照精确路径见下方，覆盖未跟踪源码；本批构建/测试均已退出 |
| 未完成/失败/待人工验证 | 本批确认异常及字段漂移已修，用户实际重建后原路线/手感复试未确认；其他人工体验和PCF/有限子集限制仍待 |
| 下一条可执行工作 | 用户VS重建后复试S转身/球区、横/斜走、原Ramp上下/侧挡；无需清用户设计；按反馈同范围修复，Git保存由用户决定 |
| 恢复时需要用户处理的事项 | 同范围无需再开工确认；重大新范围/权限或实际阻塞才讨论，最终用户体验待验收 |

## 新对话/意外中断恢复顺序

1. 使用**同一个现有本地目录**；先读根 README → AGENTS → status/handoff → 本页，再按任务读取实施方案/准备清单/架构。不要重新讨论已经明确的选型，也不要先创建新克隆来“恢复”。
2. 只读检查 Git 根、分支、HEAD、status、相关 diff/未跟踪文件及本页当前单元；核对用户改动、资源与已有日志，不能以文档声称完成代替文件事实。
3. 确认旧任务/子任务/应用是否仍在写入或运行，再接管未完成单元；不盲目杀进程或启动第二个写入者。打断后实际检查代理列表，interrupted不能当作running；send_message仅投递，要followup_task明确恢复或重新分派后复核running。旧代理不可用不妨碍从文件重建任务。
4. 若中断落在修改或构建中间，先检查这批文件是否完整，再做最小必要编译/行为验证；区分旧输出和本次产物。保留损坏或失败证据，不能自动回滚全部工作区。
5. 修正台账：真实已完成/未验证/受阻项、最后有效检查、待做步骤以及授权。清晰记录的既有授权在范围不变时继续有效；新权限/范围变化按规则讨论。**当前完整V1已获明确授权；恢复同范围工作直接继续。**
6. 继续未完成部分，实际验证后更新状态。恢复的是项目进度，不是原会话完整上下文、进程调用栈、内存或未保存内容；磁盘故障/目录丢失还需要独立备份，不能靠换对话解决。

## 本次风险复查

| 风险 | 实施时的处理 |
| --- | --- |
| 新依赖只验证了文件和旧探针 | 优先做Jolt/ImGui/OpenAL的最小实际调用及清理，再扩展功能；避免最后才发现部署/API问题 |
| 多模块集成与学习质量 | 公共接口先明确，每批集成验证；维护实际架构/数据流、关键注释和代码—笔记映射，避免全部结束后补写 |
| 无人值守遇到权限、网络或额度阻塞 | 保持安全边界、保存阻塞点，继续独立可做的工作；不能承诺自动续额度或绕过审批 |
| 台账落后于突然中断、并行写入冲突 | 小批更新、操作证据、明确文件责任人；恢复时核对实际工作区 |
| 仅本机保存，未跟踪与忽略文件不会随克隆出现 | 同工作区接续；按授权建立本地快照，异地同步需另按Git/备份授权流程办理 |
| 自动检查无法替代手感与视听验收 | 最终列出助手实际验证、待人工项目和已知限制；D5仍暂缓，不虚报跨设备兼容 |

**用户已确认D53：GPT-6.1 Sol Ultra主实施，GPT-6 Astra Ultra关键评审与独立核查，两者均Ultra，质量优先。** 用户报告已在界面选择Sol Ultra；实施子任务显式采用 `gpt-6.1-sol` / `ultra`，评审/重大疑点核查显式采用 `gpt-6-astra` / `ultra`。按需要调用高质量模型，不为节省自行降档。此前High/额度优先建议为历史，不能继续当作当前要求；模型分工不是项目NuGet依赖锁定。

已采用执行方式：主对话由用户界面选择，助手按任务显式指定子任务模型/Ultra；评审给相关设计/代码/验证记录，先只读返回问题，主任务集成修复与复验，不让多个执行者无协调修改同一文件。当前会话工具支持这一设置，无需修改全局配置；[官方子代理说明](https://learn.chatgpt.com/docs/agent-configuration/subagents)也支持提示指定。只读开工前任务 `v1_preflight_astra` 已按Astra/Ultra完成：未发现需用户再选的重大设计缺口，待最终明确开工同意，证据见节点记录；不能由主模型推断子任务模型，实际不可用时明确记录/报告，不能静默降档或虚称已评审。

每个聊天有独立上下文；文件是跨聊天接续依据，见 [项目与聊天](https://learn.chatgpt.com/docs/projects)。需要跨回合持续追踪时，可由用户明确要求使用 Goal，见 [Goals](https://developers.openai.com/cookbook/examples/codex/using_goals_in_codex)；当前未创建 Goal，Goal 本身也不保证断网、额度耗尽或进程退出后自动恢复，不代替本台账。

## 实施历史摘要（当时阶段，不作为当前步骤）

- 开工快照：g104engine/.cache/execution/snapshots/20261003-160603-authorized-start；43个文件逐一SHA-256核对通过，包含未提交交接，基线cbce581。
- 当前正在建立SceneData/RenderContracts公共契约，随后并行核心场景、渲染资产动画、物理玩法；主任务负责窗口/工具UI/音频/素材准备和集成。
- 此前各处待批准状态为历史，当前明确授权优先；同范围恢复不再询问开工。

### 集成批次接续
- SceneData/RenderContracts公共DTO已落盘；核心/场景/Editor命令与SceneEditorPanel由v1_scene_core完成，Physics/Navigation/Gameplay由v1_physics_gameplay正在原生检查，Rendering/Assets/Animation由v1_render_animation完成主体并待统一GL验证。
- 根任务已写Audio/Effects/ImGuiController/LaunchOptions/TrainingWindow/Program和资产复制配置；初始场景28对象、7个转换音效与65骨骼素材已准备。
- 当时编译由physics_gameplay串行持有；现已释放，Root负责所有集成构建。Astra Ultra首轮实码评审已完成并反馈修复项。
- 最近编译：Engine曾成功；Sandbox先前窗口三处API错误已修，统一构建/--verify仍待确认；下一步汇总构建错误并运行Debug --verify/--verify-audio和隔离--exercise截图。

### 行为验证与评审修复
- --verify普通沙箱在临时文件File.Replace受限，批准的沙箱外重跑Core全部、A*和Jolt查询/角色/刚体检查通过；随后动画导入暴露实际GLB附带未使用TEXCOORD_1，资产代理已改为允许无消费UV集，材质实际消费非UV0仍明确拒绝。
- Astra Ultra实码发现并修复：编辑资源失败可能终止应用→History.PrepareChange候选准备/回滚；同GUID重新Play复用旧动画→成功提交后ResetAnimations；活动角色/门后代静态碰撞不同步→仅允许纯显示后代；导航实际端点失败结果与FXAA线性过滤由对应代理修复。
- Root已统一--verify接口并加入GameplayVerification，音频/图形独立验证仍待；所有后续Core文件验证目录已改为显式隔离根。

## 首轮完成接续

- 最新角色动画定义：assets/config/character-animation.json，动作映射/速度响应/过渡与状态时长/markers严格配置并真实非默认行为验证，正向离地Jump/向下Fall/实际接地Land；共享定义只读，实例游标独立。
- previous localPose与SceneGraph共用alpha，mesh/palette/skeleton同一显示world；GetDisplayWorld不推进逻辑或事件，暂停/恢复ResetDisplayHistory，成功切场景ResetAnimations。
- delivery-debug.log与delivery-release.log记录完整240帧最终回归，含骨架静态/角色边界；本机截图captures-delivery-release已查看，最后CPU/原生自检及实际音频记录保留。
- 用户运行/验收入口guides/v1-run-and-review.md，实际架构/学习guides/v1-architecture-and-learning.md、learning-map.md，验证/已知限制reviews/v1-implementation-review-2026-10-03.md。
- 全部当前实现仍仅本机未提交推送，准备GitHub节点cbce581保持；后续同范围修复继续已有授权，D5和长期后移专题不自动恢复。

## 最终实现快照

- 路径：`g104engine/.cache/execution/snapshots/20261003-185834-v1-delivery`，122文件逐一SHA-256核对通过，包含未跟踪源码/配置/文档/小型资产，不自动Git提交。
- 大型UAL GLB与OpenAL DLL未重复备份；工作区源文件/原ZIP及assets/licenses/asset-manifest.json SHA记录保留。日志/快照不是异地备份。
- 最新双配置重建与CPU自检已在最后追加用例后重新通过；运行实现未变化，delivery GPU证据保持有效。41份文档/466实际渲染本地链接和21资产SHA检查通过。
- 本轮无执行中的实现代理、构建或应用；下一步是用户运行验收/学习与同范围反馈修复。

## 新独立评审批次：b91dfe4之后

- 用户已自行保存完整V1到b91dfe4137686d9a5f962e40e10a3afe939cfe4c，HEAD/跟踪/GitHub main本轮核实一致，工作区起始干净。该提交是本批可恢复源码基线；原cbce581和122文件快照为前一阶段。
- 3个Astra Ultra独立read-only评审已完成/分批反馈，3个Sol Ultra任务复现修复：Editor负责Window/Panel/History与UI验回；Gameplay负责Nav/Simulation/参数合同与行为回归；Visual负责PBR/粒子排序/材质UV/导入与GPU验回。root负责Program验证入口与唯一串行构建/测试、状态文档。
- 发现及撤回猜测/证据级别见reviews/v1-independent-review-2026-10-03.md；当前先交付失败案例，不能把静态推测直接当实测。
- 下一步：根任务运行各专项失败回归，修复后Debug/Release相关检查与GPU必要回归，再Astra检查修改；所有数据仍工作区隔离，现有授权继续有效。

### 独立review中间阶段（历史）

- 三组baseline在有效构建后实际复现：四物理/AI合同FAIL，UI6FAIL+4PASS，GPU六FAIL。有效修复构建后：四合同PASS、UI11PASS、GPU首批7PASS，日志review-*-after，旧DLL中途输出不作为后验结论。
- Editor/Gameplay源码已冻结，Visual正在校正有效反射颜色/Gbuffer域与actual rough参考；root拥有唯一构建窗口，下一步新数值例baseline→修→GPU/双配置/整体回归→Astra变更复核。
- Git基线b91dfe4保持，当前修复与文档仅本机；没有真实用户存档修改，原SDK/NuGet/后移专题/D5边界保持。

### 本批最终闭环

- 逐项失败复现、修复、独立参考和有限delta复核见独立评审记录。导航三轮真实失败→double容量/float格界校正后双配置七入口PASS；Editor真实UI各11PASS；Visual与独立BRDF/source-over各10GPU PASS。
- 最新Debug/Release build/verify、graphics-final日志均成功；全新数据初次Forward随后Deferred，两配置240帧/2jump110move，无GLerror；MAE.0621/255 max22。最后源码只改一行导航中文注释，行为不变，不为该注释重复全量运行。
- 只在本机保存，未Git写操作；HEAD/跟踪仍b91dfe4。README/status/handoff、真实参数合同、架构/指南与命令入口已同步；本批源码快照在下方登记实测结果。

### 本批可恢复快照

- 路径：`g104engine/.cache/execution/snapshots/20261003-212054-v1-independent-review`，130个源码/配置/文档/小型素材文件复制后逐一SHA-256核验，包含本批未跟踪验证源码和评审文档；清单为manifest.json，范围信息为snapshot-info.json。
- 未重复复制UAL模型、OpenAL DLL与对应源码tar包，三者已在用户保存的b91dfe4及来源/SHA记录中；不纳入原笔记、私有目录、bin/obj或其他缓存。快照不代表Git提交或异地备份。
- 文档检查：44份Markdown、482个代码块外实际本地文件/目录链接、21资产SHA和OpenAL源码包SHA通过；status31行。Git diff --check通过，SDK/slnx/csproj/锁文件/原笔记/.gitignore/.gitattributes与b91dfe4无差异。
- 结束前快照中的进度文件及manifest已按最后登记更新、再次核验；新对话先以工作区当前文件为准，再按需核对快照，不自动整目录覆盖或回滚。

## 用户球/Ramp退出：本批最终闭环

- 用户先误发exit0截图，后纠正为SceneValidationException/exit1。独立C#38例有效baseline 4PASS/34FAIL（12真异常、22仅exactScale合同）；south无Physics第35次更新已复现非TRS误拒，不是球碰撞或scale超unit容差。
- Astra runtime修改SceneGraph单字段setter、稳定double四分支TransformMath及finiteScale拒、根角色直接读Quaternion，有限Core数学/父链/非法矩阵/回滚检查；Astra regression只写38例；Astra窗口任务只写800帧独立flag/9路helper，生产Physics未改。
- root有效Debug/Release build均0warn0err，contact-approaches各38/38、contact-verify完整、contact-ui各11PASS。contact-window各800帧6球+Ramp上下/侧九路、668moving无GLerror；contact-legacy各240帧2jump110moving、Forward/Deferred/保存与失败保护通过。真实数值及日志名见专项复查，测试数据全部隔离。
- Astra regression最后只读生产delta和Debug日志未见有证据P1/P2；没有自行build/run，Release由root实测。写入任务均已结束，不依赖代理名当进度证据。
- 协作失误已纠正：用户打断导致两旧代理interrupted，root曾只send_message而误以为运行；用户指出后followup_task恢复并实际核对running，之后才有正式修复。规则新增打断后代理状态检查，测试完成/生产修改/实际验回分开描述。
- 所有原review改动保留，HEAD仍b91dfe4，没有Git写/SDK包变更或用户设计、原笔记、Piccolo改动。接下来用户重新构建复试，恢复读本页/专项记录/真实Git，不自动清目录或覆盖设计。
- 最终本批快照：`g104engine/.cache/execution/snapshots/20261003-224220-v1-contact-fix`，133个源码/配置/文档/小素材文件逐一SHA核验，包含7个当前未跟踪路径及原review修复；原UAL/OpenAL DLL/源码tar三项仍由b91dfe4与来源SHA保护，不重复复制。进度登记后已刷新快照/manifest并再次核验，不代表异地备份或提交。
- 收尾完整性：45份Markdown、498个代码块外实际本地链接、21资产SHA通过；status36行，Git diff --check与SDK/slnx/csproj/锁文件/原笔记/.gitignore/.gitattributes保护比较通过。HEAD/跟踪仍b91dfe4，当前36个改动/未跟踪路径都未由助手暂存或提交。

## 用户已同步与新聊天接续

- 用户报告已提交后，root只读确认最新d2e8d40691dd76a4637e35ed2905a2ccfaefe3d2，实际提交38文件与准备清单一致；main/HEAD/origin/main及实时GitHub main相同，提交时间2026-10-03 23:00:25 +08:00，起始工作区干净。
- 本轮仅纠正README/status/handoff/本台账/publishing/修复提交清单的同步记录和可复制接续提示；这6份文档更新仅本机未再次提交，源代码/GLSL/数据/依赖未变，不重复构建或功能验证。
- 用户报告聊天已压缩约4–5次，询问是否新开。建议以已保存修复节点新开“V1验收与学习”聊天，是阶段聚焦建议，不以压缩次数断定旧聊天不可继续。使用现有同目录，不新克隆/创建工作树；新聊天读取工作区文件恢复进度，而非假定完整旧聊天/旧代理内存被继承。
- Astra Ultra只读交接核查未见阻碍恢复的真实遗漏：既有V1与同范围修复授权、依赖/私有资料/D5边界、双模型Ultra、源码—笔记要求、人工尚待验收和旧代理状态恢复均清楚。root完成实际节点更新后，自检当前表述/提示与Git事实一致。
- 下一条具体工作：用户VS重新构建后复试S转身/球区、横斜移动与原Ramp上下/侧挡，再按运行指南统一反馈、学习。自动38专项/11UI/800及240帧不代替此用户确认；同范围修复无需重复开工，重大变化再讨论。
