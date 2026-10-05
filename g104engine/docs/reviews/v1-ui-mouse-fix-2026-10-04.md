# V1 UI鼠标点击修复与回归

<a id="historical-evidence"></a>

日期：2026-10-04。状态：本机修复、Debug/Release回归及Astra Ultra有限复核完成，用户已明确确认鼠标点击修复，随后确认运行指南第1–5项V1首轮人工验收通过（初步、非穷尽）。该源码/验证及验收批已由用户提交推送为38cb85f，当前进入学习/调试；学习掌握尚未完成。

修复准备基线为4be648e，当时保留此前六份未提交验收/交接文档。实际保存结果与本轮只读同步核对见 [git-submission-history.md](git-submission-history.md#ui-acceptance)；本页随后的一致性补记属于新的本地文档审计批次。以下修前失败、自动验回及限制保持原证据，不把后来的人工反馈写成修前或自动测试当时已通过。

## 用户现象与根因

用户反馈所有按钮、下拉框和勾选框鼠标无响应，而键盘F5可以Play；进一步确认悬停Play会变亮，按住左键约1秒再松开仍不生效。用户关闭窗口后开始构建验证。此前转身、球区移动、上下坡已人工通过，结论保留。

主因在TrainingWindow的场景点击清焦逻辑：控件按下后拥有ActiveId，`IsWindowHovered(AnyWindow)`在未允许ActiveItem阻挡时返回false；程序误判为点击界面外，执行`SetWindowFocus(null)`，清除了刚激活的控件。随后松开无法完成按钮/勾选交互。这也解释悬停正常、长按仍无效，不能归因为声音按钮的播放实现。

另一个独立缺陷是ImGuiController仅轮询每帧最终MouseState；同次UI采样前已经按下又松开的短点击被吞掉。旧11项UI验证直接给ImGui注入事件，既未经过窗口输入后端，也未执行TrainingWindow的错误清焦逻辑，因此漏检。

固定来源：[ImGui 1.91.6的IsWindowHovered/FocusWindow/EndFrame](https://github.com/ocornut/imgui/blob/v1.91.6/imgui.cpp)、[同版GLFW输入后端](https://github.com/ocornut/imgui/blob/v1.91.6/backends/imgui_impl_glfw.cpp)、[OpenTK 4.9.4窗口回调](https://github.com/opentk/opentk/blob/4.9.4/src/OpenTK.Windowing.Desktop/NativeWindow.cs)、[更新/渲染顺序](https://github.com/opentk/opentk/blob/4.9.4/src/OpenTK.Windowing.Desktop/GameWindow.cs)。

## 实际修改

- [TrainingWindow](../../samples/G104.Sandbox/TrainingWindow.cs)：共享`ReleaseUiFocusForSceneClick`依据`WantCaptureMouse`判断点击归属，保留控件激活、拖动及弹窗；界面外点击仍释放键盘焦点。
- [ImGuiController](../../src/G104.Engine/Tools/ImGuiController.cs)：构造绑定窗口，按事件顺序接收鼠标移动、按钮、滚轮、焦点和文字；不再轮询覆盖按钮边沿或重复滚轮。回调选择所属ImGui context，Dispose成对退订。客户区坐标保持，未添加DPI缩放补丁。
- OpenTK先派发FocusedChanged后更新IsFocused，处理器用事件的焦点值；失焦先置无效鼠标位置、再释放按钮，避免同批恢复时完成旧点击。
- [窗口输入验证](../../samples/G104.Sandbox/Tools/UiInputVerificationWindow.cs)及Program/LaunchOptions新增`--verify-ui-input`；未改包、锁文件、SDK、项目配置、物理、渲染或用户设计。

分工：Sol Ultra生产修复，root新增验证与唯一串行构建运行，Astra Ultra只读核对根因、生产差异及有效Debug日志，未自行运行。有限复核未发现阻塞P1/P2；Release由root实测。

## 失败到通过的证据

日志均在`g104engine/.cache/execution/`，只在本机缓存，不能称为远程备份。

| 检查 | 实测结果 | 日志 |
| --- | --- | --- |
| 修前共享生产清焦策略 | 悬停PASS；长按按钮、短点击、长按勾选共3FAIL。按下帧activeBefore=True、windowHovered=False、capture=True、activeAfter=False | `ui-mouse-input-baseline.log`，构建`ui-mouse-build-baseline.log` |
| 最终Debug/Release x64构建 | 各0警告0错误，现有依赖、`--no-restore --disable-build-servers -m:1` | `ui-mouse-build-debug.log`、`ui-mouse-build-release.log` |
| 新窗口输入验证 | 各13PASS，长按激活保持至松开；覆盖按钮/勾选/Combo、连续短点击及位置顺序、拖动、文字、空白/弹窗焦点、F5可用、失焦恢复、滚轮一次性 | `ui-mouse-ui-input-debug.log`、`ui-mouse-ui-input-release.log` |
| 旧面板UI验证 | 各11PASS，含草稿、Undo及真实保存基准 | `ui-mouse-ui-debug.log`、`ui-mouse-ui-release.log` |
| 完整行为自检 | 两配置全部PASS | `ui-mouse-verify-debug.log`、`ui-mouse-verify-release.log` |
| 训练场隐藏窗口集成 | 两配置各240帧；Forward→Deferred、2次跳跃/110移动步、PlayStop/保存/失败保护、无GL错误 | `ui-mouse-window-debug.log`、`ui-mouse-window-release.log` |

旧UI保存检查首次被沙箱拒绝File.Replace（`ui-mouse-ui-debug-sandbox-denied.log`），在获准正常权限下用相同隔离数据根重跑通过，未修改正确保存语义。滚轮探针最初在Render后读值，而ImGui EndFrame会清零相对量；已改在BeginFrame后消费阶段采样，生产代码未因探针错误改动。

新增验证使用隐藏640×480窗口和定向同步Win32消息，经GLFW/OpenTK、真实生产ImGuiController及共享清焦策略驱动原生控件；不移动用户桌面鼠标。所称“同批”是两次UI采样之间的同步消息，不是宣称测试执行了真实PollEvents批次。F5检查确认键事件未被UI捕获；完整Play准备由240帧集成另覆盖。这些证据不等于真实桌面鼠标点击整个Play流程、DPI、多设备或最终试听已人工通过。

## 用户反馈与当前接续

用户于2026-10-04明确反馈：“验证鼠标点击事件已修复。”本问题记录为用户复试通过；该单项确认本身不扩展为全部控件、声音听感、DPI/失焦组合或整个V1已验收。

随后用户明确确认 [v1-run-and-review.md](../guides/v1-run-and-review.md)“建议的第一轮用户验收”第1–5项初步体验无问题，允许暂记通过、后续Bug继续反馈。因此当前记录为V1首轮人工验收通过（初步、非穷尽），涵盖操控/玩法、渲染、编辑/保存、声音及窗口操作；不再把整轮体验列为尚待确认。专项/极端组合、全部设备/DPI及源码学习掌握未由此证明。本轮接续补记没有新增代码修改或运行验证，最新学习入口与后续反馈边界见 [status.md](../status.md)。

修改前快照：`.cache/execution/snapshots/20261004-154758-ui-mouse-before`，119文件逐SHA验证，含当时原未提交文档。修后快照见 [v1-progress.md](../execution/v1-progress.md)。快照与日志仅在本机，不随Git提交上传；源码、验证及验收文档已随38cb85f保存同步，后续审计文档仍是另批本地修改。Git操作仍由用户按既有流程完成；D5及后移专题保持暂缓。
