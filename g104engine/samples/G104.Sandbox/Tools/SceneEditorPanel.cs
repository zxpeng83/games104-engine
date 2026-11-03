using G104.Engine.Editor;
using G104.Engine.Scene;
using ImGuiNET;
using NVector2 = System.Numerics.Vector2;
using NVector3 = System.Numerics.Vector3;
using OQuaternion = OpenTK.Mathematics.Quaternion;

namespace G104.Sandbox.Tools;

// 面板只提交命令；设计/渲染准备由窗口安全点处理，下一Play重建物理世界。
/// <summary>把ImGui控件交互整理为设计命令、文本草稿和单次拖动事务，在窗口安全点执行。</summary>
public sealed class SceneEditorPanel
{
    /// <summary>当前拖动事务所属编辑器，切换编辑器或Play时必须收尾。</summary>
    private SceneEditor? _transactionEditor;
    /// <summary>当前拖动控件的标签，用于跨帧合并同一手势。</summary>
    private string? _transactionOwner;
    /// <summary>文本草稿对应的对象身份，切换选择前先处理旧对象草稿。</summary>
    private Guid? _draftObject;
    /// <summary>按字段标签保存尚未提交的文本。</summary>
    private readonly Dictionary<string, string> _textDrafts = [];
    /// <summary>每份草稿的提交基线，用于避免未修改字段产生历史项。</summary>
    private readonly Dictionary<string, string> _textOriginals = [];
    /// <summary>保存草稿原对象的应用回调，字段隐藏后仍能提交到正确对象。</summary>
    private readonly Dictionary<string, Action<string>> _textApplications = [];
    /// <summary>记录仍在编辑的文本字段，防止新帧用设计值覆盖草稿。</summary>
    private readonly HashSet<string> _activeText = [];
    /// <summary>本帧实际绘制的文本字段，用于帧末识别隐藏边界。</summary>
    private readonly HashSet<string> _drawnText = [];
    /// <summary>本帧实际绘制的拖动字段，隐藏的事务在帧末收尾。</summary>
    private readonly HashSet<string> _drawnEdits = [];
    /// <summary>草稿所属编辑器，设计替换时触发旧草稿处理。</summary>
    private SceneEditor? _draftEditor;
    /// <summary>Escape取消后仍保持激活的拖动控件，待松手前抑制继续应用。</summary>
    private string? _cancelledEditOwner;
    /// <summary>表示已排队命令完成后有待发布的选择变化。</summary>
    private bool _pendingSelection;
    /// <summary>下一帧发布的新对象选择，删除时为null。</summary>
    private Guid? _nextSelection;
    /// <summary>本帧是否按下Escape，统一协调文本与拖动取消。</summary>
    private bool _cancelFrame;

    // 验证时采集真实控件区域；正常窗口未订阅，不改变交互。
    /// <summary>可选验证探针，报告真实控件标签及逻辑像素矩形。</summary>
    internal Action<string, NVector2, NVector2>? ObserveItem { get; set; }

    /// <summary>绘制树和有限设计字段，按交互顺序排队命令并返回当前选择；运行态控件只读。</summary>
    public Guid? Draw(SceneEditor editor, bool playing, Guid? selected, Action<Action> enqueue)
    {
        ArgumentNullException.ThrowIfNull(editor);
        ArgumentNullException.ThrowIfNull(enqueue);
        _drawnText.Clear();
        _drawnEdits.Clear();
        _cancelFrame = ImGui.IsKeyPressed(ImGuiKey.Escape);
        if (_cancelFrame)
        {
            _cancelledEditOwner = _transactionOwner;
            FlushTextDrafts(enqueue, cancel: true);
            FinishTransaction(enqueue, cancel: true);
        }
        if (!ReferenceEquals(_draftEditor, editor) || playing)
        {
            FlushTextDrafts(enqueue, cancel: _cancelFrame);
            _draftEditor = editor;
            _draftObject = null;
        }
        if (_pendingSelection)
        {
            selected = _nextSelection;
            _pendingSelection = false;
        }
        if (selected is Guid missing && !editor.Graph.Document.Objects.Any(item => item.Id == missing)) selected = null;
        if (_transactionEditor is not null && (!ReferenceEquals(_transactionEditor, editor) || playing)) FinishTransaction(enqueue, cancel: true);

        ImGui.SetNextWindowSize(new NVector2(350, 650), ImGuiCond.FirstUseEver);
        ImGui.SetNextWindowPos(new NVector2(12, 145), ImGuiCond.FirstUseEver);
        var open = ImGui.Begin("Scene design");
        try
        {
            if (!open)
            {
                FlushTextDrafts(enqueue, cancel: _cancelFrame);
                FinishTransaction(enqueue, cancel: false);
                return selected;
            }
            ImGui.TextUnformatted(editor.Graph.Document.Name + (editor.History.IsDirty ? " *" : ""));
            if (playing) ImGui.TextDisabled("Play mode: design is read only");
            ImGui.BeginDisabled(playing);
            try
            {
                ImGui.BeginDisabled(!editor.History.CanUndo);
                if (ImGui.Button("Undo")) enqueue(() => editor.History.Undo());
                ImGui.EndDisabled();
                ImGui.SameLine();
                ImGui.BeginDisabled(!editor.History.CanRedo);
                if (ImGui.Button("Redo")) enqueue(() => editor.History.Redo());
                ImGui.EndDisabled();
                ImGui.SameLine();
                ImGui.TextDisabled($"{editor.History.UndoCount}/{editor.History.Capacity}");

                if (ImGui.Button("+ Cube")) QueueCreate(editor, ObjectKind.StaticMesh, enqueue);
                ImGui.SameLine();
                if (ImGui.Button("+ Group")) QueueCreate(editor, ObjectKind.Group, enqueue);
                ImGui.SameLine();
                if (ImGui.Button("+ Light")) QueueCreate(editor, ObjectKind.PointLight, enqueue);
            }
            finally { ImGui.EndDisabled(); }

            ImGui.Separator();
            var children = editor.Graph.Document.Objects.ToLookup(item => item.ParentId);
            // 递归绘制设计层级；切换选择前先提交旧字段并结束旧拖动。
            Guid? DrawNode(SceneObjectData item, Guid? current)
            {
                var descendants = children[item.Id].ToArray();
                var flags = ImGuiTreeNodeFlags.OpenOnArrow | ImGuiTreeNodeFlags.SpanAvailWidth;
                if (current == item.Id) flags |= ImGuiTreeNodeFlags.Selected;
                if (descendants.Length == 0) flags |= ImGuiTreeNodeFlags.Leaf | ImGuiTreeNodeFlags.NoTreePushOnOpen;
                var expanded = ImGui.TreeNodeEx($"{item.Name} [{item.Kind}]##{item.Id}", flags);
                ObserveItem?.Invoke("Tree:" + item.Id, ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
                if (ImGui.IsItemClicked() && !ImGui.IsItemToggledOpen())
                {
                    if (current != item.Id) FlushTextDrafts(enqueue, cancel: _cancelFrame);
                    if (_transactionEditor is not null) FinishTransaction(enqueue, cancel: false);
                    current = item.Id;
                }
                if (expanded && descendants.Length != 0)
                {
                    foreach (var child in descendants) current = DrawNode(child, current);
                    ImGui.TreePop();
                }
                return current;
            }
            foreach (var root in children[null]) selected = DrawNode(root, selected);
            ImGui.Separator();

            if (selected is not Guid id)
            {
                FlushTextDrafts(enqueue, cancel: _cancelFrame);
                FinishTransaction(enqueue, cancel: false);
                ImGui.TextDisabled("Select an object to edit.");
                return selected;
            }
            var item = editor.Graph.Object(id);
            if (_draftObject != id)
            {
                // 旧对象字段可能已不再绘制；先用它自己的回调提交，再切换草稿身份。
                FlushTextDrafts(enqueue, cancel: _cancelFrame);
                FinishTransaction(enqueue, cancel: false);
                _draftObject = id;
            }
            ImGui.PushID(id.ToString());
            try
            {
                ImGui.TextDisabled(id.ToString());
                ImGui.BeginDisabled(playing);
                try
                {
                    DrawText("Name", item.Name, value => editor.SetName(id, value), enqueue);
                    var visible = item.Visible;
                    if (ImGui.Checkbox("Visible", ref visible))
                    {
                        FinishTransaction(enqueue, cancel: false);
                        enqueue(() => editor.SetVisible(id, visible));
                    }

                    ImGui.BeginDisabled(editor.Graph.Subtree(id).Any(child => editor.Graph.Object(child).Kind == ObjectKind.Player));
                    if (ImGui.Button("Delete subtree"))
                    {
                        FinishTransaction(enqueue, cancel: false);
                        enqueue(() =>
                        {
                            editor.Delete(id);
                            _pendingSelection = true;
                            _nextSelection = null;
                        });
                    }
                    ImGui.EndDisabled();

                    DrawParent(editor, item, enqueue);
                    ImGui.Separator();
                    var position = ToUi(item.Transform.Position);
                    var positionChanged = ImGui.DragFloat3("Local position (m)", ref position, 0.025f);
                    var newPosition = new Float3(position.X, position.Y, position.Z);
                    TrackEdit(editor, "Position", positionChanged, () => EditTransform(editor, id, transform => transform.Position = newPosition), enqueue);

                    var angles = item.Transform.Rotation.ToQuaternion().ToEulerAngles() * (180 / MathF.PI);
                    var rotation = new NVector3(angles.X, angles.Y, angles.Z);
                    var rotationChanged = ImGui.DragFloat3("Local rotation (deg)", ref rotation, 0.5f);
                    var newRotation = RotationData.From(OQuaternion.FromEulerAngles(rotation.X * MathF.PI / 180, rotation.Y * MathF.PI / 180, rotation.Z * MathF.PI / 180));
                    TrackEdit(editor, "Rotation", rotationChanged, () => EditTransform(editor, id, transform => transform.Rotation = newRotation), enqueue);

                    ImGui.BeginDisabled(SceneValidator.RequiresRoot(item));
                    if (children[id].Any())
                    {
                        ImGui.TextDisabled("Parent nodes require positive uniform scale.");
                        float scale = item.Transform.Scale.X;
                        var changed = ImGui.DragFloat("Uniform scale", ref scale, 0.01f, 0.001f, 100, "%.3f");
                        var newScale = new Float3(scale, scale, scale);
                        TrackEdit(editor, "Scale", changed, () => EditTransform(editor, id, transform => transform.Scale = newScale), enqueue);
                    }
                    else
                    {
                        var scale = ToUi(item.Transform.Scale);
                        var changed = ImGui.DragFloat3("Local scale", ref scale, 0.01f, 0.001f, 100, "%.3f");
                        var newScale = new Float3(scale.X, scale.Y, scale.Z);
                        TrackEdit(editor, "Scale", changed, () => EditTransform(editor, id, transform => transform.Scale = newScale), enqueue);
                    }
                    ImGui.EndDisabled();
                    if (SceneValidator.RequiresRoot(item)) ImGui.TextDisabled("Character roots use unit scale; edit dimensions.");

                    var materialExpanded = item.Kind != ObjectKind.Group && ImGui.CollapsingHeader("Material", ImGuiTreeNodeFlags.DefaultOpen);
                    if (item.Kind != ObjectKind.Group) ObserveItem?.Invoke("Material header", ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
                    if (materialExpanded)
                    {
                        var color = ToUi(item.Material.BaseColor);
                        var changed = ImGui.ColorEdit3("Base color", ref color);
                        var newColor = new Float3(color.X, color.Y, color.Z);
                        TrackEdit(editor, "Base color", changed, () => EditMaterial(editor, id, material => material.BaseColor = newColor), enqueue);
                        ImGui.BeginDisabled(item.Primitive == PrimitiveKind.Model);
                        float metallic = item.Material.Metallic;
                        changed = ImGui.DragFloat("Metallic", ref metallic, 0.01f, 0, 1);
                        var newMetallic = metallic;
                        TrackEdit(editor, "Metallic", changed, () => EditMaterial(editor, id, material => material.Metallic = newMetallic), enqueue);
                        float roughness = item.Material.Roughness;
                        changed = ImGui.DragFloat("Roughness", ref roughness, 0.01f, 0, 1);
                        var newRoughness = roughness;
                        TrackEdit(editor, "Roughness", changed, () => EditMaterial(editor, id, material => material.Roughness = newRoughness), enqueue);
                        ImGui.EndDisabled();
                        if (item.Primitive == PrimitiveKind.Model) ImGui.TextDisabled("Tint above; metallic/roughness come from imported material.");
                        DrawText("Color texture", item.Material.BaseColorTexture ?? "", value => EditMaterial(editor, id, material => material.BaseColorTexture = Optional(value)), enqueue);
                        DrawText("Normal texture", item.Material.NormalTexture ?? "", value => EditMaterial(editor, id, material => material.NormalTexture = Optional(value)), enqueue);
                    }

                    if (item.Primitive == PrimitiveKind.Model)
                        DrawText("Model asset", item.ModelPath ?? "", value => editor.SetAsset(id, Optional(value)), enqueue);
                    DrawTarget(editor, item, enqueue);
                    if (item.Parameters.Count != 0 && ImGui.CollapsingHeader("Parameters", ImGuiTreeNodeFlags.DefaultOpen))
                    {
                        foreach (var parameter in item.Parameters)
                        {
                            var name = parameter.Key;
                            float value = parameter.Value;
                            var changed = ImGui.DragFloat(name, ref value, 0.025f);
                            var newValue = value;
                            TrackEdit(editor, "Parameter " + name, changed, () => editor.SetParameter(id, name, newValue), enqueue);
                        }
                    }
                    if (item.TemplateId is string template)
                    {
                        ImGui.TextDisabled("Template: " + template);
                        ImGui.TextWrapped("Overrides: " + (item.TemplateOverrides.Count == 0 ? "none" : string.Join(", ", item.TemplateOverrides)));
                    }
                    if (_transactionEditor is not null) ImGui.TextDisabled("Editing transaction - Escape to cancel");
                    else ImGui.TextDisabled("Asset text: Enter or leave field to apply.");
                }
                finally { ImGui.EndDisabled(); }
            }
            finally { ImGui.PopID(); }
            return selected;
        }
        finally
        {
            // 收起分组等隐藏边界不会再调用原InputText/DragFloat，须在帧末收尾。
            FlushTextDrafts(enqueue, cancel: _cancelFrame, onlyUndrawn: true);
            if (_transactionOwner is string owner && !_drawnEdits.Contains(owner)) FinishTransaction(enqueue, cancel: false);
            ImGui.End();
        }
    }

    /// <summary>结束旧事务后排队创建对象，优先采用匹配模板，命令成功后发布新选择。</summary>
    private void QueueCreate(SceneEditor editor, ObjectKind kind, Action<Action> enqueue)
    {
        FinishTransaction(enqueue, cancel: false);
        enqueue(() =>
        {
            var template = editor.Graph.Document.Templates.FirstOrDefault(item => item.Defaults.Kind == kind && item.Defaults.Primitive == PrimitiveKind.Cube);
            Guid id;
            if (template is not null) id = editor.Create(template.Id);
            else
            {
                var item = new SceneObjectData { Name = kind switch { ObjectKind.Group => "Group", ObjectKind.PointLight => "Point light", _ => "Cube" },
                    Kind = kind, Primitive = PrimitiveKind.Cube, Collider = kind == ObjectKind.StaticMesh ? new ColliderData() : null };
                if (kind == ObjectKind.PointLight) { item.Parameters["intensity"] = 25; item.Parameters["radius"] = 8; item.Transform.Position = new Float3(0, 3, 0); }
                id = editor.Create(item);
            }
            _pendingSelection = true;
            _nextSelection = id;
        });
    }

    /// <summary>列出不在自身子树且满足缩放合同的父节点，角色根禁止修改父级。</summary>
    private void DrawParent(SceneEditor editor, SceneObjectData item, Action<Action> enqueue)
    {
        ImGui.BeginDisabled(SceneValidator.RequiresRoot(item));
        try
        {
            var parentLabel = item.ParentId is Guid parent ? editor.Graph.Object(parent).Name : "<scene root>";
            if (!ImGui.BeginCombo("Parent", parentLabel)) return;
            try
            {
                if (ImGui.Selectable("<scene root>", item.ParentId is null)) QueueParent(editor, item.Id, null, enqueue);
                var subtree = editor.Graph.Subtree(item.Id).ToHashSet();
                foreach (var candidate in editor.Graph.Document.Objects.Where(candidate => !subtree.Contains(candidate.Id)))
                {
                    ImGui.BeginDisabled(!SceneValidator.Uniform(candidate.Transform.Scale));
                    if (ImGui.Selectable(candidate.Name + "##parent" + candidate.Id, item.ParentId == candidate.Id)) QueueParent(editor, item.Id, candidate.Id, enqueue);
                    ImGui.EndDisabled();
                }
            }
            finally { ImGui.EndCombo(); }
        }
        finally { ImGui.EndDisabled(); }
    }

    /// <summary>结束当前事务后排队保持世界姿态的重挂接操作。</summary>
    private void QueueParent(SceneEditor editor, Guid id, Guid? parent, Action<Action> enqueue)
    {
        FinishTransaction(enqueue, cancel: false);
        enqueue(() => editor.Reparent(id, parent));
    }

    /// <summary>绘制目标GUID选择；按钮只允许门目标，选取后排队更新引用。</summary>
    private void DrawTarget(SceneEditor editor, SceneObjectData item, Action<Action> enqueue)
    {
        if (item.Kind != ObjectKind.Button && item.TargetId is null) return;
        var targetLabel = item.TargetId is Guid target ? editor.Graph.Object(target).Name : "<none>";
        if (!ImGui.BeginCombo("Target", targetLabel)) return;
        try
        {
            if (ImGui.Selectable("<none>", item.TargetId is null))
            {
                FinishTransaction(enqueue, cancel: false);
                enqueue(() => editor.SetTarget(item.Id, null));
            }
            foreach (var candidate in editor.Graph.Document.Objects.Where(candidate => candidate.Id != item.Id &&
                (item.Kind != ObjectKind.Button || candidate.Kind == ObjectKind.Door)))
                if (ImGui.Selectable(candidate.Name + "##target" + candidate.Id, item.TargetId == candidate.Id))
                {
                    FinishTransaction(enqueue, cancel: false);
                    var targetId = candidate.Id;
                    enqueue(() => editor.SetTarget(item.Id, targetId));
                }
        }
        finally { ImGui.EndCombo(); }
    }

    /// <summary>维护字段草稿，Enter或编辑后离开时排队一次提交；Escape由统一取消路径处理。</summary>
    private void DrawText(string label, string current, Action<string> apply, Action<Action> enqueue)
    {
        _drawnText.Add(label);
        _textApplications[label] = apply;
        if (!_activeText.Contains(label))
        {
            _textDrafts[label] = current;
            _textOriginals[label] = current;
        }
        var text = _textDrafts.GetValueOrDefault(label, current);
        bool enter = ImGui.InputText(label, ref text, 1024, ImGuiInputTextFlags.EnterReturnsTrue);
        ObserveItem?.Invoke(label, ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
        _textDrafts[label] = text;
        var commit = enter || ImGui.IsItemDeactivatedAfterEdit();
        if (ImGui.IsItemActive()) _activeText.Add(label); else _activeText.Remove(label);
        if (!_cancelFrame && commit && text != current)
        {
            FinishTransaction(enqueue, cancel: false);
            var value = text;
            enqueue(() => apply(value));
            _textOriginals[label] = value;
        }
    }

    /// <summary>在选择/窗口/分组隐藏边界提交或丢弃草稿，可仅处理本帧未绘制的字段。</summary>
    private void FlushTextDrafts(Action<Action> enqueue, bool cancel, bool onlyUndrawn = false)
    {
        foreach (var label in _textDrafts.Keys.Where(label => !onlyUndrawn || !_drawnText.Contains(label)).ToArray())
        {
            var value = _textDrafts[label];
            if (!cancel && value != _textOriginals.GetValueOrDefault(label, value) && _textApplications.TryGetValue(label, out var apply))
            {
                FinishTransaction(enqueue, cancel: false);
                enqueue(() => apply(value));
            }
            _textDrafts.Remove(label);
            _textOriginals.Remove(label);
            _textApplications.Remove(label);
            _activeText.Remove(label);
        }
    }

    /// <summary>把控件激活到松开合并为一条历史事务；被拒帧不结束手势，Escape后抑制残余拖动。</summary>
    private void TrackEdit(SceneEditor editor, string label, bool changed, Action apply, Action<Action> enqueue)
    {
        ObserveItem?.Invoke(label, ImGui.GetItemRectMin(), ImGui.GetItemRectMax());
        _drawnEdits.Add(label);
        if (_cancelledEditOwner == label)
        {
            if (!ImGui.IsItemActive() || ImGui.IsItemActivated()) _cancelledEditOwner = null;
            else return;
        }
        if (_cancelFrame) return;
        if ((ImGui.IsItemActivated() || changed) && _transactionOwner != label)
        {
            FinishTransaction(enqueue, cancel: false);
            _transactionEditor = editor;
            _transactionOwner = label;
            enqueue(() => { if (!editor.History.InTransaction) editor.History.BeginTransaction(label); });
        }
        if (changed) enqueue(apply);
        if (ImGui.IsItemDeactivated() && _transactionOwner == label) FinishTransaction(enqueue, cancel: false);
    }

    /// <summary>清除面板事务身份并排队提交或取消实际History事务，保持命令执行顺序。</summary>
    private void FinishTransaction(Action<Action> enqueue, bool cancel)
    {
        if (_transactionEditor is not { } editor) return;
        _transactionEditor = null;
        _transactionOwner = null;
        enqueue(() =>
        {
            if (!editor.History.InTransaction) return;
            if (cancel) editor.History.CancelTransaction();
            else editor.History.CommitTransaction();
        });
    }

    /// <summary>复制当前TRS再应用字段变化，统一交给编辑器验证和记录历史。</summary>
    private static void EditTransform(SceneEditor editor, Guid id, Action<TransformData> change)
    {
        var transform = TransformMath.Clone(editor.Graph.Object(id).Transform);
        change(transform);
        editor.SetTransform(id, transform);
    }

    /// <summary>复制完整可编辑材质再修改，避免草稿直接污染当前设计对象。</summary>
    private static void EditMaterial(SceneEditor editor, Guid id, Action<MaterialData> change)
    {
        var source = editor.Graph.Object(id).Material;
        var material = new MaterialData { BaseColor = source.BaseColor, Metallic = source.Metallic, Roughness = source.Roughness,
            BaseColorTexture = source.BaseColorTexture, NormalTexture = source.NormalTexture };
        change(material);
        editor.SetMaterial(id, material);
    }

    /// <summary>把引擎三分量值复制为ImGui使用的Numerics向量。</summary>
    private static NVector3 ToUi(Float3 value) => new(value.X, value.Y, value.Z);
    /// <summary>将空白资产文本转换为null，非空路径去除首尾空白。</summary>
    private static string? Optional(string value) => string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
