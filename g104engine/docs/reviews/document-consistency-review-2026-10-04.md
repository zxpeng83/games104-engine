# 项目文档一致性复查

<a id="historical-evidence"></a>

日期：2026-10-04。用户要求的全面文档检索/更新已完成，Astra Ultra有限独立终审未发现剩余有证据的阻塞P1/P2。本批修改45份原有Markdown并新增本记录，合计46个未提交路径；只修改文档，不新增实现、构建或运行引擎。

后续导航整理（2026-10-04）：本页49份文档、45修改＋1新增、629条链接和15锚点均是前一次一致性审计的历史结果；本次43份布局重构另见 [document-restructure.md](../execution/document-restructure.md)。本页只更新导航，未重跑原功能验证，也不以旧统计证明新布局检查通过。

## 范围与事实基线

- 原有项目Markdown共48份：根README/AGENTS 2份，docs根7份，execution 1份，guides 10份，plans 11份，reviews 11份，decisions 2份，archive 3份，OpenAL SOURCE 1份；本记录为新增第49份。
- 源码基线`38cb85f2be6d86804edd3331a54467389b3af604`，用户已提交UI修复及首轮验收16文件，时间2026-10-04 17:06:43 +08:00。本轮实查HEAD/main/origin/main及实时GitHub main一致，审计开始工作区干净。
- V1首轮实现、独立评审、转向/鼠标反馈修复完成，用户确认运行指南第1–5项首轮人工验收通过（初步、非穷尽）；学习尚未完成，同范围Bug继续反馈修复。
- 原笔记、其中26处非发布引用、私有截图和Piccolo保持既有边界；不将本次文档维护扩展到这些资料。3份archive保留原始历史文本，只检查适用性与导航，旧“当前”语句按归档日期理解。

## 修正方向

1. 总计划和模块表明确V1实际能力、未来模块目标及尚待确定的范围，纠正空模板/仅探针、未选UI库、未接入依赖、素材未部署等旧叙述。
2. 实施方案/依赖准备正文标明已执行事实与准备历史；未来实验仍保留候选状态，不因V1验收通过就批准新专题。
3. 运行/架构/学习指南与实际入口、输入事件后端和当前数据合同一致；setup优先指向V1运行和学习，环境/探针/建仓步骤按历史或条件参考说明。
4. README/status/handoff/publishing及执行台账更新真实Git节点；旧16文件提交清单记为已保存历史，本批文档更新另列实际范围。
5. 评审/决策记录保留当时证据与理由，补阶段时间和后续结果，避免历史“未授权/未提交/待验收”再次成为当前操作要求。
6. 明确7个Kenney转码WAV与1个生成loop-test共8个运行WAV；当前DTO没有活动相机/动态刚体组件，原设计中的相关约束与当前实现区分。

## 验证与剩余范围

最终静态校验：49份项目Markdown全部可按UTF-8读取，629条代码围栏外本地链接及15个本地锚点有效，代码围栏闭合，Git diff --check通过。指向课程笔记的链接只核对文件存在性，不遍历笔记内部26处非发布引用。

Git范围检查：45修改＋1新增均为Markdown，暂存区为空，HEAD保持38cb85f；源码、项目/SDK/NuGet锁定、Shader/JSON/二进制资产、原笔记及3份归档均无差异。所有已存在功能验证均引用原批次证据，本次没有重跑引擎。

| 文档区域 | 本次变更数 |
| --- | ---: |
| 根README/AGENTS | 2 |
| docs根入口/架构/学习/发布 | 7 |
| execution | 1 |
| guides | 10 |
| plans | 11 |
| reviews（含新增本记录） | 12 |
| decisions | 2 |
| OpenAL SOURCE说明 | 1 |

具体路径清单和SHA-256清单保存在本机`.cache/execution/document-audit-changed-paths.txt`与`document-audit-change-manifest.json`；检查统计在`document-audit-validation.json`。它们是本机辅助记录，不替代Git提交，也不随仓库同步。

外部网页和旧论文内容沿用固定来源，本次不声称所有外部URL重新联网验证。后续统一版本排期、具体独立练习/展示脚本、D5及各高级专题仍保持未完成/后移，不能为消除“待办”字样改成通过。

## 分工与接续

Sol Ultra三组完成plans、guides/architecture/setup、reviews/decisions维护，root完成总入口/计划/同步/学习映射及统一验证。Astra Ultra只读核对各组最终差异，抽查真实代码/配置，指出的OpenTK包数、展示验收边界、台账时态、Jolt验证性质、Voice所有权和动画/声音事件消费差异均已修正；最终未发现剩余有证据的阻塞P1/P2。全部文档写入任务已结束。

本次文档更新仅本机未提交。当前学习入口为 [architecture.md](../architecture.md)、[learning-map.md](../learning-map.md)，实际范围与授权仍见 [status.md](../status.md) 及 [v1-progress.md](../execution/v1-progress.md)。
