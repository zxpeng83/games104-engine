# G104Engine 目标、边界与下一阶段
这是稳定需求文档；实际进度只看 [status.md](status.md)，无需每轮重复读取。

## 项目目标
- 用户学完 GAMES104、有十份详细笔记，熟悉 C#/Unity，C++ 经验较少。
- 短期服务 Gameplay/游戏客户端求职，长期打磨可扩展的独立 3D 引擎。
- 首个求职版本的参考预算曾为 2–4 周、每周 10–15 小时；新对话先确认是否仍适用，不承诺一次实现所有系统。
- 渲染、动画、物理、粒子、声音、工具链、Gameplay/AI、网络和核心架构均保留学习记录；按阶段选择实现，Lumen/Nanite 属进阶研究。
- 代码需要简短中文注释；架构图、代码—课程章节—参考实现—验证证据需随确认设计与实现维护。

## 已确认开发基础
| 项目 | 基线 |
| --- | --- |
| 展示名称 / 工程目录 | G104Engine / g104engine |
| 语言与平台 | C#、.NET 10、VS2026、Windows x64；独立运行，不依赖 Unity |
| SDK | global.json 选择 10.0.401，rollForward=disable，allowPrerelease=false |
| 图形能力 | OpenGL 4.3 Core；实际上下文至少 4.3，不静默降级 |
| 直接依赖 | OpenTK.Graphics、Windowing.Desktop、Mathematics，均为 4.9.4 |
| 基础工具 | System.Text.Json；初期 Console/Trace |
| 版本控制 | games104 为 Git 根，main → origin/main；公开 zxpeng83/games104-engine |
| 参考实现 | Piccolo 固定 main 提交 f5053707fed4d3f94d270a436fb0d3a8ae54e3e5；旧案例另核对 |

其他图像、模型、物理、音频、UI 库随对应功能讨论，不提前引入。SAC 暂关仅是本机用户选择，不属于技术依赖。

## 目录与范围
~~~text
games104/                     Git 根
├─ AGENTS.md / README.md       协作规则、项目入口
├─ g104engine/
│  ├─ g104engine.slnx / global.json
│  ├─ src/G104.Engine/         目前是空类库模板，含包引用和锁文件
│  ├─ samples/G104.Sandbox/    临时 OpenGL/Smoke 探针
│  └─ docs/                   短入口、专题指南、设计、复查、历史
├─ 资料/笔记博客汇总/          原路径保留十份指定正文和一张配图
├─ 操作流程/                 仅本地，不默认读取或发布
└─ PiccoloPro/               独立参考仓库，不上传
~~~

<a id="发布文件范围"></a>
精确白名单、非发布资料与原笔记链接决定统一维护于 [publishing.md](publishing.md)。不自动扩充资料，不创建嵌套 Git 仓库。

## 执行分工
- 用户在 VS 操作安装、工程配置和 NuGet；Git 用 Git Bash／Git GUI。助手负责说明、文档与授权范围内的核验。
- global.json 曾专项委托助手创建并已完成，不扩展为所有环境操作授权。
- 每批 Git 提交/推送须先展示最终范围并取得明确确认，由用户执行；确认框不可见时接受针对该批的明确聊天确认。
- 新对话先讨论架构与详细设计，后续编码分工届时确认；不将“进入设计”自动理解为开始功能开发。

## 阶段结论
| 阶段 | 结论 |
| --- | --- |
| D0 文档与目录 | 完成；本轮将入口缩短、细节与历史按需加载 |
| D1 IDE/SDK | 本机安装核验完成 |
| D2 工程/NuGet | 项目、引用、双配置构建及模板运行通过 |
| D3 环境探针 | 本机图形和托管 smoke 通过；证据及局限见 status |
| D4 Git/GitHub | 首次提交/同步完成，上游已绑定 |
| D5 独立复现/CI | 用户要求整体暂缓，未验证；不作为当前设计讨论的前置条件 |

## 下一阶段的确认顺序
先确定最小可展示目标与验收，再讨论模块职责、接口/数据流、生命周期、数学约定和可扩展点，最后拆实施任务。
现有两个项目、包归属、GameWindow 派生类只是环境验证安排，不冻结引擎架构。
正式设计写入 [architecture.md](architecture.md)，课程映射按需查 [learning-map.md](learning-map.md)；原架构草案没有被批准，不可直接执行。
