# 搭建与学习资料导航
更新：2026-10-02。这里是索引，详细操作和补充知识已按主题保留，不需要每轮全读。
当前状态见 [status.md](status.md)，新对话任务见 [handoff.md](handoff.md)。

## 按问题查阅
| 想了解什么 | 详细资料 |
| --- | --- |
| VS/.NET 安装、项目创建、x64、SAC、仅我的代码 | [环境与排错](guides/environment.md) |
| global.json、SDK/运行时区别、NuGet 锁文件及升级操作 | [依赖与版本锁定](guides/dependencies.md) |
| OpenGL 窗口、设备输出、--smoke 示例及运行命令 | [环境探针](guides/probes.md) |
| GitHub/Git Bash/Git GUI、提交身份、清单、上游绑定 | [Git 工作流](guides/git-workflow.md) |
| 以后换设备如何复现 | [独立复现](guides/reproduction.md)，D5 整体暂缓，不立即执行 |
| 哪些文件上传、哪些保持本地 | [发布范围](publishing.md) |
| 本阶段发现的问题与证据范围 | [阶段复查](reviews/foundation-review-2026-10-02.md) |

D0–D4 已完成；D5 暂缓。指南保留已完成步骤用于学习，现有工程不必按流程重新创建。
探针示例是当时用于验证的代码，后续实际源码变更时先看代码，不用旧示例覆盖新实现。
Git 原始 31 文件清单只是首次提交记录；本次文档拆分后文件数增加，下次提交须重新检查实际清单。
每次提交仍需明确确认；用户自行操作 Git，助手不能将文档编辑视为上传授权。

## 历史保留与阅读方式
完整的整理前规则、入口、计划、进度、setup 原文保存在 [历史快照](archive/foundation-documents-2026-10-02.md)。
优先从本页选主题，再用标题或 rg 定位所需段落；不要默认加载全部 guides、archive 或课程笔记。
保存更多资料本身不要求每次读入上下文；减少默认读取和重复描述，才能减少上下文占用。
本次用行数/UTF-8 字节衡量缩减，不宣称是模型 tokenizer 的精确 token 数。

## 旧章节链接兼容入口

<a id="1-安装-vs2026-与-net-10本机已通过核验"></a>
[安装环境](guides/environment.md)

<a id="2-用户在-vs-中创建解决方案与项目"></a>
[工程创建](guides/environment.md)

<a id="21-空解决方案"></a>
[2.1 空解决方案](guides/environment.md)

<a id="22-添加两个-net-项目"></a>
[2.2 两个项目](guides/environment.md)

<a id="221-sandbox-误建为类库时如何修正"></a>
[2.2.1 Sandbox 模板修正](guides/environment.md)

<a id="223-本机-sac-拦截的处理说明仅供了解未执行关闭"></a>
[2.2.3 SAC 处理记录](guides/environment.md)

<a id="224-release-的仅我的代码警告"></a>
[2.2.4 仅我的代码](guides/environment.md)

<a id="3-用户通过-nuget-安装指定组件"></a>
[3 NuGet 安装](guides/environment.md)

<a id="41-固定-sdk版本选择"></a>
[4.1 SDK 选择与学习说明](guides/dependencies.md)

<a id="42-nuget-依赖锁文件第-41-节核验后继续"></a>
[4.2 NuGet 锁文件与依赖升级](guides/dependencies.md)

<a id="43-最小-opengl-环境探针"></a>
[4.3 OpenGL 探针](guides/probes.md)

<a id="44-非交互---smoke-检查"></a>
[4.4 Smoke](guides/probes.md)

<a id="5-用户通过网页和-vs-建仓关联与同步"></a>
[5 Git 工作流（已改用 Git Bash／Git GUI）](guides/git-workflow.md)

<a id="51-github-网页建立空仓库"></a>
[5.1 GitHub 空仓库](guides/git-workflow.md)

<a id="52-用户自行初始化并关联远程"></a>
[5.2 本地初始化](guides/git-workflow.md)

<a id="initial-submit-manifest"></a>
[首次提交 31 文件历史清单](guides/git-workflow.md)

<a id="54-首次推送后的本地上游配置"></a>
[5.4 上游配置](guides/git-workflow.md)

<a id="6-第二台设备如何接续"></a>
[6 复现流程（暂缓）](guides/reproduction.md)
