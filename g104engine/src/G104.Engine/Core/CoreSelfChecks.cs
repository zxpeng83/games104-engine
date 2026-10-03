using G104.Engine.Editor;
using G104.Engine.Scene;
using OpenTK.Mathematics;

namespace G104.Engine.Core;

public static class CoreSelfChecks
{
    public static IReadOnlyList<string> Run(string? verificationRoot = null)
    {
        var passed = new List<string>();
        CheckClock(); passed.Add("Fixed step: 30/60/144 Hz, backlog drop and reset");
        CheckInput(); passed.Add("Input: zero-step edges, single consumption and capture clearing");
        CheckTransforms(); passed.Add("Hierarchy: row-vector TRS, interpolation, keep-world reparent and invalid transforms");
        CheckHistory(); passed.Add("Editor: grouped undo, stable identity, subtree references, redo branch and dirty state");
        CheckTemplates(); passed.Add("Templates: independent one-level instances and explicit override whitelist");
        CheckSerialization(verificationRoot); passed.Add("Scene JSON: atomic replacement, corrupt-file preservation, schema and asset boundary rejection");
        return passed;
    }

    private static void CheckClock()
    {
        foreach (var rate in new[] { 30, 60, 144 })
        {
            var clock = new FixedStepClock();
            int ticks = 0;
            for (int frame = 0; frame < rate * 2; frame++) clock.Advance(1.0 / rate, _ => ticks++);
            Require(ticks == 120 && clock.Alpha < 0.0001f, $"Fixed step differs at {rate} Hz: {ticks}.");
        }
        var slow = new FixedStepClock();
        var result = slow.Advance(0.25, _ => { });
        Require(result.Steps == 5 && result.DroppedSteps == 10 && result.Alpha < 0.0001f, "Backlog was not discarded after five steps.");
        result = slow.Advance(0.5, _ => { });
        Require(result.Steps == 5 && result.DroppedSeconds > 0.4, "Frame cap did not report discarded time.");
        slow.Advance(0.005, _ => { });
        slow.Reset();
        Require(slow.Alpha == 0 && slow.Advance(0, _ => throw new Exception("Unexpected tick.")).Steps == 0, "Clock reset retained time.");
    }

    private static void CheckInput()
    {
        var input = new InputBuffer();
        input.Push(new InputState(Vector2.UnitX, new Vector2(2, 3), JumpPressed: true, InteractPressed: true, Sprint: true));
        input.Push(new InputState(Vector2.UnitX, new Vector2(4, 5), Sprint: true));
        var first = input.Consume();
        var second = input.Consume();
        Require(first.JumpPressed && first.InteractPressed && first.Sprint && !second.JumpPressed && !second.InteractPressed && second.Move == Vector2.UnitX, "Input edges did not survive zero steps or repeated during catch-up.");
        Require(input.ConsumeLook() == new Vector2(6, 8) && input.ConsumeLook() == Vector2.Zero, "Mouse look was repeated.");
        input.Push(new InputState(Vector2.One, Vector2.One, JumpPressed: true));
        input.Push(new InputState(Vector2.Zero, Vector2.Zero, CaptureKeyboard: true, CaptureMouse: true));
        Require(input.Consume().Move == Vector2.Zero && !input.Consume().JumpPressed && input.ConsumeLook() == Vector2.Zero, "UI capture leaked old inputs.");
        input.Push(new InputState(Vector2.One, Vector2.One, JumpPressed: true, Focused: false));
        Require(input.Consume().Move == Vector2.Zero && input.ConsumeLook() == Vector2.Zero, "Focus loss retained inputs.");
    }

    private static void CheckTransforms()
    {
        var root = Item("Root", ObjectKind.Group, new Vector3(3, 2, 1));
        root.Transform.Scale = new Float3(2, 2, 2);
        root.Transform.Rotation = RotationData.From(Quaternion.FromAxisAngle(Vector3.UnitY, MathF.PI / 2));
        var child = Item("Child", ObjectKind.StaticMesh, Vector3.UnitX);
        child.ParentId = root.Id;
        var other = Item("Other", ObjectKind.Group, new Vector3(-4, 1, 2));
        other.Transform.Rotation = RotationData.From(Quaternion.FromAxisAngle(Vector3.UnitX, 0.4f));
        var graph = new SceneGraph(new SceneDocument { Objects = [child, root, other] });
        Near(graph.WorldPosition(child.Id), new Vector3(3, 2, -1), "Parent row-vector order is incorrect.");
        var editor = new SceneEditor(graph);
        var before = graph.WorldMatrix(child.Id);
        editor.Reparent(child.Id, other.Id);
        Require(TransformMath.NearlyEqual(before, graph.WorldMatrix(child.Id)), "Reparent changed world pose.");
        editor.History.Undo();
        Require(child.ParentId == root.Id && TransformMath.NearlyEqual(before, graph.InterpolatedWorldMatrix(child.Id, 0)), "Undo retained interpolation from another parent.");
        Reject(() => editor.Reparent(root.Id, child.Id), "Parent cycle accepted.");
        graph.CapturePrevious();
        graph.SetWorldPosition(root.Id, new Vector3(5, 2, 1));
        Near(graph.InterpolatedWorldMatrix(child.Id, 0.5f).ExtractTranslation(), new Vector3(4, 2, -1), "Hierarchy did not interpolate local poses with one alpha.");
        graph.ResetInterpolation();
        Near(graph.InterpolatedWorldMatrix(child.Id, 0).ExtractTranslation(), graph.WorldPosition(child.Id), "Teleport history was not reset.");
        var shear = Matrix4.Identity; shear.M12 = 0.5f;
        Reject(() => TransformMath.Decompose(shear), "Shear decomposition was accepted.");
        Reject(() => TransformMath.Decompose(Matrix4.CreateScale(-1, 1, 1)), "Reflection decomposition was accepted.");
        var nonuniform = TransformMath.Clone(root.Transform); nonuniform.Scale = new Float3(1, 2, 1);
        Reject(() => editor.SetTransform(root.Id, nonuniform), "Nonuniform parent was accepted.");
        var player = Item("Player", ObjectKind.Player, Vector3.Zero);
        player.ParentId = root.Id;
        Reject(() => new SceneGraph(new SceneDocument { Objects = [root, player] }), "Character was accepted under a scene parent.");
        var invalid = Item("Bad rotation", ObjectKind.StaticMesh, Vector3.Zero);
        invalid.Transform.Rotation = new RotationData(0, 0, 0, 0);
        Reject(() => new SceneGraph(new SceneDocument { Objects = [invalid] }), "Zero quaternion accepted.");
        var invalidNpc = Item("Invalid NPC", ObjectKind.Npc, Vector3.Zero);
        invalidNpc.Parameters["navCellSize"] = 0;
        Reject(() => new SceneGraph(new SceneDocument { Objects = [invalidNpc] }), "Invalid navigation parameter accepted.");
        player.ParentId = null;
        player.Parameters["radius"] = 2;
        Reject(() => new SceneGraph(new SceneDocument { Objects = [player] }), "Impossible capsule dimensions accepted.");
        var door = Item("Moving door", ObjectKind.Door, Vector3.Zero);
        var attached = Item("Attached collision", ObjectKind.StaticMesh, Vector3.Zero);
        attached.ParentId = door.Id; attached.Collider = new ColliderData();
        Reject(() => new SceneGraph(new SceneDocument { Objects = [door, attached] }), "Static collider under moving door was accepted.");
        attached.Collider = null;
        _ = new SceneGraph(new SceneDocument { Objects = [door, attached] });
    }

    private static void CheckHistory()
    {
        var group = Item("Group", ObjectKind.Group, Vector3.Zero);
        var child = Item("Child", ObjectKind.StaticMesh, Vector3.One); child.ParentId = group.Id;
        var button = Item("Button", ObjectKind.Button, Vector3.Zero); button.TargetId = child.Id;
        var graph = new SceneGraph(new SceneDocument { Objects = [group, child, button] });
        var editor = new SceneEditor(graph);
        Reject(() => editor.Delete(group.Id), "External subtree reference was silently deleted.");
        editor.SetTarget(button.Id, null);
        editor.History.MarkSaved();
        editor.Delete(group.Id);
        Require(editor.History.IsDirty && graph.Document.Objects.Count == 1, "Delete did not become one dirty transaction.");
        editor.History.Undo();
        Require(!editor.History.IsDirty && ReferenceEquals(graph.Object(child.Id), child) && child.ParentId == group.Id, "Undo did not restore the same child identity and saved state.");
        editor.History.BeginTransaction("Drag");
        editor.SetTransform(group.Id, new TransformData { Position = new Float3(1, 0, 0) });
        editor.SetTransform(group.Id, new TransformData { Position = new Float3(2, 0, 0) });
        editor.History.CommitTransaction();
        Require(editor.History.RedoCount == 0 && editor.History.UndoCount == 2, "Drag was not grouped or redo was not cleared.");
        editor.History.Undo();
        Near(graph.WorldPosition(group.Id), Vector3.Zero, "Grouped drag undo failed.");
        Require(!editor.History.IsDirty, "Undo to saved state stayed dirty.");
        editor.History.BeginTransaction("Cancelled drag");
        editor.SetTransform(group.Id, new TransformData { Position = new Float3(9, 0, 0) });
        editor.History.CancelTransaction();
        Require(editor.History.RedoCount == 1 && !editor.History.IsDirty, "Cancelled drag erased redo or stayed dirty.");
        editor.History.Redo();
        editor.History.MarkSaved();
        editor.History.Undo();
        Require(editor.History.IsDirty, "Undo after save should become dirty.");
        editor.SetName(group.Id, "New branch");
        Require(!editor.History.CanRedo, "New edit did not clear redo.");
        for (int index = 0; index < 105; index++) editor.SetName(group.Id, $"Group {index}");
        Require(editor.History.UndoCount == 100, "Undo history exceeded its capacity.");
        var existing = graph.Object(group.Id);
        while (editor.History.Undo()) { }
        Require(ReferenceEquals(existing, graph.Object(group.Id)), "Repeated history lost a surviving DTO identity.");
        string previousName = existing.Name;
        int undoBefore = editor.History.UndoCount;
        editor.History.PrepareChange = _ => throw new InvalidDataException("Simulated GPU/asset preparation failure.");
        Reject(() => editor.SetName(group.Id, "Unprepared resource edit"), "Unprepared edit committed.");
        Require(existing.Name == previousName && editor.History.UndoCount == undoBefore, "Failed preparation changed design/history.");
        editor.History.PrepareChange = null;
        editor.SetName(group.Id, "Valid prepared edit");
        editor.History.PrepareChange = _ => throw new InvalidDataException("Unavailable resource while undoing.");
        Reject(() => editor.History.Undo(), "Failed undo preparation committed.");
        Require(existing.Name == "Valid prepared edit" && editor.History.UndoCount == undoBefore + 1, "Failed prepared undo lost current design/cursor.");
    }

    private static void CheckTemplates()
    {
        var template = Item("Cube", ObjectKind.StaticMesh, Vector3.Zero);
        template.Parameters["speed"] = 1;
        var graph = new SceneGraph(new SceneDocument { Templates = [new ObjectTemplateData { Id = "cube", Defaults = template }] });
        var editor = new SceneEditor(graph);
        var first = editor.Create("cube");
        var second = editor.Create("cube");
        editor.SetParameter(first, "speed", 4);
        Require(first != second && graph.Object(second).Parameters["speed"] == 1 && template.Parameters["speed"] == 1 && graph.Object(first).TemplateOverrides.SequenceEqual(["parameters"]), "Template defaults or instance independence failed.");
        Reject(() => editor.SetParameter(first, "runtimeHandle", 1), "Unknown parameter was edited.");
        editor.History.Undo();
        Require(graph.Object(first).Parameters["speed"] == 1 && graph.Object(first).TemplateOverrides.Count == 0, "Undo did not restore explicit template overrides.");
        graph.Object(first).TemplateOverrides.Add("id");
        Reject(graph.Rebuild, "Forbidden template override accepted.");
    }

    private static void CheckSerialization(string? verificationRoot)
    {
        string root = Path.GetFullPath(verificationRoot ?? Path.GetTempPath());
        var directory = Path.Combine(root, $"G104-CoreVerify-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directory);
        var path = Path.Combine(directory, "scene.json");
        try
        {
            var group = Item("Root", ObjectKind.Group, new Vector3(1, 2, 3));
            var child = Item("Child", ObjectKind.StaticMesh, Vector3.One); child.ParentId = group.Id;
            var document = new SceneDocument { Objects = [group, child] };
            SceneSerializer.Save(document, path);
            var loaded = SceneSerializer.Load(path, directory);
            Require(loaded.Objects[1].Id == child.Id && loaded.Objects[1].ParentId == group.Id, "Scene hierarchy identity did not round-trip.");
            document.Name = "Saved again";
            SceneSerializer.Save(document, path);
            Require(SceneSerializer.Load(path, directory).Name == "Saved again", "Atomic replacement did not save the new scene.");
            document.SchemaVersion = 99;
            var previousBytes = File.ReadAllBytes(path);
            Reject(() => SceneSerializer.Save(document, path), "Unknown schema saved.");
            Require(previousBytes.SequenceEqual(File.ReadAllBytes(path)), "Failed save damaged existing file.");
            document.SchemaVersion = SceneDocument.CurrentVersion;
            child.Material.BaseColorTexture = "missing.png";
            Reject(() => SceneSerializer.Save(document, path, directory), "Missing asset was saved through the validated overload.");
            Require(previousBytes.SequenceEqual(File.ReadAllBytes(path)), "Missing-asset save damaged existing file.");
            child.Material.BaseColorTexture = null;
            File.WriteAllText(path, "{ corrupt-json");
            var corrupt = File.ReadAllBytes(path);
            Reject(() => SceneSerializer.Load(path, directory), "Corrupt scene loaded.");
            Require(corrupt.SequenceEqual(File.ReadAllBytes(path)), "Corrupt load changed user design.");
            File.WriteAllText(path, "{\"schemaVersion\":1,\"schemaVersion\":1,\"objects\":[]}");
            Reject(() => SceneSerializer.Load(path, directory), "Duplicate JSON field loaded.");
            File.WriteAllText(path, "{\"schemaVersion\":1,\"objects\":[],\"unknownField\":1}");
            Reject(() => SceneSerializer.Load(path, directory), "Unknown JSON field loaded.");
            File.WriteAllText(path, "{\"schemaVersion\":1,\"objects\":[{\"name\":\"Missing stable identity\"}]}");
            Reject(() => SceneSerializer.Load(path, directory), "Missing object id was silently regenerated.");
            Require(AssetPath.Normalize("models\\character.glb") == "models/character.glb", "Asset normalization failed.");
            foreach (var unsafePath in new[] { "../secret", "/absolute", "C:\\private", "models/../../escape", "model//mesh", "models/mesh.glb:stream" })
                Reject(() => AssetPath.Resolve(directory, unsafePath), $"Unsafe asset path accepted: {unsafePath}.");
            Require(!Directory.EnumerateFiles(directory, "*.tmp").Any(), "Scene save left temporary files.");
        }
        finally
        {
            // 只删除本检查明确创建的随机目录；不操作真实用户Scenes。
            var target = Path.GetFullPath(directory);
            var parent = root.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
            if (!target.StartsWith(parent, StringComparison.OrdinalIgnoreCase) || !Path.GetFileName(target).StartsWith("G104-CoreVerify-", StringComparison.Ordinal))
                throw new InvalidOperationException("Verification cleanup target is outside its temporary root.");
            Directory.Delete(target, recursive: true);
        }
    }

    private static SceneObjectData Item(string name, ObjectKind kind, Vector3 position) => new()
    {
        Name = name, Kind = kind, Transform = new TransformData { Position = Float3.From(position) }
    };

    private static void Near(Vector3 actual, Vector3 expected, string message) => Require((actual - expected).Length < 0.0005f, $"{message} Expected {expected}, got {actual}.");
    private static void Require(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }

    private static void Reject(Action operation, string message)
    {
        try { operation(); }
        catch (Exception error) when (error is SceneValidationException or System.Text.Json.JsonException or ArgumentException or InvalidDataException) { return; }
        throw new InvalidOperationException(message);
    }
}
