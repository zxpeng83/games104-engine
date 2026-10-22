# 基础综合训练场V1：运行、操作与验证

本页区分开始写C#前的完整前置检查与代码完成后的训练场运行。自己复原当前V1时，先把环境、包、第三方素材和全部项目自有非C#配套准备好，完成下节检查后只剩C#实现；Play、`--verify`等入口要等相应代码完成后才使用。当前进度以 [status.md](../status.md) 为准，执行/安装/Git职责见 [agent-workflow.md](../agent-workflow.md)。这些是操作指引，本次文档维护没有创建、构建或运行任何工程。

<a id="blank-project-checks"></a>
## 空白工程：怎样确认已经可以开始写代码

主流程见 [reproduction.md](reproduction.md#new-machine-setup)。先完成 [environment.md](environment.md#blank-project) 的两个项目、引用、平台和SDK配置，再按 [dependencies.md](dependencies.md#blank-project-packages) 安装准确包、生成并锁定两份依赖清单，并完成同页的第三方及全部自有非C#配套准备。此时保留VS生成的Console模板Program即可，不需要先实现引擎功能。

全部非C#文件齐备后，按 [environment.md](environment.md#resource-copy) 加入输出复制项，再做下面的构建、调试和文件检查。只建好项目或只下载第三方ZIP，还没有完成本节的最终前置准备；OpenAL源文件尚未放好时不要提前添加它的显式Content项。

在VS里将Sandbox设为启动项目，分别以Debug/x64和Release/x64生成解决方案。也可以在 **PowerShell** 中使用下面的项目入口命令，兼容方案实际保存为`.sln`或`.slnx`；第一行是本指引统一的新工程目录示例，改成你的实际目录即可：

```powershell
& {
    $engineRoot = Join-Path $env:USERPROFILE 'source\G104Rebuild\g104engine'
    Set-Location -LiteralPath $engineRoot -ErrorAction Stop
    dotnet --version
    if ($LASTEXITCODE -ne 0) { throw '准确SDK不可用，先完成环境检查。' }
    dotnet build .\samples\G104.Sandbox\G104.Sandbox.csproj --no-restore -c Debug -p:Platform=x64
    if ($LASTEXITCODE -ne 0) { throw 'Debug构建失败，先查看SDK、还原和项目配置。' }
    dotnet build .\samples\G104.Sandbox\G104.Sandbox.csproj --no-restore -c Release -p:Platform=x64
    if ($LASTEXITCODE -ne 0) { throw 'Release构建失败，先处理生成错误。' }
    dotnet .\samples\G104.Sandbox\bin\x64\Debug\net10.0\G104.Sandbox.dll
    if ($LASTEXITCODE -ne 0) { throw '模板入口运行失败。' }
}
```

整段放在同一个脚本块中，目录不存在或任一步失败就停止，避免在错误目录继续构建/运行。执行前两项目必须已成功还原；`--no-restore`不会补下载缺包。模板程序输出Hello World或你保留的模板文字并退出，是这个阶段的正常结果，不应该期待出现训练场窗口。然后在模板Program的可执行语句上设置断点，在VS中用Debug/x64按F5，确认能命中、观察变量并继续。

| 开始写代码前检查 | 怎样判断已经完成 |
|---|---|
| 工具与配置 | 方案根目录`dotnet --version`为10.0.401；两项目net10.0/x64，Sandbox引用Engine，Engine允许unsafe |
| 依赖清单 | 九个直接包位于Engine；两项目有各自的packages.lock.json，严格模式下再还原成功；理解独立生成的传递图需要审查，不把它自动等同原仓库旧锁 |
| 构建与调试 | 两配置都能构建，模板入口能正常退出，Debug能命中断点 |
| 第三方输入 | 独立下载的OpenAL DLL及许可/对应源码、UAL1_Standard.glb及许可、七个mono/44100Hz/PCM16声音文件在依赖页规定的位置；保留来源与实际转换记录 |
| 自有非C#配套 | 19个自有运行文件齐全：13个Shader（3个vert、8个frag、2个glsl）、场景/动画2个JSON、material-probe.glb、2个PNG及loop-test.wav；它们均已逐文件复制或按依赖页准备，不留到写C#时补做 |
| 输出复制 | 在Sandbox相应bin/x64配置输出中找到assets和根目录OpenAL32.dll；NuGet原生文件保留runtimes/win-x64/native布局。复制配置由工程完成，不靠手搬bin |
| 完整齐套检查 | 源assets共有27个运行文件（19个自有＋UAL模型＋7个WAV），并保留许可/来源记录；Debug、Release输出都能找到同一套运行文件和应复制的许可 |

生成完成后，可在 **PowerShell** 执行下面的只读清点。源目录是工程根下的`assets`，构建输出资源是`samples/G104.Sandbox/bin/x64/Debug/net10.0/assets`及对应Release目录；这些路径都相对于`g104engine`。源`assets`不能误放到`src/G104.Engine/assets`或`samples/G104.Sandbox/assets`。

```powershell
& {
    $engineRoot = Join-Path $env:USERPROFILE 'source\G104Rebuild\g104engine'
    Set-Location -LiteralPath $engineRoot -ErrorAction Stop
    $authoredFiles = @(
        'shaders/mesh.vert', 'shaders/fullscreen.vert', 'shaders/particle.vert',
        'shaders/deferred.frag', 'shaders/forward.frag', 'shaders/fxaa.frag',
        'shaders/gbuffer.frag', 'shaders/particle.frag', 'shaders/shadow.frag',
        'shaders/sky.frag', 'shaders/tonemap.frag',
        'shaders/lighting.glsl', 'shaders/material.glsl',
        'scenes/training-ground.json', 'config/character-animation.json',
        'models/material-probe.glb', 'tests/checker.png', 'tests/normal.png',
        'audio/loop-test.wav'
    )
    $externalFiles = @(
        'models/UAL1_Standard.glb',
        'audio/step0.wav', 'audio/step1.wav', 'audio/door.wav', 'audio/jump.wav',
        'audio/click.wav', 'audio/complete.wav', 'audio/switch.wav'
    )
    $assetRecords = @(
        'licenses/asset-manifest.json',
        'licenses/quaternius-0-License.txt', 'licenses/quaternius-1-README.txt',
        'licenses/kenney_impact-sounds-License.txt',
        'licenses/kenney_interface-sounds-License.txt'
    )
    $assetRoots = @(
        'assets',
        'samples/G104.Sandbox/bin/x64/Debug/net10.0/assets',
        'samples/G104.Sandbox/bin/x64/Release/net10.0/assets'
    )
    $requiredFiles = @()
    foreach ($assetRoot in $assetRoots) {
        foreach ($relativeFile in ($authoredFiles + $externalFiles + $assetRecords)) {
            $requiredFiles += Join-Path $assetRoot $relativeFile
        }
    }
    $openAlRoot = 'third_party/openal-soft/1.25.2'
    foreach ($file in @('OpenAL32.dll', 'COPYING', 'LICENSE-pffft', 'readme.txt',
                        'SOURCE.md', 'openal-soft-1.25.2.tar.bz2')) {
        $requiredFiles += Join-Path $openAlRoot $file
    }
    foreach ($configuration in @('Debug', 'Release')) {
        $outputRoot = 'samples/G104.Sandbox/bin/x64/' + $configuration + '/net10.0'
        $requiredFiles += Join-Path $outputRoot 'OpenAL32.dll'
        foreach ($file in @('COPYING', 'LICENSE-pffft', 'readme.txt')) {
            $requiredFiles += Join-Path (Join-Path $outputRoot 'licenses/openal-soft') $file
        }
    }
    $missing = @($requiredFiles | Where-Object {
        -not (Test-Path -LiteralPath $_ -PathType Leaf)
    })
    if ($missing.Count -gt 0) {
        $missing | ForEach-Object { Write-Output ('Missing: ' + $_) }
        throw '文件未齐，请按缺失路径补齐来源目录或输出复制。'
    }
    $empty = @($requiredFiles | Where-Object {
        (Get-Item -LiteralPath $_ -ErrorAction Stop).Length -eq 0
    })
    if ($empty.Count -gt 0) {
        $empty | ForEach-Object { Write-Output ('Empty: ' + $_) }
        throw '发现空文件，不能作为已准备好的资源。'
    }
    foreach ($assetRoot in $assetRoots) {
        foreach ($jsonFile in @('scenes/training-ground.json',
                                'config/character-animation.json',
                                'licenses/asset-manifest.json')) {
            Get-Content -Raw -LiteralPath (Join-Path $assetRoot $jsonFile) -ErrorAction Stop |
                ConvertFrom-Json -ErrorAction Stop | Out-Null
        }
    }
    Write-Output '文件齐全且非空：19个自有＋8个外部运行资源、许可/来源及两配置输出；JSON语法可解析。'
}
```

这段只读取文件和JSON，不生成资源、不运行引擎；报Missing或Empty时先按列出的路径补齐，再重新清点。可以逐项手工检查同一份清单。通过只证明文件存在、非空、JSON语法可解析，不证明场景/动画语义正确、GLSL已编译、模型可加载或音频已经播放。

原样传送的文件可以比较复制前后SHA-256以确认传送一致；自行用FFmpeg重新转码的七个WAV，不要求与旧电脑manifest中的输出SHA相同，应保留实际输入/转换记录并按依赖页核对PCM16、mono、44100Hz，代码完成后再验证播放。不能用旧SHA是否一致代替格式或听感检查。

达到这些标准后，全部非C#前置已就绪，可以直接开始写C#代码。模板构建不能证明OpenGL、Jolt、ImGui或OpenAL真实初始化通过；它也不会加载模型或播放WAV。硬件图形能力在环境准备阶段检查支持条件，实际GL上下文要在窗口代码接入后验证。

### 编写或复制代码后，逐步增加验收

你可以自己编写各模块，也可以手动复制已有实现。复制完整Sandbox入口前，处理模板Program，避免重复顶层入口/类型；代码用到的引擎类和命名空间要完整到位。上面已准备当前V1需要的全部非C#配套；只有主动改变文件名、配置合同或Shader接口时，才需要相应适配资源。某个类尚未完成引起的编译错误属于C#实现阶段，不需要因此升级全部包。

- 只完成窗口时，验证窗口/输入和实际OpenGL版本；接入ImGui后再看面板。
- 接入物理、模型/动画、音频后，分别验证其初始化、文件读取与释放。
- 已复原或手动复制当前完整V1时，再使用下方完整工程的构建、Play、声音、保存及专项检查；若你自写了不同入口，应使用你自己的测试办法。

`--smoke`、`--verify`、`--verify-audio`和Play按钮都是本工程Program/窗口代码实现的功能，不是.NET、VS或NuGet安装后自动提供的功能。没有这些代码时不要把相应参数当作环境检查命令。

<a id="first-build"></a>
## C#代码完成后的首次构建

自建路线先在上面的[前置检查](#blank-project-checks)完成全部非C#配套，再自行写完或手动复制完整V1的C#代码，随后使用本节；选择 [reproduction.md](reproduction.md#existing-repository-setup) 获取完整仓库时，也先核对其配置与文件。准确SDK须已选中，两个项目均成功还原：新建项目按 [dependencies.md](dependencies.md#blank-project-packages) 生成并锁定依赖，已有成套项目/锁文件按 [dependencies.md](dependencies.md#first-restore) 的已有锁还原步骤处理。只有源码或包版本表不等于已有还原产物，`--no-restore`不会下载缺少的包。

1. 在VS打开实际方案：现有仓库为`g104engine/g104engine.slnx`，自己新建的方案也可以是`.sln`。右键解决方案资源管理器中的 **G104.Sandbox → 设为启动项目**。不要启动Engine类库，也不要重复新建同名项目。
2. 工具栏选择 **Debug | x64**。配置管理器中的Engine和Sandbox均应映射为x64并勾选“生成”；原方案已有映射，有疑问按 [environment.md](environment.md#222-x64-平台设置) 核对。
3. 选择 **生成 → 生成解决方案**，在“输出”窗口查看生成结果。应无构建错误；若有警告或失败，保留完整输出，先处理后再运行。
4. 检查下面的Debug输出。再切换到 **Release | x64** 生成一次，得到对应Release目录；完成后切回Debug供源码调试。不要仅改运行路径而忘记生成Release。

输出相对`g104engine`目录如下；这些由构建产生，不需要手动往bin导入：

| 输出位置 | 应有内容与作用 |
|---|---|
| `samples/G104.Sandbox/bin/x64/Debug/net10.0/` | `G104.Sandbox.dll`、`G104.Engine.dll`、`.deps.json`、`.runtimeconfig.json`及托管依赖；启动入口为Sandbox |
| 上述目录的`assets/` | 完整27个运行资源及许可/来源记录，来自工程根目录已准备好的assets（独立导入或可选仓库路线） |
| 上述目录的`OpenAL32.dll` | 项目内OpenAL Soft实现，位于应用根目录 |
| 上述目录的`runtimes/win-x64/native/` | 当前NuGet图提供的`cimgui.dll`、`glfw3.dll`、`joltc.dll`、`joltc_double.dll`，保留构建布局 |
| 上述目录的`licenses/openal-soft/` | 构建复制的OpenAL许可及readme；对应源码归档仍在工程根目录的third_party内 |

Release把路径中的`Debug`改为`Release`。不要只复制Sandbox单个DLL到别处运行，也不要把NuGet原生库从runtimes目录手动搬出；资源默认按应用输出位置查找。文件缺失时核对源资源目录、还原输出及Sandbox项目复制项，不能用旧电脑bin覆盖掩盖新机构建问题。

如需用命令行检查同一构建，先在 **PowerShell** 进入实际`g104engine`目录。下面沿用独立学习工程`source/G104Rebuild/g104engine`；可选克隆路线或其他目录须替换第一行。用Sandbox项目为入口会同时构建其Engine引用，兼容两种方案格式；已在VS生成成功时无需重复执行这组命令。

```powershell
& {
    $engineRoot = Join-Path $env:USERPROFILE 'source\G104Rebuild\g104engine'
    Set-Location -LiteralPath $engineRoot -ErrorAction Stop
    dotnet build samples/G104.Sandbox/G104.Sandbox.csproj --no-restore --disable-build-servers -m:1 -c Debug -p:Platform=x64
    if ($LASTEXITCODE -ne 0) { throw 'Debug构建失败，先检查输出。' }
    dotnet build samples/G104.Sandbox/G104.Sandbox.csproj --no-restore --disable-build-servers -m:1 -c Release -p:Platform=x64
    if ($LASTEXITCODE -ne 0) { throw 'Release构建失败，先检查输出。' }
}
```

<a id="first-machine-checks"></a>
## 完整V1在新电脑的首次功能验收

**前提是当前V1代码及全部运行资源已经到位并构建成功，包括下列参数对应的Program分支。** 这不是开始编写引擎前的要求。下面使用独立用户数据目录，便于把首次验收设计与以后的日常设计分开；它不是必须设置的系统环境变量。先停止VS中正在调试的程序，在 **PowerShell** 中进入上节的实际`g104engine`目录，再逐条执行：

```powershell
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --smoke
if ($LASTEXITCODE -ne 0) { throw '入口检查失败。' }
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify --user-data-root .cache/execution/new-computer-verify
if ($LASTEXITCODE -ne 0) { throw '逻辑或原生物理检查失败。' }
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-audio
if ($LASTEXITCODE -ne 0) { throw '音频设备或后端检查失败。' }
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --user-data-root .cache/execution/new-computer-first-run
```

| 检查 | 看什么才算这一项完成 |
|---|---|
| 托管入口 | `--smoke`退出0，打印的进程架构为X64，框架为.NET 10；它只打印部分信息，不会初始化GLFW、GL、Jolt或音频，不能单独作为全部依赖通过 |
| 逻辑与Jolt | `--verify`退出0，包含physics、animation、integration、audio等PASS和`Verification complete`；WAV检查只解析文件，不能代替音频设备播放 |
| 音频后端 | `--verify-audio`退出0并打印`PASS OpenAL actual context...`；此自动检查音量低且时间短，听感仍在可见窗口里确认 |
| 图形与原生UI | 最后一条打开完整Edit训练场，模型和工具面板可见，控制台输出GPU与GL版本；没有GL上下文/Shader/资源异常。程序会检查GL至少4.3 |
| 进入运行世界 | 点击Play，确认状态进入PLAY并能WASD移动、Space跳跃，再Stop。只看到Edit窗口不证明Jolt可用；若显示`Command rejected`或没有进入Play，应保留错误继续排查 |
| 实际声音 | 面板Audio应显示后端信息，不是Unavailable；使用2D/3D声音测试并听脚步或跳跃。音频初始化失败时窗口仍可继续，所以无声不能记为完整通过 |
| 文件写入 | 在Edit创建并命名一个测试Cube，Save design；关闭窗口后用同一条带`--user-data-root`的命令重开，确认保存的对象仍在。此设计只写到本次隔离目录，默认用户设计未被覆盖 |

然后在VS中以 **Debug | x64** 打开`TrainingWindow.cs`，在`OnUpdateFrame`内的可执行语句设置断点，按VS的F5启动，确认断点被命中、能检查变量并继续。若断点空心或提示源码不匹配，核对启动项目、配置和实际生成输出，按 [environment.md](environment.md#debugging) 处理。断点暂停后的帧耗时不是正常性能数据。

VS无参数启动使用`%LOCALAPPDATA%/G104Engine/TrainingGroundV1`中的日常设计；上面的隔离Cube不会自动出现，这是两套用户数据路径的正常区别。运行窗口内的F5是Play快捷键，VS里的F5是启动/继续调试。

最后以 **Release | x64** 用Ctrl+F5普通运行，确认仍可显示、Play/Stop及播放声音。上述检查通过即可记录完整V1在该机运行通过，不要求跑完下面所有专项回归。记录新机日期、源码版本或手工复制来源（有Git时记录HEAD）、SDK/VS、GPU/GL、声音和未通过项；这些步骤只有实际执行后才形成新机验证结果。全部非C#前置在本页第一节确认后就可以开始写C#，不必等到完整代码后的功能验收。

若失败，按本页[失败处理与结果解释](#失败处理与结果解释)保留第一处异常；准确SDK/VS见环境页，NuGet/素材缺失见依赖页。不要通过升级全部包、关闭锁定、删除用户设计或更换旧版源码来凑出PASS。

<a id="run-and-verify"></a>
<a id="命令行验证入口"></a>
## 目录、前提与最短运行方式

本机工程目录是E:\game_study\games104\g104engine，解决方案是g104engine.slnx；启动项目G104.Sandbox，Debug或Release/x64。已锁SDK10.0.401、net10.0、九个Engine直接包及双锁文件；运行所需assets/OpenAL按项目复制到输出。FFmpeg/Python仅参与已完成离线准备，普通运行不需要它们。

已有输出可直接运行DLL；需要构建时只使用已有还原依赖。新电脑先完成本页[首次构建](#first-build)；没有对应SDK/还原产物时按环境及依赖页处理，不在验证命令中混入自动restore、升级包或重建工程。构建更新bin/obj，运行的当前目录和应用assets目录不是同一概念。下面的E盘路径是原电脑例子，换机须使用实际路径。

在PowerShell中：

```powershell
Set-Location -LiteralPath 'E:\game_study\games104\g104engine'
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll
```

无参数为可见Edit训练场；Play/F5开始运行，Stop返回当前内存设计。普通无参数运行会读取真实用户保存设计，下方所有自动验证使用隔离根。可选在VS打开现有方案，选择G104.Sandbox与Debug/x64，用F5调试或Ctrl+F5普通运行。

## 在VS中运行

打开 [g104engine.slnx](../../g104engine.slnx)，使用现有Debug或Release/x64配置，启动G104.Sandbox。默认入口现在是完整训练场，运行不需要FFmpeg或Python；已部署素材与OpenAL由项目复制到输出目录。保持已锁定SDK/NuGet，不需要新增包。

启动为Edit。按Play或F5创建独立运行世界；Stop回到当前内存设计，未保存编辑及Undo历史保留。UI用英文标签，详细原理和操作在本项目中文文档中解释。

| 操作 | 行为 |
| --- | --- |
| WASD | 镜头相对移动 |
| 左Shift | 跑步 |
| Space | 接地时跳跃；按键边沿只消费一次 |
| E | 靠近橙色按钮时切换它明确引用的门；门洞有角色时拒绝关闭 |
| 右键拖动 / 滚轮 | 相机环绕 / 距离；工具捕获的输入不驱动角色 |
| F5 / Play / Stop | 编辑与运行切换 |
| Escape | 运行时停止；编辑活动拖动时取消，否则可退出 |
| Ctrl+Z / Ctrl+Y / Ctrl+S | 编辑态Undo / Redo / 保存；文本/UI捕获时不抢快捷键 |

如果键盘焦点在工具控件，点击画面空白处再操作。玩家初始在场地南侧，朝前走到橙色按钮附近按E，门升起后穿过门洞进入绿色目标区。侧面设有墙滑区、有限坡和四级台阶；NPC使用平面A*巡逻/感知/跟随/搜索/返回，路径可在右侧开启叠加显示。

<a id="design-protection"></a>
## 编辑和数据保护

左侧树选择对象，可创建Cube/Group/Light、修改局部位置/旋转/缩放、材质和已声明参数、重挂接及删除。连续拖动合并为一条Undo，Escape恢复原值。删除组覆盖整棵子树；若外部按钮仍引用门，先解除引用才能删除。玩家/NPC为根节点、单位缩放；有子对象的父节点用正统一缩放，角色/移动门下只允许纯显示后代。

文本草稿在换选中对象、折叠或隐藏字段前提交，Escape取消文本草稿。非法拖动帧被拒绝后仍保留原事务，接着拖动仍一条Undo；Escape恢复整段起点。启动没有有效磁盘存档时显示Unsaved；Reset seed与实际存档不同也显示Unsaved，只有Save更新磁盘基准。

模型Base color是导入材质的实例tint，最终有效反射色限定[0,1]，灯光HDR颜色另行校验；模型M/R保留导入值并在面板禁用。模型贴图覆盖需要网格有UV0。模型/贴图改变先准备资源，坏路径、坏内容或超出支持合同被拒绝，原设计与历史保持。按钮Target只提供Door，`<none>`明确解除控制，不自动控制第一扇门。

Save design保存设计配置，不保存运行位置、动画游标或机关进度。默认路径为 `%LOCALAPPDATA%/G104Engine/TrainingGroundV1/Scenes/training-ground.json`，实际路径显示在右侧；重新构建输出不覆盖用户设计。

Load saved/Reset seed在未保存状态下提供Save and load、Discard and load、Cancel。Reset seed恢复随应用的初始设计，只有再次Save才写入用户保存文件。设计JSON或引用的资源损坏时，启动报告问题并用种子设计恢复可用预览；首次覆盖保存前把被拒绝原件复制为`.rejected-日期-GUID.json`，界面显示备份位置。加载/准备失败不会替换当前有效设计。

### Load saved / Reset seed的含义与手动验收

这两个操作用于编辑设计，先Stop回到EDIT；Play期间它们禁用。若只是重新玩一轮，用Stop→Play即可。

| 操作 | 加载/保存的内容 |
| --- | --- |
| Save design | 把当前设计写到用户保存文件，覆盖上一次保存；不记录游玩位置或机关进度 |
| Load saved | 重新读取你最后一次Save design的设计 |
| Reset seed | 读取随程序附带的初始训练场设计；本身不覆盖用户保存文件 |

有未保存改动时，两个加载操作都会询问：`Cancel`取消加载并保留当前编辑；`Discard and load`放弃未保存改动后加载所选目标；`Save and load`先保存当前编辑，再加载所选目标。因此对Load saved选择Save and load，会重新载入刚保存的新版本；对Reset seed选择该项，会先保护当前编辑到存档，再显示初始场景。

以下用临时Cube区分保存版和未保存版。Save会更新用户文件，先确认当前需要保留的编辑已包含其中；若还需保留之前磁盘上的另一版本，先复制右侧Save路径显示的文件。成功Load/Reset会清空当前Undo/Redo历史，测试后通过Load saved恢复保存设计。

1. Stop后创建临时Cube，名称改为`Test_A`并按Enter提交，点击Save design；预期显示Saved。
2. 将它改名`Test_B`并提交，暂不保存；预期显示Unsaved。
3. 点击Load saved→Cancel；预期仍有Test_B，仍为Unsaved。
4. 再点Load saved→Discard and load；预期恢复Test_A，显示Saved。
5. 改名`Test_C`并提交、不保存，点击Reset seed→Cancel；预期仍有Test_C。再点Reset seed→Discard and load；预期显示初始训练场，临时Cube消失，因与保存版Test_A不同而显示Unsaved。此时不要点Save design。
6. 点击Load saved→Discard and load；预期Test_A恢复，显示Saved，证明Reset没有覆盖用户存档。
7. 补测Save and load：把Test_A改为Test_D并提交、不保存，点Reset seed→Save and load；预期显示初始训练场。不要保存初始场景，再点Load saved→Discard and load；预期恢复Test_D，证明重置前的编辑已保存。
8. 测试结束，在恢复的自定义设计中删除临时Cube并Save，确认其余原有对象和参数保留。测试结果由用户反馈后再记为通过。

Forward/Deferred在编辑态选择，下一次Play生效；共用场景、材质、骨骼与光源。右侧View可观察Base color、Normals、Roughness、Depth、Shadow，另外有选中骨架/路径叠加、动画状态与事件、声音测试、FPS/帧耗时。Loop test是低音测试信号，用于循环/停止观察，不是成品背景音乐。


## 按任务选择验证

先选最相关入口，不能把本表当成每次必须整轮执行。CPU/原生/GL/音频输出与手感/学习掌握各自形成证据。

| 目的 | 参数 | 预期执行与边界 |
| --- | --- | --- |
| 核对托管入口/架构/三OpenTK程序集 | --smoke | 打印Mode/Framework/OS/Process arch及三个程序集版本，Smoke result: PASS/exit0；没有独立断言，不初始化GLFW/GL，不验证全部九包 |
| Core/导航/物理/玩法/动画/场景/WAV行为 | --verify | 各模块PASS及Verification complete；含原生Jolt，WAV只解析，不创建GL/音频device |
| 已有七个具名导航/玩法专项 | --review-baseline | 每项PASS/FAIL汇总，失败exit1；完整verify也覆盖相应行为 |
| 编辑草稿/事务/Saved基准 | --verify-ui | 真实cimgui独立上下文，11项；不经过生产OpenTK输入，不创建GL |
| 生产窗口鼠标/文字/焦点等输入 | --verify-ui-input | 隐藏Windows/GL窗口，Win32→GLFW/OpenTK→生产ImGui适配和共享清焦；13项，不读取保存设计或移动桌面鼠标，不直接完成完整Play准备 |
| BRDF/HDR/透明/UV/共享mesh | --verify-render | 隐藏GL4.3，10项独立公式/实际GPU读回，验证自己准备及清理的资源；不等于所有材质/视角正确 |
| 场景转向/接触回归 | --verify-contacts | 38种变换/真实种子路线；部分检查创建Jolt，全部汇总后失败exit1 |
| 球区/Ramp生产隐藏窗口九路 | --exercise-contacts | 默认800帧，显式frames不得少于800；真正Stop/Play/运动/动画/绘制，六球纯显示无Collider，Ramp有Box |
| 后端播放与清理 | --verify-audio | 真正device/context/buffer及2D/3D source、loop暂停恢复停止清理；正常打印PASS OpenAL actual...，不替代左右/远近听辨 |
| 集成绘制/PlayStop/编辑保存/生命周期 | --exercise --frames 240 | 隐藏GL与脚本输入，检查完整链及GL错误；可选capture-root产生隔离截图，不替代真实键鼠/效果观察 |

### 可复制命令与输出位置

命令工作目录均是上面的g104engine。Debug已有依赖下的显式构建为：

```powershell
dotnet build g104engine.slnx --no-restore --disable-build-servers -m:1 -c Debug -p:Platform=x64
```

这条命令禁止还原，不等于重新验证锁定还原；缺失缓存会失败。需要Release时把-c Debug改为-c Release，并使用对应Release输出；不只改运行参数。

```powershell
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --smoke
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify --user-data-root .cache/execution/manual-verify
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --review-baseline --user-data-root .cache/execution/manual-baseline
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-ui --user-data-root .cache/execution/manual-ui
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-ui-input --user-data-root .cache/execution/manual-ui-input
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-render --user-data-root .cache/execution/manual-render
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-contacts --user-data-root .cache/execution/manual-contacts
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --exercise-contacts --frames 800 --user-data-root .cache/execution/manual-contact-window
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --verify-audio --user-data-root .cache/execution/manual-audio
dotnet samples/G104.Sandbox/bin/x64/Debug/net10.0/G104.Sandbox.dll --exercise --frames 240 --user-data-root .cache/execution/manual-graphics --capture-root .cache/execution/manual-captures
```

每次一个模式，不把不同验证参数堆成一个调用；Program按分支选择，并不会自动把它们全部运行。user-data-root相对当前目录解析为工作区.cache下绝对路径；验证文件在该根或随机子目录（verify使用Checks），不要指向真实用户Scenes。exercise强制显式隔离根，其他命令也按本页传入它。应用资源默认来自相应输出目录assets。

结果首先打印到终端；参数不会自动保存统一.log。需要保留证据时，另给本次操作命名并将终端输出存工作区.cache，记录命令/配置/日期/退出码，不覆盖旧批次日志。bin/obj、隔离数据和截图是本机生成物，不随Git同步。exercise-contacts不生成旧240帧截图序列；capture-root参数存在不代表已经产出图片，核对实际文件。

### 失败处理与结果解释

- 非零退出码或FAIL：保留首个异常及完整输出、命令、配置、SDK/DLL路径、隔离根和复现步骤；Program顶层异常输出到stderr并exit1。单纯出现PASS字符串不足以抵消其他失败。
- 构建找不到SDK/包：核对global.json与实际SDK/还原产物，返回VS处理；不删锁文件、改目标框架或省略no-restore掩盖问题。
- 原生DLL/GL初始化失败：区分x64部署、加载、当前GL4.3 Core和驱动能力，保留版本/vendor/renderer；不静默降级或自动升级驱动。OpenAL不可用的状态不记为播放通过。
- File.Replace等被受限环境拒绝：保留权限/路径证据，用获授权的可写验证环境核对；不取消正确原子保存语义，也不改用真实用户目录绕过。
- 隐藏窗口成功只覆盖该输入/相机/帧数。CPU/GPU公式、GL无错误、人工画面/听感、学习解释、性能与跨设备不能相互代替；FPS/双管线图像接近不证明性能优劣。

## 定向人工操作与学习

1. Debug/x64移动/跑跳/墙滑/坡台，用E开门进入目标，观察相机及NPC巡逻→跟随→失视搜索→返回。
2. Stop后选Deferred再Play，对比同设计，观察阴影/天空/PBR球/纹理及调试视图。
3. 编辑Cube/Group、Undo/Redo、保world重挂接；Play再Stop确认未保存设计仍在。
4. Save后关闭/重启，看实际用户保存路径及恢复；Load/Reset用上面临时Cube步骤区分Cancel、Discard、Save and load。
5. 听2D/3D/脚步/跳跃/机关及Loop停止；观察缩放/最小化恢复/失焦/关闭。按操作—实际—预期反馈，只针对问题复试，不把尚未覆盖的组合先记为通过。

门洞有人拒绝关闭；进入绿色中心后Runtime/Render debug显示Training complete及Goal: True。E的后续反馈可能替换提示文字，Goal锁存至Stop；Stop→Play开启新一轮。六PBR球无Collider，走过显示位置符合设计；Ramp有真实旋转Box碰撞。

动画定义在 [character-animation.json](../../assets/config/character-animation.json)，修改源码目录配置后重建/重启生效，不具备运行热重载。Clip采样、跨Clip混合与局部Pose显示插值分别实现；身体与骨架共用alpha，下落为Fall，实际接地才Land。完整W/Space断点与自测在 [learning-map.md](../learning-map.md#first-input-lesson)，这是可选下一任务，不是运行前提。

<details>
<summary>可选：2026-10-04首轮人工反馈与已修专项的来源</summary>

## 建议的第一轮用户验收

**本轮结论：通过（初步、非穷尽）。** 2026-10-04用户明确表示当时指南第1–5项（操控玩法、渲染、设计编辑、保存恢复、声音/窗口）初步验收没问题，可暂记为通过，未穷尽所有分支，后续发现Bug再反馈。不把穷尽分支作为本轮通过的前置条件，也不推定跨设备/所有极端组合或源码学习已完成；本页上方的现行操作说明保留为学习与问题复试依据，不要求重复整轮验收。

用户反馈的球/Ramp附近SceneValidationException已专项修复，转身、球区移动、上下坡也已有单独人工确认。六个PBR球是纯材质展示、没有Collider，经过它们是当前设计；Ramp有真实Box碰撞，见 [v1-contact-exit-fix-2026-10-03.md](../reviews/v1-contact-exit-fix-2026-10-03.md)。

**UI鼠标修复已人工通过：** 2026-10-04用户明确反馈“验证鼠标点击事件已修复”。[v1-ui-mouse-fix-2026-10-04.md](../reviews/v1-ui-mouse-fix-2026-10-04.md)保留误清控件激活状态/短点击丢失的修复及双配置回归证据；随后整体第1–5项也获初步验收通过。

**基础操控与玩法主线操作参考：** 左Shift跑、Space跳、贴墙斜走与四级台阶；右键环绕/滚轮调距，观察镜头遮挡及动作衔接。靠近橙色按钮按E，穿过打开的门进入绿色目标区中心，查看右上角`Runtime / Render debug`面板：`NPC: ... | Path: ...`下一行应显示`Training complete! Stop and Play to repeat.`，上方状态变为`Door: open | Goal: True`。提示是面板普通文字，后续按E交互可能替换该文字；当前Play内`Goal: True`仍保留完成状态，Stop后该运行状态不显示，下次Play重新开始。若仍为`Goal: False`，检查门已打开且角色走入绿色目标中心。开启路径叠加观察NPC的Patrol；从正面无遮挡处靠近触发Follow，再跑远或躲到实体墙后持续保持失视，观察Search→Return→Patrol；再次被看见会恢复Follow，NPC感知与开门/目标无触发依赖。后续出现问题按“操作—实际现象—预期”反馈；本页上方的“定向人工操作与学习”清单作后续学习与定向复试参考，不代表其整理后的全部细项已逐项人工验证，首轮通过后无需重新整轮执行。


</details>

可选证据：[v1-progress.md](../execution/v1-progress.md)、[v1-independent-review-2026-10-03.md](../reviews/v1-independent-review-2026-10-03.md)、[v1-ui-mouse-fix-2026-10-04.md](../reviews/v1-ui-mouse-fix-2026-10-04.md)、[v1-contact-exit-fix-2026-10-03.md](../reviews/v1-contact-exit-fix-2026-10-03.md)。2026-10-03 GPU10项及接触38项/800帧、2026-10-04 UI13项/旧UI11项/verify/240帧属于不同批次，旧PASS不能代替改动后的验回；本轮文档改造未重跑。

素材许可/SHA与离线工具合同见 [dependencies.md](dependencies.md#assets-and-conversion) 和 [asset-manifest.json](../../assets/licenses/asset-manifest.json)。历史蓝色探针仅用于基础环境，不应覆盖现有Program；其教学原文可选查 [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#foundation-20261002)。
