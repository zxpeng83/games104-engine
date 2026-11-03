using System.Runtime.InteropServices;
using G104.Engine.Editor;
using G104.Engine.Scene;
using ImGuiNET;
using NVector2 = System.Numerics.Vector2;

namespace G104.Sandbox.Tools;

// 原生ImGui输入/控件/队列检查，无窗口、GL或用户存档访问。
/// <summary>在真实原生ImGui控件和命令队列上验证编辑事务边界，不创建窗口或GL资源。</summary>
public static class EditorUiVerification
{
    /// <summary>逐项运行编辑UI定向检查并汇总失败；仅存档检查访问显式隔离根。</summary>
    public static IReadOnlyList<string> Run(string verificationRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationRoot);
        var passed = new List<string>();
        var failures = new List<string>();
        // 每项单独收集通过或失败，避免首个异常遮蔽其余交互边界。
        void Check(string name, Action test)
        {
            try { test(); passed.Add(name); Console.WriteLine("UI PASS: " + name); }
            catch (Exception error) { failures.Add(name + ": " + error.Message); Console.WriteLine("UI FAIL: " + name + ": " + error.Message); }
        }
        foreach (string label in new[] { "Name", "Model asset", "Color texture", "Normal texture" })
            Check(label + " draft commits when tree selection changes", () => CheckTextSelection(label));
        Check("Rejected drag frame keeps one undo transaction", () => CheckRejectedDrag(cancel: false));
        Check("Escape after rejected drag restores drag start", () => CheckRejectedDrag(cancel: true));
        Check("Enter commits text once and Escape cancels its draft", CheckTextKeys);
        Check("Collapsing material flushes its hidden texture draft", CheckMaterialCollapse);
        Check("Collapsing scene window flushes its hidden name draft", CheckWindowCollapse);
        Check("Toolbar mouse release observes the preceding text commit", CheckToolbarOrder);
        Check("Reset seed retains the actual saved-file baseline", () => CheckSavedBaseline(verificationRoot));
        if (failures.Count != 0) throw new InvalidOperationException(string.Join(Environment.NewLine, failures));
        return passed;
    }

    /// <summary>验证文本未提前提交，换树选择时提交一次，并可用一次Undo恢复旧对象。</summary>
    private static void CheckTextSelection(string label)
    {
        using var ui = new UiSession();
        var a = ui.Editor.Graph.Object(ui.First);
        string value = label == "Name" ? "Edited A" : label == "Model asset" ? "models/edited.glb" : "textures/edited.png";
        ui.ReplaceText(label, value);
        Require(Read(a, label) != value, "Text committed before Enter or leaving field.");
        ui.Click("Tree:" + ui.Second);
        Require(ui.Selected == ui.Second, "Mouse click did not select the second tree object.");
        Require(Read(a, label) == value, "Draft was lost when the old field stopped drawing.");
        Require(ui.Editor.History.UndoCount == 1, "Text leave should create exactly one history entry.");
        ui.Editor.History.Undo();
        Require(Read(a, label) != value, "Undo did not restore the original text.");
    }

    /// <summary>验证非法拖动帧只回滚该帧，整段手势仍合并为一次Undo或被Escape完整取消。</summary>
    private static void CheckRejectedDrag(bool cancel)
    {
        using var ui = new UiSession(character: true);
        var player = ui.Editor.Graph.Object(ui.First);
        float start = player.Parameters["radius"];
        var point = ui.Center("Parameter radius");
        ui.Mouse(point, true);
        ui.Mouse(point + new NVector2(10, 0), true);
        Require(player.Parameters["radius"] > start && ui.Editor.History.InTransaction, "Legal drag did not start a transaction.");
        float legal = player.Parameters["radius"];
        ui.Mouse(point + new NVector2(50, 0), true);
        Require(ui.Rejected != 0 && player.Parameters["radius"] == legal && ui.Editor.History.InTransaction,
            "Impossible capsule dimensions did not reject only the failed frame and retain the gesture.");
        ui.Mouse(point + new NVector2(45, 0), true);
        ui.Mouse(point + new NVector2(43, 0), true);
        if (cancel)
        {
            ui.Key(ImGuiKey.Escape);
            ui.Mouse(point + new NVector2(49, 0), true);
            ui.Mouse(point + new NVector2(49, 0), false);
            Require(player.Parameters["radius"] == start && !ui.Editor.History.InTransaction && ui.Editor.History.UndoCount == 0,
                "Escape did not restore the start after a rejected frame.");
        }
        else
        {
            ui.Mouse(point + new NVector2(43, 0), false);
            Require(!ui.Editor.History.InTransaction && ui.Editor.History.UndoCount == 1, "One drag produced multiple history entries or left the transaction open.");
            ui.Editor.History.Undo();
            Require(player.Parameters["radius"] == start, "Single Undo did not restore the whole drag.");
        }
    }

    /// <summary>验证Enter只提交一次，Escape取消的草稿不会在后续选择变化时复活。</summary>
    private static void CheckTextKeys()
    {
        using var ui = new UiSession();
        var a = ui.Editor.Graph.Object(ui.First);
        ui.ReplaceText("Name", "Committed"); ui.Key(ImGuiKey.Enter); ui.Frame();
        Require(a.Name == "Committed" && ui.Editor.History.UndoCount == 1, "Enter did not commit exactly one edit.");
        ui.ReplaceText("Name", "Cancelled"); ui.Key(ImGuiKey.Escape);
        ui.Click("Tree:" + ui.Second);
        Require(a.Name == "Committed" && ui.Editor.History.UndoCount == 1, "Escape left a draft that committed at a later boundary.");
    }

    /// <summary>验证收起材质分组时仍提交被隐藏的贴图草稿。</summary>
    private static void CheckMaterialCollapse()
    {
        using var ui = new UiSession();
        ui.ReplaceText("Color texture", "textures/collapsed.png");
        ui.Click("Material header");
        Require(ui.Editor.Graph.Object(ui.First).Material.BaseColorTexture == "textures/collapsed.png" && ui.Editor.History.UndoCount == 1,
            "Texture draft was lost when its header hid the field.");
    }

    /// <summary>验证场景面板窗口收起后仍提交被隐藏的名称草稿。</summary>
    private static void CheckWindowCollapse()
    {
        using var ui = new UiSession();
        ui.ReplaceText("Name", "Collapsed A");
        ui.Mouse(new NVector2(22, 155), true); ui.Mouse(new NVector2(22, 155), false); ui.Frame();
        Require(ui.Editor.Graph.Object(ui.First).Name == "Collapsed A" && ui.Editor.History.UndoCount == 1,
            "Name draft was lost when its scene window collapsed.");
    }

    /// <summary>验证工具栏松开触发的命令能看到此前鼠标按下造成的文本提交。</summary>
    private static void CheckToolbarOrder()
    {
        using var ui = new UiSession();
        string? observed = null;
        ui.BeforePanel = () =>
        {
            ImGui.SetNextWindowPos(new NVector2(12, 12), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new NVector2(520, 90), ImGuiCond.Always);
            ImGui.Begin("Verification toolbar", ImGuiWindowFlags.NoCollapse | ImGuiWindowFlags.NoResize);
            if (ImGui.Button("Save design")) ui.Enqueue(() => observed = ui.Editor.Graph.Object(ui.First).Name);
            ui.Observe("Toolbar save");
            ImGui.End();
        };
        ui.Frame(); ui.ReplaceText("Name", "Toolbar A"); ui.Click("Toolbar save");
        Require(observed == "Toolbar A" && ui.Editor.History.UndoCount == 1, "Toolbar command missed the text committed on mouse down.");
    }

    /// <summary>在隔离目录验证加载/重置/保存的dirty基线，既有自定义存档不被Reset改写。</summary>
    private static void CheckSavedBaseline(string verificationRoot)
    {
        string root = Path.GetFullPath(verificationRoot);
        string directory = Path.Combine(root, "G104-UiVerify-" + Guid.NewGuid().ToString("N"));
        // 所有文件只在调用者指定的隔离根下；清理前再次核对具体目录边界。
        Require(directory.StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
            "Verification directory escapes its explicit root.");
        Directory.CreateDirectory(directory);
        try
        {
            string path = Path.Combine(directory, "saved.json");
            var seed = new SceneDocument { Name = "Seed", Objects = [new SceneObjectData { Name = "Seed group", Kind = ObjectKind.Group }] };
            var custom = SceneSerializer.Clone(seed); custom.Objects[0].Name = "Custom group";
            SceneSerializer.Save(custom, path);
            var actualSaved = SceneSerializer.Load(path, directory);
            var loaded = TrainingWindow.CreateDesignEditor(new SceneGraph(SceneSerializer.Clone(actualSaved)), actualSaved);
            Require(!loaded.History.IsDirty, "Load saved should establish that file as the clean baseline.");
            var reset = TrainingWindow.CreateDesignEditor(new SceneGraph(SceneSerializer.Clone(seed)), actualSaved);
            Require(reset.History.IsDirty && reset.History.UndoCount == 0, "Fresh reset history incorrectly marked the seed Saved against a custom file.");
            Require(SceneSerializer.Load(path, directory).Objects[0].Name == "Custom group", "Reset changed the existing saved file.");
            SceneSerializer.Save(reset.Graph.Document, path); reset.History.MarkSaved();
            Require(!reset.History.IsDirty, "Saving the seed did not establish its new baseline.");
            var savedSeed = SceneSerializer.Load(path, directory);
            var reloaded = TrainingWindow.CreateDesignEditor(new SceneGraph(savedSeed), savedSeed);
            Require(!reloaded.History.IsDirty && reloaded.Graph.Document.Objects[0].Name == "Seed group", "Load saved did not recover the newly saved seed.");
            var noSave = TrainingWindow.CreateDesignEditor(new SceneGraph(SceneSerializer.Clone(seed)), null);
            Require(noSave.History.IsDirty, "Missing/rejected save should not label a seed as saved to the user's path.");
        }
        finally
        {
            Require(Path.GetFullPath(directory).StartsWith(root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase),
                "Cleanup target escapes its explicit verification root.");
            Directory.Delete(directory, recursive: true);
        }
    }

    /// <summary>按测试字段标签读取设计值，以同一断言覆盖名称和资产路径。</summary>
    private static string Read(SceneObjectData item, string label) => label switch
    {
        "Name" => item.Name,
        "Model asset" => item.ModelPath ?? "",
        "Color texture" => item.Material.BaseColorTexture ?? "",
        "Normal texture" => item.Material.NormalTexture ?? "",
        _ => throw new ArgumentOutOfRangeException(nameof(label))
    };

    /// <summary>将未满足的交互或持久化条件转为可汇总失败。</summary>
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    /// <summary>独占临时ImGui上下文和两对象编辑场景，驱动真实面板/命令路径并采集控件区域。</summary>
    private sealed class UiSession : IDisposable
    {
        /// <summary>创建验证会话前的ImGui上下文，释放时恢复。</summary>
        private readonly nint _previous = ImGui.GetCurrentContext();
        /// <summary>本会话创建并销毁的隔离ImGui上下文。</summary>
        private readonly nint _context;
        /// <summary>被测生产面板实例，跨帧保留真实草稿和事务状态。</summary>
        private readonly SceneEditorPanel _panel = new();
        /// <summary>从真实绘制采集的标签到逻辑像素矩形映射。</summary>
        private readonly Dictionary<string, (NVector2 Min, NVector2 Max)> _items = [];
        /// <summary>按生产安全点顺序执行的编辑命令队列。</summary>
        private readonly Queue<Action> _commands = [];
        /// <summary>本会话的设计/历史所有者，用于核对实际提交结果。</summary>
        public SceneEditor Editor { get; }
        /// <summary>第一个被编辑对象的GUID，可配置为角色或模型。</summary>
        public Guid First { get; }
        /// <summary>第二个树节点GUID，用于触发选择离开边界。</summary>
        public Guid Second { get; }
        /// <summary>面板返回的当前对象选择。</summary>
        public Guid? Selected { get; private set; }
        /// <summary>实际命令执行中被场景验证拒绝的帧数。</summary>
        public int Rejected { get; private set; }
        /// <summary>可选帧前绘制入口，用于验证工具栏与面板命令先后。</summary>
        public Action? BeforePanel { get; set; }

        /// <summary>创建无窗口验证上下文、字体及测试设计；关闭ini持久化并预绘制以获得控件位置。</summary>
        public UiSession(bool character = false)
        {
            _context = ImGui.CreateContext();
            ImGui.SetCurrentContext(_context);
            var io = ImGui.GetIO();
            // ImGuiIOPtr没有可写IniFilename属性；按已加载绑定的实际布局清空原生指针。
            Marshal.WriteIntPtr(GetNativeIo(), Marshal.OffsetOf<ImGuiIO>(nameof(ImGuiIO.IniFilename)).ToInt32(), nint.Zero);
            io.DisplaySize = new NVector2(1440, 1400);
            io.DisplayFramebufferScale = NVector2.One;
            io.DeltaTime = 1f / 60;
            io.Fonts.AddFontDefault();
            io.Fonts.GetTexDataAsRGBA32(out nint _, out int _, out int _, out int _);
            io.Fonts.SetTexID((nint)1);
            var a = new SceneObjectData { Name = "A", Kind = character ? ObjectKind.Player : ObjectKind.StaticMesh,
                Primitive = character ? PrimitiveKind.Cube : PrimitiveKind.Model, ModelPath = character ? null : "models/original.glb" };
            if (character) { a.Parameters["radius"] = .35f; a.Parameters["height"] = 1.9f; }
            var b = new SceneObjectData { Name = "B", Kind = ObjectKind.Group };
            First = a.Id; Second = b.Id; Selected = First;
            Editor = new SceneEditor(new SceneGraph(new SceneDocument { Objects = [a, b] }));
            _panel.ObserveItem = (label, min, max) => _items[label] = (min, max);
            Frame(); Frame();
        }

        /// <summary>取得实际数值框或树文本内的点击点，避开标签和展开箭头。</summary>
        public NVector2 Center(string label)
        {
            Require(_items.ContainsKey(label), "Control not drawn: " + label);
            var rect = _items[label];
            // 排除右侧标签；树点击在箭头右侧，DragFloat点击数值框。
            return new NVector2(rect.Min.X + Math.Min(70, (rect.Max.X - rect.Min.X) * .35f), (rect.Min.Y + rect.Max.Y) * .5f);
        }

        /// <summary>向真实ImGui IO发送按下/松开并额外推进一帧完成点击。</summary>
        public void Click(string label) { var point = Center(label); Mouse(point, true); Mouse(point, false); Frame(); }
        /// <summary>提交逻辑像素位置及左键状态后推进面板一帧。</summary>
        public void Mouse(NVector2 point, bool down)
        {
            var io = ImGui.GetIO(); io.AddMousePosEvent(point.X, point.Y); io.AddMouseButtonEvent(0, down); Frame();
        }
        /// <summary>以按下帧和松开帧提交一个ImGui键。</summary>
        public void Key(ImGuiKey key)
        {
            ImGui.GetIO().AddKeyEvent(key, true); Frame(); ImGui.GetIO().AddKeyEvent(key, false); Frame();
        }
        /// <summary>点击真实输入框，通过Ctrl+A和UTF-8输入替换其草稿。</summary>
        public void ReplaceText(string label, string value)
        {
            Click(label);
            var io = ImGui.GetIO();
            io.AddKeyEvent(ImGuiKey.ModCtrl, true); io.AddKeyEvent(ImGuiKey.A, true); Frame();
            io.AddKeyEvent(ImGuiKey.A, false); io.AddKeyEvent(ImGuiKey.ModCtrl, false); Frame();
            io.AddInputCharactersUTF8(value); Frame();
        }
        /// <summary>把工具栏验证操作排到面板共用命令队列尾部。</summary>
        public void Enqueue(Action command) => _commands.Enqueue(command);
        /// <summary>记录最近绘制控件的实际逻辑像素区域。</summary>
        public void Observe(string label) => _items[label] = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
        /// <summary>依次绘制工具栏/面板、通过生产命令执行器消费队列，再结束ImGui帧。</summary>
        public void Frame()
        {
            ImGui.NewFrame();
            BeforePanel?.Invoke();
            ImGui.SetNextWindowPos(new NVector2(12, 145), ImGuiCond.Always);
            ImGui.SetNextWindowSize(new NVector2(520, 1150), ImGuiCond.Always);
            Selected = _panel.Draw(Editor, false, Selected, _commands.Enqueue);
            while (_commands.TryDequeue(out var action)) TrainingWindow.ExecuteEditorCommand(Editor, action, error =>
            {
                if (error is not SceneValidationException) throw new InvalidOperationException("Unexpected UI command failure.", error);
                Rejected++;
            });
            ImGui.Render();
        }
        /// <summary>销毁临时上下文并恢复进入验证前的上下文，不拥有外部GL或窗口资源。</summary>
        public void Dispose() { ImGui.DestroyContext(_context); ImGui.SetCurrentContext(_previous); }
    }

    /// <summary>调用已加载cimgui取得原生IO地址，仅供关闭测试ini写入。</summary>
    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igGetIO")]
    private static extern nint GetNativeIo();
}
