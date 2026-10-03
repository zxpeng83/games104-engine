using G104.Engine.Scene;

namespace G104.Engine.Editor;

public sealed class SceneEditor
{
    public SceneEditor(SceneGraph graph)
    {
        Graph = graph;
        History = new EditorHistory(graph);
    }

    public SceneGraph Graph { get; }
    public EditorHistory History { get; }

    public Guid Create(string templateId, Guid? parentId = null)
    {
        var item = new TemplateCatalog(Graph.Document).Instantiate(templateId);
        item.ParentId = parentId;
        History.Execute($"Create {item.Name}", () => Graph.Document.Objects.Add(item));
        return item.Id;
    }

    public Guid Create(SceneObjectData description)
    {
        var item = SceneSerializer.Clone(new SceneDocument { Objects = [description] }).Objects[0];
        if (Graph.Document.Objects.Any(existing => existing.Id == item.Id)) throw new SceneValidationException("Create requires a new stable object ID.");
        History.Execute($"Create {item.Name}", () => Graph.Document.Objects.Add(item));
        return item.Id;
    }

    public void Delete(Guid id)
    {
        var subtree = Graph.Subtree(id).ToHashSet();
        if (Graph.Document.Objects.Any(item => subtree.Contains(item.Id) && item.Kind == ObjectKind.Player)) throw new SceneValidationException("The required player cannot be deleted.");
        var external = Graph.Document.Objects.FirstOrDefault(item => !subtree.Contains(item.Id) && item.TargetId is Guid target && subtree.Contains(target));
        if (external is not null) throw new SceneValidationException($"Cannot delete: {external.Name} ({external.Id}) references this subtree. Clear its TargetId first.");
        History.Execute("Delete subtree", () => Graph.Document.Objects.RemoveAll(item => subtree.Contains(item.Id)));
    }

    public void SetTransform(Guid id, TransformData value) => History.Execute("Transform", () =>
    {
        var item = Graph.Object(id);
        item.Transform = TransformMath.Clone(value);
        TemplateCatalog.MarkOverride(item, "transform");
    });

    public void Reparent(Guid id, Guid? parentId, bool keepWorld = true)
    {
        var item = Graph.Object(id);
        if (parentId is Guid parent)
        {
            Graph.Object(parent);
            if (Graph.Subtree(id).Contains(parent)) throw new SceneValidationException("An object cannot be parented to itself or its descendant.");
        }
        var world = Graph.WorldMatrix(id);
        var local = keepWorld ? TransformMath.Decompose(world * (parentId is Guid newParent ? TransformMath.Inverse(Graph.WorldMatrix(newParent)) : OpenTK.Mathematics.Matrix4.Identity)) : TransformMath.Clone(item.Transform);
        History.Execute("Reparent", () =>
        {
            item.ParentId = parentId;
            item.Transform = local;
            if (keepWorld) TemplateCatalog.MarkOverride(item, "transform");
        });
    }

    public void SetParameter(Guid id, string name, float value) => History.Execute($"Parameter {name}", () =>
    {
        var item = Graph.Object(id);
        // 白名单由已有设计/模板声明；UI不能添加任意运行时字段。
        if (!item.Parameters.ContainsKey(name)) throw new SceneValidationException($"Parameter is not editable: {name}.");
        if (!float.IsFinite(value)) throw new SceneValidationException("Parameter value must be finite.");
        item.Parameters[name] = value;
        TemplateCatalog.MarkOverride(item, "parameters");
    });

    public void SetMaterial(Guid id, MaterialData value) => History.Execute("Material", () =>
    {
        var item = Graph.Object(id);
        item.Material = new MaterialData { BaseColor = value.BaseColor, Metallic = value.Metallic, Roughness = value.Roughness,
            BaseColorTexture = value.BaseColorTexture is null ? null : AssetPath.Normalize(value.BaseColorTexture), NormalTexture = value.NormalTexture is null ? null : AssetPath.Normalize(value.NormalTexture) };
        TemplateCatalog.MarkOverride(item, "material");
    });

    public void SetCollider(Guid id, ColliderData? value) => History.Execute("Collider", () =>
    {
        var item = Graph.Object(id);
        item.Collider = value is null ? null : new ColliderData { Kind = value.Kind, Size = value.Size, Center = value.Center, Friction = value.Friction };
        TemplateCatalog.MarkOverride(item, "collider");
    });

    public void SetAsset(Guid id, string? modelPath) => History.Execute("Model asset", () =>
    {
        var item = Graph.Object(id);
        item.ModelPath = modelPath is null ? null : AssetPath.Normalize(modelPath);
        TemplateCatalog.MarkOverride(item, "modelPath");
    });

    public void SetTarget(Guid id, Guid? targetId) => History.Execute("Interaction target", () =>
    {
        var item = Graph.Object(id);
        item.TargetId = targetId;
        TemplateCatalog.MarkOverride(item, "targetId");
    });

    public void SetName(Guid id, string name) => History.Execute("Name", () =>
    {
        var item = Graph.Object(id);
        item.Name = name;
        TemplateCatalog.MarkOverride(item, "name");
    });

    public void SetVisible(Guid id, bool visible) => History.Execute("Visibility", () =>
    {
        var item = Graph.Object(id);
        item.Visible = visible;
        TemplateCatalog.MarkOverride(item, "visible");
    });

    public void SetRenderPipeline(RenderPipelineMode mode) => History.Execute("Render pipeline", () => Graph.Document.Rendering.Pipeline = mode);
}
