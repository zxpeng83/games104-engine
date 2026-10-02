# Git 建仓、提交与同步操作记录

从原 setup.md 按主题迁移，保留详细步骤与学习内容。按需查阅，不是新对话必读。
当前完成状态以 [status.md](../status.md) 为准；[返回搭建导航](../setup.md)。已完成步骤不要重复执行。

## 5. 用户通过 GitHub 网页及 Git Bash／Git GUI 建仓、关联与同步

**开始本节前，助手先检查发布清单、根目录的 `.gitignore` 和 `.gitattributes`。同步范围以 [plan.md](../plan.md) 为准。** 本地 Git 根为 `E:\game_study\games104`；现有 Piccolo 独立仓库不作子模块上传。

`games104/操作流程/` 是用户个人截图目录，已用根规则 `/操作流程/` 显式忽略。不要暂存、强制添加、上传其中内容，也不把它加入解决方案项、项目资源或工程文档。提交前助手核对忽略状态和实际文件范围，不需要读取截图。

### 5.1 GitHub 网页建立空仓库

**本机对应远程已完成：** 用户创建了 [zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)，HTTPS 地址为 `https://github.com/zxpeng83/games104-engine.git`。2026-10-02 API 查询为 public、size=0、默认分支名 main，`git ls-remote` 成功且未返回任何引用，确认当时无已有提交。下方保留建仓过程供复现，不要重复创建。当前发布候选为 31 个文件，原笔记引用保持原样。

1. 登录自己的 GitHub 账号，检查是否已有 `games104-engine`。同名仓库存在时先让助手核对，不重建或覆盖。
2. 网页“+ → New repository”，Owner 选本人账号，名称填 `games104-engine`，可见性选 **Public**。
3. 不套用模板，不生成 README、`.gitignore` 或 License，创建一个空仓库。本地文档已存在；此处额外初始化会增加需要处理的远程提交。
4. 创建后保留页面中的 **HTTPS 地址**，告知助手仓库地址；不把访问令牌或密码写进聊天和项目文件。

GitHub 官方说明支持网页建仓，并建议导入已有内容时不要额外预填文件，见 [创建仓库](https://docs.github.com/en/repositories/creating-and-managing-repositories/creating-a-new-repository)。

### 5.2 用户自行初始化并关联远程

当前用户选择使用 Git Bash／Git GUI，暂不使用 VS Git。**最新核验：Git 根、origin、main 分支和提交姓名/邮箱均已配置正确，第 5.2 节完成。** 以下初始化命令和本机收尾过程保留供复现，不需要重复执行。

需要达成的结果：本地根为 **`E:\game_study\games104`**，初始分支名 `main`，远程名 `origin`，地址 `https://github.com/zxpeng83/games104-engine.git`。保留已有 `.gitignore`、`.gitattributes` 和 README。

用户如选择 Git Bash，可按以下顺序操作（助手不代执行）：

```bash
cd /e/game_study/games104
pwd
git init -b main
git remote -v
```

确认 `pwd` 是目标目录。新仓库的 `git remote -v` 通常无输出；确认没有 `origin` 后再添加：

```bash
git remote add origin https://github.com/zxpeng83/games104-engine.git
git rev-parse --show-toplevel
git branch --show-current
git remote -v
```

若用户选择 Git GUI，使用客户端的创建本地仓库及添加远程功能，核对上面相同的目录、分支和 URL；具体菜单因客户端而异。若客户端自动提交，应停在提交动作前，遵守原有确认规则。单独 `git init` 和 `git remote add` 不提交或上传文件。

不要把空远程克隆到已有工程内形成第二层目录；不要在 `g104engine` 或 `资料` 中初始化。若发现仓库或 `origin` 已存在，先查看并核对，不能重复添加或直接覆盖。完成后告知助手“本地初始化与关联完成”，先验收再提交。

#### 本机收尾：分支名与提交身份

2026-10-02 早先核验发现 HEAD 为 `master` 且尚无提交，提交身份未配置；用户随后已完成更名和身份设置，最新检查为 `main`、姓名/邮箱可读取。以下为当时的处理步骤，保留供学习：

```bash
cd /e/game_study/games104
git branch -m main
```

接着配置**当前仓库**的提交身份。以下引号内是说明性占位文字，先替换成你实际选择的值再执行：

```bash
git config --local user.name "你希望提交记录显示的姓名"
git config --local user.email "你的提交邮箱"
```

姓名不强制与 GitHub 用户名相同；邮箱选择 GitHub 已验证邮箱或账户“Settings → Emails”页面给出的 noreply 地址。公开提交中的姓名和邮箱属于提交元数据；这里不填写账号密码或访问令牌。仓库级 `--local` 配置只影响当前仓库。依据：[Git 分支更名](https://git-scm.com/docs/git-branch)、[GitHub 提交邮箱说明](https://docs.github.com/en/account-and-profile/how-tos/email-preferences/setting-your-commit-email-address)。

完成后可用以下只读命令自查并通知助手，无须先提交：

```bash
git branch --show-current
git config --get user.name
git config --get user.email
git remote -v
```

预期分支为 `main`，身份为你选择的值，origin 地址不变。目前没有 upstream 是正常的，首次推送时再建立跟踪关系；不要为了消除这一状态执行一次未经确认的提交或推送。

### 5.3 首次提交与推送

**首次提交已完成并核验：** 本地和 GitHub main 均为 `3ea75150225e6df5fad9c92a5d03f2b1ad087be7`，31 文件清单及提交树一致。以下清单和命令保留为首次提交的操作记录，不要重复执行初始化提交。后续提交仍走“暂存 → 实际清单核对 → 明确确认 → 用户提交/推送 → 核验”的流程。

<a id="initial-submit-manifest"></a>
#### 首次提交清单（31 个文件）

以下路径均相对仓库根 `E:\game_study\games104`，每行一个文件；首次提交为一次完整的开发基础快照。

```text
.gitattributes
.gitignore
AGENTS.md
README.md
g104engine/docs/architecture.md
g104engine/docs/archive/architecture-draft-2026-10-01.md
g104engine/docs/decisions/0001-foundation.md
g104engine/docs/decisions/0002-defer-implementation-architecture.md
g104engine/docs/learning-map.md
g104engine/docs/plan.md
g104engine/docs/setup.md
g104engine/docs/status.md
g104engine/g104engine.slnx
g104engine/global.json
g104engine/samples/G104.Sandbox/G104.Sandbox.csproj
g104engine/samples/G104.Sandbox/Program.cs
g104engine/samples/G104.Sandbox/packages.lock.json
g104engine/src/G104.Engine/Class1.cs
g104engine/src/G104.Engine/G104.Engine.csproj
g104engine/src/G104.Engine/packages.lock.json
资料/笔记博客汇总/第04-07节_渲染部分总结/AI梳理2/GAMES104_第4-7节_渲染部分知识体系总结.md
资料/笔记博客汇总/第08-09节_动画部分总结/GAMES104第08-09节_游戏动画系统完整梳理.md
资料/笔记博客汇总/第10-11节_物理系统部分总结/GAMES104_第10-11节_物理系统详细总结.md
资料/笔记博客汇总/第12节_粒子和声效部分总结/GAMES104_第12节_粒子与声音系统详细总结.md
资料/笔记博客汇总/第13-14节_引擎工具链/GAMES104_第13-14节_引擎工具链详细梳理.md
资料/笔记博客汇总/第15-17节_Gameplay玩法总结/GAMES104_第15-17节_Gameplay玩法系统完整梳理.md
资料/笔记博客汇总/第15-17节_Gameplay玩法总结/image/GAMES104_第15-17节_Gameplay玩法系统完整梳理/1789309063459.png
资料/笔记博客汇总/第18-19节_网络游戏部分总结/GAMES104_第18-19节_网络游戏架构完整学习讲义.md
资料/笔记博客汇总/第20节_现代游戏引擎架构总结/GAMES104_第20节_现代游戏引擎架构_详细总结.md
资料/笔记博客汇总/第21节_动态全局光照和Lumen总结/GAMES104_第21节_动态全局光照和Lumen知识体系总结.md
资料/笔记博客汇总/第22节_GPU驱动的几何管线-nanite总结/GAMES104_第22节_GPU驱动的几何管线与Nanite_知识体系总结.md
```

分组为根配置与入口 4 个、工程/源码/SDK/锁文件 8 个、工程文档 8 个、笔记 10 个及配图 1 张。`Class1.cs` 仍是类库模板，本次如实纳入，不将其记为引擎功能；历史架构草案也有明确的非执行依据标记。笔记中的非发布引用按用户决定保留。

排除 `操作流程/`、Piccolo、其他课程资料、备份、`.vs`、`bin`、`obj`、下载缓存。Git 元数据 `.git` 由 Git 自行管理，不作为普通文件提交。两个锁文件纳入清单，`obj/project.assets.json` 不纳入。

#### 首次操作记录：暂存并交回核对（已完成）

在 Git Bash 中执行：

```bash
cd /e/game_study/games104
git add --all
git -c core.quotepath=false diff --cached --name-status
git diff --cached --stat
git diff --cached --check
```

本次可以使用 `git add --all`，因为当前 31 个未跟踪文件及生效的忽略规则已核对，且没有其他已跟踪改动；它不会忽略 `.gitignore` 的排除规则。不要加 `-f`。这一命令不是以后每次都可以不经检查全量暂存的规则。

预期暂存清单恰好对应上面 31 个文件，均为 `A`（新增）。`--check` 无输出表示未发现它检查的空白错误，不是全部代码测试。若数量或内容不同、出现被排除目录，先保留状态交给助手，暂不提交。完成后告知“已暂存，请核对”，不必在聊天里重复抄写长清单。

#### 提交说明与确认后的操作

建议首次提交说明：

```text
chore: initialize G104Engine development foundation
```

摘要：建立 .NET 10/OpenTK 4.9.4 验证工程，固定 SDK 和 NuGet 依赖，加入 OpenGL/Smoke 探针、维护文档、十份课程笔记与配图。验证依据为本机双配置构建、用户可见 OpenGL 4.3 Core 运行和助手复核的双配置 smoke；CI、其他设备及引擎功能仍待完成。

目标为公开仓库 `zxpeng83/games104-engine` 的 `main`，执行人为用户。提交身份按实际 Git 配置在确认框中展示；不在指南重复保存私人邮箱。提交/推送范围必须按 [协作规则](../../../AGENTS.md) 经用户明确确认。

**以下仅供了解；实际暂存清单核对并确认后才执行：**

```bash
git commit -m "chore: initialize G104Engine development foundation"
git push -u origin main
```

第一条创建本地提交，第二条将 main 推送到 origin 并建立上游跟踪；若本次只确认本地提交，不执行第二条。认证由用户完成，遇到拒绝或远程已有历史时先检查，不使用强制推送。

### 助手随后验证

只读检查 Git 根、分支、提交、远程地址与同步状态，核对 GitHub 文件树和发布清单。文件保存、本地提交、成功推送是三个独立状态，分别记录。当前没有必要安装 GitHub CLI，也不由助手代为初始化、关联、提交或推送。

### 5.4 首次推送后的本地上游配置

**本机已完成并核验（2026-10-02）：** 用户在所用 Git GUI 界面中没有找到上游绑定入口，改用 Git Bash 执行下方命令后成功。本记录只描述本次操作，不据此断言所有 Git GUI 都不支持设置上游。后续遇到同样情况可以直接使用命令行，无需继续寻找 GUI 菜单。

origin 是远程仓库地址，upstream 则指定“当前分支通常与远程哪个分支同步”；二者不是同一配置。本机此前已成功推送，但尚未配置 upstream，按下面步骤补齐。

用户在 Git Bash 中执行：

```bash
cd /e/game_study/games104
git branch --set-upstream-to=origin/main main
git branch -vv
git status -sb
```

成功提示应包含 `branch 'main' set up to track 'origin/main'`；`git branch -vv` 显示 `[origin/main]`，`git status -sb` 首行显示 `main...origin/main`。用户截图符合这些结果，助手也只读确认 `branch.main.remote=origin`、`branch.main.merge=refs/heads/main`，HEAD 相对本地上游缓存的领先/落后计数为 `0 0`。

该操作只建立本地跟踪关系，不修改项目文件、创建提交或重新上传。`status` 下方的 `M README.md` 等是尚未提交的文档修改，与绑定失败无关；`0 0` 只比较提交，不表示工作区没有改动。本轮没有重新从服务器获取状态，远程首次提交一致性的证据见前轮验收记录。当前无需再次绑定，上述步骤保留供复现。

