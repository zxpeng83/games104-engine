# 从0到1：开发阶段与专题经验

本页解释项目怎样走到现在，不维护实时任务状态。每行给出当时的问题/决定及证据，按兴趣选择专题即可；原始快照和完整评审均为可选深入。
基础、V1、评审修复与本次文档改造是不同阶段，不能混用当时的“下一步”。需要接续现实工作时查 [status.md](status.md#resume)。

<a id="timeline"></a>
## 开发时间索引

| 阶段／日期 | 当时的问题、决定或结果 | 专题与原始证据 |
|---|---|---|
| 2026-10-01：确定基础 | 利用C#/Unity经验做独立引擎；确认.NET/VS/OpenGL/OpenTK、单仓与全模块目标，替代早期C++/双仓/助手自动安装候选 | [decisions.md](decisions.md#legacy-0001) |
| 2026-10-01：撤回前瞻架构 | 尚无实现时过细模块图容易被误读成批准；先搭基础，再对具体目标设计。原文及SHA归档，后来由真实设计接续 | [decisions.md](decisions.md#legacy-0002)、[architecture-draft-2026-10-01.md](archive/architecture-draft-2026-10-01.md) |
| 2026-10-02：建立可运行基础 | 实际生成小写g104engine.slnx，建立Engine/Sandbox、x64、SDK选择和NuGet锁定；GL窗口与smoke各验证不同层次 | [foundation-review-2026-10-02.md](reviews/foundation-review-2026-10-02.md)、[foundation-setup-and-probes.md](archive/foundation-setup-and-probes.md) |
| 2026-10-02：环境排错 | 模板、x64、SAC与“仅我的代码”等按当时设备和证据处理，单机排错操作不是所有机器复现必做项 | [environment.md](guides/environment.md)、[foundation-documents-2026-10-02.md](archive/foundation-documents-2026-10-02.md) |
| 2026-10-02：首次公开保存 | 用户通过GitHub网页及自己的Git Bash／Git GUI建仓；31文件是当时清单，十份笔记/实际配图和26处不处理引用边界确定 | [git-and-publishing.md](guides/git-and-publishing.md#publishing-scope)、[foundation-review-2026-10-02.md](reviews/foundation-review-2026-10-02.md) |
| 2026-10-03：收敛V1 | 最小3D并入综合训练场；Deferred/Undo纳入V1，IBL和平台推箱后移；统一版本名“基础综合训练场V1（用于面试展示）” | [v1-baseline.md](plans/v1-baseline.md#m1-integration-proposal)、[m1-minimal-3d-draft-2026-10-03.md](archive/m1-minimal-3d-draft-2026-10-03.md) |
| 2026-10-03：输入与后端准备 | 核对四个ZIP、同骨架模型与动作，选择Kenney Ogg离线转PCM16、OpenAL DLL及对应源码；区分格式、来源许可、哈希和原生调用 | [v1-input-archives-check-2026-10-03.md](reviews/v1-input-archives-check-2026-10-03.md)、[dependencies.md](guides/dependencies.md) |
| 2026-10-03：正式实施 | 用户明确同意完整范围连续实施；Sol Ultra主实施/Astra Ultra评审，建立阶段、快照、准备失败保护与运行素材 | [v1-start-checkpoint-2026-10-03.md](reviews/v1-start-checkpoint-2026-10-03.md)、[v1-implementation-review-2026-10-03.md](reviews/v1-implementation-review-2026-10-03.md) |
| 2026-10-03：独立评审修复 | 复现编辑事务/保存基准、导航边界、GGX/HDR/粒子/资产问题后修正；两管线相近不能替代独立正确性参考，旧DLL不证明修后状态 | [v1-independent-review-2026-10-03.md](reviews/v1-independent-review-2026-10-03.md) |
| 2026-10-03至10-04：球/Ramp退出 | 新截图纠正原exit0判断；六球无碰撞，近180°TRS矩阵往返误拒可纯场景复现；修复单字段TRS、稳定分解和根Quaternion后回归并获人工复试 | [v1-contact-exit-fix-2026-10-03.md](reviews/v1-contact-exit-fix-2026-10-03.md)、[scene-and-editor.md](guides/scene-and-editor.md) |
| 2026-10-04：UI鼠标修复 | 悬停正常/长按无效帮助定位误清ActiveId，另有帧间短点击丢失；窗口事件队列与WantCaptureMouse修复生产链，新增13项输入验证 | [v1-ui-mouse-fix-2026-10-04.md](reviews/v1-ui-mouse-fix-2026-10-04.md) |
| 2026-10-04：初步体验通过 | 用户确认运行指南1–5项初步无问题，允许后续发现Bug再反馈；体验、专项覆盖、源码掌握分别记录 | [v1-run-and-review.md](guides/v1-run-and-review.md#run-and-verify)、[v1-progress.md](execution/v1-progress.md) |
| 2026-10-04：保存UI与验收 | 38cb85f保存UI修复/首轮验收16文件；提交准备、实际范围与当时远端核查各自有日期 | [git-submission-history.md](reviews/git-submission-history.md#ui-acceptance) |
| 2026-10-04：文档一致性校正 | 纠正旧空模板/待选库/待验收等表述；49份文档/629链接/15锚点通过属于当时批次，45修改＋1新增尚未提交 | [document-consistency-review-2026-10-04.md](reviews/document-consistency-review-2026-10-04.md) |
| 2026-10-04：信息与阅读重构 | 用户要求保全学习记录、专题＋时间索引、实际减文件、减少必读回跳并预留自动化扩展；先只读讨论，再明确批准49→43方案实施 | [decisions.md](decisions.md#document-redesign)、[document-restructure.md](execution/document-restructure.md) |

<a id="lessons"></a>
## 按专题理解开发经验

| 专题 | 学习主线 | 可选深入 |
|---|---|---|
| 架构与场景 | 先明确阶段边界，再建立固定步、唯一写入者、逻辑/显示和设计/运行隔离；资源准备成功后发布，失败保持旧状态 | [architecture.md](architecture.md#overview)、[scene-and-editor.md](guides/scene-and-editor.md) |
| 数学与物理 | 世界、GLSL和Jolt包装层的约定不同；查询预转置仅在适配边界处理。近180°故障说明不必为单字段更新反复分解已有TRS | [physics-and-gameplay.md](guides/physics-and-gameplay.md)、[v1-contact-exit-fix-2026-10-03.md](reviews/v1-contact-exit-fix-2026-10-03.md) |
| 渲染与动画 | 模型/mesh/skin空间分开，in-place不驱动角色世界位移；GGX固定偏置影响能量，修正后HDR数值域又需验证 | [rendering-and-animation.md](guides/rendering-and-animation.md)、[v1-independent-review-2026-10-03.md](reviews/v1-independent-review-2026-10-03.md) |
| 工具与验证 | 原有11项UI测试绕过生产输入适配而漏检；草稿、拖动事务和真实保存基准也是不同合同 | [v1-ui-mouse-fix-2026-10-04.md](reviews/v1-ui-mouse-fix-2026-10-04.md)、[scene-and-editor.md](guides/scene-and-editor.md) |
| 素材与声音 | 文件格式、动作相容、来源许可、部署、原生播放和听感各有证据；离线转换工具与运行依赖分离 | [v1-input-archives-check-2026-10-03.md](reviews/v1-input-archives-check-2026-10-03.md)、[foundation-setup-and-probes.md](archive/foundation-setup-and-probes.md) |
| AI与Gameplay | 门的画面、碰撞和导航在同一模拟边界提交；NPC决策与角色执行分阶段，到达、受阻、无路分别处理 | [physics-and-gameplay.md](guides/physics-and-gameplay.md) |
| 协作与证据 | 消息投递不等于恢复代理；测试代理完成不等于生产修复；本机快照、源码保存、远端同步分别核实 | [v1-contact-exit-fix-2026-10-03.md](reviews/v1-contact-exit-fix-2026-10-03.md)、[agent-workflow.md](agent-workflow.md#recovery) |

## 版本与历史使用

3ea7515是首次公开保存，cbce581是开工前准备历史，b91dfe4是首轮V1，d2e8d40包含独立评审/转向修复，4be648e是前次交接节点，38cb85f保存UI修复与初步验收。精确清单、身份和核验时点在提交历史；旧文件数量不能当作下一批固定范围。
D0–D4是开发基础编号，D01–D60是决定索引，R/P/G等是模块工作分块；它们不是发布版本数量。统一后续版本表、独立练习与展示脚本仍待结合学习细化。
历史“待授权”“未提交”“下一步”描述当时；后来变化保留来源和理由，不用旧语句撤销新决定。网络、动态GI、GPU几何等目标保留，学习目标本身不授权实施，也不自动恢复D5。
本次时间索引没有重跑引擎，不把本机缓存称为GitHub证据。追溯链接是可选阅读，不要求返回首页重走必读流程。
