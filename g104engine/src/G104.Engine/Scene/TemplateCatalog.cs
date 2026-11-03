namespace G104.Engine.Scene;

/// <summary>从场景内的单层模板生成独立对象，并记录明确允许的设计覆盖。</summary>
/// <param name="document">提供模板定义的场景文档；目录只读取其模板数据。</param>
public sealed class TemplateCatalog(SceneDocument document)
{
    /// <summary>允许实例显式覆盖的设计字段集合；身份、类型和结构不在白名单内。</summary>
    public static IReadOnlySet<string> AllowedOverrides { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "name", "transform", "material", "collider", "modelPath", "targetId", "visible", "parameters"
    };

    /// <summary>深复制模板默认值，分配新ID并清除父级/目标引用，实例互不共享可变设计。</summary>
    public SceneObjectData Instantiate(string templateId)
    {
        var template = document.Templates.SingleOrDefault(item => item.Id == templateId) ?? throw new SceneValidationException($"Unknown template: {templateId}.");
        if (template.Defaults.TemplateId is not null) throw new SceneValidationException("Nested templates are unsupported.");
        var temporary = new SceneDocument { Objects = [template.Defaults] };
        var instance = SceneSerializer.Clone(temporary).Objects[0];
        // 模板自身的身份和引用不直接带进场景实例，避免重复ID或悬空引用。
        instance.Id = Guid.NewGuid();
        instance.ParentId = null;
        instance.TargetId = null;
        instance.TemplateId = templateId;
        instance.TemplateOverrides = [];
        return instance;
    }

    /// <summary>校验覆盖字段；模板实例只记录一次该字段，独立对象不添加覆盖标记。</summary>
    public static void MarkOverride(SceneObjectData item, string field)
    {
        if (!AllowedOverrides.Contains(field)) throw new SceneValidationException($"Field is not a template override: {field}.");
        if (item.TemplateId is not null && !item.TemplateOverrides.Contains(field, StringComparer.Ordinal)) item.TemplateOverrides.Add(field);
    }
}
