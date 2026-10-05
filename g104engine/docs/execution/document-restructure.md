# 文档与 Agent 协作改造：执行与信息迁移记录

日期：2026-10-04。状态：已完成并通过本批文档验收，仅本机未提交。本文件记录本次文档改造，不把原V1测试结果记为本轮运行。

## 授权、目标与当前单元

用户在本聊天明确发送“PLEASE IMPLEMENT THIS PLAN”，并附完整“精简文件、减少必读跳转”计划；当前已切回可执行模式。此次授权包括工程文档重构、合并撤下14份旧文件、新增8份、快照/哈希及只读验证；不包括Git写操作、安装升级、引擎运行或资料修改。
目标：正式工程Markdown由49份变为43份；三条主要阅读路线无必读回跳；决策、计划、下一步、理由和学习经验有可核对去向。
用户明确选择：信息保全而非逐句复刻；按专题组织并附时间索引；实际减少文件数；未来Git/CI/Skills等可扩展，但本轮仅建立约定。

当前单元：内容集成及本批验收已完成。A/B/C写入、独立语义复核和三项无父上下文演练均已结束；root完成问题修正和最终收尾。实际43份、326项信息去向、受保护文件不变，本次未提交/推送。后续从源码学习接续，不重复启动已结束的写入代理。

## 迁移前基线与保护

- 本地HEAD/main/origin/main均为 `38cb85f2be6d86804edd3331a54467389b3af604`；本次只读查询本地引用，没有重新查询实时远端。
- 开始时工作区已有45份已跟踪文档修改＋1份未跟踪评审记录，暂存状态按原样保护；以实际工作区内容为基线，不用HEAD覆盖它们。
- 完整49份文档按原字节复制到本机 `g104engine/.cache/execution/document-restructure-20261004-210900/before/`，逐文件SHA-256一致。清单为同目录 `documents-before.json`；原Git状态与HEAD各有记录。
- 同目录 `protected-before.json` 已记录3097个受保护文件的SHA-256，共1,363,035,213字节，包含资料全目录、非缓存引擎文件、配置、资产、第三方文件、根忽略规则和草稿；私有截图及Piccolo未遍历。
- 本机缓存不随Git同步。迁移表和足够解释结论的摘要存入本文件；不能把本地快照称为远程备份。
- 既有三份archive的历史正文/快照代码块保持原文；仅允许更新必要的围栏外导航。SOURCE逐字节保持。
- 本轮不构建、不运行引擎，不恢复D5，不改上层Git或个人资料。

## 文档职责与必读关系

规则入口 → 当前状态 → 本次任务正文。README供人按“运行、学习、架构、恢复、未来计划、历史、Git、协作”直达任务章节。
执行前提就近说明；概念可用短摘要帮助理解；来源和完整历史属于可选深入。禁止专题页要求重新回到首页开始必读流程。
当前操作政策主要维护于 [agent-workflow.md](../agent-workflow.md)，任务记录保留当时适用政策和授权；政策记录不能生成技术权限。
数量：49−14＋8＝43；本次执行记录也计入，不以转移到缓存或资料目录伪造减少。

## 参考依据与本项目适配

| 来源（2026-10-04已查阅） | 采用内容 | 本项目适配及边界 |
|---|---|---|
| [近期Codex规则指导](https://developers.openai.com/blog/rethinking-skills-and-prompts-for-gpt-6-astra) | 简短入口、按任务加载、避免冗长重复规则 | 必读关系无环；不要求删除有学习价值的历史 |
| [官方开发工作流](https://developers.openai.com/cookbook/examples/codex/iterating-development-workflows-with-codex) | 目标/计划/进度/上下文/评审各有归属，保留理由和实际证据 | 沿现有工程术语合并文件；不照搬每阶段审批、额外目录或所有模板 |
| [install.md](https://github.com/openai/codex/blob/main/docs/install.md) | 就地列环境、目录、实际命令和相关检查 | 使用本工程已有.NET/原生/GL/音频入口，不照搬Rust命令 |
| [官方扩展说明](https://learn.chatgpt.com/docs/customization/overview) | Skills、MCP、子Agent职责分开 | 当前只定义接入要求，不安装或启用新能力 |
| [GitHub Action](https://learn.chatgpt.com/docs/github-action) | 自动化执行与工具权限分别配置 | CI与Git写权限分别决定，D5其余项不自动恢复 |
| [SKILL.md](https://github.com/openai/plugins/blob/main/plugins/notion/skills/notion-knowledge-capture/SKILL.md) | 保留事实、备选方案、理由、结果和来源 | 保存在本仓库，不引入Notion |
| [工作流评估](https://developers.openai.com/blog/eval-skills) | 以任务结果和过程验证质量 | 进行只读接续演练，不宣称已验证真实Git/CI执行 |

[旧Skills仓库](https://github.com/openai/skills)已标记弃用，[旧ExecPlans示例](https://developers.openai.com/cookbook/articles/codex_exec_plans)已归档；不作为现行配置规范。官方长任务案例属于实验，不能保证本项目质量。
这些来源支持原则与机制，并非对本项目43份布局的官方认证。两次导航目标是本项目验收约定。

## 信息迁移总表（49份基线）

表中的 `docs/` 均相对 `g104engine/`。旧路径是迁移来源标识，不是新导航；精确原文字节与行号可在上述before快照中恢复。
处理范围覆盖每份文件不等于每份必须改写；逐项去向和独立复核在下方补充。

| 编号 | 原位置 | 新位置／主要归属 | 必须保留的信息／处理 | 负责人 | 状态 |
|---|---|---|---|---|---|
| M01 | `AGENTS.md` | AGENTS.md；docs/agent-workflow.md | 规则、边界、分工与持久化；短路由＋当前政策分离 | root | 已归位，关键语义复核通过 |
| M02 | `README.md` | README.md；docs/status.md；docs/development-history.md | 项目入口、当前状态和同步历史拆分 | root | 已归位，关键语义复核通过 |
| M03 | `g104engine/docs/status.md` | docs/status.md | 当前事实、下一步、限制与同步状态；合并交接 | root | 已归位，关键语义复核通过 |
| M04 | `g104engine/docs/setup.md` | README.md；docs/archive/foundation-setup-and-probes.md | 按任务导航并入README；兼容历史及学习信息归位 | root/A | 已归位，关键语义复核通过 |
| M05 | `g104engine/docs/reviews/v1-ui-mouse-fix-2026-10-04.md` | docs/reviews/v1-ui-mouse-fix-2026-10-04.md | 保留日期、阶段、修前失败/修后结果及限制；只更新当前导航 | C | 已归位，关键语义复核通过 |
| M06 | `g104engine/docs/reviews/v1-ui-acceptance-commit-checklist-2026-10-04.md` | docs/reviews/git-submission-history.md | 逐批保留准备、确认、实际文件、身份/分支/提交与同步核验；撤下旧页 | C | 已归位，关键语义复核通过 |
| M07 | `g104engine/docs/reviews/v1-start-checkpoint-2026-10-03.md` | docs/reviews/v1-start-checkpoint-2026-10-03.md | 保留日期、阶段、修前失败/修后结果及限制；只更新当前导航 | C | 已归位，关键语义复核通过 |
| M08 | `g104engine/docs/reviews/v1-repair-commit-checklist-2026-10-03.md` | docs/reviews/git-submission-history.md | 逐批保留准备、确认、实际文件、身份/分支/提交与同步核验；撤下旧页 | C | 已归位，关键语义复核通过 |
| M09 | `g104engine/docs/reviews/v1-input-archives-check-2026-10-03.md` | docs/reviews/v1-input-archives-check-2026-10-03.md | 保留日期、阶段、修前失败/修后结果及限制；只更新当前导航 | C | 已归位，关键语义复核通过 |
| M10 | `g104engine/docs/reviews/v1-independent-review-2026-10-03.md` | docs/reviews/v1-independent-review-2026-10-03.md | 保留日期、阶段、修前失败/修后结果及限制；只更新当前导航 | C | 已归位，关键语义复核通过 |
| M11 | `g104engine/docs/reviews/v1-implementation-review-2026-10-03.md` | docs/reviews/v1-implementation-review-2026-10-03.md | 保留日期、阶段、修前失败/修后结果及限制；只更新当前导航 | C | 已归位，关键语义复核通过 |
| M12 | `g104engine/docs/reviews/v1-contact-exit-fix-2026-10-03.md` | docs/reviews/v1-contact-exit-fix-2026-10-03.md | 保留日期、阶段、修前失败/修后结果及限制；只更新当前导航 | C | 已归位，关键语义复核通过 |
| M13 | `g104engine/docs/reviews/v1-commit-checklist-2026-10-03.md` | docs/reviews/git-submission-history.md | 逐批保留准备、确认、实际文件、身份/分支/提交与同步核验；撤下旧页 | C | 已归位，关键语义复核通过 |
| M14 | `g104engine/docs/reviews/foundation-review-2026-10-02.md` | docs/reviews/foundation-review-2026-10-02.md | 保留日期、阶段、修前失败/修后结果及限制；只更新当前导航 | C | 已归位，关键语义复核通过 |
| M15 | `g104engine/docs/reviews/document-consistency-review-2026-10-04.md` | docs/reviews/document-consistency-review-2026-10-04.md | 保留日期、阶段、修前失败/修后结果及限制；只更新当前导航 | C | 已归位，关键语义复核通过 |
| M16 | `g104engine/docs/reviews/design-reference-checks-2026-10-03.md` | docs/reviews/design-reference-checks-2026-10-03.md | 保留日期、阶段、修前失败/修后结果及限制；只更新当前导航 | C | 已归位，关键语义复核通过 |
| M17 | `g104engine/docs/publishing.md` | docs/guides/git-and-publishing.md；docs/agent-workflow.md | 精确发布白名单、排除范围、26处链接决定及Git分工 | C/root | 已归位，关键语义复核通过 |
| M18 | `g104engine/docs/plans/v1-implementation-draft.md` | docs/plans/v1-baseline.md；docs/architecture.md；专题指南 | 批准方案、设计理由、受控合同和演变 | B/A | 已归位，关键语义复核通过 |
| M19 | `g104engine/docs/plans/v1-dependencies-and-assets.md` | docs/guides/dependencies.md；docs/archive/foundation-setup-and-probes.md；输入核查 | 固定依赖、备选理由、素材来源/许可/转换与准备历史 | A/B | 已归位，关键语义复核通过 |
| M20 | `g104engine/docs/plans/tools-debug-roadmap.md` | docs/plans/tools-debug-roadmap.md | 模块终点、已选路线、取舍、候选、依赖和验收；去重当前进度 | B | 已归位，关键语义复核通过 |
| M21 | `g104engine/docs/plans/rendering-roadmap.md` | docs/plans/rendering-roadmap.md | 模块终点、已选路线、取舍、候选、依赖和验收；去重当前进度 | B | 已归位，关键语义复核通过 |
| M22 | `g104engine/docs/plans/physics-character-roadmap.md` | docs/plans/physics-character-roadmap.md | 模块终点、已选路线、取舍、候选、依赖和验收；去重当前进度 | B | 已归位，关键语义复核通过 |
| M23 | `g104engine/docs/plans/particles-audio-roadmap.md` | docs/plans/particles-audio-roadmap.md | 模块终点、已选路线、取舍、候选、依赖和验收；去重当前进度 | B | 已归位，关键语义复核通过 |
| M24 | `g104engine/docs/plans/gameplay-ai-roadmap.md` | docs/plans/gameplay-ai-roadmap.md | 模块终点、已选路线、取舍、候选、依赖和验收；去重当前进度 | B | 已归位，关键语义复核通过 |
| M25 | `g104engine/docs/plans/core-architecture-roadmap.md` | docs/plans/core-architecture-roadmap.md | 模块终点、已选路线、取舍、候选、依赖和验收；去重当前进度 | B | 已归位，关键语义复核通过 |
| M26 | `g104engine/docs/plans/basic-training-ground-v1-draft.md` | docs/plans/v1-baseline.md | V1范围、M1合并及授权/候选区别 | B | 已归位，关键语义复核通过 |
| M27 | `g104engine/docs/plans/assets-scene-roadmap.md` | docs/plans/assets-scene-roadmap.md | 模块终点、已选路线、取舍、候选、依赖和验收；去重当前进度 | B | 已归位，关键语义复核通过 |
| M28 | `g104engine/docs/plans/animation-roadmap.md` | docs/plans/animation-roadmap.md | 模块终点、已选路线、取舍、候选、依赖和验收；去重当前进度 | B | 已归位，关键语义复核通过 |
| M29 | `g104engine/docs/plan.md` | docs/plan.md；docs/plans/v1-baseline.md；docs/agent-workflow.md | 稳定目标、全模块和未来阶段；现行政策单点维护 | B | 已归位，关键语义复核通过 |
| M30 | `g104engine/docs/learning-map.md` | docs/learning-map.md | 十份笔记映射、全部模块、自研/集成边界；补单页W/Space学习 | A | 已归位，关键语义复核通过 |
| M31 | `g104engine/docs/handoff.md` | docs/status.md；docs/decisions.md；docs/agent-workflow.md；docs/development-history.md | 接续、D01–D54、授权、模型、修复历史分别归位；撤下旧页 | root | 已归位，关键语义复核通过 |
| M32 | `g104engine/docs/guides/v1-run-and-review.md` | docs/guides/v1-run-and-review.md | 现行操作/算法及专题经验保留，按任务提供完整说明 | A | 已归位，关键语义复核通过 |
| M33 | `g104engine/docs/guides/v1-architecture-and-learning.md` | docs/architecture.md；docs/learning-map.md | 逐文件入口/帧流/矩阵/生命周期与学习自测完整归并 | A | 已归位，关键语义复核通过 |
| M34 | `g104engine/docs/guides/scene-and-editor.md` | docs/guides/scene-and-editor.md | 现行操作/算法及专题经验保留，按任务提供完整说明 | A | 已归位，关键语义复核通过 |
| M35 | `g104engine/docs/guides/reproduction.md` | docs/guides/reproduction.md | 现行操作/算法及专题经验保留，按任务提供完整说明 | A | 已归位，关键语义复核通过 |
| M36 | `g104engine/docs/guides/rendering-and-animation.md` | docs/guides/rendering-and-animation.md | 现行操作/算法及专题经验保留，按任务提供完整说明 | A | 已归位，关键语义复核通过 |
| M37 | `g104engine/docs/guides/probes.md` | docs/guides/v1-run-and-review.md；docs/archive/foundation-setup-and-probes.md | 现行命令与覆盖边界；旧探针代码/操作为历史 | A | 已归位，关键语义复核通过 |
| M38 | `g104engine/docs/guides/physics-and-gameplay.md` | docs/guides/physics-and-gameplay.md | 现行操作/算法及专题经验保留，按任务提供完整说明 | A | 已归位，关键语义复核通过 |
| M39 | `g104engine/docs/guides/git-workflow.md` | docs/guides/git-and-publishing.md；docs/reviews/git-submission-history.md；docs/archive/foundation-setup-and-probes.md | 当前Git操作、首次建仓和各次提交事实 | C/A | 已归位，关键语义复核通过 |
| M40 | `g104engine/docs/guides/environment.md` | docs/guides/environment.md | 现行操作/算法及专题经验保留，按任务提供完整说明 | A | 已归位，关键语义复核通过 |
| M41 | `g104engine/docs/guides/dependencies.md` | docs/guides/dependencies.md | 现行操作/算法及专题经验保留，按任务提供完整说明 | A | 已归位，关键语义复核通过 |
| M42 | `g104engine/docs/execution/v1-progress.md` | docs/execution/v1-progress.md；docs/status.md；docs/agent-workflow.md | V1各批次原证据和恢复经验保留；当前接续与通用流程分离 | root | 已归位，关键语义复核通过 |
| M43 | `g104engine/docs/decisions/0002-defer-implementation-architecture.md` | docs/decisions.md | 原0002历史决定、理由、推进方式和替代关系 | root | 已归位，关键语义复核通过 |
| M44 | `g104engine/docs/decisions/0001-foundation.md` | docs/decisions.md；docs/agent-workflow.md | 原0001背景、全部选择及理由、分工来源 | root | 已归位，关键语义复核通过 |
| M45 | `g104engine/docs/archive/m1-minimal-3d-draft-2026-10-03.md` | docs/archive/m1-minimal-3d-draft-2026-10-03.md | 历史正文/快照不变；必要时仅修正围栏外导航 | root | 已归位，关键语义复核通过 |
| M46 | `g104engine/docs/archive/foundation-documents-2026-10-02.md` | docs/archive/foundation-documents-2026-10-02.md | 历史正文/快照不变；必要时仅修正围栏外导航 | root | 已归位，关键语义复核通过 |
| M47 | `g104engine/docs/archive/architecture-draft-2026-10-01.md` | docs/archive/architecture-draft-2026-10-01.md | 历史正文/快照不变；必要时仅修正围栏外导航 | root | 已归位，关键语义复核通过 |
| M48 | `g104engine/docs/architecture.md` | docs/architecture.md；docs/learning-map.md | 实际结构、契约、理由及学习路线分离 | A | 已归位，关键语义复核通过 |
| M49 | `g104engine/third_party/openal-soft/1.25.2/SOURCE.md` | 原路径不变 | 原文件逐字节保护 | root | 已归位，关键语义复核通过 |

## 分工与信息项核对

- root：README、AGENTS、status、agent-workflow、decisions、development-history、当前台账与全局链接；统一核验后撤下旧文件。
- A / Sol Ultra：架构、学习映射、运行/技术/环境/依赖指南及新增准备历史；提供逐信息项去向。
- B / Sol Ultra：总计划、八份模块路线、V1基线；保全目标/取舍/未定专题。
- C / Sol Ultra：Git与发布指南、提交历史、现有评审导航；保全每批历史语义。
- 独立评审使用Astra Ultra；无父上下文演练另行委派，结果按实际记录。主对话模型由界面选择，本记录不推断其实际型号。
- 未完成代理、未核对迁移和未执行验证均不得提前记为通过。

## 2026-10-04首轮重构落盘

root已写README/AGENTS/status、协作政策与嵌入任务模板、D01–D54完整索引和原0001/0002理由、D55–D60本轮决定、开发时间索引；V1台账只改适用性说明，原历史记录仍保留。
A已写架构/学习/运行与技术环境依赖专题及准备补遗；B已写总计划、V1基线和八条模块路线；C已写Git/发布与三批提交合并、九份评审导航。这是当时的首轮落盘节点；随后各组去向已汇总并完成独立复核，不能仅据文件存在推断完成。
B初步提交94项来源映射及7项跨组技术合同，C保留108/38/16提交文件围栏、10正文＋1图白名单并核对原字节文本；这些属于分组自查，最终由root和独立评审复核。

## 独立语义复核与已修事项（2026-10-04）

read_evidence_review最初以显式gpt-6-astra／ultra／fork_turns=none启动，后续followup保持该配置。该审阅以before快照、各组迁移报告、差异和关键源码抽查为依据；不宣称全部语义零遗漏，不替代完整结构/保护检查或新上下文演练。
- D01–D54无缺号，53条正文语义未改，D46补历史时态；原0001/0002的背景、选择/撤回理由与阶段取代均可定位。
- 九份review逐份差异核对，FAIL/PASS、撤回判断、数值和人工反馈范围未改。三份提交清单11个原代码块（含108/38/16路径与命令）完整进入合并页，十正文＋一图白名单无缺失。
- 八路线、V1基线和跨组合同保留长期目标、受控父子/失败保护及重要时间/资源/格式数值。
- 运行入口与W/Space学习链定向对照Program、LaunchOptions、InputBuffer、Clock、Window、Simulation；隔离路径、Jolt/GL/音频层次和事件消费相符。
- 修正政策中“有确认框时展示”的弱化表述，恢复用户既定首次新代码范围的弹窗要求；Git不可见框聊天先例不自动扩展，V1/本次已有授权不重问。
- 修正工具路线中第三方与自研后端职责：ImGui.NET提供控件/API绑定、OpenTK提供窗口/GL接口，项目ImGuiController负责输入/绘制适配。
- 修正运行页历史验收“本节/下列”的指向，明确当时五类初步体验，不把新整理的细项写成用户已逐项验证。
以上三项已由root落实，并经同一独立评审复核。评审结论：审查范围内未发现剩余有证据P1/P2。未进行新引擎测试。

<a id="acceptance-summary"></a>
## 最终验收与新上下文阅读（2026-10-04）

正式清单49−14＋8＝43；撤下前14源文件与before快照逐一哈希相符。326条信息项覆盖49来源，列出的目的文件及锚点均有效；全部去向在下方折叠表，缓存information-migration.json仅作辅助，不替代此公开记录。
3097个受保护文件逐一SHA-256与开始前一致，无缺失或新增，包括资料全目录、非缓存引擎/配置/资产/第三方及根忽略规则。三份既有归档通过原字节对比（只接受两处指定导航href替换），SOURCE原字节保持。HEAD未变，暂存/提交/推送未执行。
自动链接、锚点、围栏和Git空白检查通过；新增文件另以no-index检查，三处文件末尾空行已修正；外部来源仅对本次采用原则核对，未声称逐个重测全部历史网页。最终统计与变更清单保存本机validation-final.json及delivery-manifest.json；不能把缓存说成远程备份。

| 演练代理（均显式Sol Ultra，fork_turns=none） | 实际阅读及结果 | 限制 |
|---|---|---|
| drill_resume | 规则→状态→当前任务/政策；准确识别V1初步验收、学习/未测/D5及下一步，无必读回跳。假设仅委托commit不推导push/merge，令牌不是授权 | 演练时文档任务处于收尾，现已更新完成；没有执行Git/CI |
| drill_learning | 人类README→第一输入单元，1次文档跳转；无需其他指南即可解释零步/多步/失焦、职责、断点和自测 | 初检Look采集来源有缺口；root补右键Delta/UI捕获/单帧yaw-pitch/补步共用，代理实际重读修正后通过；不等于用户已学会 |
| drill_run_extension | 人类README→运行页1次导航；能选UI13生产链、区别旧UI11、给目录/隔离/预期/失败处理。扩展另直达政策，Skill/CI启用条件与Git权限分离 | 只读确认已有输出存在，未启动程序、安装Skill或恢复CI |

Agent预读规则与人类导航分别计量；来源/历史/源码选读不等于必读回跳。任一真实动作仍按当前政策核对所需授权，不能为达跳转指标省略前提。
独立Astra审阅和三个演练都以真实文件为依据；结论是本批范围通过，不能保证任何未来任务/极端分支或所有语义都已穷尽。

## 阅读体量的实测口径

集成阶段相同统计范围：原正式49份为6600行、778756 UTF-8字节；新43份为6803行、853897字节。总文字没有声称减少：本轮增加了单页输入学习、政策扩展约定以及326项可折叠迁移证据，后者属于可选审计内容。
恢复入口原AGENTS＋status＋handoff共225行/38379字节，新AGENTS＋status为77行/8623字节（该次测量）；动作相关政策按任务另读一次，不把省略必要授权当作阅读优化。
这些是行数/字节，不是模型token精确值；收尾记录增加后全仓库数字会变化，最终以size-comparison.json测量为准。主要验收仍是实际阅读路线能否正确完成任务，而不是把所有来源链接删掉。

## 验证与待办

| 项目 | 当前证据 |
|---|---|
| 49份原文快照 | 已复制并逐文件SHA-256核对 |
| 3097个非改造文件 | 已逐文件SHA-256比较，变化/缺失/新增均0；包含资料全目录 |
| 正式文档43份／撤下14新增8 | 实际清单一致，14旧页非空壳；删除前原文件与快照哈希逐一相符 |
| 信息去向与关键语义 | 326项覆盖49来源，所有目标文件/锚点有效；Astra有限核对无剩余有证据P1/P2 |
| 本地链接／锚点／围栏／Git空白 | 最终952本地链接/412锚点、清单/围栏、已跟踪diff --check和8新增文件no-index空白检查通过 |
| 三条主路线及政策扩展演练 | 三个无父上下文代理均通过；学习初检Look缺口已补齐并局部复核 |
| 本次功能测试／Git写操作／CI | 未执行；不属于本批 |

## 下一步与恢复

先检查实际文件、此表和代理列表；保留已完成写入，不重复启动同文件写入者。本批已完成，后续学习/修复用新的任务边界；如果另有未完成任务被中断，先核实真实文件和代理状态。
本批没有待修阻断项。学习掌握、未来Git/CI/Skill实际接入与D5仍是各自未完成/暂缓事项，不被文档验收勾选。用户Git保存另按当前政策执行。


## 逐信息项迁移去向

以下326项覆盖49份源文件。原位置/行号以本次before快照为准；来源标识不是当前导航。按源文件折叠，主阅读路线无需展开本节。新位置可直接打开，合并说明保留为什么迁移。自动检查已确认列出的文件/锚点存在；语义结论仍需独立审阅。

<details>
<summary>AGENTS.md（6项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT001 | 1-9 开始对话 | 规则/状态短入口；读取任务必要材料；不假定旧聊天记忆；核对真实Git/文件 | [AGENTS.md](../../../AGENTS.md)、[status.md](../status.md#resume) | 将AGENTS/status/handoff循环改为规则→状态→任务；不是丢弃恢复要求 |
| ROOT002 | 11-22 当前边界与分工 | V1范围/同范围授权；全模块目标与统一命名；用户环境/Git分工；连续设计与普通细节定案；Sol Ultra/Astra Ultra；新范围确认与环境专项不扩权 | [agent-workflow.md](../agent-workflow.md#current-policy)、[agent-workflow.md](../agent-workflow.md#roles)、[decisions.md](../decisions.md#decision-index)、[v1-baseline.md](../plans/v1-baseline.md) | 稳定原则留根规则，动态分工与具体授权集中到政策，设计目标由基线/决定承接 |
| ROOT003 | 24-29 Git与发布 | 每批实际范围/身份/分支/推送确认；确认框不可见的聊天替代；26处不处理；私有截图/Piccolo/上层Git保护 | [AGENTS.md](../../../AGENTS.md)、[agent-workflow.md](../agent-workflow.md#current-policy)、[git-and-publishing.md](../guides/git-and-publishing.md#publishing-scope) | 权限和操作/白名单分离，当前权限不放宽 |
| ROOT004 | 31-36 代码与学习 | 英文标识/中文解释职责理由；时空单位/生命周期/线程；自研集成研究分别记录；固定Piccolo及homework01各版本；图表候选/确认/实际 | [AGENTS.md](../../../AGENTS.md)、[learning-map.md](../learning-map.md)、[decisions.md](../decisions.md) | 保留方法与来源边界 |
| ROOT005 | 38-50 跨对话持久化 | 重要决定/纠正/验证/阶段结束及时保存；不重复流水账；Plan只读不写；未跟踪快照、本机与远端区别；中断检查代理与进程；send_message不等于恢复 | [AGENTS.md](../../../AGENTS.md#cross-chat-persistence)、[agent-workflow.md](../agent-workflow.md#recovery)、[agent-workflow.md](../agent-workflow.md#task-template) | 通用流程嵌入协作页，旧V1批次另保留 |
| ROOT006 | 52-59 文档维护 | 按信息主要归属维护；当前/历史/候选不混用；下一步/证据/限制完整 | [agent-workflow.md](../agent-workflow.md#document-ownership)、[document-restructure.md](document-restructure.md) | 取消循环入口与无意义多页状态广播；替代旧路径规则有用户D59依据 |

</details>

<details>
<summary>README.md（2项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT007 | 项目介绍与入口 | C#/.NET/OpenGL独立引擎；基础综合训练场V1；运行/学习/验证入口；资料/发布边界 | [README.md](../../../README.md#start)、[v1-baseline.md](../plans/v1-baseline.md) | 合并setup任务导航，技术及操作正文按任务直接进章节 |
| ROOT008 | 同步与资料 | 38cb85f及前史节点；旧45修改+1新增；用户Git工具与公开仓库；原笔记保持 | [status.md](../status.md#resume)、[development-history.md](../development-history.md#timeline)、[git-submission-history.md](../reviews/git-submission-history.md) | 进度单点维护，历史节点不在首页逐批更新 |

</details>

<details>
<summary>g104engine/docs/architecture.md（8项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A001 | 1，原20–51行 | 两项目职责、依赖方向、对象/GUID而非完整ECS、串联训练场和取舍 | [architecture.md](../architecture.md#overview) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A002 | 2，原53–68行 | 设计、输入、feet、NPC、逻辑显示、动画和事实的权威写入者 | [architecture.md](../architecture.md#数据身份与写入者) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A003 | 3，原70–80行 | 固定60Hz、五补步/0.25/丢时、输入边沿、UI安全点、事件分工、主线程/性能限制 | [architecture.md](../architecture.md#frame-flow) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A004 | 4，原82–96行 | Y-up/米秒/行矩阵/GL列数学/独立Jolt预转置、根/统一缩放、自研角色边界 | [architecture.md](../architecture.md#space-contracts) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A005 | 5，原98–114行 | 共享资源、Prepare/Resize失败、owner、Buffer/Voice、图形/粒子有限预算 | [architecture.md](../architecture.md#ownership) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A006 | 6，原116–124行 | PlayClone/Stop保设计、原子保存/坏数据保护、Undo100条、模板与准备提交 | [architecture.md](../architecture.md#scene-lifecycle) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A007 | 7，原126–144行 | 固定参考与差异、全模块学习工程目标、网络/GI/GPU尚未选方法 | [learning-map.md](../learning-map.md#fixed-references) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A008 | 收尾/独立校正，原146–156行 | 动画显示/配置及GGX32F、radiance、导航尾格、近180TRS与UI事件校正 | [architecture.md](../architecture.md#render-contracts) | 当前合同在架构/技术页，修前数值和批次在可选历史/reviews，不复制最新状态 |

</details>

<details>
<summary>g104engine/docs/archive/architecture-draft-2026-10-01.md（1项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT072 | 原历史正文与快照 | 原方案、日期、当时约束、代码围栏、原SHA | [architecture-draft-2026-10-01.md](../archive/architecture-draft-2026-10-01.md) | 正文保护；仅两份的顶部导航href按新路径修正 |

</details>

<details>
<summary>g104engine/docs/archive/foundation-documents-2026-10-02.md（1项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT073 | 原历史正文与快照 | 原方案、日期、当时约束、代码围栏、原SHA | [foundation-documents-2026-10-02.md](../archive/foundation-documents-2026-10-02.md) | 正文保护；仅两份的顶部导航href按新路径修正 |

</details>

<details>
<summary>g104engine/docs/archive/m1-minimal-3d-draft-2026-10-03.md（1项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT074 | 原历史正文与快照 | 原方案、日期、当时约束、代码围栏、原SHA | [m1-minimal-3d-draft-2026-10-03.md](../archive/m1-minimal-3d-draft-2026-10-03.md) | 正文保护；仅两份的顶部导航href按新路径修正 |

</details>

<details>
<summary>g104engine/docs/decisions/0001-foundation.md（1项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT069 | 原0001全部章节 | 用户语言背景和求职/长期目标；8项技术/仓库选择的原因边界；sln候选→实际slnx；私有截图；环境与Git分工及确认框例外；旧C++双仓方案替代；未来选型触发 | [decisions.md](../decisions.md#legacy-0001)、[agent-workflow.md](../agent-workflow.md) | 完整理由原章节归并，现行操作政策独立 |

</details>

<details>
<summary>g104engine/docs/decisions/0002-defer-implementation-architecture.md（1项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT070 | 原0002全部章节 | 2026-10-01先基础后架构；无实现时过细图误导批准；原文SHA归档；两项目非永久冻结；后来正式V1接续 | [decisions.md](../decisions.md#legacy-0002) | 日期/原因/当时尚无Git与后来替代均保留 |

</details>

<details>
<summary>g104engine/docs/execution/v1-progress.md（1项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT071 | 全部历史与恢复记录 | 正式授权原话/来源；P0-P6/分批实现回归；快照路径/模型/中断教训；各次失败修复及38cb85f同步；当时下一步 | [v1-progress.md](v1-progress.md)、[agent-workflow.md](../agent-workflow.md#recovery) | 保留原历史正文，仅加适用性说明和更新围栏外链接；新的当前状态另页维护 |

</details>

<details>
<summary>g104engine/docs/guides/dependencies.md（3项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A054 | 版本/SDK，原6–95行 | 九包/SDK精确配置、global选择/不安装/目录解析/工具类型区别及验证 | [dependencies.md](../guides/dependencies.md#dependency-locks) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A055 | 锁定A/B与还原，原97–180行 | 项目意图/锁图结果、true互补、Sandbox应用/类库消费者限制、还原/运行区别 | [dependencies.md](../guides/dependencies.md#package-locks) | 保留解释；首次可视化A/B完整快照存在，不重复历史步骤 |
| A056 | 主动升级C，原182–228行 | 双项目临时false受控升级/双锁审查/双配置相关功能验证/恢复true/失败策略 | [dependencies.md](../guides/dependencies.md#dependency-upgrade) | 完整操作C保留，启动权限按具体任务 |

</details>

<details>
<summary>g104engine/docs/guides/environment.md（5项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A049 | 当前/初建，原6–125行 | 现有slnx/OutputType/项目方向、旧空模板A/B范围与完整最小代码 | [environment.md](../guides/environment.md#existing-project) | 当前方式正文；完整首次代码基础快照保留，新archive有日期索引 |
| A050 | x64，原127–147行 | 两配置/两项目映射与PlatformTarget、AnyCPU含义、模板/原生结果不同 | [environment.md](../guides/environment.md#222-x64-平台设置) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A051 | SAC，原149–166行 | 状态1→0、错误事件、用户取舍/其他保护未知、整机影响/重开未实测 | [environment.md](../guides/environment.md#sac-diagnosis) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A052 | JMC，原168–179行 | 优化模块/Release普通运行/Debug断点、临时关闭恢复及历史确认 | [environment.md](../guides/environment.md#debugging) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A053 | 安装/三包，原12–35/181–206行 | 组件/SDK vsRuntime/可选workload不等于VS、安装者与历史三包已替换 | [environment.md](../guides/environment.md#missing-environment) | 按专题合并保留；动态结果使用日期明确的可选证据 |

</details>

<details>
<summary>g104engine/docs/guides/git-workflow.md（8项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A066 | 首次Git全部章节，原14–194行 | 初建/身份/master/31清单/首推SHA/tree/GUI上游/0 0本地缓存/M README未提交/未查服务器 | [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#git-foundation) | 完整已有命令和manifest原字节快照不重复制；新增演变说明和当时条件全部保留；现行guide归C |
| C006 | 当前同步与每批提交流程 | 用户Git Bash/Git GUI、助手只读核对；本地refs不等实时服务器；实际范围/暂存index/验证/说明/身份/目标/推送意图逐批核对和明确确认；本机保存、commit、push分别记录；历史31文件不是固定清单 | [git-and-publishing.md](../guides/git-and-publishing.md#daily-git)、[agent-workflow.md](../agent-workflow.md) | Git命令/真实范围/身份/index/验证流程保持可复用；当前用户执行与助手只读分工、确认形式和授权政策集中在agent-workflow，指南不生成或固定执行者权限 |
| C007 | 5 建仓环境与私有目录 | Git Bash /e/game_study/games104对应Windows根；现有main/origin/upstream已建立；VS Git暂不用；私有操作流程排除、Piccolo独立仓库及不子模块 | [git-and-publishing.md](../guides/git-and-publishing.md#daily-git)、[git-and-publishing.md](../guides/git-and-publishing.md#publishing-scope) | 当前操作前提和范围就近提供，首次过程按历史查阅 |
| C008 | 5.1 GitHub网页建立空仓库 | 2026-10-02 public/size=0/main及ls-remote无引用是当时为空证据；空仓库创建步骤、无额外README/gitignore/license、HTTPS且不传令牌；当前已有多批历史，不重复创建覆盖 | [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#git-foundation) | 首次完整过程已在旧foundation-documents快照覆盖；A新准备历史索引并补时态，不再复制相同代码 |
| C009 | 5.2 本地初始化、关联、分支与身份 | git init -b main、remote add只在目标根且空origin时；曾master无提交/身份，用户后续更名并仓库级补设；姓名/已验证邮箱或noreply选择；不写密码令牌；当前配置已完成；不再初始化/重绑/重设 | [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#git-foundation)、[git-and-publishing.md](../guides/git-and-publishing.md#daily-git) | 首次命令唯一完整快照由A索引，日常核查保留现行指南 |
| C010 | 5.3 31文件首次manifest及add/commit/push | 3ea75150225e6df5fad9c92a5d03f2b1ad087be7本地/远端及树曾一致；完整31路径；4根/8工程配置源码/8文档/10正文/1图；Class1只是模板、当时本机双配置/用户GL4.3Core/smoke，不是V1实现；当时31未跟踪且无其它跟踪改动才能add --all；不是现行全量暂存许可；首次推荐提交说明/commit与push -u、审批流程、缓存排除及锁文件保留 | [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#git-foundation) | 旧archive已含完整manifest/命令；新archive保留阶段补记与唯一原文索引 |
| C011 | 5.4 首推后上游补设 | 当次GUI未找到绑定入口，不推成所有GUI不支持；用户Git Bash --set-upstream-to、branch -vv/status -sb证据；branch.main.remote/merge与0 0只比较缓存提交；M README仍未提交；当时未重新查服务器；当前已绑定无需重复 | [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#git-foundation) | 保留操作经验和证据限制；C已发A必要后续事实清单 |
| C012 | 助手随后验证 | 只读核对根/分支/身份/说明/提交路径/远端树/白名单；本机保存/本地提交/实时同步三个状态分列；无需安装gh，核查不夹Git写操作 | [git-and-publishing.md](../guides/git-and-publishing.md#daily-git) | 日常流程保留只读复核动作与状态分列；执行者按当前政策指定，不在指南建立第二份永久分工 |

</details>

<details>
<summary>g104engine/docs/guides/physics-and-gameplay.md（7项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A036 | 职责尺寸，原5–20行 | 自研/集成、feet/胶囊/默认Skin半径合同/球Ramp、显示逻辑分离 | [physics-and-gameplay.md](../guides/physics-and-gameplay.md#职责与数据) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A037 | 矩阵寿命，原22–32行 | Jolt托管/原生固定提交、ToJolt/QueryTransform、尺寸Center和Foundation多世界 | [physics-and-gameplay.md](../guides/physics-and-gameplay.md#jolt-matrix) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A038 | 控制器，原34–45行 | ray/sweep/overlap接口、运动顺序/皮肤/跨步/斜坡与明确未支持 | [physics-and-gameplay.md](../guides/physics-and-gameplay.md#character) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A039 | 导航NPC，原47–63行 | 范围半开/2.05格心/ULP/零尾格/double容量、A*预算净空、感知当前朝向/下一步意图 | [physics-and-gameplay.md](../guides/physics-and-gameplay.md#npc-navigation) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A040 | 机关，原65–71行 | 按钮Target/距离视线占用、画面Body网格同步、Tick事实/反馈键/Goal锁存 | [physics-and-gameplay.md](../guides/physics-and-gameplay.md#interaction-events) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A041 | 验证及历史，原73–117行 | 38例/800九路，三轮真实fail/pass/原生行为/日志/精确数值与旧警告 | [physics-and-gameplay.md](../guides/physics-and-gameplay.md#physics-evidence) | 入口在正文，长历史全文保留为可选展开，无新验回声明 |
| A042 | 课程固定参考，原119–123行 | 物理/Gameplay/AI章节及Piccolo目标overlap不冒称完整sweep | [physics-and-gameplay.md](../guides/physics-and-gameplay.md#课程与参考映射) | 按专题合并保留；动态结果使用日期明确的可选证据 |

</details>

<details>
<summary>g104engine/docs/guides/probes.md（2项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A028 | 当前入口，原6–16行 | 保留有限smoke/PASS打印语义、已有DLL运行及真实GL/验证区别 | [v1-run-and-review.md](../guides/v1-run-and-review.md#按任务选择验证) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A029 | 4.3/4.4，原18–240行 | 临时完整GL/Smoke代码、当时实际API/上下文/版本/运行与验收 | [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#foundation-20261002) | 完整相同代码已在1415行基础原字节快照，补遗链接并记后续替代，无重复复制 |

</details>

<details>
<summary>g104engine/docs/guides/rendering-and-animation.md（6项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A043 | 入口/空间，原5–36行 | 格式与Shader职责、row/column、mesh相对palette/镜像/negative skin/128容量/前向脚底 | [rendering-and-animation.md](../guides/rendering-and-animation.md#skinning-space) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A044 | 格式，原38–50行 | 全部支持拒绝子集、有效UV/覆盖、skin/rigid共享mesh、来源素材统计 | [rendering-and-animation.md](../guides/rendering-and-animation.md#实际导入子集) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A045 | Pass颜色数值，原52–84行 | PCF偏移理由、GGX稳定公式/half单位化/roughness、32F预算、radiance和基础色分域、透明Z/FXAA/统计 | [rendering-and-animation.md](../guides/rendering-and-animation.md#render-passes) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A046 | 动画，原86–117行 | 三层插值、采样/短弧、四状态/跳落与事件、配置所有字段/错误/共享只读及重启 | [rendering-and-animation.md](../guides/rendering-and-animation.md#animation-events) | 补充DrainAnimationEvents仅调试字符串，Voice/Burst消费Simulation事实 |
| A047 | API寿命，原119–129行 | Prepare/ResetScene/ResetAnimations/ResetDisplayHistory/resize/context/dispose及UTF8长度EOF原因 | [rendering-and-animation.md](../guides/rendering-and-animation.md#renderer-lifecycle) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A048 | 验证和历史，原131–167行 | 独立GPUfixtures/参考/十项数值、动画检查、首轮截图/MAE/PCF/tangent及未收集FXAA镜像 | [rendering-and-animation.md](../guides/rendering-and-animation.md#验证边界与参考) | 全部历史数值/路径保留在两个可选展开，算法理由仍就地保留 |

</details>

<details>
<summary>g104engine/docs/guides/reproduction.md（1项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A065 | 6，原6–17行 | D5整暂缓/未来克隆而非重建/精确环境/同步缓存/同机聊天/未测试 | [reproduction.md](../guides/reproduction.md#reproduction) | 按专题合并保留；动态结果使用日期明确的可选证据 |

</details>

<details>
<summary>g104engine/docs/guides/scene-and-editor.md（5项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A031 | 数据/空间，原5–17行 | GUID/字段写入/稳定TRS分解、父限制/DTO无活动相机刚体、同alpha局部组合 | [scene-and-editor.md](../guides/scene-and-editor.md#scene-contracts) | 详细合同保留，单字段修复理由与1e-4/double不放宽约束 |
| A032 | 时钟输入，原19–25行 | Clock接口/丢步/统计Reset、InputBuffer消费、生产UI事件/清焦两职责 | [scene-and-editor.md](../guides/scene-and-editor.md#time-and-input) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A033 | 文件模板，原27–33行 | 严格JSON/16MiB/path及链接根、原子保存/Clone、模板白名单 | [scene-and-editor.md](../guides/scene-and-editor.md#文件与模板) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A034 | 命令历史，原35–43行 | 全部SceneEditor方法、参数约束/20000格、子树事务、100条History、草稿和Saved | [scene-and-editor.md](../guides/scene-and-editor.md#editor-transactions) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A035 | 检查/保护，原45–59行 | CPU/11UI/13生产输入不同覆盖、PrepareChange/资源失败回滚/坏文件 | [scene-and-editor.md](../guides/scene-and-editor.md#cpu-检查与验收边界) | 按专题合并保留；动态结果使用日期明确的可选证据 |

</details>

<details>
<summary>g104engine/docs/guides/v1-architecture-and-learning.md（8项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A009 | 证据分层，原5–19行 | 构建/verify/UI/GPU/接触/audio/图形/人工/学习各自覆盖与不能推出结论 | [v1-run-and-review.md](../guides/v1-run-and-review.md#按任务选择验证) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A010 | 逐文件入口，原21–47行 | Program到Audio的全部实际入口、归属与阅读问题 | [architecture.md](../architecture.md#实际文件入口) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A011 | 输入/帧流，原49–101行 | 一次意图→角色→动画/逻辑/事实/声音画面与实际帧图、UI事件职责 | [architecture.md](../architecture.md#frame-flow) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A012 | 设计/准备，原103–126行 | 三棵树、单层模板、Play准备提交图、资源回滚、保存路径与隔离根 | [architecture.md](../architecture.md#scene-lifecycle) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A013 | 所有权，原128–141行 | GL当前context线程、部分失败释放、Jolt Foundation世界计数、声音先Voice后Buffer | [architecture.md](../architecture.md#ownership) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A014 | 矩阵，原143–151行 | 三个矩阵边界、65/128palette、mesh/实例/root各应用一次与Vulkan差异 | [architecture.md](../architecture.md#space-contracts) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A015 | 阅读/自测，原153–168行 | 首次启动/帧循环/W Space/E链及六模块阅读问题 | [learning-map.md](../learning-map.md#first-input-lesson) | 扩为就地最低背景/调用数据/断点/0步多步失焦例/自测，保留全模块映射 |
| A016 | 长期/限制，原170–180行 | 固定参考限制、未来全部实践、有限V1与动画Reset两层 | [learning-map.md](../learning-map.md#module-scope) | 按专题合并保留；动态结果使用日期明确的可选证据 |

</details>

<details>
<summary>g104engine/docs/guides/v1-run-and-review.md（4项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A024 | 运行/键鼠，原5–22行 | 现有VS/x64/默认Edit/PlayStop和所有按键、镜头NPC/门目标 | [v1-run-and-review.md](../guides/v1-run-and-review.md#run-and-verify) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A025 | 编辑保存，原24–59行 | 树/材质约束、文本/拖动事务、Saved基准、真实路径、坏原件保护及8步Load/Reset | [v1-run-and-review.md](../guides/v1-run-and-review.md#design-protection) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A026 | 人工/学习，原61–79行 | 人工五项步骤、2026-10-04明确反馈、球无Collider/Ramp、Goal面板与动画 | [v1-run-and-review.md](../guides/v1-run-and-review.md#定向人工操作与学习) | 执行操作正文保留；当时首轮反馈置可选展开，当前结论只查status |
| A027 | 验证，原81–106行 | no-restore构建、九类模式/隔离根/实际本机缓存和批次不可混同 | [v1-run-and-review.md](../guides/v1-run-and-review.md#run-and-verify) | 新增按任务选入口、预期输出/退出码、明确目录/数据位置和失败处理，不运行 |

</details>

<details>
<summary>g104engine/docs/handoff.md（56项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT011 | 1-25 当前接续与三批修复 | 用户无法穷尽分支仍确认初步通过；球/Ramp与UI具体失败和修复；代理interrupted投递不等于恢复；V1完整连续开工授权；本机与远端区别 | [status.md](../status.md)、[development-history.md](../development-history.md)、[agent-workflow.md](../agent-workflow.md#recovery)、[v1-progress.md](v1-progress.md) | 事实/经验/授权分别归位；原始细节保留对应专项review |
| ROOT012 | 换聊天说明/107-134 | 同工作区读取未提交文件；不因换聊新克隆/工作树；压缩次数不是强制换聊条件；未落盘/跨设备限制；用户环境与Git职责 | [agent-workflow.md](../agent-workflow.md#recovery)、[AGENTS.md](../../../AGENTS.md)、[status.md](../status.md) | 恢复提示成为短流程；无需复制旧聊天上下文 |
| ROOT013 | D01 | 短期 Gameplay/客户端求职，长期独立 3D 引擎；渲染、物理、动画、Gameplay、AI、工具等所有模块均需不同程度工程实践 | [decisions.md](../decisions.md#d01) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT014 | D02 | 按掌握程度推进；旧 2–4 周、每周 10–15 小时不是当前总期限；未设新硬期限 | [decisions.md](../decisions.md#d02) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT015 | D03 | 关键原理自研配合成熟库；说明自研、集成及简化边界；以笔记和固定 Piccolo 参考实现自己的引擎 | [decisions.md](../decisions.md#d03) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT016 | D04 | 助手主要编码，用户学习/运行/调试/验收/讨论；完整V1按D54连续实施，普通内部检查不逐项等待，重大取舍先讨论 | [decisions.md](../decisions.md#d04) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT017 | D05 | 综合第三人称训练场：移动、跳跃、交互/机关、NPC；基础场景当前不含战斗 | [decisions.md](../decisions.md#d05) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT018 | D06 | 运行窗口内有限调试编辑面板已采用ImGui.NET实现；查看状态、调参和设计保存重载已落地 | [decisions.md](../decisions.md#d06) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT019 | D07 | 借鉴 Piccolo 的对象组件组织，明确系统更新阶段；起步不采用完整 ECS | [decisions.md](../decisions.md#d07) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT020 | D08 | 固定模拟60Hz、绘制独立；V1已实现五补步上限、丢时、焦点/暂停清理、输入边沿消费和显示插值，具体合同见实际指南 | [decisions.md](../decisions.md#d08) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT021 | D09 | 控制器驱动世界位移，基础动画 in-place；相对镜头移动；首块控制器先验收平地/墙面/滑动/跳跃，复杂地形后续 | [decisions.md](../decisions.md#d09) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT022 | D10 | 世界 Y-up 右手；默认局部前 -Z、右 +X；米/秒；CPU 沿用 OpenTK 原生行向量、GLSL 列向量，集中记录映射 | [decisions.md](../decisions.md#d10) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT023 | D11 | 关卡资源共享至场景卸载；新场景准备成功后才替换旧场景 | [decisions.md](../decisions.md#d11) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT024 | D12 | 基础阶段保留 Engine/Sandbox 两项目：通用能力与演示/玩法分工；后续按实际需求再评估拆分 | [decisions.md](../decisions.md#d12) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT025 | D13 | 早期绘制采用环绕观察相机，合入 V1 后仍保留；不是角色相机，也不追认原 M1 的所有细节 | [decisions.md](../decisions.md#d13) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT026 | D14 | 网络、动态 GI、GPU 几何后续开专题分支研究；先保留稳定基线，允许有理由且可验收的局部重构 | [decisions.md](../decisions.md#d14) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT027 | D15 | 保留可学习的基线/改进对照；V1已提供编辑态选择Forward/Deferred、下次Play生效，后续专题的即时切换/重载另定 | [decisions.md](../decisions.md#d15) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT028 | D16 | 架构文档/图、中文注释、代码—笔记—固定参考—简化方案—证据随进展维护；新对话依靠文件接续 | [decisions.md](../decisions.md#d16) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT029 | D17 | 正式光照先 Forward，再实际实现 Deferred 对比；基础绘制在 V1 内验证，原独立 M1 流程已被 D44 合并 | [decisions.md](../decisions.md#d17) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT030 | D18 | 此前已选渲染模块目标含 PBR、基础阴影、IBL、天空盒、HDR/色调映射/FXAA；当前 V1 逐项归属以范围草案/明确决定为准，不要求全部渲染完成才接入玩法 | [decisions.md](../decisions.md#d18) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT031 | D19 | 地形、天空/云、AO、雾等扩展专题，每类先做代表性实验，再按需整合进训练场 | [decisions.md](../decisions.md#d19) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT032 | D20 | 主要输入采用glTF/GLB，SharpGLTF及受控支持子集已实现并部署素材；当前不读FBX，后续外部转换工具按素材任务选定 | [decisions.md](../decisions.md#d20) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT033 | D21 | 基础版先保存设计场景；运行状态存档另设阶段，不将全部 Runtime 内存直接写回场景 | [decisions.md](../decisions.md#d21) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT034 | D22 | 单层模板与明确实例覆盖已实现；嵌套/变体/完整Apply-Revert不在V1，后续再细化 | [decisions.md](../decisions.md#d22) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT035 | D23 | 角色/物理长期含坡台、平台、推箱；D47收敛的V1平地/墙滑/跳跃/有限坡台已实现，平台/推箱后移 | [decisions.md](../decisions.md#d23) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT036 | D24 | 自研角色规则配合JoltPhysicsSharp后端查询/刚体适配，已完成本机原生调用与行为验证 | [decisions.md](../decisions.md#d24) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT037 | D25 | 布娃娃、布料/PBD/XPBD、破坏、车辆等进阶物理先独立代表实验，再按需整合 | [decisions.md](../decisions.md#d25) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT038 | D26 | 基础动画终点为 Idle/Walk/Run 速度混合、跳跃状态机、平滑过渡和基础事件；Mask/Additive/脚部 IK 后续实验 | [decisions.md](../decisions.md#d26) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT039 | D27 | 动画采用小型可配置状态机与混合运行时，先有状态/权重调试显示，不同时制作完整节点编辑器 | [decisions.md](../decisions.md#d27) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT040 | D28 | V1已采用同一骨架相容动作，65关节模型及43个LINEAR clips已核验；重定向另做专题 | [decisions.md](../decisions.md#d28) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT041 | D29 | 基础玩法先 C# 规则＋数据配置，Lua/可视化脚本留后续专题；数据重载不等于代码热更新 | [decisions.md](../decisions.md#d29) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT042 | D30 | 基础 AI 先 FSM，再小型 BT 对照同一巡逻/跟随/搜索场景；与动画 FSM 职责分离 | [decisions.md](../decisions.md#d30) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT043 | D31 | 自研受控平面网格A*与NPC控制器跟随已实现；NavMesh对照的库/范围仍待后续专题 | [decisions.md](../decisions.md#d31) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT044 | D32 | 粒子先 CPU 生命周期/Billboard，再 GPU Compute 实际对比；不等于提前启动 Nanite/GPU 几何专题 | [decisions.md](../decisions.md#d32) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT045 | D33 | 声音基础版做到 2D/3D 播放、方位距离、一次性/循环事件及基本播放管理；遮挡/混响等后续实验 | [decisions.md](../decisions.md#d33) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT046 | D34 | V1 Listener随相机位置/朝向，OpenTK OpenAL＋OpenAL Soft后端已接入；后续可按试听问题评估调整 | [decisions.md](../decisions.md#d34) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT047 | D35 | ImGui.NET有限场景编辑、约定Undo/Redo和PlayStop保护已实现；通用编辑器/Gizmo/视口拾取等另行分期 | [decisions.md](../decisions.md#d35) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT048 | D36 | Play 使用当前内存设计数据，Stop 恢复设计预览并保留未保存编辑；运行变化不自动写回设计，也不等于运行存档 | [decisions.md](../decisions.md#d36) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT049 | D37 | 用户关注初版成本后确认：初版仅 FPS/帧耗时，CPU/GPU 详细计时与相关计数按问题或对比实验需要加入；不作为初版验收要求，模块调试显示仍保留 | [decisions.md](../decisions.md#d37) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT050 | D38 | 基础主线程按明确阶段更新与提交图形，同步准备场景；允许加载短暂停顿，不代表整个进程只有一条线程 | [decisions.md](../decisions.md#d38) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT051 | D39 | 后续数据布局对比＋小型 ECS 实验，再按需局部接入；不预定主引擎整体迁移 | [decisions.md](../decisions.md#d39) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT052 | D40 | 先用 .NET 并行库，再自研有限任务调度原型，先用成熟同步原语；Fiber/复杂无锁等后续细化，详细计时按实验需要加入 | [decisions.md](../decisions.md#d40) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT053 | D41 | Deferred已纳入并实现V1，与Forward共用场景/资产；G-buffer/格式/切换合同见实际指南，其他渲染扩展后移 | [decisions.md](../decisions.md#d41) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT054 | D42 | V1有限设计操作Undo/Redo已实现，含拖动事务/草稿提交及真实保存基准；运行状态不纳入撤销 | [decisions.md](../decisions.md#d42) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT055 | D43 | 统一以“基础综合训练场 V1”为主名称，注明“用于面试展示”；旧面试 Demo/面试 V1 指同一版本；M1 后续按 D44 合入，不因名称统一批准其余范围 | [decisions.md](../decisions.md#d43) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT056 | D44 | 最小 3D 里程碑正式合入基础综合训练场 V1，必要绘制/变换/资源验证保留为内部工作；旧草案归档，不再独立交付或审批，原参数不自动获批 | [decisions.md](../decisions.md#d44) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT057 | D45 | 开始写代码前必须弹窗展示具体实施范围，得到用户明确同意后再开始；普通范围/选型回答和“推进下一步”不代替开工确认 | [decisions.md](../decisions.md#d45) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT058 | D46 | V1 渲染含 Forward/Deferred、PBR、基础阴影、天空盒、HDR/色调映射/FXAA；IBL 后移，具体算法参数待实施方案 | [decisions.md](../decisions.md#d46) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT059 | D47 | V1 角色/物理含平地、墙滑、跳跃与有限坡台；平移平台/受控推箱后移，自研角色规则分工保持 | [decisions.md](../decisions.md#d47) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT060 | D48 | V1有限创建/删除/变换/参数编辑、Undo/Redo、Play/Stop已落地并保留未保存设计，字段/事务以实际指南为准 | [decisions.md](../decisions.md#d48) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT061 | D49 | V1实际采用SharpGLTF、StbImageSharp、JoltPhysicsSharp/Native、ImGui.NET、OpenTK OpenAL及OpenAL Soft；依赖/素材部署、功能验证与正式授权均已完成 | [decisions.md](../decisions.md#d49) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT062 | D50 | Kenney Ogg保持离线转PCM16路线，7个选定音效已转换，另有1个生成循环测试音；不新增运行时Ogg解码库 | [decisions.md](../decisions.md#d50) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT063 | D51 | 必要设计需完整连贯，普通细节由助手按授权决定，重大取舍集中询问；完整V1必要设计与D54开工授权已完成，新范围沿用此规则 | [decisions.md](../decisions.md#d51) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT064 | D52 | 已实现受控父子层级、挂接/保存/Undo；当前Player/Npc为根单位缩放、父组限正统一缩放，角色/移动门下仅纯显示后代。设计期活动相机/动态刚体组件限制属扩展约束，当前DTO未提供这两类组件 | [decisions.md](../decisions.md#d52) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT065 | D53 | GPT-6.1 Sol Ultra主实施与实施子任务，GPT-6 Astra Ultra关键评审/独立核查；两者均Ultra，质量优先，不为节省自行降档；主对话由界面选择，子任务显式指定 | [decisions.md](../decisions.md#d53) | 原编号与决定语义保留；旧来源改为合并后正文 |
| ROOT066 | D54 | 正式弹窗明确同意完整V1连续实施；代码/Shader/设计数据/必要复制配置、既有素材/OpenAL/便携FFmpeg校验转换、构建运行修复与文档/快照已授权，同范围恢复不重问 | [decisions.md](../decisions.md#d54) | 原编号与决定语义保留；旧来源改为合并后正文 |

</details>

<details>
<summary>g104engine/docs/learning-map.md（7项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A017 | 1，原7–15行 | 理论/参考/本工程三个进度，不等同原课学完或工程全部实现 | [learning-map.md](../learning-map.md#learning-progress) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A018 | 2，原17–34行 | 十二模块理论/参考/自研集成与未实施子集 | [learning-map.md](../learning-map.md#module-scope) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A019 | 3.1–3.10，原36–136行 | 十份主笔记路径与精确章节、实际代码、自研/集成/仅研究及未来路线 | [learning-map.md](../learning-map.md#notes-by-module) | 完整主笔记引用保留原路径；当前代码与后续实验在正文，历史通过在可选证据 |
| A020 | 3.11，原138–148行 | 输入/三树/矩阵/事实/设计运行/寿命/未来实验跨模块复习 | [learning-map.md](../learning-map.md#311-跨模块复习顺序) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A021 | 4，原150–164行 | Piccolo固定SHA具体入口及不同实现、homework01原版语境 | [learning-map.md](../learning-map.md#fixed-references) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A022 | 5，原166–180行 | 知识点/状态/代码/笔记/固定参考/简化/性质/证据/接续记录字段 | [learning-map.md](../learning-map.md#recording-template) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A023 | 首轮基线及模块证据/收尾，原5、21–32、48/59/70/80/91/101/195行 | 历史240帧/38原生等覆盖及学习/专项缺口、动画配置/显示验回 | [learning-map.md](../learning-map.md#learning-evidence) | 日期固定可选历史原语义；当前验收/接续统一status |

</details>

<details>
<summary>g104engine/docs/plan.md（10项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| B062 | L1–14 | 原页首当前版本/素材/修复/输入批次 | [v1-baseline.md](../plans/v1-baseline.md#scope) | 范围与输入取舍保留在V1基线；动态阶段/验收/保存节点不继续复制到总计划 |
| B063 | L15–24 | 项目目标、用户背景、全模块实际实践、无硬期限、自研集成、学习证据 | [plan.md](../plan.md#goals) | 保全稳定需求与学习推进条件 |
| B064 | L25–40 | 已确认开发基础、精确依赖/部署、参考版本与输入路线 | [plan.md](../plan.md#foundation) | 保留基础方向/理由，精确锁定版本和来源归A依赖指南 |
| B065 | L42–59 | 功能方向、两项目、对象/阶段、时间运动、空间、共享/准备失败与专题边界 | [plan.md](../plan.md#foundation) | 方向保留；V1设计取舍与实际低级合同分别归baseline和A架构 |
| B066 | L61–79 | 版本口径、M1、未定排期、内部编号与扩展方法 | [plan.md](../plan.md#stages) | 保留版本数量/顺序/期限未定，模块编号与Git分支不是版本 |
| B067 | L81–102 | 十三类模块目标、V1范围与未来实践 | [plan.md](../plan.md#roadmap) | 保留完整模块/长期目标；V1列改为批准边界，未来实验不是授权/完成 |
| B068 | L104–122 | 目录与发布白名单、私有资料和参考仓库边界 | [plan.md](../plan.md#execution-boundaries) | 保留范围保护的短说明；精确发布路径交C的Git与发布指南 |
| B069 | L124–130 | 分工/权限/确认方式/global专项委托/完整V1授权 | [v1-baseline.md](../plans/v1-baseline.md#authorization-history) | 历史授权解释归baseline；当前权限政策归root的agent-workflow，不形成总计划重复政策 |
| B070 | L132–141 | D0–D4基础阶段、D5整体暂缓 | [plan.md](../plan.md#stages) | 基础历史归root发展历史；D5整体暂缓保留执行前提，未来计划不恢复它 |
| B071 | L143–149 | 当前下一步与首轮体验/源码掌握差别 | [plan.md](../plan.md#stages) | 仅保留稳定学习推进方式，当前具体下一步移交status |

</details>

<details>
<summary>g104engine/docs/plans/animation-roadmap.md（7项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| B001 | L1–8 | 页首：模块定位、当前状态/验收/修复与实际入口 | [animation-roadmap.md](../plans/animation-roadmap.md#choices)、[status.md](../status.md)、[development-history.md](../development-history.md) | 保留模块定位和起步选择；重复动态进展/批次结果由status与历史/评审单点维护，不作为计划表完成证明 |
| B002 | L9–18 | 1. 已确认的选择 | [animation-roadmap.md](../plans/animation-roadmap.md#choices) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B003 | L19–28 | 2. 先打通可解释的数据流 | [animation-roadmap.md](../plans/animation-roadmap.md#data-flow) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B004 | L29–43 | 3. 基础运行时的实现映射与后续分块 | [animation-roadmap.md](../plans/animation-roadmap.md#practice) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B005 | L44–55 | 4. 输入和时间的技术边界 | [animation-roadmap.md](../plans/animation-roadmap.md#contracts) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B006 | L56–73 | 5. 进阶代表实践与覆盖边界 | [animation-roadmap.md](../plans/animation-roadmap.md#advanced) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B007 | L74–82 | 6. 素材与学习验收 | [animation-roadmap.md](../plans/animation-roadmap.md#materials) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |

</details>

<details>
<summary>g104engine/docs/plans/assets-scene-roadmap.md（8项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| B008 | L1–8 | 页首：模块定位、当前状态/验收/修复与实际入口 | [assets-scene-roadmap.md](../plans/assets-scene-roadmap.md#choices)、[status.md](../status.md)、[development-history.md](../development-history.md) | 保留模块定位和起步选择；重复动态进展/批次结果由status与历史/评审单点维护，不作为计划表完成证明 |
| B009 | L9–18 | 1. 已确认的选择 | [assets-scene-roadmap.md](../plans/assets-scene-roadmap.md#choices) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B010 | L19–30 | 2. 要完成的闭环与三层数据 | [assets-scene-roadmap.md](../plans/assets-scene-roadmap.md#data-layers) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B011 | L31–40 | 3. 已实现输入子集与扩展边界 | [assets-scene-roadmap.md](../plans/assets-scene-roadmap.md#input) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B012 | L41–53 | 4. 设计场景、模板和运行状态 | [assets-scene-roadmap.md](../plans/assets-scene-roadmap.md#save) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B013 | L54–65 | 5. 已采用的身份与引用 | [assets-scene-roadmap.md](../plans/assets-scene-roadmap.md#identity) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B014 | L66–82 | 6. 实现映射与后续分块 | [assets-scene-roadmap.md](../plans/assets-scene-roadmap.md#practice) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B015 | L83–89 | 7. 笔记、Piccolo 与后续讨论 | [assets-scene-roadmap.md](../plans/assets-scene-roadmap.md#references) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |

</details>

<details>
<summary>g104engine/docs/plans/basic-training-ground-v1-draft.md（7项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| B072 | L1–35 | 目的/命名/批准范围/后移项目/成果与学习口径 | [v1-baseline.md](../plans/v1-baseline.md#scope) | 合并成唯一V1范围表，保留缩小深度而不降低行为正确性 |
| B073 | L37–53 | Deferred与Undo设计深度、参数/格式/资源及验收边界 | [v1-baseline.md](../plans/v1-baseline.md#design-choices) | 保留功能深度/设计理由，低级Pass/字段/参数由A指南维护 |
| B074 | L55–67 | 候选3–5分钟演示、三条解释链路、反馈/失败与发布边界 | [v1-baseline.md](../plans/v1-baseline.md#acceptance) | 保留建议性质，不把演示时长/录像等升级为批准验收要求 |
| B075 | L69–76 | 后续层次/未知版本总数/专题开工 | [v1-baseline.md](../plans/v1-baseline.md#version-continuation) | 保留后续分期原则、没有冻结大V2/V3和新授权 |
| B076 | L77–94 | M1合并时间/当时基线/工作映射/旧候选不生效 | [v1-baseline.md](../plans/v1-baseline.md#m1-integration-proposal) | 保留M1合并与随后正式开工授权的区别，旧原文仍在已有归档 |
| B077 | L95–109 | 连续开发、自查预览、不逐检查审批、必要准备与范围确认 | [v1-baseline.md](../plans/v1-baseline.md#authorization-history) | 保留用户希望完整版本和内部增量验证的组织选择、正式授权来源 |
| B078 | L111–117 | 历史基线、求职展示来源、当前未完学习/未来目标 | [v1-baseline.md](../plans/v1-baseline.md#acceptance) | 保留经验和来源适用限制，动态完成/未完成由状态维护 |

</details>

<details>
<summary>g104engine/docs/plans/core-architecture-roadmap.md（8项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| B016 | L1–6 | 页首：模块定位、当前状态/验收/修复与实际入口 | [core-architecture-roadmap.md](../plans/core-architecture-roadmap.md#choices)、[status.md](../status.md)、[development-history.md](../development-history.md) | 保留模块定位和起步选择；重复动态进展/批次结果由status与历史/评审单点维护，不作为计划表完成证明 |
| B017 | L7–18 | 1. 起步选择与长期边界 | [core-architecture-roadmap.md](../plans/core-architecture-roadmap.md#choices) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B018 | L19–26 | 2. 已实现基础主流程与分工 | [core-architecture-roadmap.md](../plans/core-architecture-roadmap.md#main-loop) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B019 | L27–40 | 3. 数据布局与小型 ECS 的候选实践 | [core-architecture-roadmap.md](../plans/core-architecture-roadmap.md#layout) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B020 | L41–69 | 4. 并行计算与小型任务调度的候选实践 | [core-architecture-roadmap.md](../plans/core-architecture-roadmap.md#jobs) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B021 | L70–81 | 5. 保留的进阶议题与代表实验候选 | [core-architecture-roadmap.md](../plans/core-architecture-roadmap.md#advanced) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B022 | L82–90 | 6. 如何判断实验完成 | [core-architecture-roadmap.md](../plans/core-architecture-roadmap.md#acceptance) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B023 | L91–97 | 7. 参考与接续 | [core-architecture-roadmap.md](../plans/core-architecture-roadmap.md#references) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |

</details>

<details>
<summary>g104engine/docs/plans/gameplay-ai-roadmap.md（8项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| B024 | L1–8 | 页首：模块定位、当前状态/验收/修复与实际入口 | [gameplay-ai-roadmap.md](../plans/gameplay-ai-roadmap.md#choices)、[status.md](../status.md)、[development-history.md](../development-history.md) | 保留模块定位和起步选择；重复动态进展/批次结果由status与历史/评审单点维护，不作为计划表完成证明 |
| B025 | L9–18 | 1. 已确认的选择 | [gameplay-ai-roadmap.md](../plans/gameplay-ai-roadmap.md#choices) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B026 | L19–33 | 2. 核心Gameplay的V1范围 | [gameplay-ai-roadmap.md](../plans/gameplay-ai-roadmap.md#gameplay) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B027 | L34–41 | 3. 请求、事实、记忆与动画各管什么 | [gameplay-ai-roadmap.md](../plans/gameplay-ai-roadmap.md#contracts) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B028 | L42–75 | 4. 基础AI闭环与后续分块 | [gameplay-ai-roadmap.md](../plans/gameplay-ai-roadmap.md#ai) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B029 | L76–86 | 5. 导航不是角色控制器的替代品 | [gameplay-ai-roadmap.md](../plans/gameplay-ai-roadmap.md#navigation) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B030 | L87–102 | 6. 进阶专题候选实践 | [gameplay-ai-roadmap.md](../plans/gameplay-ai-roadmap.md#advanced) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B031 | L103–107 | 7. Piccolo 参考与后续收敛 | [gameplay-ai-roadmap.md](../plans/gameplay-ai-roadmap.md#references) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |

</details>

<details>
<summary>g104engine/docs/plans/particles-audio-roadmap.md（8项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| B032 | L1–8 | 页首：模块定位、当前状态/验收/修复与实际入口 | [particles-audio-roadmap.md](../plans/particles-audio-roadmap.md#choices)、[status.md](../status.md)、[development-history.md](../development-history.md) | 保留模块定位和起步选择；重复动态进展/批次结果由status与历史/评审单点维护，不作为计划表完成证明 |
| B033 | L9–18 | 1. 已确认的选择 | [particles-audio-roadmap.md](../plans/particles-audio-roadmap.md#choices) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B034 | L19–28 | 2. 先区分模板、实例和可见/可听结果 | [particles-audio-roadmap.md](../plans/particles-audio-roadmap.md#data-layers) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B035 | L29–43 | 3. 粒子实现映射与后续分块 | [particles-audio-roadmap.md](../plans/particles-audio-roadmap.md#particles) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B036 | L44–56 | 4. 已实现声音终点与后续分块 | [particles-audio-roadmap.md](../plans/particles-audio-roadmap.md#audio) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B037 | L57–66 | 5. 第三人称听者与跨系统反馈 | [particles-audio-roadmap.md](../plans/particles-audio-roadmap.md#feedback) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B038 | L67–79 | 6. 声音及表现的后续代表实践 | [particles-audio-roadmap.md](../plans/particles-audio-roadmap.md#advanced) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B039 | L80–86 | 7. 参考、未决事项与接续 | [particles-audio-roadmap.md](../plans/particles-audio-roadmap.md#references) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |

</details>

<details>
<summary>g104engine/docs/plans/physics-character-roadmap.md（8项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| B040 | L1–10 | 页首：模块定位、当前状态/验收/修复与实际入口 | [physics-character-roadmap.md](../plans/physics-character-roadmap.md#choices)、[status.md](../status.md)、[development-history.md](../development-history.md) | 保留模块定位和起步选择；重复动态进展/批次结果由status与历史/评审单点维护，不作为计划表完成证明 |
| B041 | L11–20 | 1. 已确认的选择 | [physics-character-roadmap.md](../plans/physics-character-roadmap.md#choices) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B042 | L21–30 | 2. 推荐的三层实践 | [physics-character-roadmap.md](../plans/physics-character-roadmap.md#layers) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B043 | L31–41 | 3. 自研原理实验：候选最小范围 | [physics-character-roadmap.md](../plans/physics-character-roadmap.md#principles) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B044 | L42–54 | 4. 训练场中的物理与角色能力 | [physics-character-roadmap.md](../plans/physics-character-roadmap.md#character) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B045 | L55–66 | 5. 运行保障的实践边界 | [physics-character-roadmap.md](../plans/physics-character-roadmap.md#runtime) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B046 | L67–78 | 6. 进阶专题候选深度 | [physics-character-roadmap.md](../plans/physics-character-roadmap.md#advanced) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B047 | L79–89 | 7. 自研/后端边界与 Piccolo 参考 | [physics-character-roadmap.md](../plans/physics-character-roadmap.md#references) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |

</details>

<details>
<summary>g104engine/docs/plans/rendering-roadmap.md（7项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| B048 | L1–12 | 页首：模块定位、当前状态/验收/修复与实际入口 | [rendering-roadmap.md](../plans/rendering-roadmap.md#choices)、[status.md](../status.md)、[development-history.md](../development-history.md) | 保留模块定位和起步选择；重复动态进展/批次结果由status与历史/评审单点维护，不作为计划表完成证明 |
| B049 | L13–22 | 1. 已确认的三个选择 | [rendering-roadmap.md](../plans/rendering-roadmap.md#choices) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B050 | L23–32 | 2. 为什么建议这样推进 | [rendering-roadmap.md](../plans/rendering-roadmap.md#reasons) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B051 | L33–49 | 3. 已实现V1与长期终点的分块映射 | [rendering-roadmap.md](../plans/rendering-roadmap.md#practice) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B052 | L50–70 | 4. 扩展章节的代表性实践（候选清单） | [rendering-roadmap.md](../plans/rendering-roadmap.md#advanced) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B053 | L71–78 | 5. 自研、集成与掌握证据 | [rendering-roadmap.md](../plans/rendering-roadmap.md#evidence) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B054 | L79–85 | 6. Piccolo 参考边界与下一步 | [rendering-roadmap.md](../plans/rendering-roadmap.md#references) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |

</details>

<details>
<summary>g104engine/docs/plans/tools-debug-roadmap.md（7项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| B055 | L1–12 | 页首：模块定位、当前状态/验收/修复与实际入口 | [tools-debug-roadmap.md](../plans/tools-debug-roadmap.md#choices)、[status.md](../status.md)、[development-history.md](../development-history.md) | 保留模块定位和起步选择；重复动态进展/批次结果由status与历史/评审单点维护，不作为计划表完成证明 |
| B056 | L13–24 | 1. 已确认的选择与理由 | [tools-debug-roadmap.md](../plans/tools-debug-roadmap.md#choices) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B057 | L25–37 | 2. 编辑界面与实际修改分工 | [tools-debug-roadmap.md](../plans/tools-debug-roadmap.md#editing) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B058 | L38–58 | 3. 保留设计数据，再创建可丢弃的运行实例 | [tools-debug-roadmap.md](../plans/tools-debug-roadmap.md#isolation) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B059 | L59–73 | 4. 调试显示与性能观察分开安排 | [tools-debug-roadmap.md](../plans/tools-debug-roadmap.md#measurement) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B060 | L74–87 | 5. V1实现映射、后续分块与学习验收 | [tools-debug-roadmap.md](../plans/tools-debug-roadmap.md#practice) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |
| B061 | L88–96 | 6. 笔记、参考与未决事项 | [tools-debug-roadmap.md](../plans/tools-debug-roadmap.md#references) | 保留已选路线/理由、原理说明、自研与后端边界、候选实验/前置/验收；当前低级字段/参数改由专题指南维护 |

</details>

<details>
<summary>g104engine/docs/plans/v1-dependencies-and-assets.md（12项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A057 | 1，原7–25行 | 九直接/十三解析/target/许可、OpenAL独立原生来源/DLL选择/版本差异/来源 | [dependencies.md](../guides/dependencies.md#dependency-locks) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A058 | 2，原27–31行 | Bepu2.4纯托管学习优势/overlap及sweep成本、用户选Jolt自研CCT | [dependencies.md](../guides/dependencies.md#物理与解码方案为什么这样选) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A059 | 3/3.1，原33–58行 | 锁定先false再六包VS还原/恢复true，准备当时无正式代码开工 | [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#v1-preparation-20261003) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A060 | 3.2，原60–76行 | 九包/双十三hash、obj成功/locked、双输出托管/PE/原生hash、原OpenAL缺/只改Engine双锁cbce581 | [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#v1-preparation-20261003) | 完整历史核验表保留，不转成当前缺口 |
| A061 | 3.3，原78–94行 | 四原始ZIP免费Standard/CC0/RM、下载目录缓存/只ZIP内存查不提取/编码未知 | [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#external-input-batch) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A062 | 4/4.1，原96–117行 | 素材文件/许可/逐SHA、28seed/自有probe/背景非IBL/七Ogg转WAV/110loop/PCM合同和OpenAL文件 | [dependencies.md](../guides/dependencies.md#assets-and-conversion) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A063 | 4.2，原119–125行 | Gyan/FFmpeg9.0.2固定ZIP/SHA/mono44100pcm_s16le/缓存非运行依赖/未装系统PATH | [dependencies.md](../guides/dependencies.md#便携工具的适用范围) | 按专题合并保留；动态结果使用日期明确的可选证据 |
| A064 | 5，原127–139行 | 准备/正式授权/部署/真实功能/用户体验分别，旧提前弹窗无回答非授权、D5不恢复 | [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#deployment-transition) | 现行授权root agent-workflow/decisions维护；日期固定演变保留 |
| B091 | L27–31 | Jolt/Bepu比较、查询适配和自研规则边界 | [v1-baseline.md](../plans/v1-baseline.md#design-choices) | 保留物理取舍概要，完整备选版本/查询成本交A依赖指南 |
| B092 | L33–94 | 用户VS/NuGet及外部ZIP准备历史、元数据≠功能证据 | [v1-baseline.md](../plans/v1-baseline.md#authorization-history) | 保留用户准备/助手正式部署授权的时序；完整操作/实查由A准备历史与输入核查接收 |
| B093 | L96–125 | 素材/OpenAL/许可/哈希、PCM子集和便携FFmpeg转换 | [v1-baseline.md](../plans/v1-baseline.md#design-choices) | 保留输入/后端/转换理由；精确版本/出处/许可/哈希/流程交A依赖指南 |
| B094 | L127–139 | 选择/锁定/准备/正式开工与学习/测试边界 | [v1-baseline.md](../plans/v1-baseline.md#authorization-history) | 保留正式完整范围及准备完成不能生成权限/学习证据；最新状态由root单点维护 |

</details>

<details>
<summary>g104engine/docs/plans/v1-implementation-draft.md（13项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| A067 | 3–7及9.1，原44–104/151–174行 | 时间输入/安全点/voice寿命、场景格式/glTF、角色/Jolt、Gbuffer32F/PCF、Undo/PlayStop与受控父子 | [architecture.md](../architecture.md#frame-flow) | 与B协同：低级合同去架构/场景/物理/动画稳定锚点，范围理由去新V1基线 |
| B079 | L1–13 | 批准方案、正式授权、交付目标与排除项 | [v1-baseline.md](../plans/v1-baseline.md#scope) | 并入唯一V1批准范围，不继续维护第二份完整范围描述 |
| B080 | L15–42 | Engine/Sandbox、设计/运行/事实职责图 | [v1-baseline.md](../plans/v1-baseline.md#design-boundaries) | 保留职责边界，实际文件/帧流程与图合并至A架构 |
| B081 | L44–56 | 固定步/补步/丢时/输入/安全点/资源与退出合同 | [v1-baseline.md](../plans/v1-baseline.md#design-boundaries) | 保留设计约束/失败保护与正确性理由；低级顺序/参数/释放路径归A架构 |
| B082 | L58–70 | schema/GUID/路径/保存保护、glTF支持与蒙皮/采样/素材 | [v1-baseline.md](../plans/v1-baseline.md#design-choices) | 保留场景/素材路线理由与支持边界，格式/矩阵/配置细则归A指南 |
| B083 | L72–80 | 自研角色/Jolt查询、相机、请求→事实、门/NPC/导航边界 | [v1-baseline.md](../plans/v1-baseline.md#scope) | 保留有限能力、自研/集成分工；算法/过滤/参数和修复证据归A指南/已有评审 |
| B084 | L82–94 | 双管线/G-buffer/HDR/阴影/颜色/透明、CPU粒子与PCM声音 | [v1-baseline.md](../plans/v1-baseline.md#design-choices) | 保留范围/取舍/输入路线，当前格式/灯光/池/Voice/转换参数由A集中 |
| B085 | L96–104 | 有限编辑/事务/稳定身份/Saved/PlayStop/ImGui分工 | [v1-baseline.md](../plans/v1-baseline.md#design-boundaries) | 保留失败保护与设计/运行语义，低级字段/参数/命令归A场景编辑指南 |
| B086 | L106–123 | 连续实施、增量验证、预览非闸门、真实证据层/学习/交付 | [v1-baseline.md](../plans/v1-baseline.md#acceptance) | 保留验收与学习口径，现行命令/成功判据由运行指南集中 |
| B087 | L125–150 | D51设计收敛/助手普通细节/定案表/数字与来源 | [v1-baseline.md](../plans/v1-baseline.md#design-choices) | 重要取舍及普通细节归属保全，不伪称用户逐项选参数；现行低级合同由A接收 |
| B088 | L151–174 | D52受控父子选择与全部限制/矩阵/子树Undo/显示历史 | [v1-baseline.md](../plans/v1-baseline.md#design-boundaries) | 保留选择理由、可编辑范围和物理根/正统一父缩放/失败边界；精确矩阵与插值合同归A架构 |
| B089 | L176–185 | 开工准备与无效提前弹窗、正式完整授权/D53模型分工 | [v1-baseline.md](../plans/v1-baseline.md#authorization-history) | 保全日期/范围/分工来源，同范围持续有效与新范围确认分别记录 |
| B090 | L187–189 | 独立验收后的HDR16F→32F及同范围合同校正 | [v1-baseline.md](../plans/v1-baseline.md#design-choices) | 保留改变理由/数值/代价/授权归属，详细失败修后证据链接已有独立评审 |

</details>

<details>
<summary>g104engine/docs/publishing.md（5项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C001 | 页首当前保存节点及历史节点 | 38cb85f UI/验收16文件实际保存；旧b91dfe4/d2e8d40/4be648e与31/40/55历史；旧10/4审计实时远端一致和后续文档另批未提交 | [status.md](../status.md)、[git-submission-history.md](../reviews/git-submission-history.md#batch-index)、[v1-start-checkpoint-2026-10-03.md](../reviews/v1-start-checkpoint-2026-10-03.md#historical-evidence) | 当前同步单点由root维护；各批实际保存证据进入历史，不在现行指南广播本轮进度 |
| C002 | 发布文件范围：仓库与长期白名单 | 公开仓库zxpeng83/games104-engine及HTTPS地址；根入口/规则、新工程源码/配置/文档、必要CI；不放行整个games104；每批真实暂存范围须核对 | [git-and-publishing.md](../guides/git-and-publishing.md#publishing-scope) | 合并为唯一长期白名单；发布许可不等于当前执行授权 |
| C003 | 发布文件范围：正式资产及OpenAL配套 | assets内Shader/场景/配置/测试素材、CC0模型与声音及许可；third_party/openal-soft/1.25.2官方DLL/原许可/完整同版源码归档/SOURCE；b91dfe4已经保存；对应源码在公开DLL前补齐；不可当缓存排除 | [git-and-publishing.md](../guides/git-and-publishing.md#publishing-scope) | 保留恢复所必需资产与许可边界；SOURCE原字节不改 |
| C004 | 十份原路径正文与实际配图 | 十份课程正文的完整原路径逐项一致；Gameplay 1789309063459.png唯一已引用图片；新增引用图需核对并更新白名单，不放行资料或所有Markdown | [git-and-publishing.md](../guides/git-and-publishing.md#publishing-scope) | 完整精确白名单直接在现行页可查，不依赖历史跳转 |
| C005 | 排除范围与26处原笔记引用 | Piccolo/网页存档/PDF/其它笔记/备份/.vs/bin/obj/下载缓存排除；操作流程私有截图显式排除、不默认读取遍历引用复制上传、不加入工程或演示；原资料留本地、上层E:/game_study/.git不清理；26处非发布本地引用用户决定不处理；不改原文链接、不加标注、不扩范围 | [git-and-publishing.md](../guides/git-and-publishing.md#publishing-scope) | 发布边界和用户保留决定完整归并 |

</details>

<details>
<summary>g104engine/docs/reviews/design-reference-checks-2026-10-03.md（14项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C029 | 页首适用阶段、后续反馈与导航 | 固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 只改当前导航并加稳定锚点；原历史正文逆向还原一致 |
| C030 | 核查范围与证据等级 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C031 | Piccolo：源码能够支持的结论 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C032 | 补充核查：渲染模块范围（2026-10-03） | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C033 | 补充核查：资产、场景与模板（2026-10-03） | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C034 | 补充核查：物理查询、过滤与所有权（2026-10-03） | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C035 | 补充核查：动画求值、蒙皮与跨 Pass 一致性（2026-10-03） | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C036 | 补充核查：Gameplay、Lua 与 AI 边界（2026-10-03） | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C037 | 补充核查：粒子链路与音频接入范围（2026-10-03） | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C038 | 补充核查：工具编辑、模式切换与观察边界（2026-10-03） | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C039 | 补充核查：主流程、同步加载与内部线程（2026-10-03） | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C040 | OpenTK 4.9.4：矩阵与 GPU 上传 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C041 | OpenTK 4.9.4：循环、尺寸与清理 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C042 | 开工前的验证建议及当时未验证事项 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；固定Piccolo f5053707及OpenTK4.9.4；源码事实/数学推导/候选建议分开，参考库局部能力与缺失范围；CPU行/GLSL列上传等价、独立算术非库/GPU测试；Run异常不保证OnUnload，GPU资源所有权/当前线程/部分初始化建议；开工前未运行与后来实现/验收分时态 | [design-reference-checks-2026-10-03.md](../reviews/design-reference-checks-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |

</details>

<details>
<summary>g104engine/docs/reviews/document-consistency-review-2026-10-04.md（5项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C043 | 页首适用阶段、后续反馈与导航 | 前次文审49文档/45修改+1新增/629链接/15锚点；旧实时GitHub main与38cb85f一致/开始干净；8运行WAV与当前DTO差异、Astra有限终审；本次导航加带日期说明，不将旧统计作为43新布局验证 | [document-consistency-review-2026-10-04.md](../reviews/document-consistency-review-2026-10-04.md#historical-evidence) | 只改当前导航并加稳定锚点；原历史正文逆向还原一致 |
| C044 | 范围与事实基线 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；前次文审49文档/45修改+1新增/629链接/15锚点；旧实时GitHub main与38cb85f一致/开始干净；8运行WAV与当前DTO差异、Astra有限终审；本次导航加带日期说明，不将旧统计作为43新布局验证 | [document-consistency-review-2026-10-04.md](../reviews/document-consistency-review-2026-10-04.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C045 | 修正方向 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；前次文审49文档/45修改+1新增/629链接/15锚点；旧实时GitHub main与38cb85f一致/开始干净；8运行WAV与当前DTO差异、Astra有限终审；本次导航加带日期说明，不将旧统计作为43新布局验证 | [document-consistency-review-2026-10-04.md](../reviews/document-consistency-review-2026-10-04.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C046 | 验证与剩余范围 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；前次文审49文档/45修改+1新增/629链接/15锚点；旧实时GitHub main与38cb85f一致/开始干净；8运行WAV与当前DTO差异、Astra有限终审；本次导航加带日期说明，不将旧统计作为43新布局验证 | [document-consistency-review-2026-10-04.md](../reviews/document-consistency-review-2026-10-04.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C047 | 分工与接续 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；前次文审49文档/45修改+1新增/629链接/15锚点；旧实时GitHub main与38cb85f一致/开始干净；8运行WAV与当前DTO差异、Astra有限终审；本次导航加带日期说明，不将旧统计作为43新布局验证 | [document-consistency-review-2026-10-04.md](../reviews/document-consistency-review-2026-10-04.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |

</details>

<details>
<summary>g104engine/docs/reviews/foundation-review-2026-10-02.md（8项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C048 | 页首适用阶段、后续反馈与导航 | D0-D4基础已具备，D5整体暂缓；R1-R8纠正/待办及Smoke打印PASS非全面断言；ClientSize/framebuffer语义和未测DPI/退出组合；当时OpenTK三直接引用/空Engine/临时探针事实；31首批/后续整理、78行7808字节/17文档112链接与归档保全证明保留 | [foundation-review-2026-10-02.md](../reviews/foundation-review-2026-10-02.md#historical-evidence) | 只改当前导航并加稳定锚点；原历史正文逆向还原一致 |
| C049 | 结论与范围 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；D0-D4基础已具备，D5整体暂缓；R1-R8纠正/待办及Smoke打印PASS非全面断言；ClientSize/framebuffer语义和未测DPI/退出组合；当时OpenTK三直接引用/空Engine/临时探针事实；31首批/后续整理、78行7808字节/17文档112链接与归档保全证明保留 | [foundation-review-2026-10-02.md](../reviews/foundation-review-2026-10-02.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C050 | 需要纠正或继续跟进的事项 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；D0-D4基础已具备，D5整体暂缓；R1-R8纠正/待办及Smoke打印PASS非全面断言；ClientSize/framebuffer语义和未测DPI/退出组合；当时OpenTK三直接引用/空Engine/临时探针事实；31首批/后续整理、78行7808字节/17文档112链接与归档保全证明保留 | [foundation-review-2026-10-02.md](../reviews/foundation-review-2026-10-02.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C051 | 当前代码和配置核对 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；D0-D4基础已具备，D5整体暂缓；R1-R8纠正/待办及Smoke打印PASS非全面断言；ClientSize/framebuffer语义和未测DPI/退出组合；当时OpenTK三直接引用/空Engine/临时探针事实；31首批/后续整理、78行7808字节/17文档112链接与归档保全证明保留 | [foundation-review-2026-10-02.md](../reviews/foundation-review-2026-10-02.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C052 | 验证结果及边界 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；D0-D4基础已具备，D5整体暂缓；R1-R8纠正/待办及Smoke打印PASS非全面断言；ClientSize/framebuffer语义和未测DPI/退出组合；当时OpenTK三直接引用/空Engine/临时探针事实；31首批/后续整理、78行7808字节/17文档112链接与归档保全证明保留 | [foundation-review-2026-10-02.md](../reviews/foundation-review-2026-10-02.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C053 | 文档分层及保留策略 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；D0-D4基础已具备，D5整体暂缓；R1-R8纠正/待办及Smoke打印PASS非全面断言；ClientSize/framebuffer语义和未测DPI/退出组合；当时OpenTK三直接引用/空Engine/临时探针事实；31首批/后续整理、78行7808字节/17文档112链接与归档保全证明保留 | [foundation-review-2026-10-02.md](../reviews/foundation-review-2026-10-02.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C054 | 交接完成的含义 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；D0-D4基础已具备，D5整体暂缓；R1-R8纠正/待办及Smoke打印PASS非全面断言；ClientSize/framebuffer语义和未测DPI/退出组合；当时OpenTK三直接引用/空Engine/临时探针事实；31首批/后续整理、78行7808字节/17文档112链接与归档保全证明保留 | [foundation-review-2026-10-02.md](../reviews/foundation-review-2026-10-02.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C055 | 本轮文档验证 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；D0-D4基础已具备，D5整体暂缓；R1-R8纠正/待办及Smoke打印PASS非全面断言；ClientSize/framebuffer语义和未测DPI/退出组合；当时OpenTK三直接引用/空Engine/临时探针事实；31首批/后续整理、78行7808字节/17文档112链接与归档保全证明保留 | [foundation-review-2026-10-02.md](../reviews/foundation-review-2026-10-02.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |

</details>

<details>
<summary>g104engine/docs/reviews/v1-commit-checklist-2026-10-03.md（5项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C013 | 页首准备日期、实际补记和后续反馈 | cbce581准备基线；108路径/23修改85新增/分类数；b91dfe4137686d9a5f962e40e10a3afe939cfe4c 2026-10-03 19:41:08 +08；推荐待用户验收与实际待验收说明不同；OpenAL源码补齐、身份/目标/保护边界、当时测试、后来人工反馈分时态 | [git-submission-history.md](../reviews/git-submission-history.md#v1-initial) | 按原批次组织；不把后来的保存/反馈改写为准备时通过 |
| C014 | 提交前候选范围（历史） | 该节全部原信息保留；cbce581准备基线；108路径/23修改85新增/分类数；b91dfe4137686d9a5f962e40e10a3afe939cfe4c 2026-10-03 19:41:08 +08；推荐待用户验收与实际待验收说明不同；OpenAL源码补齐、身份/目标/保护边界、当时测试、后来人工反馈分时态；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#v1-initial) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |
| C015 | 本批特别核对 | 该节全部原信息保留；cbce581准备基线；108路径/23修改85新增/分类数；b91dfe4137686d9a5f962e40e10a3afe939cfe4c 2026-10-03 19:41:08 +08；推荐待用户验收与实际待验收说明不同；OpenAL源码补齐、身份/目标/保护边界、当时测试、后来人工反馈分时态；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#v1-initial) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |
| C016 | 当时用户操作与建议命令（历史） | 该节全部原信息保留；cbce581准备基线；108路径/23修改85新增/分类数；b91dfe4137686d9a5f962e40e10a3afe939cfe4c 2026-10-03 19:41:08 +08；推荐待用户验收与实际待验收说明不同；OpenAL源码补齐、身份/目标/保护边界、当时测试、后来人工反馈分时态；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#v1-initial-commands) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |
| C017 | 实际文件清单 | 该节全部原信息保留；cbce581准备基线；108路径/23修改85新增/分类数；b91dfe4137686d9a5f962e40e10a3afe939cfe4c 2026-10-03 19:41:08 +08；推荐待用户验收与实际待验收说明不同；OpenAL源码补齐、身份/目标/保护边界、当时测试、后来人工反馈分时态；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#v1-initial-files) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |

</details>

<details>
<summary>g104engine/docs/reviews/v1-contact-exit-fix-2026-10-03.md（7项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C056 | 页首适用阶段、后续反馈与导航 | 用户退出1截图纠正/SceneValidationException；展示六球无Collider、Ramp原Box碰撞；非球碰撞根因；TRS反复分解/近180数值误差；修前4PASS34FAIL且无效PS探针撤回；实际单字段更新/double分解/父链与scale合同；各38CPU/800帧9路线及240集成，Astra只读/Root运行职责；interrupted/send_message误用纠正和followup_task恢复；当时待用户复试与10/4实际反馈分开 | [v1-contact-exit-fix-2026-10-03.md](../reviews/v1-contact-exit-fix-2026-10-03.md#historical-evidence) | 只改当前导航并加稳定锚点；原历史正文逆向还原一致 |
| C057 | 用户反馈与保存基线 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；用户退出1截图纠正/SceneValidationException；展示六球无Collider、Ramp原Box碰撞；非球碰撞根因；TRS反复分解/近180数值误差；修前4PASS34FAIL且无效PS探针撤回；实际单字段更新/double分解/父链与scale合同；各38CPU/800帧9路线及240集成，Astra只读/Root运行职责；interrupted/send_message误用纠正和followup_task恢复；当时待用户复试与10/4实际反馈分开 | [v1-contact-exit-fix-2026-10-03.md](../reviews/v1-contact-exit-fix-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C058 | 根因与先失败证据 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；用户退出1截图纠正/SceneValidationException；展示六球无Collider、Ramp原Box碰撞；非球碰撞根因；TRS反复分解/近180数值误差；修前4PASS34FAIL且无效PS探针撤回；实际单字段更新/double分解/父链与scale合同；各38CPU/800帧9路线及240集成，Astra只读/Root运行职责；interrupted/send_message误用纠正和followup_task恢复；当时待用户复试与10/4实际反馈分开 | [v1-contact-exit-fix-2026-10-03.md](../reviews/v1-contact-exit-fix-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C059 | 实际修复与文件责任 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；用户退出1截图纠正/SceneValidationException；展示六球无Collider、Ramp原Box碰撞；非球碰撞根因；TRS反复分解/近180数值误差；修前4PASS34FAIL且无效PS探针撤回；实际单字段更新/double分解/父链与scale合同；各38CPU/800帧9路线及240集成，Astra只读/Root运行职责；interrupted/send_message误用纠正和followup_task恢复；当时待用户复试与10/4实际反馈分开 | [v1-contact-exit-fix-2026-10-03.md](../reviews/v1-contact-exit-fix-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C060 | 最终真实验证 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；用户退出1截图纠正/SceneValidationException；展示六球无Collider、Ramp原Box碰撞；非球碰撞根因；TRS反复分解/近180数值误差；修前4PASS34FAIL且无效PS探针撤回；实际单字段更新/double分解/父链与scale合同；各38CPU/800帧9路线及240集成，Astra只读/Root运行职责；interrupted/send_message误用纠正和followup_task恢复；当时待用户复试与10/4实际反馈分开 | [v1-contact-exit-fix-2026-10-03.md](../reviews/v1-contact-exit-fix-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C061 | 本批协作状态纠正 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；用户退出1截图纠正/SceneValidationException；展示六球无Collider、Ramp原Box碰撞；非球碰撞根因；TRS反复分解/近180数值误差；修前4PASS34FAIL且无效PS探针撤回；实际单字段更新/double分解/父链与scale合同；各38CPU/800帧9路线及240集成，Astra只读/Root运行职责；interrupted/send_message误用纠正和followup_task恢复；当时待用户复试与10/4实际反馈分开 | [v1-contact-exit-fix-2026-10-03.md](../reviews/v1-contact-exit-fix-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C062 | 修复收尾时的下一步与边界（历史） | 本节原日期/版本/事实/失败与通过结果/限制完整保留；用户退出1截图纠正/SceneValidationException；展示六球无Collider、Ramp原Box碰撞；非球碰撞根因；TRS反复分解/近180数值误差；修前4PASS34FAIL且无效PS探针撤回；实际单字段更新/double分解/父链与scale合同；各38CPU/800帧9路线及240集成，Astra只读/Root运行职责；interrupted/send_message误用纠正和followup_task恢复；当时待用户复试与10/4实际反馈分开 | [v1-contact-exit-fix-2026-10-03.md](../reviews/v1-contact-exit-fix-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |

</details>

<details>
<summary>g104engine/docs/reviews/v1-implementation-review-2026-10-03.md（6项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C063 | 页首适用阶段、后续反馈与导航 | 首次完整V1授权/实施与当时待人工验收；Engine各模块、128UBO、动画四状态、Jolt/glTF/PCM16子集；DebugRelease/verify/240图形/MAE.0651/资产21SHA；Jolt查询适配/UTF8 ShaderSource截断/模型输入修复；PCF限制/未来模块/D5与当时cbce581未提交边界 | [v1-implementation-review-2026-10-03.md](../reviews/v1-implementation-review-2026-10-03.md#historical-evidence) | 只改当前导航并加稳定锚点；原历史正文逆向还原一致 |
| C064 | 实际实现范围 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；首次完整V1授权/实施与当时待人工验收；Engine各模块、128UBO、动画四状态、Jolt/glTF/PCM16子集；DebugRelease/verify/240图形/MAE.0651/资产21SHA；Jolt查询适配/UTF8 ShaderSource截断/模型输入修复；PCF限制/未来模块/D5与当时cbce581未提交边界 | [v1-implementation-review-2026-10-03.md](../reviews/v1-implementation-review-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C065 | 本机证据 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；首次完整V1授权/实施与当时待人工验收；Engine各模块、128UBO、动画四状态、Jolt/glTF/PCM16子集；DebugRelease/verify/240图形/MAE.0651/资产21SHA；Jolt查询适配/UTF8 ShaderSource截断/模型输入修复；PCF限制/未来模块/D5与当时cbce581未提交边界 | [v1-implementation-review-2026-10-03.md](../reviews/v1-implementation-review-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C066 | 实际修复与评审 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；首次完整V1授权/实施与当时待人工验收；Engine各模块、128UBO、动画四状态、Jolt/glTF/PCM16子集；DebugRelease/verify/240图形/MAE.0651/资产21SHA；Jolt查询适配/UTF8 ShaderSource截断/模型输入修复；PCF限制/未来模块/D5与当时cbce581未提交边界 | [v1-implementation-review-2026-10-03.md](../reviews/v1-implementation-review-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C067 | 首轮实现结束时待用户验收与持续限制 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；首次完整V1授权/实施与当时待人工验收；Engine各模块、128UBO、动画四状态、Jolt/glTF/PCM16子集；DebugRelease/verify/240图形/MAE.0651/资产21SHA；Jolt查询适配/UTF8 ShaderSource截断/模型输入修复；PCF限制/未来模块/D5与当时cbce581未提交边界 | [v1-implementation-review-2026-10-03.md](../reviews/v1-implementation-review-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C068 | 最后收尾核对 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；首次完整V1授权/实施与当时待人工验收；Engine各模块、128UBO、动画四状态、Jolt/glTF/PCM16子集；DebugRelease/verify/240图形/MAE.0651/资产21SHA；Jolt查询适配/UTF8 ShaderSource截断/模型输入修复；PCF限制/未来模块/D5与当时cbce581未提交边界 | [v1-implementation-review-2026-10-03.md](../reviews/v1-implementation-review-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |

</details>

<details>
<summary>g104engine/docs/reviews/v1-independent-review-2026-10-03.md（6项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C069 | 页首适用阶段、后续反馈与导航 | b91dfe4起始干净及实际模型分工；编辑草稿/事务/Saved、导航ceil尾格/1ULP容量、NPC FOV/skin合同；GGX/HDR RGBA32F/baseColor/roughness/half/粒子排序及UV/skin合同；所有先FAIL后PASS、有效新构建、无效旧DLL与沙箱失败限制；10GPU项/独立double/source-over/240图形/MAE.0621与有限复核；当时尚未提交/待体验与10/4后续保存反馈分开 | [v1-independent-review-2026-10-03.md](../reviews/v1-independent-review-2026-10-03.md#historical-evidence) | 只改当前导航并加稳定锚点；原历史正文逆向还原一致 |
| C070 | 保存基线与分工 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；b91dfe4起始干净及实际模型分工；编辑草稿/事务/Saved、导航ceil尾格/1ULP容量、NPC FOV/skin合同；GGX/HDR RGBA32F/baseColor/roughness/half/粒子排序及UV/skin合同；所有先FAIL后PASS、有效新构建、无效旧DLL与沙箱失败限制；10GPU项/独立double/source-over/240图形/MAE.0621与有限复核；当时尚未提交/待体验与10/4后续保存反馈分开 | [v1-independent-review-2026-10-03.md](../reviews/v1-independent-review-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C071 | 核实问题、修复与证据 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；b91dfe4起始干净及实际模型分工；编辑草稿/事务/Saved、导航ceil尾格/1ULP容量、NPC FOV/skin合同；GGX/HDR RGBA32F/baseColor/roughness/half/粒子排序及UV/skin合同；所有先FAIL后PASS、有效新构建、无效旧DLL与沙箱失败限制；10GPU项/独立double/source-over/240图形/MAE.0621与有限复核；当时尚未提交/待体验与10/4后续保存反馈分开 | [v1-independent-review-2026-10-03.md](../reviews/v1-independent-review-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C072 | 先失败、再修复的记录 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；b91dfe4起始干净及实际模型分工；编辑草稿/事务/Saved、导航ceil尾格/1ULP容量、NPC FOV/skin合同；GGX/HDR RGBA32F/baseColor/roughness/half/粒子排序及UV/skin合同；所有先FAIL后PASS、有效新构建、无效旧DLL与沙箱失败限制；10GPU项/独立double/source-over/240图形/MAE.0621与有限复核；当时尚未提交/待体验与10/4后续保存反馈分开 | [v1-independent-review-2026-10-03.md](../reviews/v1-independent-review-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C073 | 最终回归和复核边界 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；b91dfe4起始干净及实际模型分工；编辑草稿/事务/Saved、导航ceil尾格/1ULP容量、NPC FOV/skin合同；GGX/HDR RGBA32F/baseColor/roughness/half/粒子排序及UV/skin合同；所有先FAIL后PASS、有效新构建、无效旧DLL与沙箱失败限制；10GPU项/独立double/source-over/240图形/MAE.0621与有限复核；当时尚未提交/待体验与10/4后续保存反馈分开 | [v1-independent-review-2026-10-03.md](../reviews/v1-independent-review-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C074 | 评审结束时的下一步与恢复（历史） | 本节原日期/版本/事实/失败与通过结果/限制完整保留；b91dfe4起始干净及实际模型分工；编辑草稿/事务/Saved、导航ceil尾格/1ULP容量、NPC FOV/skin合同；GGX/HDR RGBA32F/baseColor/roughness/half/粒子排序及UV/skin合同；所有先FAIL后PASS、有效新构建、无效旧DLL与沙箱失败限制；10GPU项/独立double/source-over/240图形/MAE.0621与有限复核；当时尚未提交/待体验与10/4后续保存反馈分开 | [v1-independent-review-2026-10-03.md](../reviews/v1-independent-review-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |

</details>

<details>
<summary>g104engine/docs/reviews/v1-input-archives-check-2026-10-03.md（6项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C075 | 页首适用阶段、后续反馈与导航 | 四ZIP长度/条目/SHA/未提取部署的静态事实；OpenAL soft_oal实现库与router区别、1.25.2字符串/1.25.1PE元数据差异；UAL GLB 67节点65关节43clip/素材SHA/根运动与-Z适配；Kenney230 Ogg头、CC0、离线PCM16已选/当时未转换；未加载DLL/导入/GPU/播放/签名验证边界与后来部署分开 | [v1-input-archives-check-2026-10-03.md](../reviews/v1-input-archives-check-2026-10-03.md#historical-evidence) | 只改当前导航并加稳定锚点；原历史正文逆向还原一致 |
| C076 | 1. 四份原始 ZIP | 本节原日期/版本/事实/失败与通过结果/限制完整保留；四ZIP长度/条目/SHA/未提取部署的静态事实；OpenAL soft_oal实现库与router区别、1.25.2字符串/1.25.1PE元数据差异；UAL GLB 67节点65关节43clip/素材SHA/根运动与-Z适配；Kenney230 Ogg头、CC0、离线PCM16已选/当时未转换；未加载DLL/导入/GPU/播放/签名验证边界与后来部署分开 | [v1-input-archives-check-2026-10-03.md](../reviews/v1-input-archives-check-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C077 | 2. OpenAL Soft：实际实现库与版本差异 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；四ZIP长度/条目/SHA/未提取部署的静态事实；OpenAL soft_oal实现库与router区别、1.25.2字符串/1.25.1PE元数据差异；UAL GLB 67节点65关节43clip/素材SHA/根运动与-Z适配；Kenney230 Ogg头、CC0、离线PCM16已选/当时未转换；未加载DLL/导入/GPU/播放/签名验证边界与后来部署分开 | [v1-input-archives-check-2026-10-03.md](../reviews/v1-input-archives-check-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C078 | 3. 角色与动画：Standard 包满足基础动作输入 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；四ZIP长度/条目/SHA/未提取部署的静态事实；OpenAL soft_oal实现库与router区别、1.25.2字符串/1.25.1PE元数据差异；UAL GLB 67节点65关节43clip/素材SHA/根运动与-Z适配；Kenney230 Ogg头、CC0、离线PCM16已选/当时未转换；未加载DLL/导入/GPU/播放/签名验证边界与后来部署分开 | [v1-input-archives-check-2026-10-03.md](../reviews/v1-input-archives-check-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C079 | 4. 两个 Kenney 声音包：需要格式处理 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；四ZIP长度/条目/SHA/未提取部署的静态事实；OpenAL soft_oal实现库与router区别、1.25.2字符串/1.25.1PE元数据差异；UAL GLB 67节点65关节43clip/素材SHA/根运动与-Z适配；Kenney230 Ogg头、CC0、离线PCM16已选/当时未转换；未加载DLL/导入/GPU/播放/签名验证边界与后来部署分开 | [v1-input-archives-check-2026-10-03.md](../reviews/v1-input-archives-check-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C080 | 5. 该轮静态核对结论与当时接续 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；四ZIP长度/条目/SHA/未提取部署的静态事实；OpenAL soft_oal实现库与router区别、1.25.2字符串/1.25.1PE元数据差异；UAL GLB 67节点65关节43clip/素材SHA/根运动与-Z适配；Kenney230 Ogg头、CC0、离线PCM16已选/当时未转换；未加载DLL/导入/GPU/播放/签名验证边界与后来部署分开 | [v1-input-archives-check-2026-10-03.md](../reviews/v1-input-archives-check-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |

</details>

<details>
<summary>g104engine/docs/reviews/v1-repair-commit-checklist-2026-10-03.md（5项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C018 | 页首准备日期、实际补记和后续反馈 | b91dfe4准备基线及实时远端/暂存空；38路径/30修改8新增/分类数；d2e8d40691dd76a4637e35ed2905a2ccfaefe3d2 2026-10-03 23:00:25 +08；实际说明fix:修复与推荐fix: 修复保留差异；当时人工路线尚未确认、10/4复试反馈、4be648e后续独立文档节点 | [git-submission-history.md](../reviews/git-submission-history.md#v1-repair) | 按原批次组织；不把后来的保存/反馈改写为准备时通过 |
| C019 | 提交前核对的保存节点与范围（历史） | 该节全部原信息保留；b91dfe4准备基线及实时远端/暂存空；38路径/30修改8新增/分类数；d2e8d40691dd76a4637e35ed2905a2ccfaefe3d2 2026-10-03 23:00:25 +08；实际说明fix:修复与推荐fix: 修复保留差异；当时人工路线尚未确认、10/4复试反馈、4be648e后续独立文档节点；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#v1-repair) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |
| C020 | 验证证据 | 该节全部原信息保留；b91dfe4准备基线及实时远端/暂存空；38路径/30修改8新增/分类数；d2e8d40691dd76a4637e35ed2905a2ccfaefe3d2 2026-10-03 23:00:25 +08；实际说明fix:修复与推荐fix: 修复保留差异；当时人工路线尚未确认、10/4复试反馈、4be648e后续独立文档节点；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#v1-repair) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |
| C021 | 当时身份、提交说明与用户操作（历史） | 该节全部原信息保留；b91dfe4准备基线及实时远端/暂存空；38路径/30修改8新增/分类数；d2e8d40691dd76a4637e35ed2905a2ccfaefe3d2 2026-10-03 23:00:25 +08；实际说明fix:修复与推荐fix: 修复保留差异；当时人工路线尚未确认、10/4复试反馈、4be648e后续独立文档节点；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#v1-repair-commands) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |
| C022 | 实际路径清单 | 该节全部原信息保留；b91dfe4准备基线及实时远端/暂存空；38路径/30修改8新增/分类数；d2e8d40691dd76a4637e35ed2905a2ccfaefe3d2 2026-10-03 23:00:25 +08；实际说明fix:修复与推荐fix: 修复保留差异；当时人工路线尚未确认、10/4复试反馈、4be648e后续独立文档节点；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#v1-repair-files) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |

</details>

<details>
<summary>g104engine/docs/reviews/v1-start-checkpoint-2026-10-03.md（5项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C081 | 页首适用阶段、后续反馈与导航 | cbce581c608412b6b981eb859ad73af7240c897b本地/实时远端；2026-10-03 15:44:33 +08 docs提交/26变更/55跟踪/开始干净；Class1/Program/global/slnx相对3f98d1b无差异；四ZIP缓存不随Git/当时未部署；Ultra最新要求与Astra预核查；完整开工弹窗当时未答复与后来获得授权分开 | [v1-start-checkpoint-2026-10-03.md](../reviews/v1-start-checkpoint-2026-10-03.md#historical-evidence) | 只改当前导航并加稳定锚点；原历史正文逆向还原一致 |
| C082 | 已核实的仓库节点 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；cbce581c608412b6b981eb859ad73af7240c897b本地/实时远端；2026-10-03 15:44:33 +08 docs提交/26变更/55跟踪/开始干净；Class1/Program/global/slnx相对3f98d1b无差异；四ZIP缓存不随Git/当时未部署；Ultra最新要求与Astra预核查；完整开工弹窗当时未答复与后来获得授权分开 | [v1-start-checkpoint-2026-10-03.md](../reviews/v1-start-checkpoint-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C083 | 本地输入与功能边界 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；cbce581c608412b6b981eb859ad73af7240c897b本地/实时远端；2026-10-03 15:44:33 +08 docs提交/26变更/55跟踪/开始干净；Class1/Program/global/slnx相对3f98d1b无差异；四ZIP缓存不随Git/当时未部署；Ultra最新要求与Astra预核查；完整开工弹窗当时未答复与后来获得授权分开 | [v1-start-checkpoint-2026-10-03.md](../reviews/v1-start-checkpoint-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C084 | 用户最新执行决定 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；cbce581c608412b6b981eb859ad73af7240c897b本地/实时远端；2026-10-03 15:44:33 +08 docs提交/26变更/55跟踪/开始干净；Class1/Program/global/slnx相对3f98d1b无差异；四ZIP缓存不随Git/当时未部署；Ultra最新要求与Astra预核查；完整开工弹窗当时未答复与后来获得授权分开 | [v1-start-checkpoint-2026-10-03.md](../reviews/v1-start-checkpoint-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C085 | 开工前代码授权接续点（历史） | 本节原日期/版本/事实/失败与通过结果/限制完整保留；cbce581c608412b6b981eb859ad73af7240c897b本地/实时远端；2026-10-03 15:44:33 +08 docs提交/26变更/55跟踪/开始干净；Class1/Program/global/slnx相对3f98d1b无差异；四ZIP缓存不随Git/当时未部署；Ultra最新要求与Astra预核查；完整开工弹窗当时未答复与后来获得授权分开 | [v1-start-checkpoint-2026-10-03.md](../reviews/v1-start-checkpoint-2026-10-03.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |

</details>

<details>
<summary>g104engine/docs/reviews/v1-ui-acceptance-commit-checklist-2026-10-04.md（6项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C023 | 页首准备日期、实际补记和后续反馈 | 4be648ea2923c62ccd561dfc0fc3cc4c771f2265准备基线/实时远端当时未核；16路径/13修改3新增/5源码11文档；38cb85f2be6d86804edd3331a54467389b3af604 2026-10-04 17:06:43 +08；该批实际16与候选一致、前次审计实时GitHub main一致/工作区干净；用户首轮1–5初步非穷尽验收、13输入/11UI/240帧、Astra有限复核、后续文档另批 | [git-submission-history.md](../reviews/git-submission-history.md#ui-acceptance) | 按原批次组织；不把后来的保存/反馈改写为准备时通过 |
| C024 | 本批行为与验收 | 该节全部原信息保留；4be648ea2923c62ccd561dfc0fc3cc4c771f2265准备基线/实时远端当时未核；16路径/13修改3新增/5源码11文档；38cb85f2be6d86804edd3331a54467389b3af604 2026-10-04 17:06:43 +08；该批实际16与候选一致、前次审计实时GitHub main一致/工作区干净；用户首轮1–5初步非穷尽验收、13输入/11UI/240帧、Astra有限复核、后续文档另批；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#ui-acceptance) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |
| C025 | 提交准备时的基线、身份与目标（历史） | 该节全部原信息保留；4be648ea2923c62ccd561dfc0fc3cc4c771f2265准备基线/实时远端当时未核；16路径/13修改3新增/5源码11文档；38cb85f2be6d86804edd3331a54467389b3af604 2026-10-04 17:06:43 +08；该批实际16与候选一致、前次审计实时GitHub main一致/工作区干净；用户首轮1–5初步非穷尽验收、13输入/11UI/240帧、Astra有限复核、后续文档另批；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#ui-acceptance) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |
| C026 | 提交前候选范围（已与实际16文件核对） | 该节全部原信息保留；4be648ea2923c62ccd561dfc0fc3cc4c771f2265准备基线/实时远端当时未核；16路径/13修改3新增/5源码11文档；38cb85f2be6d86804edd3331a54467389b3af604 2026-10-04 17:06:43 +08；该批实际16与候选一致、前次审计实时GitHub main一致/工作区干净；用户首轮1–5初步非穷尽验收、13输入/11UI/240帧、Astra有限复核、后续文档另批；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#ui-acceptance-files) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |
| C027 | 该批提交准备时已有的有效验证 | 该节全部原信息保留；4be648ea2923c62ccd561dfc0fc3cc4c771f2265准备基线/实时远端当时未核；16路径/13修改3新增/5源码11文档；38cb85f2be6d86804edd3331a54467389b3af604 2026-10-04 17:06:43 +08；该批实际16与候选一致、前次审计实时GitHub main一致/工作区干净；用户首轮1–5初步非穷尽验收、13输入/11UI/240帧、Astra有限复核、后续文档另批；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#ui-acceptance) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |
| C028 | 当时用户操作顺序与命令（历史） | 该节全部原信息保留；4be648ea2923c62ccd561dfc0fc3cc4c771f2265准备基线/实时远端当时未核；16路径/13修改3新增/5源码11文档；38cb85f2be6d86804edd3331a54467389b3af604 2026-10-04 17:06:43 +08；该批实际16与候选一致、前次审计实时GitHub main一致/工作区干净；用户首轮1–5初步非穷尽验收、13输入/11UI/240帧、Astra有限复核、后续文档另批；用户zxpeng83/2118168362@qq.com、main/origin及远程地址、候选/确认要求/实际结果分开 | [git-submission-history.md](../reviews/git-submission-history.md#ui-acceptance-commands) | 原正文与全部代码围栏保留；历史快照清单中的实际旧路径不机械改名 |

</details>

<details>
<summary>g104engine/docs/reviews/v1-ui-mouse-fix-2026-10-04.md（5项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| C086 | 页首适用阶段、后续反馈与导航 | 4be648e准备基线与38cb85f实际保存；用户悬停正常长按不生效、ActiveId误清/轮询丢短点击双根因；WantCaptureMouse/有序窗口事件/失焦释放/订阅退订；修前3FAIL、各13输入/11旧UI/完整verify/240集成；File.Replace沙箱失败及滚轮Render后清零错误采样已纠正；真实生产输入路径和有限隐藏窗口/Win32同步消息边界；用户单项反馈与随后首轮1–5初步非穷尽通过分开 | [v1-ui-mouse-fix-2026-10-04.md](../reviews/v1-ui-mouse-fix-2026-10-04.md#historical-evidence) | 只改当前导航并加稳定锚点；原历史正文逆向还原一致 |
| C087 | 用户现象与根因 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；4be648e准备基线与38cb85f实际保存；用户悬停正常长按不生效、ActiveId误清/轮询丢短点击双根因；WantCaptureMouse/有序窗口事件/失焦释放/订阅退订；修前3FAIL、各13输入/11旧UI/完整verify/240集成；File.Replace沙箱失败及滚轮Render后清零错误采样已纠正；真实生产输入路径和有限隐藏窗口/Win32同步消息边界；用户单项反馈与随后首轮1–5初步非穷尽通过分开 | [v1-ui-mouse-fix-2026-10-04.md](../reviews/v1-ui-mouse-fix-2026-10-04.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C088 | 实际修改 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；4be648e准备基线与38cb85f实际保存；用户悬停正常长按不生效、ActiveId误清/轮询丢短点击双根因；WantCaptureMouse/有序窗口事件/失焦释放/订阅退订；修前3FAIL、各13输入/11旧UI/完整verify/240集成；File.Replace沙箱失败及滚轮Render后清零错误采样已纠正；真实生产输入路径和有限隐藏窗口/Win32同步消息边界；用户单项反馈与随后首轮1–5初步非穷尽通过分开 | [v1-ui-mouse-fix-2026-10-04.md](../reviews/v1-ui-mouse-fix-2026-10-04.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C089 | 失败到通过的证据 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；4be648e准备基线与38cb85f实际保存；用户悬停正常长按不生效、ActiveId误清/轮询丢短点击双根因；WantCaptureMouse/有序窗口事件/失焦释放/订阅退订；修前3FAIL、各13输入/11旧UI/完整verify/240集成；File.Replace沙箱失败及滚轮Render后清零错误采样已纠正；真实生产输入路径和有限隐藏窗口/Win32同步消息边界；用户单项反馈与随后首轮1–5初步非穷尽通过分开 | [v1-ui-mouse-fix-2026-10-04.md](../reviews/v1-ui-mouse-fix-2026-10-04.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |
| C090 | 用户反馈与当前接续 | 本节原日期/版本/事实/失败与通过结果/限制完整保留；4be648e准备基线与38cb85f实际保存；用户悬停正常长按不生效、ActiveId误清/轮询丢短点击双根因；WantCaptureMouse/有序窗口事件/失焦释放/订阅退订；修前3FAIL、各13输入/11旧UI/完整verify/240集成；File.Replace沙箱失败及滚轮Render后清零错误采样已纠正；真实生产输入路径和有限隐藏窗口/Win32同步消息边界；用户单项反馈与随后首轮1–5初步非穷尽通过分开 | [v1-ui-mouse-fix-2026-10-04.md](../reviews/v1-ui-mouse-fix-2026-10-04.md#historical-evidence) | 章节继续留原评审页；正文差异限定为可审阅导航（详见review-navigation-c.json） |

</details>

<details>
<summary>g104engine/docs/setup.md（3项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT067 | 5-26 按问题导航 | 运行/学习/场景/物理/渲染/验证/环境/依赖/历史探针/Git/复现/公开范围/评审入口；D5暂缓、旧探针不能覆盖Program、首次31非未来清单 | [README.md](../../../README.md#start)、[foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md)、[git-and-publishing.md](../guides/git-and-publishing.md) | 导航直接进任务；移走重复状态和中转层 |
| ROOT068 | 28-88 历史与兼容锚点 | 原setup全文在历史快照；按需读资料、行数/字节不是token精确值；旧锚点原本指向专题 | [foundation-documents-2026-10-02.md](../archive/foundation-documents-2026-10-02.md)、[document-restructure.md](document-restructure.md) | 旧页撤下不保留空壳；修正现行链接，记录旧路径对应；不承诺旧URL兼容 |
| A030 | 按问题导航/旧章节兼容，原5–88行 | 阅读主题/迁移前章节与初建历史保留方式 | [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#foundation-20261002) | 现行导航root归README；历史说明保留索引，旧锚点无空壳 |

</details>

<details>
<summary>g104engine/docs/status.md（2项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT009 | 当前事实与限制 | V1初步非穷尽验收；转向38/800与UI13/11/verify/240各自批次；10GPU/双管线差异非性能证明；HDR/PCF/8WAV；缓存非远端 | [status.md](../status.md)、[v1-progress.md](v1-progress.md)、[v1-ui-mouse-fix-2026-10-04.md](../reviews/v1-ui-mouse-fix-2026-10-04.md) | 保留有效证据边界，明确本轮未重跑 |
| ROOT010 | 下一步与同步 | W/Space→E学习；练习脚本/排期待定；同范围Bug修复；D5暂缓；本地refs与过去实时远端核验区分 | [status.md](../status.md#resume)、[learning-map.md](../learning-map.md#first-input-lesson) | 恢复页给直接下一动作，不返回handoff重复读取 |

</details>

<details>
<summary>g104engine/third_party/openal-soft/1.25.2/SOURCE.md（1项）</summary>

| 项 | 原章节／位置 | 保留的信息 | 新位置 | 处理理由 |
|---|---|---|---|---|
| ROOT075 | 全文件 | 二进制/源码来源哈希、许可、验证范围与发布边界 | [SOURCE.md](../../third_party/openal-soft/1.25.2/SOURCE.md) | 逐字节不改 |

</details>

## 2026-10-05：统一文档链接显示名（后续维护）

用户要求链接显示名固定为文档原文件名，并明确要求写进规则。本轮在已有43份工程文档基础上处理，没有新增正式文档或改动文件路径。
- 已调整41份中的812处标签：802个内联链接、10个引用式链接。名称包含扩展名；目录、用途和章节说明放在链接外，地址里的章节锚点保留。
- 先保存43份原字节快照，按标签替换计划严格重建文本，非标签差异为0、剩余非规范文档标签为0；随后单独新增AGENTS长期规则与本次维护记录。
- 链接验证通过：952个本地链接、412个章节锚点有效。网页没有文件名、同页章节、图片、源码及代码示例按原语义保留。
- Astra Ultra独立抽查指定7份正文的483处标签，所有URL/锚点及十个引用ID/定义逐字符相同；旧路径标识/折叠标题未变。三份原archive的六个历史代码围栏原字节保持，原foundation快照整文件不变。
- 快照、替换计划和验证结果保存本机g104engine/.cache/execution/link-labels-20261005-021037/；缓存不随Git同步。此次未构建/运行引擎，未暂存、提交或推送，资料与源码/配置/资产保持。

命名约定的唯一长期位置是根AGENTS.md“文档维护”段。这里记录2026-10-05这次修改，不回写或冒充2026-10-04原重构的验收结果。
