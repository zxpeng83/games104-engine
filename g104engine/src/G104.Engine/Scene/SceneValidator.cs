namespace G104.Engine.Scene;

public static class SceneValidator
{
    public static void Validate(SceneDocument document)
    {
        ArgumentNullException.ThrowIfNull(document);
        if (document.SchemaVersion != SceneDocument.CurrentVersion) throw new SceneValidationException($"Unsupported schemaVersion {document.SchemaVersion}.");
        if (document.Objects is null || document.Templates is null || document.Rendering is null) throw new SceneValidationException("Scene collections and render settings cannot be null.");
        if (string.IsNullOrWhiteSpace(document.Name) || document.Objects.Count > 10000 || document.Templates.Count > 1000) throw new SceneValidationException("Invalid scene name or collection size.");
        var rendering = document.Rendering;
        if (!Enum.IsDefined(rendering.Pipeline) || !TransformMath.Finite(rendering.SunDirection.ToVector()) || rendering.SunDirection.ToVector().LengthSquared < 1e-12f || !TransformMath.Finite(rendering.SunColor.ToVector()) || rendering.SunColor.X < 0 || rendering.SunColor.Y < 0 || rendering.SunColor.Z < 0 || !float.IsFinite(rendering.SunIntensity) || rendering.SunIntensity < 0 || !float.IsFinite(rendering.Exposure) || rendering.Exposure <= 0)
            throw new SceneValidationException("Invalid render settings.");
        var templates = new Dictionary<string, ObjectTemplateData>(StringComparer.Ordinal);
        foreach (var template in document.Templates)
        {
            if (template is null || string.IsNullOrWhiteSpace(template.Id) || !templates.TryAdd(template.Id, template) || template.Defaults is null)
                throw new SceneValidationException("Template IDs must be named and unique.");
            ValidateObject(template.Defaults);
            if (template.Defaults.TemplateId is not null || template.Defaults.ParentId is not null || template.Defaults.TargetId is not null || template.Defaults.TemplateOverrides.Count != 0)
                throw new SceneValidationException($"Nested or externally referenced template is unsupported: {template.Id}.");
        }
        var objects = new Dictionary<Guid, SceneObjectData>();
        foreach (var item in document.Objects)
        {
            if (item is null || item.Id == Guid.Empty || !objects.TryAdd(item.Id, item)) throw new SceneValidationException("Scene object IDs must be nonempty and unique.");
            ValidateObject(item);
            if (item.TemplateId is string templateId)
            {
                if (!templates.TryGetValue(templateId, out var template)) throw new SceneValidationException($"Missing template {templateId} for {item.Name}.");
                if (item.Kind != template.Defaults.Kind || item.Primitive != template.Defaults.Primitive) throw new SceneValidationException($"Template kind/primitive cannot be overridden: {item.Name}.");
            }
            else if (item.TemplateOverrides.Count != 0) throw new SceneValidationException($"Object without a template has overrides: {item.Name}.");
        }
        var colors = new Dictionary<Guid, int>();
        var depths = new Dictionary<Guid, int>();
        void Visit(SceneObjectData item, int depth)
        {
            if (depth > 128) throw new SceneValidationException("Scene hierarchy exceeds depth 128.");
            var color = colors.GetValueOrDefault(item.Id);
            if (color == 1) throw new SceneValidationException($"Parent cycle at {item.Name} ({item.Id}).");
            if (color == 2) return;
            colors[item.Id] = 1;
            if (item.ParentId is Guid parent)
            {
                if (!objects.TryGetValue(parent, out var parentObject)) throw new SceneValidationException($"Missing parent {parent} for {item.Name}.");
                if (RequiresRoot(item)) throw new SceneValidationException($"{item.Kind} {item.Name} must remain a scene root.");
                if (!Uniform(parentObject.Transform.Scale)) throw new SceneValidationException($"Parent {parentObject.Name} must have positive uniform scale.");
                Visit(parentObject, depth + 1);
                depths[item.Id] = depths[parent] + 1;
            }
            else depths[item.Id] = 0;
            if (depths[item.Id] > 128) throw new SceneValidationException("Scene hierarchy exceeds depth 128.");
            if (item.TargetId is Guid target && !objects.ContainsKey(target)) throw new SceneValidationException($"Missing target {target} for {item.Name}.");
            colors[item.Id] = 2;
        }
        foreach (var item in document.Objects) Visit(item, 0);
        foreach (var item in document.Objects.Where(o => o.Collider is { Kind: not ColliderKind.None }))
        {
            Guid? parent = item.ParentId;
            while (parent is Guid id)
            {
                var ancestor = objects[id];
                if (ancestor.Kind is ObjectKind.Player or ObjectKind.Npc or ObjectKind.Door)
                    throw new SceneValidationException($"Moving {ancestor.Kind} {ancestor.Name} only supports visual descendants; remove collider from {item.Name}.");
                parent = ancestor.ParentId;
            }
        }
    }

    public static bool RequiresRoot(SceneObjectData item) => item.Kind is ObjectKind.Player or ObjectKind.Npc;
    public static bool Uniform(Float3 value) => value.X > 0 && value.Y > 0 && value.Z > 0 &&
        MathF.Abs(value.X - value.Y) <= TransformMath.Tolerance * MathF.Max(value.X, value.Y) &&
        MathF.Abs(value.X - value.Z) <= TransformMath.Tolerance * MathF.Max(value.X, value.Z);

    public static void ValidateObject(SceneObjectData item)
    {
        if (string.IsNullOrWhiteSpace(item.Name)) throw new SceneValidationException("Object name cannot be empty.");
        if (!Enum.IsDefined(item.Kind) || !Enum.IsDefined(item.Primitive)) throw new SceneValidationException($"Invalid object or primitive kind for {item.Name}.");
        if (item.Transform is null || item.Material is null || item.Parameters is null || item.TemplateOverrides is null) throw new SceneValidationException($"Object fields cannot be null: {item.Name}.");
        if (item.TemplateOverrides.Distinct(StringComparer.Ordinal).Count() != item.TemplateOverrides.Count || item.TemplateOverrides.Any(name => !TemplateCatalog.AllowedOverrides.Contains(name))) throw new SceneValidationException($"Invalid template override for {item.Name}.");
        var transform = item.Transform;
        if (!TransformMath.Finite(transform.Position.ToVector()) || !TransformMath.Finite(transform.Scale.ToVector()) || transform.Scale.X <= 0 || transform.Scale.Y <= 0 || transform.Scale.Z <= 0)
            throw new SceneValidationException($"Invalid position/scale for {item.Name}.");
        TransformMath.Normalize(transform.Rotation.ToQuaternion());
        if (RequiresRoot(item) && (MathF.Abs(transform.Scale.X - 1) > TransformMath.Tolerance || MathF.Abs(transform.Scale.Y - 1) > TransformMath.Tolerance || MathF.Abs(transform.Scale.Z - 1) > TransformMath.Tolerance))
            throw new SceneValidationException($"Physics character {item.Name} requires unit scale; use character dimensions.");
        if (item.Parameters.Any(pair => string.IsNullOrWhiteSpace(pair.Key) || !float.IsFinite(pair.Value))) throw new SceneValidationException($"Parameters must be named and finite for {item.Name}.");
        SceneParameterRules.Validate(item);
        if (!TransformMath.Finite(item.Material.BaseColor.ToVector()) || !float.IsFinite(item.Material.Metallic) || item.Material.Metallic < 0 || item.Material.Metallic > 1 || !float.IsFinite(item.Material.Roughness) || item.Material.Roughness < 0 || item.Material.Roughness > 1)
            throw new SceneValidationException($"Invalid material for {item.Name}.");
        if (item.Material.BaseColor.X < 0 || item.Material.BaseColor.Y < 0 || item.Material.BaseColor.Z < 0) throw new SceneValidationException($"Material color must be nonnegative for {item.Name}.");
        if (item.ModelPath is string model) AssetPath.Normalize(model);
        if (item.Material.BaseColorTexture is string color) AssetPath.Normalize(color);
        if (item.Material.NormalTexture is string normal) AssetPath.Normalize(normal);
        if (item.Primitive == PrimitiveKind.Model && item.ModelPath is null) throw new SceneValidationException($"Model object needs a modelPath: {item.Name}.");
        if (item.Collider is { } collider && (!Enum.IsDefined(collider.Kind) || !TransformMath.Finite(collider.Size.ToVector()) || collider.Size.X <= 0 || collider.Size.Y <= 0 || collider.Size.Z <= 0 || !TransformMath.Finite(collider.Center.ToVector()) || !float.IsFinite(collider.Friction) || collider.Friction < 0))
            throw new SceneValidationException($"Invalid collider for {item.Name}.");
    }
}
