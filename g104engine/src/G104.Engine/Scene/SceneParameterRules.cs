namespace G104.Engine.Scene;

// 已声明键构成编辑白名单；这里校验V1实际消费的数值与关联约束，未知扩展键仍须有限。
public static class SceneParameterRules
{
    public static void Validate(SceneObjectData item)
    {
        float Value(string key, float fallback) => item.Parameters.GetValueOrDefault(key, fallback);
        void Nonnegative(params string[] keys)
        {
            foreach (var key in keys)
                if (item.Parameters.TryGetValue(key, out var value) && value < 0) throw new SceneValidationException($"{item.Name}: {key} must be nonnegative.");
        }
        void Positive(params string[] keys)
        {
            foreach (var key in keys)
                if (item.Parameters.TryGetValue(key, out var value) && value <= 0) throw new SceneValidationException($"{item.Name}: {key} must be positive.");
        }
        if (item.Kind is ObjectKind.Player or ObjectKind.Npc)
        {
            Positive("radius", "height", "gravity");
            Nonnegative("walkSpeed", "runSpeed", "jumpSpeed", "stepHeight");
            if (Value("height", 1.9f) <= Value("radius", 0.35f) * 2) throw new SceneValidationException($"{item.Name}: capsule height must exceed its diameter.");
            var slope = Value("maxSlopeDegrees", 45);
            if (slope is < 0 or > 80) throw new SceneValidationException($"{item.Name}: maxSlopeDegrees must be in [0,80].");
        }
        if (item.Kind == ObjectKind.Npc)
        {
            Positive("sightRange", "navCellSize");
            Nonnegative("followDistance", "memoryTime", "searchTime", "patrolRadius");
            var fov = Value("fieldOfView", 150);
            if (fov is <= 0 or > 360) throw new SceneValidationException($"{item.Name}: fieldOfView must be in (0,360].");
            var width = Math.Ceiling(((double)Value("navMaxX", 12) - Value("navMinX", -12)) / Value("navCellSize", 0.65f));
            var depth = Math.Ceiling(((double)Value("navMaxZ", 12) - Value("navMinZ", -12)) / Value("navCellSize", 0.65f));
            if (width <= 0 || depth <= 0 || width * depth > 20000) throw new SceneValidationException($"{item.Name}: navigation bounds must be ordered and at most 20,000 cells.");
        }
        if (item.Kind == ObjectKind.PointLight) { Positive("radius", "range"); Nonnegative("intensity"); }
        if (item.Kind == ObjectKind.Button) Positive("interactDistance");
        if (item.Kind == ObjectKind.Door) Positive("openHeight");
        if (item.Kind == ObjectKind.Goal) Positive("radius", "triggerRadius");
    }
}
