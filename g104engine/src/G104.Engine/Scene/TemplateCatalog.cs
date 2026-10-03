namespace G104.Engine.Scene;

public sealed class TemplateCatalog(SceneDocument document)
{
    public static IReadOnlySet<string> AllowedOverrides { get; } = new HashSet<string>(StringComparer.Ordinal)
    {
        "name", "transform", "material", "collider", "modelPath", "targetId", "visible", "parameters"
    };

    public SceneObjectData Instantiate(string templateId)
    {
        var template = document.Templates.SingleOrDefault(item => item.Id == templateId) ?? throw new SceneValidationException($"Unknown template: {templateId}.");
        if (template.Defaults.TemplateId is not null) throw new SceneValidationException("Nested templates are unsupported.");
        var temporary = new SceneDocument { Objects = [template.Defaults] };
        var instance = SceneSerializer.Clone(temporary).Objects[0];
        instance.Id = Guid.NewGuid();
        instance.ParentId = null;
        instance.TargetId = null;
        instance.TemplateId = templateId;
        instance.TemplateOverrides = [];
        return instance;
    }

    public static void MarkOverride(SceneObjectData item, string field)
    {
        if (!AllowedOverrides.Contains(field)) throw new SceneValidationException($"Field is not a template override: {field}.");
        if (item.TemplateId is not null && !item.TemplateOverrides.Contains(field, StringComparer.Ordinal)) item.TemplateOverrides.Add(field);
    }
}
