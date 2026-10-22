# 新电脑首次搭建与跨设备接续

本页是从一台尚未准备开发环境的Windows电脑，到能够修改、构建和调试当前G104Engine V1的顺序入口。沿用仓库内已有的Engine/Sandbox项目、锁文件和正式素材；“从零搭建”指新电脑的环境准备，不要求从空白解决方案重写引擎。

2026-10-22补齐本指引，下载与安装细节分别维护在环境和依赖页。下面的命令供你在新电脑上执行；本次文档维护没有执行安装、克隆、还原、构建或第二设备测试。指南的静态核查与实际换机成功分别记录，当前实际结果以 [status.md](../status.md) 为准。

<a id="reproduction"></a>
<a id="new-machine-setup"></a>
## 按顺序完成这六步

| 步骤 | 你要完成什么 | 完成后应得到什么 |
|---|---|---|
| 1. 确定版本和电脑条件 | 保存需要带走的工程版本，检查Windows x64、图形与声音条件 | 确定要使用的源码版本；电脑能够提供OpenGL 4.3 Core，声音输出可用 |
| 2. 安装开发工具 | 按 [environment.md](environment.md#new-machine-environment) 安装Git、兼容的VS及必要组件、准确.NET SDK | Git可使用；准确SDK已安装，VS能支持当前工程格式和目标框架 |
| 3. 获取工程 | 按本页下方步骤克隆仓库并检查文件 | 已有完整源码、两个项目、锁文件、assets和OpenAL正式目录 |
| 4. 核对SDK并首次还原 | 在工程目录核对SDK；按 [dependencies.md](dependencies.md#first-restore) 在VS还原 | 选择到global.json指定版本，包获取成功，两个锁文件保持不变 |
| 5. 首次构建 | 按 [v1-run-and-review.md](v1-run-and-review.md#first-build) 选择Sandbox/x64并生成Debug和Release | 得到可运行输出；素材和原生DLL由构建自动部署 |
| 6. 在这台电脑完成检查 | 按 [v1-run-and-review.md](v1-run-and-review.md#first-machine-checks) 检查窗口、Play、声音、保存和断点 | 记录新电脑自己的结果，之后开始改代码和学习 |

第1、2步的环境说明无需先读项目历史；第4步只需要依赖页的首次还原及文件获取表。原包下载/离线加工是该页的可选分支，普通搭建不必执行。完成准备也不要求先通读架构、课程笔记或全部验证专题。

### 1. 先确定要带走什么

若从已有电脑接续，先核对那台电脑需要保留的修改已经保存、提交并同步到目标仓库；操作见 [git-and-publishing.md](git-and-publishing.md#daily-git)。只在旧机编辑过的文件、未提交修改和本机缓存，不会因新机克隆自动出现。不要仅靠旧机的origin/main缓存或领先/落后0 0推定服务器已有全部内容。

硬件与工具清单见 [environment.md](environment.md#new-machine-environment)。当前代码使用Windows x64原生文件，并申请和检查OpenGL 4.3 Core；只有“能安装.NET”还不能证明可以显示训练场。使用本地桌面做首次图形检查，远程桌面或虚拟机的图形能力另行判断。

### 2. 安装工具后，获取现有仓库

先完成 [environment.md](environment.md#new-machine-environment) 的工具安装。项目仓库入口为 [zxpeng83/games104-engine](https://github.com/zxpeng83/games104-engine)，HTTPS克隆地址是 `https://github.com/zxpeng83/games104-engine.git`。

下面使用 **Git Bash**，将工程放在当前用户的`source/games104`中。`~`在Git Bash中代表当前用户主目录；通常对应Windows的`C:\Users\你的用户名`。你可以选另一个可写位置，后文路径同步替换即可，不要求存在旧电脑的E盘。

```bash
mkdir -p ~/source
cd ~/source
git clone --branch main https://github.com/zxpeng83/games104-engine.git games104
cd games104
git --no-optional-locks rev-parse --show-toplevel
git --no-optional-locks rev-parse HEAD
git --no-optional-locks status --short
```

记录实际HEAD作为这次搭建的源码版本，首次克隆后工作区应无改动。目标`games104`目录若已经存在且非空，先确认里面是什么，再改选一个空的新目录，不用删除旧目录解决克隆错误。Git GUI也可选择“克隆已有仓库”，填写同一个HTTPS地址和本地目录。

克隆会获取现有仓库与历史；不再执行`git init`、创建新的GitHub仓库或手动关联同名远程，也不需要GitHub CLI。提交姓名、邮箱及推送认证属于以后保存代码的流程，按Git指南处理。克隆机制与界面来源：[GitHub克隆说明](https://docs.github.com/en/repositories/creating-and-managing-repositories/cloning-a-repository)、[git-clone](https://git-scm.com/docs/git-clone)。

网络失败时保留原错误，检查网络和仓库访问权限，修复后再获取；不要转到来源不明的代码镜像。当前指引只承诺仓库实际保存的内容，本机私有截图和独立Piccolo参考仓库不参与G104Engine构建。

### 3. 检查拿到的文件

在资源管理器打开仓库目录，以下结构应已经存在：

```text
games104/                              仓库根目录，名称可以自定
  README.md
  g104engine/
    g104engine.slnx
    global.json
    src/G104.Engine/                   .csproj、packages.lock.json和源码
    samples/G104.Sandbox/              .csproj、packages.lock.json和源码
    assets/                           模型、Shader、场景、动画配置、贴图、WAV和许可
    third_party/openal-soft/1.25.2/    OpenAL32.dll、许可、SOURCE.md和对应源码归档
```

文件获取方式与精确清单见 [dependencies.md](dependencies.md#acquisition-map)。缺少已跟踪文件时，先检查是否取得完整仓库和正确版本；正常获取仓库已经带有这些正式素材，不需要先去作者网站重新下载。

新目录没有`.vs`、`bin`、`obj`、NuGet缓存或`.cache`是正常现象。项目会在首次还原/构建时生成需要的中间文件和输出；原始素材ZIP、Python和FFmpeg不在普通启动链路中。旧笔记中的“新建解决方案/项目”和首次蓝色窗口探针是历史学习材料，不覆盖现在的工程。

### 4. 在正确目录核对SDK，然后还原

打开 **PowerShell**，进入你实际取得的`g104engine`目录。沿用上面的克隆位置时：

```powershell
$engineRoot = Join-Path $env:USERPROFILE 'source\games104\g104engine'
Set-Location -LiteralPath $engineRoot
Get-Content -LiteralPath '.\global.json'
dotnet --version
dotnet --info
```

`dotnet --version`应与`global.json`中的准确版本一致；`--info`应能定位这个文件，并显示x64 SDK信息。准确版本缺失或选错dotnet时，先按 [environment.md](environment.md#new-machine-environment) 修正，不通过删除global.json、修改net10.0或放宽rollForward绕过。

随后在VS通过“文件 → 打开 → 项目/解决方案”打开`g104engine.slnx`。按 [dependencies.md](dependencies.md#first-restore) 检查包源并执行首次还原；工程打开时若VS已自动还原成功，也按同一完成标准检查。还原期间会下载NuGet包、创建`obj`，这是新机准备的正常步骤。不要进入依赖升级流程或手工重新添加九个包。

### 5. 首次构建与启动

按 [v1-run-and-review.md](v1-run-and-review.md#first-build) 完成首次构建和输出检查，再进行该页的首次环境验收。该步骤接在成功还原之后；尚未生成输出时不能直接运行示例中的DLL，缺还原产物时也不能从`--no-restore`命令开始。

达到以下标准，才把这台电脑记录为当前V1开发环境已就绪：

- 准确SDK被选中，VS能打开现有方案，首次锁定还原成功，项目声明与两个锁文件没有意外变化。
- `Debug | x64`和`Release | x64`均构建成功，输出包含正式素材、OpenAL和NuGet原生库。
- 新机检查能运行Jolt及场景/动画/WAV逻辑；可见训练场能够显示、进入Play并移动角色，工具面板可操作。
- 音频后端初始化与人工声音测试通过；只有窗口成功、音频显示Unavailable时，声音部分仍未完成。
- 设计能够保存并重新加载，Debug能够命中断点。完成这些检查后，开始 [learning-map.md](../learning-map.md#first-input-lesson) 的源码学习或你自己的代码修改。

记录本次设备、日期、源码HEAD、SDK/VS、GPU/GL、音频结果以及未通过项。`--smoke`只打印入口/架构及部分程序集信息，单独PASS不能代替图形、原生物理、声音和调试检查；上一台电脑的PASS也不能作为本机证据。

### 可选：迁移个人设计或重做素材

用户保存的训练场设计位于旧机的`%LOCALAPPDATA%\G104Engine\TrainingGroundV1\Scenes\training-ground.json`，不在Git里。新机首次启动会使用仓库的种子场景；个人设计不是首次搭建必需文件。需要迁移时先在新机完成种子场景验收，再关闭程序、备份两端已有存档，复制所需设计；自定义设计引用的额外资源也要单独完整保留。读取/拒绝/备份规则见 [v1-run-and-review.md](v1-run-and-review.md#design-protection)。

希望学习原素材的准备过程，或明确需要重新生成时，再按 [dependencies.md](dependencies.md#optional-asset-preparation) 获取原ZIP、Python和FFmpeg。下载链接、准确文件名、哈希、提取位置及转换命令在该处；缓存ZIP不能替代工程内的正式文件。

## 已有历史与实际验证边界

2026-10-02用户先因无第二台电脑暂缓设备测试，随后明确将本机新目录克隆、CI配置/验证、第二设备全部暂缓。2026-10-22用户授权补齐新机搭建文档及下载来源；本次完成文档不等于已经在新电脑执行或验证成功，也不启动CI。

本机保存、Git提交/推送、独立克隆验证和第二设备结果分别核实。助手实际执行安装、还原、Git写入或新设备操作时，按具体用户委托及 [agent-workflow.md](../agent-workflow.md#current-policy) 办理；用户使用本页准备自己的新电脑，不需要因历史暂缓文字重做旧的工程创建步骤。

## 同一设备换聊天

同一设备、同一工作区直接从规则和status恢复当前任务，不为聊天新克隆、建工作树或自动启动旧代理。现有自动回归、人工初步体验、源码学习与跨设备是不同进度；按真正运行结果保存新的复现证据。
