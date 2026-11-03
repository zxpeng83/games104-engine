using G104.Engine.Scene;

namespace G104.Engine.Editor;

/// <summary>为UI提供可校验、可撤销的设计命令；不直接操纵运行物理或GPU句柄。</summary>
public sealed class SceneEditor
{
    /// <summary>把同一个设计图交给命令层和历史层管理。</summary>
    public SceneEditor(SceneGraph graph)
    {
        Graph = graph;
        History = new EditorHistory(graph);
    }

    /// <summary>当前受编辑的设计场景图。</summary>
    public SceneGraph Graph { get; }
    /// <summary>所有编辑命令共用的撤销、重做与事务历史。</summary>
    public EditorHistory History { get; }

    /// <summary>从单层模板建立独立实例，设置父级并作为一条历史提交。</summary>
    public Guid Create(string templateId, Guid? parentId = null)
    {
        var item = new TemplateCatalog(Graph.Document).Instantiate(templateId);
        item.ParentId = parentId;
        History.Execute($"Create {item.Name}", () => Graph.Document.Objects.Add(item));
        return item.Id;
    }

    /// <summary>深复制调用者提供的对象设计，要求ID未被当前场景使用。</summary>
    public Guid Create(SceneObjectData description)
    {
        var item = SceneSerializer.Clone(new SceneDocument { Objects = [description] }).Objects[0];
        if (Graph.Document.Objects.Any(existing => existing.Id == item.Id)) throw new SceneValidationException("Create requires a new stable object ID.");
        History.Execute($"Create {item.Name}", () => Graph.Document.Objects.Add(item));
        return item.Id;
    }

    /// <summary>整棵删除子树；保留必需玩家，并拒绝仍被外部TargetId引用的子树。</summary>
    public void Delete(Guid id)
    {
        var subtree = Graph.Subtree(id).ToHashSet();
        if (Graph.Document.Objects.Any(item => subtree.Contains(item.Id) && item.Kind == ObjectKind.Player)) throw new SceneValidationException("The required player cannot be deleted.");
        var external = Graph.Document.Objects.FirstOrDefault(item => !subtree.Contains(item.Id) && item.TargetId is Guid target && subtree.Contains(target));
        if (external is not null) throw new SceneValidationException($"Cannot delete: {external.Name} ({external.Id}) references this subtree. Clear its TargetId first.");
        History.Execute("Delete subtree", () => Graph.Document.Objects.RemoveAll(item => subtree.Contains(item.Id)));
    }

    /// <summary>深复制局部TRS并记录模板覆盖，由历史层统一校验与回滚。</summary>
    public void SetTransform(Guid id, TransformData value) => History.Execute("Transform", () =>
    {
        var item = Graph.Object(id);
        item.Transform = TransformMath.Clone(value);
        TemplateCatalog.MarkOverride(item, "transform");
    });

    /// <summary>修改父级并可保持世界姿态；拒绝自身/后代父级和无法表达的局部TRS。</summary>
    public void Reparent(Guid id, Guid? parentId, bool keepWorld = true)
    {
        var item = Graph.Object(id);
        if (parentId is Guid parent)
        {
            Graph.Object(parent);
            if (Graph.Subtree(id).Contains(parent)) throw new SceneValidationException("An object cannot be parented to itself or its descendant.");
        }
        // 保持世界姿态时用world * inverse(newParentWorld)反求局部矩阵。
        var world = Graph.WorldMatrix(id);
        var local = keepWorld ? TransformMath.Decompose(world * (parentId is Guid newParent ? TransformMath.Inverse(Graph.WorldMatrix(newParent)) : OpenTK.Mathematics.Matrix4.Identity)) : TransformMath.Clone(item.Transform);
        History.Execute("Reparent", () =>
        {
            item.ParentId = parentId;
            item.Transform = local;
            if (keepWorld) TemplateCatalog.MarkOverride(item, "transform");
        });
    }

    /// <summary>仅修改设计已声明的有限数值键，防止UI注入运行时字段。</summary>
    public void SetParameter(Guid id, string name, float value) => History.Execute($"Parameter {name}", () =>
    {
        var item = Graph.Object(id);
        // 白名单由已有设计/模板声明；UI不能添加任意运行时字段。
        if (!item.Parameters.ContainsKey(name)) throw new SceneValidationException($"Parameter is not editable: {name}.");
        if (!float.IsFinite(value)) throw new SceneValidationException("Parameter value must be finite.");
        item.Parameters[name] = value;
        TemplateCatalog.MarkOverride(item, "parameters");
    });

    /// <summary>复制设计材质并规范化贴图路径，记录显式材质覆盖。</summary>
    public void SetMaterial(Guid id, MaterialData value) => History.Execute("Material", () =>
    {
        var item = Graph.Object(id);
        item.Material = new MaterialData { BaseColor = value.BaseColor, Metallic = value.Metallic, Roughness = value.Roughness,
            BaseColorTexture = value.BaseColorTexture is null ? null : AssetPath.Normalize(value.BaseColorTexture), NormalTexture = value.NormalTexture is null ? null : AssetPath.Normalize(value.NormalTexture) };
        TemplateCatalog.MarkOverride(item, "material");
    });

    /// <summary>复制或移除碰撞设计；实际物理世界在下次Play重建。</summary>
    public void SetCollider(Guid id, ColliderData? value) => History.Execute("Collider", () =>
    {
        var item = Graph.Object(id);
        item.Collider = value is null ? null : new ColliderData { Kind = value.Kind, Size = value.Size, Center = value.Center, Friction = value.Friction };
        TemplateCatalog.MarkOverride(item, "collider");
    });

    /// <summary>设置或清除模型相对路径，资源预备失败由历史层回滚。</summary>
    public void SetAsset(Guid id, string? modelPath) => History.Execute("Model asset", () =>
    {
        var item = Graph.Object(id);
        item.ModelPath = modelPath is null ? null : AssetPath.Normalize(modelPath);
        TemplateCatalog.MarkOverride(item, "modelPath");
    });

    /// <summary>修改显式交互目标，引用有效性由场景校验统一检查。</summary>
    public void SetTarget(Guid id, Guid? targetId) => History.Execute("Interaction target", () =>
    {
        var item = Graph.Object(id);
        item.TargetId = targetId;
        TemplateCatalog.MarkOverride(item, "targetId");
    });

    /// <summary>通过历史修改显示名称，并记录模板名称覆盖。</summary>
    public void SetName(Guid id, string name) => History.Execute("Name", () =>
    {
        var item = Graph.Object(id);
        item.Name = name;
        TemplateCatalog.MarkOverride(item, "name");
    });

    /// <summary>通过历史修改绘制可见性，不直接切换碰撞或玩法。</summary>
    public void SetVisible(Guid id, bool visible) => History.Execute("Visibility", () =>
    {
        var item = Graph.Object(id);
        item.Visible = visible;
        TemplateCatalog.MarkOverride(item, "visible");
    });

    /// <summary>记录设计中的管线选择，由窗口在后续Play使用。</summary>
    public void SetRenderPipeline(RenderPipelineMode mode) => History.Execute("Render pipeline", () => Graph.Document.Rendering.Pipeline = mode);
}
