using System.Runtime.InteropServices;
using G104.Engine.Tools;
using ImGuiNET;
using OpenTK.Mathematics;
using OpenTK.Windowing.Common;
using OpenTK.Windowing.Desktop;
using OpenTK.Windowing.GraphicsLibraryFramework;
using NVector2 = System.Numerics.Vector2;

namespace G104.Sandbox.Tools;

// 隐藏窗口定向发送Win32消息，经GLFW/OpenTK和生产ImGui后端；不移动桌面鼠标。
/// <summary>用隐藏窗口定向Win32消息覆盖GLFW/OpenTK到生产ImGui后端的真实输入链路。</summary>
internal sealed class UiInputVerificationWindow : GameWindow
{
    /// <summary>本窗口创建的生产UI后端，在GL上下文仍有效时释放。</summary>
    private ImGuiController? _ui;
    /// <summary>核实属于本进程的Win32窗口句柄，仅向该窗口发送测试消息。</summary>
    private nint _handle;
    /// <summary>_buttons统计按钮激活；_downEvents/_upEvents统计OpenTK收到的原生按下/松开事件。</summary>
    private int _buttons, _downEvents, _upEvents;
    /// <summary>_checked为复选值；_leftPressed为本帧原生左键按下；_textActive为文本激活状态。</summary>
    /// <remarks>_requestModal/_closeModal为模态开关请求；_modalOpen为实际模态开启结果。</remarks>
    private bool _checked, _leftPressed, _textActive, _requestModal, _closeModal, _modalOpen;
    /// <summary>真实下拉框选择，0为Forward、1为Deferred。</summary>
    private int _selection;
    /// <summary>_drag为真实拖动控件值；_wheelInFrame保存ImGui清帧前消费到的滚轮相对量。</summary>
    private float _drag, _wheelInFrame;
    /// <summary>真实文本控件草稿，用于检查WM_CHAR是否经过OpenTK到达ImGui。</summary>
    private string _text = "Original";
    /// <summary>_buttonPoint/_checkPoint/_comboPoint分别为按钮、复选框、下拉框的逻辑像素点击点。</summary>
    /// <remarks>_optionPoint为选项；_textPoint为输入框；_dragPoint为拖动数值框，均从实际控件区域采集。</remarks>
    private NVector2 _buttonPoint, _checkPoint, _comboPoint, _optionPoint, _textPoint, _dragPoint;
    /// <summary>任一输入检查失败后置真，供外层启动路径决定退出结果。</summary>
    public bool Failed { get; private set; }

    /// <summary>创建唯一标题的隐藏OpenGL 4.3核心窗口，以便定位本进程的测试目标。</summary>
    public UiInputVerificationWindow() : base(GameWindowSettings.Default, new NativeWindowSettings
    {
        ClientSize = new Vector2i(640, 480), API = ContextAPI.OpenGL, APIVersion = new Version(4, 3),
        Profile = ContextProfile.Core, Flags = ContextFlags.ForwardCompatible, StartVisible = false,
        Title = "G104 UI input verification " + Guid.NewGuid().ToString("N")
    }) { }

    /// <summary>建立生产UI后端并执行原生点击、拖动、文本、模态、快捷键、焦点和滚轮定向检查。</summary>
    protected override void OnLoad()
    {
        base.OnLoad();
        if (!OperatingSystem.IsWindows()) throw new PlatformNotSupportedException("Window mouse verification requires Windows.");
        _ui = new ImGuiController(this);
        _handle = FindWindowW(null, Title);
        GetWindowThreadProcessId(_handle, out uint process);
        if (_handle == 0 || process != Environment.ProcessId) throw new InvalidOperationException("Cannot identify own verification window.");
        MouseDown += e => { _downEvents++; _leftPressed |= e.Button == MouseButton.Left; };
        MouseUp += _ => _upEvents++;
        SendMessageW(_handle, 0x0007, 0, 0); // WM_SETFOCUS，不激活用户窗口。
        Console.WriteLine($"Window mouse verification: focus={IsFocused}; client={ClientSize}; framebuffer={FramebufferSize}");
        Frame(); Frame();
        Check("native focus and client-coordinate hover", () =>
        {
            MoveMouse(_buttonPoint); Frame(); Frame();
            Require(IsFocused && NVector2.Distance(ImGui.GetIO().MousePos, _buttonPoint) < 1.5f, "Focus/client position did not reach ImGui.");
        });
        Check("held left click reaches production button", () =>
        {
            int before = _buttons;
            MoveMouse(_buttonPoint); Frame(); Down();
            for (int i = 0; i < 60; i++) Frame(i is 0 or 1 or 59);
            Up(); Frame(true); Frame(true);
            Require(_buttons == before + 1, $"Expected one button press, got {_buttons - before}; native down/up={_downEvents}/{_upEvents}.");
        });
        Check("down/up before one UI frame reaches production button", () =>
        {
            int before = _buttons;
            MoveMouse(_buttonPoint); Frame(); Down(); Up();
            for (int i = 0; i < 5; i++) Frame(true);
            Require(_buttons == before + 1, $"Expected one button press, got {_buttons - before}.");
        });
        Check("held left click toggles checkbox", () =>
        {
            bool before = _checked;
            MoveMouse(_checkPoint); Frame(); Frame(); Down(); Frame(true); Frame(); Up(); Frame(true); Frame();
            Require(_checked != before, "Checkbox did not change.");
        });
        Check("native combo opens and selects an option", () =>
        {
            Click(_comboPoint);
            Require(_optionPoint != NVector2.Zero, "Combo did not open.");
            Click(_optionPoint);
            Require(_selection == 1, "Combo selection did not change.");
        });
        Check("two complete clicks before one UI frame remain two clicks", () =>
        {
            int before = _buttons;
            MoveMouse(_buttonPoint); Frame(); Down(); Up(); Down(); Up(); Frames(10);
            Require(_buttons == before + 2, $"Expected two clicks, got {_buttons - before}.");
        });
        Check("click retains its position before later mouse movement", () =>
        {
            int before = _buttons;
            MoveMouse(_buttonPoint); Down(); Up(); MoveMouse(new NVector2(610, 450)); Frames(8);
            Require(_buttons == before + 1, "Trailing movement relocated or lost the click.");
        });
        Check("native drag retains activation until release", () =>
        {
            float before = _drag;
            MoveMouse(_dragPoint); Frames(2); Down(); Frames(2);
            MoveMouse(_dragPoint + new NVector2(25, 0)); Frames(2);
            MoveMouse(_dragPoint + new NVector2(45, 0)); Frames(2); Up(); Frames(2);
            Require(_drag > before && !ImGui.IsAnyItemActive(), "Drag value/activation is incorrect.");
        });
        Check("text focus survives UI click and leaves on scene click", () =>
        {
            Click(_textPoint);
            Require(_textActive && ImGui.GetIO().WantCaptureKeyboard, "Text did not acquire keyboard focus.");
            string before = _text;
            SendMessageW(_handle, 0x0102, 'x', 1); Frames(3); // WM_CHAR经OpenTK TextInput。
            Require(_text != before && _text.Contains('x'), "Native text input did not reach the active field.");
            Click(new NVector2(610, 450)); Frames(2);
            Require(!_textActive && !ImGui.GetIO().WantCaptureKeyboard, "Scene click retained text/keyboard focus.");
        });
        Check("outside click preserves modal capture", () =>
        {
            _requestModal = true; Frames(3);
            Click(new NVector2(610, 450)); Frames(2);
            Require(_modalOpen && ImGui.GetIO().WantCaptureMouse, "Scene click dismissed or bypassed the modal.");
            _closeModal = true; Frames(3);
        });
        Check("F5 remains available after scene click", () =>
        {
            Click(new NVector2(610, 450));
            SendMessageW(_handle, 0x0100, 0x74, 0x003f0001);
            Frame();
            bool available = KeyboardState.IsKeyPressed(Keys.F5) && !_ui.WantsKeyboard;
            SendMessageW(_handle, 0x0101, 0x74, unchecked((int)0xc03f0001)); Frames(2);
            Require(available, "Native F5 did not reach the unblocked gameplay shortcut path.");
        });
        Check("focus loss and regain cancel held click", () =>
        {
            int before = _buttons;
            MoveMouse(_buttonPoint); Frames(2); Down(); Frames(2);
            SendMessageW(_handle, 0x0008, 0, 0); // WM_KILLFOCUS
            SendMessageW(_handle, 0x0007, 0, 0);
            Up(); Frames(8);
            Require(_buttons == before && !ImGui.GetIO().MouseDown[0], "Focus transition activated or stuck a held click.");
            Click(_buttonPoint);
            Require(_buttons == before + 1, "Fresh click after refocus did not recover.");
        });
        Check("native wheel delta is submitted exactly once", () =>
        {
            MoveMouse(_buttonPoint); Frames(2);
            SendMessageW(_handle, 0x020a, 120 << 16, 0);
            float total = 0;
            for (int i = 0; i < 4; i++) { Frame(); total += _wheelInFrame; }
            Require(Math.Abs(total - 1) < 1e-5f, $"One wheel notch produced {total} units.");
        });
        _ui.Dispose(); _ui = null;
        Close();
    }

    /// <summary>推进生产UI帧并绘制真实控件，调用生产场景点击清焦路径，可输出输入状态证据。</summary>
    private void Frame(bool trace = false)
    {
        _ui!.BeginFrame(this, 1f / 60);
        _wheelInFrame = ImGui.GetIO().MouseWheel; // EndFrame会清零滚轮相对量，在消费阶段留证。
        ImGui.SetNextWindowPos(new NVector2(12, 12), ImGuiCond.Always);
        ImGui.SetNextWindowSize(new NVector2(480, 350), ImGuiCond.Always);
        ImGui.Begin("Actual native input controls", ImGuiWindowFlags.NoResize | ImGuiWindowFlags.NoCollapse);
        if (ImGui.Button("Play (F5)")) _buttons++;
        _buttonPoint = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
        bool hover = ImGui.IsItemHovered(), active = ImGui.IsItemActive();
        ImGui.Checkbox("NPC path overlay", ref _checked);
        _checkPoint = ImGui.GetItemRectMin() + new NVector2(8, 8);
        _optionPoint = NVector2.Zero;
        bool combo = ImGui.BeginCombo("Pipeline", _selection == 0 ? "Forward" : "Deferred");
        _comboPoint = ImGui.GetItemRectMin() + new NVector2(45, 8);
        if (combo)
        {
            if (ImGui.Selectable("Forward", _selection == 0)) _selection = 0;
            if (ImGui.Selectable("Deferred", _selection == 1)) _selection = 1;
            _optionPoint = (ImGui.GetItemRectMin() + ImGui.GetItemRectMax()) * .5f;
            ImGui.EndCombo();
        }
        ImGui.DragFloat("Value", ref _drag, .1f);
        _dragPoint = ImGui.GetItemRectMin() + new NVector2(50, 8);
        ImGui.InputText("Name", ref _text, 64);
        _textPoint = ImGui.GetItemRectMin() + new NVector2(50, 8);
        _textActive = ImGui.IsItemActive();
        if (_requestModal) { ImGui.OpenPopup("Modal input guard"); _requestModal = false; }
        _modalOpen = ImGui.BeginPopupModal("Modal input guard", ImGuiWindowFlags.AlwaysAutoResize);
        if (_modalOpen)
        {
            ImGui.TextUnformatted("Preserve capture until explicitly dismissed.");
            if (_closeModal) { ImGui.CloseCurrentPopup(); _closeModal = false; }
            ImGui.EndPopup();
        }
        ImGui.End();
        bool activeBefore = ImGui.IsAnyItemActive(), windowHover = ImGui.IsWindowHovered(ImGuiHoveredFlags.AnyWindow);
        TrainingWindow.ReleaseUiFocusForSceneClick(_leftPressed);
        _leftPressed = false;
        if (trace) Console.WriteLine($"FOCUS activeBefore={activeBefore} windowHovered={windowHover} capture={ImGui.GetIO().WantCaptureMouse} activeAfter={ImGui.IsAnyItemActive()}");
        if (trace) Console.WriteLine($"INPUT native={MouseState.IsButtonDown(MouseButton.Left)} io={ImGui.GetIO().MouseDown[0]} clicked={ImGui.IsMouseClicked(ImGuiMouseButton.Left)} released={ImGui.IsMouseReleased(ImGuiMouseButton.Left)} hover={hover} active={active} pos={ImGui.GetIO().MousePos} buttons={_buttons}");
        _ui.Render();
    }

    /// <summary>发送窗口客户区逻辑像素WM_MOUSEMOVE，不移动桌面鼠标。</summary>
    private void MoveMouse(NVector2 p) => SendMessageW(_handle, 0x0200, 0, (nint)(((int)p.Y << 16) | (ushort)p.X));
    /// <summary>推进指定数量UI帧，让原生输入队列经过生产后端消费。</summary>
    private void Frames(int count) { for (int i = 0; i < count; i++) Frame(); }
    /// <summary>按位置移动、稳定、按下、松开和收尾的顺序模拟一次窗口点击。</summary>
    private void Click(NVector2 point) { MoveMouse(point); Frames(2); Down(); Frames(2); Up(); Frames(3); }
    /// <summary>定向发送窗口左键按下消息。</summary>
    private void Down() => SendMessageW(_handle, 0x0201, 1, 0);
    /// <summary>定向发送窗口左键松开消息。</summary>
    private void Up() => SendMessageW(_handle, 0x0202, 0, 0);
    /// <summary>将未满足的实际输入行为转为失败异常。</summary>
    private static void Require(bool condition, string error) { if (!condition) throw new InvalidOperationException(error); }
    /// <summary>捕获单项失败、设置窗口失败标记并继续收集其余检查结果。</summary>
    private void Check(string name, Action action)
    {
        try { action(); Console.WriteLine("INPUT PASS: " + name); }
        catch (Exception error) { Failed = true; Console.WriteLine("INPUT FAIL: " + name + ": " + error.Message); }
    }
    /// <summary>在窗口上下文仍有效时释放残留UI资源，覆盖加载检查异常路径。</summary>
    protected override void OnUnload() { _ui?.Dispose(); _ui = null; base.OnUnload(); }
    /// <summary>按唯一窗口标题查找测试句柄；使用前还需核对进程身份。</summary>
    [DllImport("user32.dll", CharSet = CharSet.Unicode)] private static extern nint FindWindowW(string? className, string name);
    /// <summary>读取窗口所属线程和进程，用于拒绝把消息发到其他进程。</summary>
    [DllImport("user32.dll")] private static extern uint GetWindowThreadProcessId(nint window, out uint process);
    /// <summary>同步向已核实的测试窗口发送Win32消息，让GLFW/OpenTK处理输入。</summary>
    [DllImport("user32.dll")] private static extern nint SendMessageW(nint window, uint message, nint wParam, nint lParam);
}
