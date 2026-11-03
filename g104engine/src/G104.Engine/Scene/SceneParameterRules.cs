using G104.Engine.Physics;

namespace G104.Engine.Scene;

// 已声明键构成编辑白名单；这里校验V1实际消费的数值与关联约束，未知扩展键仍须有限。
/// <summary>按V1消费者的实际合同验证设计参数，不建立未来组件的通用模式。</summary>
public static class SceneParameterRules
{
    // V1设计参数没有Skin字段，校验必须沿用角色控制器的同一默认值。
    /// <summary>角色默认碰撞安全间隙，单位米；胶囊半径必须大于它。</summary>
    private static readonly float DefaultCharacterSkin = new CharacterSettings().Skin;

    /// <summary>按对象职责检查已声明参数的范围与尺寸、视野和导航容量关系。</summary>
    public static void Validate(SceneObjectData item)
    {
        // 未声明的可选参数沿用运行消费者的同一默认值。
        float Value(string key, float fallback) => item.Parameters.GetValueOrDefault(key, fallback);
        // 只检查实际声明的键，允许0用于关闭对应速度或时长。
        void Nonnegative(params string[] keys)
        {
            foreach (var key in keys)
                if (item.Parameters.TryGetValue(key, out var value) && value < 0) throw new SceneValidationException($"{item.Name}: {key} must be nonnegative.");
        }
        // 只检查实际声明的键，要求必须严格大于0的尺寸或范围。
        void Positive(params string[] keys)
        {
            foreach (var key in keys)
                if (item.Parameters.TryGetValue(key, out var value) && value <= 0) throw new SceneValidationException($"{item.Name}: {key} must be positive.");
        }
        if (item.Kind is ObjectKind.Player or ObjectKind.Npc)
        {
            Positive("radius", "height", "gravity");
            Nonnegative("walkSpeed", "runSpeed", "jumpSpeed", "stepHeight");
            if (Value("radius", 0.35f) <= DefaultCharacterSkin)
                throw new SceneValidationException($"{item.Name}: radius must exceed the default character skin ({DefaultCharacterSkin} m).");
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
            // 用double计算网格跨度与容量，避免float相减抹掉可表示的尾格。
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
