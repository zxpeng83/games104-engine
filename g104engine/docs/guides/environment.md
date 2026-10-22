# 环境准备、x64与故障定位

新电脑从空白文件夹开始：先安装下面的环境，再按本页创建方案和两个C#项目，配置包，并备齐第三方素材及项目自有Shader/JSON/测试资产，最后配置构建复制；全部非C#配套准备好以后由你手写或手动复制C#引擎代码。完整顺序从 [reproduction.md](reproduction.md#new-machine-setup) 进入。已经有完整工程时，才使用本页“打开现有工程”。精确SDK、包和素材在 [dependencies.md](dependencies.md#dependency-locks)，初始准备检查在 [v1-run-and-review.md](v1-run-and-review.md#blank-project-checks)，协作职责见 [agent-workflow.md](../agent-workflow.md)。

<a id="new-machine-environment"></a>
## 新电脑：准备系统、工具和驱动

这条路线面向当前V1的Windows x64开发环境。先在Windows“设置 → 系统 → 系统信息”确认64位操作系统和基于x64的处理器；新机采用仍在微软支持期内的Windows 11 x64。下面使用的VS2026支持Windows 11，其他系统是否符合要求查 [VS2026系统要求](https://learn.microsoft.com/en-us/visualstudio/releases/2026/vs-system-requirements)。本项目尚未完成第二设备验证，ARM64、Linux、macOS不在当前复现路线内。

### 1. 确认显卡、声音和下载条件

| 要准备什么 | 去哪里、怎样确认 |
| --- | --- |
| OpenGL 4.3 Core显卡与驱动 | 在“设备管理器 → 显示适配器”确认显卡型号。缺少合适驱动时，按型号从电脑厂商支持页，或 [NVIDIA驱动](https://www.nvidia.com/en-us/drivers/)、[AMD驱动](https://www.amd.com/en/support/download/drivers.html)、[Intel驱动](https://www.intel.com/content/www/us/en/download-center/home.html) 获取对应Windows驱动。显卡硬件和当前驱动必须能创建OpenGL 4.3 Core上下文；首次运行还要检查控制台打印的GPU/GL版本。OpenTK包不能提供驱动，单装名为“OpenGL”的SDK也不能补足不支持的硬件。 |
| 声音输出 | 要听声音，连接扬声器或耳机，在“设置 → 系统 → 声音”选择可用输出并测试。音频设备初始化失败时普通窗口仍会继续，声音状态显示Unavailable；声音验收和`--verify-audio`需要可用设备。OpenAL实现随工程部署，获取方式见 [dependencies.md](dependencies.md#openal-deployment)。 |
| 网络与安装权限 | 浏览器须能访问下面的官方下载页、NuGet包源及素材作者下载页。安装VS和系统级.NET SDK需要管理员权限；安装位置可按磁盘空间选择，工程不要求放在E盘。 |

不需要为新机预先关闭SAC、Defender、SmartScreen或防火墙；发生实际拦截时按本页“SAC拦截”保留错误并诊断。

### 2. 安装Visual Studio

1. 从 [Visual Studio官方下载页](https://visualstudio.microsoft.com/downloads/) 选择 **Visual Studio 2026 Community → Free download**，使用Stable稳定通道；个人学习可用Community，其他用途按其许可选版本。目标框架使用`net10.0`，微软 [.NET SDK与VS兼容表](https://learn.microsoft.com/en-us/dotnet/core/porting/versioning-sdk-msbuild-vs) 列明.NET10/10.0.4xx的支持关系。VS2022的旧安装选择仅保留为历史，不作为这条新机路线。
2. 运行Visual Studio Installer，在“工作负载”勾选 **“.NET桌面开发”**。保留它的必需组件；在右侧安装详细信息中确认 **“.NET SDK”**，并保留 **“NuGet包管理器”**。已有VS2026时，在Installer对应实例选择“修改”进入同一界面。图形化步骤见 [VS官方安装](https://learn.microsoft.com/en-us/visualstudio/install/install-visual-studio?view=visualstudio)，组件对应见 [组件清单](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-community?view=visualstudio)。
3. 完成安装后启动VS，在“帮助 → 关于Microsoft Visual Studio”确认版本，随后按本页“从空白文件夹创建方案和项目”操作。当前依赖采用预编译原生库，普通C#开发不需要额外勾选“使用C++的桌面开发”、Unity、CMake或游戏引擎工作负载。

Git是可选的源码版本管理工具，不是创建或运行这套C#工程的前提。需要时从 [Git for Windows官方下载页](https://git-scm.com/install/windows) 选择 **Git for Windows/x64 Setup**；下载页版本会更新，Git版本不由引擎锁文件固定。选择直接使用完整仓库时，另走 [reproduction.md](reproduction.md#existing-repository-setup) 的可选路线。

2026-10-22核查时官网Stable为VS2026 18.11.1；这是官方下载入口当日提供的版本，不是项目新增的实测结果。Community当前只支持Stable最新版本，见 [VS发布历史](https://learn.microsoft.com/en-us/visualstudio/releases/2026/release-history)；不要求找到历史18.10.3安装包。VS随带的SDK可能已经更新，下一步仍须单独核对本项目的精确SDK。

### 3. 安装准确的.NET SDK 10.0.401 x64

1. 打开微软 [.NET10下载页](https://dotnet.microsoft.com/en-us/download/dotnet/10.0)，找到 **SDK 10.0.401 → Windows → Installers → x64**；也可直接进入 [.NET SDK10.0.401 Windows x64安装入口](https://dotnet.microsoft.com/en-us/download/dotnet/thank-you/sdk-10.0.401-windows-x64-installer)。确认页面版本和文件名为`dotnet-sdk-10.0.401-win-x64.exe`，下载并运行安装程序。
2. 选择 **SDK**，不要只下载Runtime/Desktop Runtime，不选x86、Arm64、预览版或页面最上方其他SDK。SDK已经包含运行时；准确版本由工程`global.json`指定，`rollForward=disable`不会接受只有10.0.402等其他SDK的机器。可以并存其他SDK，不需要为此卸载它们。
3. 安装后重新打开VS及终端。默认x64 SDK通常位于`C:\Program Files\dotnet\sdk\10.0.401\`；该路径用于理解和诊断，工程中没有要求手工填写这个绝对路径。安装器路线无需为项目另建环境变量或手动设置`DOTNET_ROOT`。
4. 在VS通过“视图 → 终端”打开PowerShell，先检查已安装的SDK：

```powershell
dotnet --list-sdks
```

列表须包含`10.0.401`。此时还没有新工程的`global.json`，所以尚不要求`dotnet --version`恰好选中它；下一节会创建该文件，再在工程根目录核对。若没有识别到SDK，先重开应用，按 [Windows安装与路径排查](https://learn.microsoft.com/en-us/dotnet/core/install/windows#no-net-sdk-was-found) 检查是否误用了x86的`dotnet`。

2026-10-22官网仍列出该准确版本和x64安装入口；本次只核查网页，没有下载或安装文件，也没有在新设备验证。将来若该版本入口失效，先核查官方下载记录，不用“最新SDK”替换锁定版本。

安装完成后继续下面的空白工程步骤，无需先获取仓库。上面的软件下载入口沿用2026-10-22核查日期；本次2026-10-23补充的是新建工程和资源复制方法。

<a id="blank-project"></a>
## 从空白文件夹创建方案和项目

下面只建立工程配置，不提供引擎实现。统一示例根目录为`%USERPROFILE%\source\G104Rebuild\g104engine`；`%USERPROFILE%`代表你的Windows用户目录，在VS位置框中填写展开后的实际完整路径。可以换成自己的可写目录，后面的`src`、`samples`、`assets`、`third_party`都直接放在这个根目录下。

### 1. 创建空白方案并选择准确SDK

1. 在VS选择“文件 → 新建 → 项目”，搜索 **Blank Solution / 空白解决方案**，名称填 **`g104engine`**，位置选择父目录`%USERPROFILE%\source\G104Rebuild`的实际路径。创建后用资源管理器确认方案文件直接在`%USERPROFILE%\source\G104Rebuild\g104engine`中；不要多套一层`g104engine\g104engine`。如果已经准备了名为`g104engine`的空文件夹，也以最终方案文件位置为准核对输入。官方步骤见 [创建方案与项目](https://learn.microsoft.com/en-us/visualstudio/ide/creating-solutions-and-projects?view=visualstudio)。
2. VS可能生成`g104engine.sln`，可以直接保留。已有参考工程使用`.slnx`，它是另一种正式方案格式；需要时使用VS的方案格式转换/另存功能，不能只改扩展名。新建路线的检查按项目`.csproj`执行，不要求先转成`.slnx`；格式说明见 [微软方案格式说明](https://devblogs.microsoft.com/visualstudio/new-simpler-solution-file-format/)。
3. 在VS新建文本文件，把下面完整JSON保存为根目录的 **`global.json`**，与方案文件同级，确认不是`global.json.txt`：

```json
{
  "sdk": {
    "version": "10.0.401",
    "rollForward": "disable",
    "allowPrerelease": false
  }
}
```

4. 在VS终端将当前目录切到实际的`g104engine`根目录，再执行：

```powershell
dotnet --version
dotnet --info
```

`--version`须为`10.0.401`，`--info`中核对x64与刚保存的`global.json`路径。文件只选择已安装SDK，不会安装SDK；缺少准确版本时返回上一节安装。保存后重新打开方案，让VS也读取这份选择。SDK解析从当前目录或方案/项目目录向上查找，依据见 [global.json机制](https://learn.microsoft.com/en-us/dotnet/core/tools/global-json)。

### 2. 添加Engine类库和Sandbox控制台

在磁盘的`g104engine`根目录下准备`src`和`samples`两个实际文件夹。VS中的“解决方案文件夹”只是逻辑分组，不能代替磁盘位置。

| 在同一个方案中添加 | VS模板与名称 | “位置”填写父目录 | 创建后的文件必须位于 |
| --- | --- | --- | --- |
| Engine | 右键方案 → 添加 → 新建项目；选C# **类库 / Class Library**，名称`G104.Engine`，框架 **.NET 10.0** | `%USERPROFILE%\source\G104Rebuild\g104engine\src`的实际路径 | `src/G104.Engine/G104.Engine.csproj` |
| Sandbox | 再右键同一个方案 → 添加 → 新建项目；选C# **控制台应用 / Console App**，名称`G104.Sandbox`，框架 **.NET 10.0** | `%USERPROFILE%\source\G104Rebuild\g104engine\samples`的实际路径 | `samples/G104.Sandbox/G104.Sandbox.csproj` |

两个模板都选择现代.NET版本，名称中带 **“.NET Framework”** 的模板不适用。不要在“位置”中再填一遍`G104.Engine`或`G104.Sandbox`，VS会按项目名建立这一层目录。官方示例见 [.NET类库与项目引用](https://learn.microsoft.com/en-us/dotnet/core/tutorials/create-class-library?pivots=vs) 和 [C#控制台项目](https://learn.microsoft.com/en-us/visualstudio/get-started/csharp/tutorial-console?view=visualstudio)。

此时磁盘结构应为：

```text
g104engine/
  global.json
  g104engine.sln                 # 或VS实际生成/转换的g104engine.slnx
  src/G104.Engine/G104.Engine.csproj
  samples/G104.Sandbox/G104.Sandbox.csproj
  assets/                       # 后续准备素材
  third_party/openal-soft/1.25.2/  # 后续准备OpenAL与许可
```

模板生成的`Class1.cs`和`Program.cs`可以先保留，后面由你安排引擎实现；当前还不会出现训练场窗口。已有参考工程不能照此步骤删掉重建。

### 3. 设置SDK项目属性、项目引用和x64

右键各项目 →“编辑项目文件”，两份文件的根元素应为`<Project Sdk="Microsoft.NET.Sdk">`。在各自现有的`PropertyGroup`中核对下面的同名属性；这是需要保留的配置片段，不是让你用它覆盖整个`.csproj`。后面加入的包和项目引用都要保留。

`G104.Engine.csproj`的属性：

```xml
<PropertyGroup>
  <TargetFramework>net10.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <AllowUnsafeBlocks>true</AllowUnsafeBlocks>
  <Platforms>AnyCPU;x64</Platforms>
</PropertyGroup>
<PropertyGroup Condition="'$(Platform)' == 'x64'">
  <PlatformTarget>x64</PlatformTarget>
</PropertyGroup>
```

`G104.Sandbox.csproj`的属性：

```xml
<PropertyGroup>
  <OutputType>Exe</OutputType>
  <TargetFramework>net10.0</TargetFramework>
  <ImplicitUsings>enable</ImplicitUsings>
  <Nullable>enable</Nullable>
  <Platforms>AnyCPU;x64</Platforms>
</PropertyGroup>
<PropertyGroup Condition="'$(Platform)' == 'x64'">
  <PlatformTarget>x64</PlatformTarget>
</PropertyGroup>
```

Engine的`AllowUnsafeBlocks=true`允许当前引擎使用指针等unsafe接口；默认关闭时，后续复制这些代码会产生编译错误。可在项目属性“生成”中核对“允许不安全代码”，机制见 [AllowUnsafeBlocks](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/language#allowunsafeblocks)。两份条件属性都覆盖`Debug/x64`和`Release/x64`；平台名称和实际`PlatformTarget`须同时正确，不能只看方案工具栏的x64标签。

1. 展开 **G104.Sandbox → 依赖项**，右键 →“添加项目引用”，勾选 **G104.Engine**。引用方向是Sandbox使用Engine；不要添加Engine到Sandbox的反向引用，也不通过浏览旧Engine DLL代替。Sandbox项目文件中应出现一次：

```xml
<ItemGroup>
  <ProjectReference Include="..\..\src\G104.Engine\G104.Engine.csproj" />
</ItemGroup>
```

2. 右键 **G104.Sandbox → 设为启动项目**。Engine是类库，不作为可执行启动项目。
3. 打开“生成 → 配置管理器”，按本页“x64平台设置与核对”创建/选择方案x64，确认两个项目在Debug和Release中都映射为x64，并勾选“生成”。上面条件组会将这两个x64配置的实际CPU目标固定为x64。

项目引用依据见 [项目引用管理](https://learn.microsoft.com/en-us/visualstudio/ide/managing-references-in-a-project?view=visualstudio#project-to-project-references)。SDK/框架、ImplicitUsings和Nullable是项目配置，NuGet库不会自动替你创建这两个项目或这些属性。

现在按 [dependencies.md](dependencies.md#blank-project-packages) 安装固定版本NuGet包并生成首次锁文件；锁模式的首次开启顺序由那一节统一说明。再按 [dependencies.md](dependencies.md#standalone-assets) 下载并整理第三方素材、OpenAL及许可，随后按 [dependencies.md](dependencies.md#project-authored-assets) 取得19个自有非C#运行文件及来源记录。两组文件都齐全后继续下面的复制配置，不能把Shader/JSON或循环测试音留到“只写C#”之后再补。

<a id="resource-copy"></a>
## 素材与OpenAL准备好后：配置构建复制

下面配置加入 **`samples/G104.Sandbox/G104.Sandbox.csproj`**，放在`<Project>`与`</Project>`之间。所有`..\..\`都相对于该`.csproj`所在目录，正好回到`g104engine`根；不要按方案文件或终端的当前目录解释它。

**先准备文件，再粘贴这段。** 根目录中应已有完整`assets`，包含第三方模型/七段声音及19个自有Shader、JSON和测试资产，同时保留许可/来源记录；`third_party/openal-soft/1.25.2/`应有DLL、许可和说明。`OpenAL32.dll`必须是独立素材步骤指定的x64实现。没有DLL时不要先加入指名该文件的Content项再构建，否则会因找不到待复制文件报错（例如MSB3030）；外部与自有文件分别按 [dependencies.md](dependencies.md#standalone-assets) 和 [dependencies.md](dependencies.md#project-authored-assets) 准备。

```xml
<ItemGroup>
  <Content Include="..\..\assets\**\*"
           Link="assets\%(RecursiveDir)%(Filename)%(Extension)"
           CopyToOutputDirectory="PreserveNewest" />
  <Content Include="..\..\third_party\openal-soft\1.25.2\OpenAL32.dll"
           Link="OpenAL32.dll"
           CopyToOutputDirectory="PreserveNewest" />
  <Content Include="..\..\third_party\openal-soft\1.25.2\*.txt"
           Link="licenses\openal-soft\%(Filename)%(Extension)"
           CopyToOutputDirectory="PreserveNewest" />
  <Content Include="..\..\third_party\openal-soft\1.25.2\COPYING"
           Link="licenses\openal-soft\COPYING"
           CopyToOutputDirectory="PreserveNewest" />
  <Content Include="..\..\third_party\openal-soft\1.25.2\LICENSE-pffft"
           Link="licenses\openal-soft\LICENSE-pffft"
           CopyToOutputDirectory="PreserveNewest" />
</ItemGroup>
```

这段与现有工程的复制规则等效：构建把`assets`目录保留层次复制到Sandbox输出，把OpenAL DLL放在应用输出根，把相应说明/许可放在`licenses/openal-soft`。`PreserveNewest`负责按源文件更新复制；只在VS中看见文件、只新建目录或把文件放到项目根，都不能代替此配置。第三方源码归档仍按素材页保存，不是运行时资源项。微软机制说明见 [Content及复制元数据](https://learn.microsoft.com/en-us/visualstudio/msbuild/common-msbuild-project-items?view=visualstudio#content)。

到这里按 [v1-run-and-review.md](v1-run-and-review.md#blank-project-checks) 检查工程、包、全部非C#文件和两配置的输出复制结果，再集中写入或手动复制C#代码。模板控制台通过只证明准备中的基础环节，不等于完整V1已经运行；全部文件必须另按清单核对。

<a id="existing-project"></a>
## 打开现有工程

在VS打开仓库中的`g104engine/g104engine.slnx`（当前机器示例为`E:\game_study\games104\g104engine\g104engine.slnx`），将G104.Sandbox设为启动项目，选Debug或Release/x64。新机可使用自己的目录和盘符，保持仓库相对结构。方案内已有Engine/Sandbox项目及Sandbox→Engine引用；无参数是完整训练场Edit窗口。不要重复新建解决方案、同名项目、NuGet引用，不用Hello World/旧蓝色探针覆盖现有Program。

| 项目 | 含义及核对位置 |
| --- | --- |
| SDK与目标框架 | global.json选构建SDK；两个csproj是net10.0。Runtime不等于SDK，文件本身不安装工具 |
| .slnx | 实际XML方案格式，由VS/dotnet sln支持；不只改后缀成.sln |
| 平台 | 方案x64和两个项目Debug/Release平台映射、实际PlatformTarget需同时正确 |
| 包与原生后端 | 双锁文件/还原产物与输出的x64 Jolt/cimgui/OpenAL各层分别核对 |
| 应用资产 | 模型/Shader/动画JSON/WAV及许可按Sandbox复制到输出assets；无需运行时FFmpeg/Python |
| 项目Git根 | games104而非g104engine；工程调试不自动创建、关联或发布仓库 |

<a id="222-x64-平台设置"></a>
## x64平台设置与核对

空白新工程在此创建x64平台；现有工程的映射已经建立，只在发现缺失时调整。不要按旧截图重复新建平台。

1. 点击**右上角“活动解决方案平台”**的下拉框；有 `x64` 时直接选，没有则选“<新建…>”。
2. 新平台选择 `x64`，“从此处复制设置”选择 `Any CPU`；如果显示“创建新项目平台”，勾选它，再确定。
3. 回到表格，确认 `G104.Engine` 和 `G104.Sandbox` 两行的平台均为 `x64`，“生成”保持勾选。不能只改上方方案平台、却让表内仍映射到 `Any CPU`。若某行没有 x64，在该行的平台下拉中创建/选择 x64。
4. 左上角先检查 `Debug`，再切换到 `Release`，都核对 **方案 x64 → 两个项目 x64** 的映射。
5. 开发时切回 `Debug | x64`，关闭配置管理器并保存。当前两个项目不需要勾选“部署”。

若平台名称已为x64，但项目属性页仍显示CPU目标为Any CPU，在项目“生成”相关属性页为Debug/Release x64选择x64；新工程也可核对上文的条件`PlatformTarget`配置。C#的x64目标不要求安装C++工具链；实际属性与方案映射须一致，后续检查方法见 [v1-run-and-review.md](v1-run-and-review.md#blank-project-checks)。

`Any CPU` 并不等于强制 32 位；本阶段设置 x64 是为了明确目标架构，便于后续核对原生图形依赖，不表示 x64 自动使代码更快。参考 [VS 平台设置](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-configure-projects-to-target-platforms?view=visualstudio) 与 [C# PlatformTarget / OutputType](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/output)。

Any CPU并不强制32位，x64也不使代码自动更快；该设置明确原生依赖目标。C#平台配置不要求C++工具链。区分方案标签、项目映射和MSBuild求值后的PlatformTarget；只看到一个下拉框不够，运行时ProcessArchitecture用于另行核对。

<a id="debugging"></a>
## Debug与Release调试

- 日常源码学习用Debug/x64、F5，在Program/TrainingWindow.OnUpdateFrame逐个断点观察。断点暂停会改变真实delta，恢复后的丢时/补步不能直接作为正常性能结果。
- Release/x64用Ctrl+F5普通运行，不附加调试器。Release默认优化，断点/变量/单步可能与Debug不同。
- “仅我的代码”是VS全局调试偏好；优化模块可能被视为非用户代码，出现警告不等于编译/原生加载/SAC失败。
- 需要时停止调试，在工具→选项→调试→常规核对“启用仅我的代码”；可搜索名称。为调查只在Release出现的问题可临时关闭后F5，结束恢复偏好，无需关闭优化。

2026-10-02用户反馈重新勾选仅我的代码、Debug断点通过，属于历史实际反馈，不代表本轮读过VS设置。依据：[Just My Code](https://learn.microsoft.com/en-us/visualstudio/debugger/just-my-code?view=visualstudio)、[优化与调试](https://learn.microsoft.com/en-us/visualstudio/debugger/jit-optimization-and-debugging?view=visualstudio)。

<a id="missing-environment"></a>
## 将来确有环境缺失时

新电脑的完整准备顺序从本页环境安装和空白工程步骤继续；这里用于已有环境的故障定位。环境安装和NuGet操作由用户在VS等安装界面完成；旧三OpenTK包流程不是今天完整包清单。先区分缺SDK、缺还原资产、缺x64原生DLL、错误启动项目和驱动上下文失败，再选择动作。没有问题证据时不升级驱动/工具或重新创建已有工程。

1. 在VS Installer核对“.NET桌面开发”及其必需.NET SDK组件（Microsoft.NetCore.Component.SDK）；Runtime条目、dotnet可选工作负载列表不能代替VS组件核对。本机历史dotnet工作负载列表为空，VS组件核对却已通过，不为这个提示加额外工作负载。
2. 用dotnet --list-sdks / --info查看完整版本、x64和路径；如果新安装未被当前进程识别，先重启相关应用/终端。精确版本要求按global.json，SDK已含相应Runtime，单装Runtime不够。
3. 若确需安装VS，参考 [VS官方安装](https://learn.microsoft.com/en-us/visualstudio/install/install-visual-studio?view=visualstudio) 与 [组件清单](https://learn.microsoft.com/en-us/visualstudio/install/workload-component-id-vs-community?view=visualstudio)。个人学习的Community/保留既有VS2022选择属于2026-10-02决定；未来版本/适用许可需按当时环境核对，不把旧安装号当新推荐。
4. 若准确SDK缺失，通过 [.NET10下载SDK区](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) / [Windows说明](https://learn.microsoft.com/en-us/dotnet/core/install/windows)处理。已有工程按现有csproj/双锁文件还原；空白新工程按 [dependencies.md](dependencies.md#blank-project-packages) 首次安装和生成锁文件，顺序不同。
5. 用户自己从零搭建时按本页“从空白文件夹创建方案和项目”；已有完整工程直接开方案，不删Sandbox重建。旧空模板删除A/OutputType替代B保留为历史取舍，不要求在已有业务代码上重做。

<a id="223-本机-sac-拦截的处理说明仅供了解未执行关闭"></a>
<a id="sac-diagnosis"></a>
## SAC拦截：历史取舍与诊断

2026-10-02模板Release及后来Debug曾报0x800711C7，CodeIntegrity3033/3077、3099和状态值1定位为Smart App Control；用户自行在安全中心关闭后两配置恢复运行，助手当时只读核对VerifiedAndReputablePolicyState=0。用户明确选择本机开发阶段暂保持SAC关闭，这不是引擎依赖、其他设备必做或助手可自动更改的设置。

SAC检查信任/签名，无针对本项目的单独放行开关；关闭影响整机这一层拦截。Defender、SmartScreen、防火墙是独立设置，不能从SAC值推断它们状态。既有建议保留这些保护、不添加整个工程杀毒排除；本轮没有读取或改变它们。管理设备由管理员决定。

需要诊断时保留最新CodeIntegrity事件、异常/退出码和实际状态；用户实际调整后再按获授权范围验证，不能假设关一个选项完成全部验收。重新开启/换机需按具体Windows版本和 [微软SAC FAQ](https://support.microsoft.com/en-us/windows/security/threat-malware-protection/smart-app-control-frequently-asked-questions)核对；2026-10-02页面/界面有重新开启提示，本机未实测重新开启，本轮未联网复查。

## 可选：首次搭建原文与历史结果

2026-10-02实际检出VS2026 Community18.10.3 Stable、SDK10.0.401 x64、Runtime/Desktop10.0.12；两项目/空模板重建、x64映射、三OpenTK包、SAC与调试反馈已完成。本轮未重新查询IDE/系统Runtime，也不将Hello World构建成功当V1验证。

完整可视化安装、空解决方案、Sandbox空模板A/B代码、x64、NuGet及探针原文保留在 [foundation-setup-and-probes.md](../archive/foundation-setup-and-probes.md#foundation-20261002)。历史步骤不得覆盖现有V1；同设备新聊天直接使用同工作区，跨设备D5恢复条件见status及 [reproduction.md](reproduction.md)。

来源补充：[创建/移除项目](https://learn.microsoft.com/en-us/visualstudio/ide/creating-solutions-and-projects?view=visualstudio)、[dotnet sln](https://learn.microsoft.com/en-us/dotnet/core/tools/dotnet-sln)、[平台设置](https://learn.microsoft.com/en-us/visualstudio/ide/how-to-configure-projects-to-target-platforms?view=visualstudio)、[C# PlatformTarget/OutputType](https://learn.microsoft.com/en-us/dotnet/csharp/language-reference/compiler-options/output)。这些解释对应配置机制；已有完整工程无需再新建。
