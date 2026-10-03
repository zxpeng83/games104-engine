using System.Runtime.InteropServices;
using G104.Engine.Editor;
using G104.Engine.Scene;
using ImGuiNET;
using NVector2 = System.Numerics.Vector2;

namespace G104.Sandbox.Tools;

// 原生ImGui输入/控件/队列检查，无窗口、GL或用户存档访问。
public static class EditorUiVerification
{
    public static IReadOnlyList<string> Run(string verificationRoot)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(verificationRoot);
        var passed = new List<string>();
        var failures = new List<string>();
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

    private static void CheckMaterialCollapse()
    {
        using var ui = new UiSession();
        ui.ReplaceText("Color texture", "textures/collapsed.png");
        ui.Click("Material header");
        Require(ui.Editor.Graph.Object(ui.First).Material.BaseColorTexture == "textures/collapsed.png" && ui.Editor.History.UndoCount == 1,
            "Texture draft was lost when its header hid the field.");
    }

    private static void CheckWindowCollapse()
    {
        using var ui = new UiSession();
        ui.ReplaceText("Name", "Collapsed A");
        ui.Mouse(new NVector2(22, 155), true); ui.Mouse(new NVector2(22, 155), false); ui.Frame();
        Require(ui.Editor.Graph.Object(ui.First).Name == "Collapsed A" && ui.Editor.History.UndoCount == 1,
            "Name draft was lost when its scene window collapsed.");
    }

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

    private static string Read(SceneObjectData item, string label) => label switch
    {
        "Name" => item.Name,
        "Model asset" => item.ModelPath ?? "",
        "Color texture" => item.Material.BaseColorTexture ?? "",
        "Normal texture" => item.Material.NormalTexture ?? "",
        _ => throw new ArgumentOutOfRangeException(nameof(label))
    };

    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private sealed class UiSession : IDisposable
    {
        private readonly nint _previous = ImGui.GetCurrentContext();
        private readonly nint _context;
        private readonly SceneEditorPanel _panel = new();
        private readonly Dictionary<string, (NVector2 Min, NVector2 Max)> _items = [];
        private readonly Queue<Action> _commands = [];
        public SceneEditor Editor { get; }
        public Guid First { get; }
        public Guid Second { get; }
        public Guid? Selected { get; private set; }
        public int Rejected { get; private set; }
        public Action? BeforePanel { get; set; }

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

        public NVector2 Center(string label)
        {
            Require(_items.ContainsKey(label), "Control not drawn: " + label);
            var rect = _items[label];
            // 排除右侧标签；树点击在箭头右侧，DragFloat点击数值框。
            return new NVector2(rect.Min.X + Math.Min(70, (rect.Max.X - rect.Min.X) * .35f), (rect.Min.Y + rect.Max.Y) * .5f);
        }

        public void Click(string label) { var point = Center(label); Mouse(point, true); Mouse(point, false); Frame(); }
        public void Mouse(NVector2 point, bool down)
        {
            var io = ImGui.GetIO(); io.AddMousePosEvent(point.X, point.Y); io.AddMouseButtonEvent(0, down); Frame();
        }
        public void Key(ImGuiKey key)
        {
            ImGui.GetIO().AddKeyEvent(key, true); Frame(); ImGui.GetIO().AddKeyEvent(key, false); Frame();
        }
        public void ReplaceText(string label, string value)
        {
            Click(label);
            var io = ImGui.GetIO();
            io.AddKeyEvent(ImGuiKey.ModCtrl, true); io.AddKeyEvent(ImGuiKey.A, true); Frame();
            io.AddKeyEvent(ImGuiKey.A, false); io.AddKeyEvent(ImGuiKey.ModCtrl, false); Frame();
            io.AddInputCharactersUTF8(value); Frame();
        }
        public void Enqueue(Action command) => _commands.Enqueue(command);
        public void Observe(string label) => _items[label] = (ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
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
        public void Dispose() { ImGui.DestroyContext(_context); ImGui.SetCurrentContext(_previous); }
    }

    [DllImport("cimgui", CallingConvention = CallingConvention.Cdecl, EntryPoint = "igGetIO")]
    private static extern nint GetNativeIo();
}
