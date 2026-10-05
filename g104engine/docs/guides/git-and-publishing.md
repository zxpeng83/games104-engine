# Git 操作与发布范围

本页提供日常保存流程和完整发布白名单。步骤由当前政策指定的已授权执行者办理，工具说明以Git Bash／Git GUI为准。操作政策以 [agent-workflow.md](../agent-workflow.md) 为准，当前授权、未提交差异和实际同步节点查 [status.md](../status.md)；阅读命令、准备清单或本地保存文件都不产生暂存、提交或推送授权。

已建立的仓库为 [zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)，HTTPS远程 `https://github.com/zxpeng83/games104-engine.git`。本地Git根为 `E:\game_study\games104`，分支 `main`，远程 `origin`，上游 `origin/main`。首次建仓、身份补设和上游配置已经完成，完整过程见 [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#git-foundation)，不要因阅读指南重复初始化或关联。

<a id="daily-git"></a>
## 每批日常保存

以下命令环境是 **Git Bash**，目录写作 `/e/game_study/games104`，不能直接当作PowerShell的目录语法。Git GUI可完成同样的文件选择和保存操作；当前客户端选择、执行分工和确认要求均查 [agent-workflow.md](../agent-workflow.md)。

### 1. 确认仓库、身份和真实差异

```bash
cd /e/game_study/games104
git --no-optional-locks rev-parse --show-toplevel
git --no-optional-locks branch --show-current
git --no-optional-locks remote -v
git --no-optional-locks config --get user.name
git --no-optional-locks config --get user.email
git --no-optional-locks -c core.quotepath=false status --short
git --no-optional-locks -c core.quotepath=false diff --name-status
git --no-optional-locks ls-files --others --exclude-standard
```

核对根目录、`main`、`origin`地址和当次实际姓名/邮箱；公开提交会保存姓名/邮箱元数据，不填写密码或令牌。身份或远程异常先解释，再按政策授权处理，不在核对时夹带配置写入。已配置的身份不因查阅历史而重设；姓名不必等同GitHub用户名，邮箱使用用户选择的已验证邮箱或noreply地址。

根据真实修改、删除和未跟踪文件列出本批范围，并逐项对照下方 [发布白名单](#publishing-scope)。同一目录内的新文件不自动属于该批；`.gitignore`不会排除已经跟踪的文件，也不会判断内容是否适合发布。未跟踪的正式源码/文档需明确纳入，缓存、用户设计或私有资料不能因全选遗漏检查。

### 2. 按范围暂存并核对实际index

Git GUI按已核对清单选择文件；Git Bash用 `git add --` 后逐项列出同一批真实路径。首次31文件使用过 `git add --all`，原因与当时清单仅存于建仓历史，不构成后续全量暂存授权；不使用 `-f` 强制加入忽略资料。

暂存后检查：

```bash
git --no-optional-locks -c core.quotepath=false diff --cached --name-status
git --no-optional-locks diff --cached --stat
git --no-optional-locks diff --cached --check
```

按实际路径和内容核对，不能只比较文件数。`diff --check`无输出仅表示未发现其检查的空白错误，不是功能验证。出现清单外变化时保留现场并重新核对，不继续提交。

提交前展示该批实际暂存范围、已有验证及未验证项、拟用提交说明、身份、仓库/分支和是否推送，并按 [agent-workflow.md](../agent-workflow.md) 完成该批要求的确认。实际范围变化时重新核对并按政策处理，不沿用已失效的范围确认。

### 3. 执行获准的保存动作

本地提交已获准后，在Git Bash使用该批已经确认的提交说明；推送也在授权范围内时才执行第二条：

```bash
git commit -m "填写本批已经确认的提交说明"
git push origin main
```

第一条创建本地提交，第二条上传到远端。仅Stage不会保存到GitHub，仅commit也不证明推送成功。Git GUI执行相同动作，认证按既有授权完成；拒绝或远端已有新历史时先核查，不覆盖历史或强制推送。不需要为本流程安装GitHub CLI。

### 4. 只读复核并分别记状态

核对实际HEAD、提交说明/时间、提交路径和剩余工作区，再检查本地上游缓存：

```bash
git --no-optional-locks log -1 --format=fuller
git --no-optional-locks -c core.quotepath=false show --format= --name-status HEAD
git --no-optional-locks -c core.quotepath=false status --short
git --no-optional-locks branch -vv
git --no-optional-locks rev-parse HEAD refs/heads/main refs/remotes/origin/main
git --no-optional-locks rev-list --left-right --count HEAD...refs/remotes/origin/main
```

`origin/main`是本地跟踪缓存，`0 0`只比较提交，不证明工作区干净或实时服务器一致。需要证明实时远端一致时，在当次可用网络权限内另行只读查询：

```bash
git --no-optional-locks ls-remote --heads origin main
```

分别记录“文件已在本机保存”“已本地提交”“已推送且实时核对”，注明核查时间与未执行的检查。不为核对执行fetch、暂存、提交、推送或上游重设；本次只查本地refs时，明确不能声称重新验证实时远端。

<a id="publishing-scope"></a>
<a id="发布文件范围"></a>
## 完整发布白名单

长期允许发布仓库入口和规则文件、新工程源码/项目配置及文档、必要CI配置，以及以下 **十份原路径正文和一张实际引用图片**。白名单不是整个 `games104` 目录；必要CI配置可发布也不意味着当前D5/CI实施已恢复，执行范围仍查状态与协作流程。

运行必需的 `g104engine/assets/` 已包括Shader、场景、配置、测试资产、已核对CC0模型/声音及许可；`g104engine/third_party/openal-soft/1.25.2/` 包括官方DLL、原许可、同版本完整源码归档及SOURCE说明。这些已随首次完整V1节点b91dfe4保存，属于正式恢复材料，不是bin/obj下载缓存。公开保存DLL前已补对应官方源码，版本、来源、SHA与许可说明见 [SOURCE.md](../../third_party/openal-soft/1.25.2/SOURCE.md)。后续新增资产或第三方文件仍核对来源、许可和实际暂存范围，不以目录已发布代替核对。

下列路径均相对 `资料/笔记博客汇总/`：

1. `第04-07节_渲染部分总结/AI梳理2/GAMES104_第4-7节_渲染部分知识体系总结.md`
2. `第08-09节_动画部分总结/GAMES104第08-09节_游戏动画系统完整梳理.md`
3. `第10-11节_物理系统部分总结/GAMES104_第10-11节_物理系统详细总结.md`
4. `第12节_粒子和声效部分总结/GAMES104_第12节_粒子与声音系统详细总结.md`
5. `第13-14节_引擎工具链/GAMES104_第13-14节_引擎工具链详细梳理.md`
6. `第15-17节_Gameplay玩法总结/GAMES104_第15-17节_Gameplay玩法系统完整梳理.md`
7. `第18-19节_网络游戏部分总结/GAMES104_第18-19节_网络游戏架构完整学习讲义.md`
8. `第20节_现代游戏引擎架构总结/GAMES104_第20节_现代游戏引擎架构_详细总结.md`
9. `第21节_动态全局光照和Lumen总结/GAMES104_第21节_动态全局光照和Lumen知识体系总结.md`
10. `第22节_GPU驱动的几何管线-nanite总结/GAMES104_第22节_GPU驱动的几何管线与Nanite_知识体系总结.md`

已找到并纳入上述正文引用范围的唯一图片：

`第15-17节_Gameplay玩法总结/image/GAMES104_第15-17节_Gameplay玩法系统完整梳理/1789309063459.png`

助手已编写精确忽略规则，并在首次发布前核对实际暂存清单。以后新增正文引用图片时，核对实际引用并更新白名单，不能放行整个 `资料` 或全部Markdown。

### 排除范围与保留决定

Piccolo、网页存档、PDF、其他笔记、备份、`.vs`、`bin`、`obj` 和下载缓存不发布。Git元数据 `.git` 由Git自行管理，不作为普通文件提交；NuGet锁文件应保存，`obj/project.assets.json`不纳入。Piccolo是独立参考仓库，不作为子模块，不修改其分支、源码或产物。

`操作流程/` 以根规则 `/操作流程/` 单独显式排除，其中截图仅由用户本地查看，不默认读取、遍历、引用、复制、暂存或上传；不加入解决方案/项目，不建指向截图的工程文档链接，不作为学习映射或演示资源。用户明确提供的附件仅用于本次核对，新设备不依赖该目录即可复现工程。

未发布原资料留在本地，不清理；已有上层 `E:\game_study\.git` 不在本任务处理范围。

2026-10-02在十份正文中发现 **26处本地引用目标不在发布范围**。用户明确决定“不处理”：保留原笔记正文、路径和链接，不插入未发布标注、不替换公开链接、不扩大资料白名单。公开仓库中这些链接可能无法访问，已作为限制记录，当时未阻碍首次同步；以后整理需另行提出。

<a id="git-history"></a>
## 保存历史的查阅入口

首次建仓/31文件/首推/上游步骤查 [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#git-foundation)；开工前31/40/55文件节点查 [v1-start-checkpoint-2026-10-03.md](../reviews/v1-start-checkpoint-2026-10-03.md)；首次V1、评审/转向修复、UI/人工验收三批的候选、实际文件清单及同步证据查 [git-submission-history.md](../reviews/git-submission-history.md)。这些历史文件数和命令不能用作下一批固定清单。最新同步状态只在 [status.md](../status.md) 维护。
