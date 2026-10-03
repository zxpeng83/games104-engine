using G104.Engine.Assets;
using G104.Engine.Rendering;
using OpenTK.Mathematics;
using System.Text.Json.Nodes;

namespace G104.Engine.Animation;

public static class AnimationVerification
{
    public static IReadOnlyList<string> Run(string assetRoot)
    {
        var model = new GltfModelLoader(new(assetRoot)).Load("models/UAL1_Standard.glb");
        Require(model.Skins.Length == 1 && model.Skins[0].Joints.Length == 65, "实际65骨骼完整导入");
        string[] required = ["Idle_Loop", "Walk_Loop", "Jog_Fwd_Loop", "Jump_Start", "Jump_Loop", "Jump_Land"];
        Require(required.All(model.Clips.ContainsKey), "所需同骨架动作存在");
        var bounds = model.ComputeBounds();
        Require(bounds.Max.Y - bounds.Min.Y is > 1.7f and < 2.0f, "导入根变换后的模型为米级Y-up");
        var probe = new GltfModelLoader(new(assetRoot)).Load("models/material-probe.glb");
        Require(probe.Primitives.Any(p => p.Material.BaseColorImage is not null && p.Material.NormalImage is not null), "glTF相对PNG与法线贴图导入");
        var settings = AnimationSettings.Load(new(assetRoot));
        settings.ValidateCharacterModel(model);
        VerifySettings(model, probe, settings, File.ReadAllText(new AssetRoot(assetRoot).Resolve(AnimationSettings.DefaultAssetPath)));
        VerifyDisplayInterpolation(model, AnimationSettings.Default); // 固定非平凡动作样例，不限制用户JSON可映射静态T-pose。

        var linear = new AnimationClip("linear", 1, [new(0, TrackProperty.Translation, [0,1], [Vector4.Zero,new(10,0,0,0)], false)]);
        var pose = new[] { new NodePose(Vector3.Zero, Quaternion.Identity, Vector3.One) };
        AnimationSampler.Sample(linear, .25f, pose); Require(MathF.Abs(pose[0].Translation.X - 2.5f) < .0001f, "LINEAR求值");
        var step = linear with { Tracks = [linear.Tracks[0] with { Step = true }] };
        AnimationSampler.Sample(step, .999f, pose); Require(pose[0].Translation.X == 0, "STEP保持前一帧");
        AnimationSampler.Sample(step, 1, pose); Require(pose[0].Translation.X == 10, "STEP精确关键帧取值");
        var rotation = NodePose.ShortSlerp(Quaternion.Identity, new(0,0,0,-1), .5f);
        Require(MathF.Abs(rotation.W) > .9999f, "四元数最短弧");

        // 独立非单位mesh节点样例，防止palette漏掉inverse(meshWorld)。
        var spaces = new GltfModel
        {
            SourcePath = "synthetic", Nodes = [new("mesh", -1, new(new(3,0,0),Quaternion.Identity,Vector3.One)), new("joint", -1, new(new(6,0,0),Quaternion.Identity,Vector3.One))],
            EvaluationOrder = [0,1], Primitives = [], Skins = [new([1],[Matrix4.CreateTranslation(-2,0,0)])], Clips = new Dictionary<string, AnimationClip>()
        };
        var world = new Matrix4[2]; spaces.EvaluateWorld(spaces.CreateDefaultPose(), world);
        Vector3 transformed = Vector3.TransformPosition(Vector3.Zero, spaces.CreatePalette(0,0,world)[0] * world[0]);
        Require(MathF.Abs(transformed.X - 4) < .0001f, "逆绑定/当前关节/mesh局部空间公式");

        var controller = new AnimationController(model);
        for (int i = 0; i < 180; i++) controller.Update(1f / 60, new(2.2f, true, 0));
        var footsteps = controller.DrainEvents();
        Require(footsteps.Length >= 3 && footsteps.All(e => e.Name == "Footstep") && footsteps.Select(e => e.Sequence).Distinct().Count() == footsteps.Length, "循环脚步事件与序列去重");
        controller.Update(1f / 60, new(2.2f, false, 5));
        for (int i = 0; i < 30; i++) controller.Update(1f / 60, new(2.2f, false, -1));
        Require(controller.State == CharacterAnimationState.JumpLoop, "起跳→腾空");
        controller.Update(1f / 60, new(0, true, 0));
        for (int i = 0; i < 20; i++) controller.Update(1f / 60, new(0, true, 0));
        Require(controller.State == CharacterAnimationState.Locomotion, "落地→移动");
        var jumps = controller.DrainEvents();
        Require(jumps.Count(e => e.Name == "Jump") == 1 && jumps.Count(e => e.Name == "Land") == 1, "跳跃事实只发一次事件");
        var falling = new AnimationController(model, settings);
        falling.Update(1f / 60, new(1, false, -1));
        var fallEvents = falling.DrainEvents();
        Require(falling.State == CharacterAnimationState.JumpLoop && fallEvents.Count(e => e.Name == "Fall") == 1 && fallEvents.All(e => e.Name != "Jump"), "向下离地直接Fall，不伪装主动起跳");
        falling.Update(1f / 60, new(0, true, 0));
        Require(falling.DrainEvents().Count(e => e.Name == "Land") == 1, "落下后的真实接地触发Land");
        foreach (var primitive in model.Primitives)
            if (primitive.Skin >= 0)
                Require(model.CreatePalette(primitive.Skin, primitive.Node, controller.World).All(m => Finite(m)), "65骨骼动画palette全部有限");
        return ["SharpGLTF: 67 nodes / 65 joints / 43 clips / meter-scale Y-up", "Material probe: PNG + normal texture + relative URI", "Animation: LINEAR / STEP / shortest arc / mesh-relative palette", "Animation controller: locomotion blend / JumpStart-Loop-Land / unique loop events", "Animation config: strict JSON / actual non-default clip-speed-time-marker behavior / shared immutable definition", "Animation facts: upward Jump / downward Fall / actual-contact Land", "Animation display: previous-current local TRS / endpoints / shared alpha for mesh and palette / no logic-event advancement"];
    }

    private static void VerifyDisplayInterpolation(GltfModel model, AnimationSettings settings)
    {
        var controller = new AnimationController(model, settings);
        NodePose[] previousPose = controller.Pose.ToArray(); Matrix4[] previousWorld = controller.World.ToArray();
        controller.Update(.2f, new(4.8f, true, 0));
        Require(EqualMatrices(controller.GetDisplayWorld(0), previousWorld), "显示alpha0使用上一逻辑姿态");
        Require(EqualMatrices(controller.GetDisplayWorld(1), controller.World) && !ReferenceEquals(controller.GetDisplayWorld(1), controller.World), "显示alpha1使用当前姿态但独立存储");
        var middlePose = previousPose.Select((pose, i) => NodePose.Blend(pose, controller.Pose[i], .5f)).ToArray();
        var expectedMiddle = new Matrix4[model.Nodes.Length]; model.EvaluateWorld(middlePose, expectedMiddle);
        Require(EqualMatrices(controller.GetDisplayWorld(.5f), expectedMiddle), "显示中间帧先混合局部TRS再组合层级");
        // 层级中的旋转子节点走弧线；直接对世界矩阵逐元素Lerp会缩短骨链并引入剪切。
        bool nonlinear = false;
        for (int i = 0; i < previousWorld.Length; i++)
        {
            if (model.Nodes[i].Parent < 0) continue;
            Vector3 naive = Vector3.Lerp(previousWorld[i].ExtractTranslation(), controller.World[i].ExtractTranslation(), .5f);
            if ((expectedMiddle[i].ExtractTranslation() - naive).LengthSquared > 1e-10f) { nonlinear = true; break; }
        }
        Require(nonlinear, "实际骨链中间结果不同于世界矩阵/位置直接Lerp");
        CharacterAnimationState state = controller.State; float stateTime = controller.StateTime, phase = controller.LocomotionPhase;
        NodePose[] logicalPose = controller.Pose.ToArray(); Matrix4[] logicalWorld = controller.World.ToArray(); controller.DrainEvents();
        for (int i = 0; i < 8; i++) { controller.GetDisplayWorld(0); controller.GetDisplayWorld(.5f); controller.GetDisplayWorld(1); }
        Require(controller.State == state && controller.StateTime == stateTime && controller.LocomotionPhase == phase && !Different(controller.Pose, logicalPose) &&
            EqualMatrices(controller.World, logicalWorld) && controller.DrainEvents().Length == 0, "重复显示求值不推进FSM/逻辑姿态/时间/事件");
        controller.ResetInterpolation();
        Require(EqualMatrices(controller.GetDisplayWorld(0), logicalWorld) && controller.State == state && controller.StateTime == stateTime &&
            controller.LocomotionPhase == phase && controller.DrainEvents().Length == 0, "显示历史重置消除旧步且保留FSM/时间/事件");
        var pending = new AnimationController(model, settings); pending.Update(1f / 60, new(0, false, 5));
        float pendingTime = pending.StateTime; pending.ResetInterpolation();
        Require(pending.State == CharacterAnimationState.JumpStart && pending.StateTime == pendingTime && pending.DrainEvents().Single().Name == "Jump", "显示历史重置不丢弃尚未消费事件");
    }
    private static bool EqualMatrices(Matrix4[] a, Matrix4[] b)
    {
        if (a.Length != b.Length) return false;
        for (int i = 0; i < a.Length; i++)
            if ((a[i].Row0 - b[i].Row0).LengthSquared > 1e-8f || (a[i].Row1 - b[i].Row1).LengthSquared > 1e-8f ||
                (a[i].Row2 - b[i].Row2).LengthSquared > 1e-8f || (a[i].Row3 - b[i].Row3).LengthSquared > 1e-8f) return false;
        return true;
    }

    private static void VerifySettings(GltfModel model, GltfModel probe, AnimationSettings loaded, string json)
    {
        var actualFileController = new AnimationController(model, loaded);
        var actualFilePose = model.CreateDefaultPose();
        AnimationSampler.Sample(model.Clips[loaded.ClipMap.Idle], 0, actualFilePose);
        Require(ReferenceEquals(actualFileController.Settings, loaded) && !Different(actualFileController.Pose, actualFilePose), "实际JSON定义用于实例求值");
        var defaults = AnimationSettings.Default;
        JsonObject config = JsonNode.Parse(json)!.AsObject();
        config["walkReferenceSpeed"] = 1; config["runReferenceSpeed"] = 2; config["speedResponse"] = 24;
        config["transitionSeconds"] = .24; config["jumpStartSeconds"] = .75; config["jumpLandSeconds"] = .6;
        config["minJumpVelocity"] = defaults.MinJumpVelocity;
        foreach (var (role, clip) in defaults.ClipMap.Entries()) config["clipMap"]![role] = clip;
        config["clipMap"]!["run"] = "Sprint_Loop"; config["footstepMarkers"] = new JsonArray(.25f);
        var custom = AnimationSettings.Parse(config.ToJsonString());
        var changed = new AnimationController(model, custom); var baseline = new AnimationController(model, defaults);
        changed.Update(.25f, new(3, true, 0)); baseline.Update(.25f, new(3, true, 0));
        Require(changed.RunWeight > .99f && changed.RunWeight > baseline.RunWeight + .3f && changed.ActiveClip == "Sprint_Loop", "速度/响应/Run映射改变真实混合结果");
        var expected = model.CreateDefaultPose();
        AnimationSampler.Sample(model.Clips["Sprint_Loop"], changed.LocomotionPhase * model.Clips["Sprint_Loop"].Duration, expected);
        Require(!Different(changed.Pose, expected), "配置Run映射实际采样Sprint，而非仅保存字段");
        var slowResponse = new AnimationSettings(defaults.ClipMap, 2.2f, 4.8f, .5f, .12f, .18f, .22f, .1f, [.15f,.65f]);
        var fastResponse = new AnimationSettings(defaults.ClipMap, 2.2f, 4.8f, 24, .12f, .18f, .22f, .1f, [.15f,.65f]);
        var slow = new AnimationController(model, slowResponse); var fast = new AnimationController(model, fastResponse);
        slow.Update(.1f, new(4.8f, true, 0)); fast.Update(.1f, new(4.8f, true, 0));
        Require(fast.RunWeight > slow.RunWeight + .5f && Different(slow.Pose, fast.Pose), "独立调整speedResponse改变真实权重/Pose");

        changed = new(model, custom); baseline = new(model, defaults);
        changed.Update(.1f, new(0, false, 5)); baseline.Update(.1f, new(0, false, 5));
        Require(MathF.Abs(changed.TransitionWeight - .1f / .24f) < .0001f && changed.TransitionWeight < baseline.TransitionWeight, "配置过渡时长改变姿态权重");
        changed.Update(.1f, new(0, false, 2)); baseline.Update(.1f, new(0, false, 2));
        Require(changed.State == CharacterAnimationState.JumpStart && baseline.State == CharacterAnimationState.JumpLoop && Different(changed.Pose, baseline.Pose), "配置起跳时长改变切换时机与实际Pose");
        changed.Update(.1f, new(0, true, 0)); baseline.Update(.1f, new(0, true, 0));
        changed.Update(.2f, new(0, true, 0)); baseline.Update(.2f, new(0, true, 0));
        Require(changed.State == CharacterAnimationState.JumpLand && baseline.State == CharacterAnimationState.Locomotion, "配置落地时长改变FSM");
        var thresholdConfig = JsonNode.Parse(json)!.AsObject(); thresholdConfig["minJumpVelocity"] = 1;
        var belowThreshold = new AnimationController(model, AnimationSettings.Parse(thresholdConfig.ToJsonString()));
        belowThreshold.Update(1f / 60, new(0, false, .5f));
        Require(belowThreshold.State == CharacterAnimationState.JumpLoop && belowThreshold.DrainEvents().Single().Name == "Fall", "配置minJumpVelocity实际区分低速离地");

        var sibling = new AnimationController(model, custom); var active = new AnimationController(model, custom);
        NodePose[] siblingPose = sibling.Pose.ToArray(); active.Update(.1f, new(2, true, 0));
        Require(ReferenceEquals(active.Settings, sibling.Settings) && sibling.LocomotionPhase == 0 && !Different(sibling.Pose, siblingPose) && sibling.DrainEvents().Length == 0, "共享定义且游标/Pose/事件独立");
        float previousPhase = active.LocomotionPhase; int expectedSteps = 0, actualSteps = active.DrainEvents().Count(e => e.Name == "Footstep");
        expectedSteps += previousPhase >= .25f ? 1 : 0;
        for (int i = 0; i < 180; i++)
        {
            active.Update(1f / 60, new(2, true, 0));
            float current = active.LocomotionPhase;
            if (current < previousPhase) { if (.25f > previousPhase || .25f <= current) expectedSteps++; }
            else if (previousPhase < .25f && current >= .25f) expectedSteps++;
            actualSteps += active.DrainEvents().Count(e => e.Name == "Footstep"); previousPhase = current;
        }
        Require(actualSteps == expectedSteps && actualSteps > 2, "配置单脚步marker实际影响跨循环事件数量");
        var noEventsConfig = JsonNode.Parse(json)!.AsObject(); noEventsConfig["footstepMarkers"] = new JsonArray();
        var silent = new AnimationController(model, AnimationSettings.Parse(noEventsConfig.ToJsonString()));
        for (int i = 0; i < 180; i++) silent.Update(1f / 60, new(2.2f, true, 0));
        Require(silent.DrainEvents().Length == 0, "空markers禁用脚步事件");
        var staticController = new AnimationController(probe, custom, character: false);
        NodePose[] bindPose = staticController.Pose.ToArray(); staticController.Update(.3f, new(5, false, 5));
        Require(!Different(staticController.Pose, bindPose) && staticController.DrainEvents().Length == 0, "无角色clips的静态模型保留bind pose");
        Reject(() => new AnimationController(probe, custom, character: true), "角色不能因0clips而免检");

        void Bad(Action<JsonObject> mutate)
        { var invalid = JsonNode.Parse(json)!.AsObject(); mutate(invalid); Reject(() => AnimationSettings.Parse(invalid.ToJsonString()), "坏配置拒绝"); }
        Bad(c => c["unknownField"] = 1); Bad(c => c.Remove("schemaVersion")); Bad(c => c["schemaVersion"] = 2);
        Bad(c => c["jumpStartSeconds"] = -1); Bad(c => c["transitionSeconds"] = 0); Bad(c => c["walkReferenceSpeed"] = 0);
        Bad(c => c["runReferenceSpeed"] = c["walkReferenceSpeed"]!.GetValue<float>()); Bad(c => c["speedResponse"] = "NaN"); Bad(c => c["minJumpVelocity"] = -1);
        Bad(c => c["footstepMarkers"] = new JsonArray(1f)); Bad(c => c["footstepMarkers"] = new JsonArray(.25f, .25f));
        Bad(c => c["clipMap"]!["run"] = "");
        string compact = JsonNode.Parse(json)!.ToJsonString();
        Reject(() => AnimationSettings.Parse(compact.Replace("\"schemaVersion\":1", "\"schemaVersion\":1,\"schemaVersion\":1", StringComparison.Ordinal)), "重复JSON字段拒绝");
        var missing = JsonNode.Parse(json)!.AsObject(); missing["clipMap"]!["run"] = "Missing_Clip";
        Reject(() => new AnimationController(model, AnimationSettings.Parse(missing.ToJsonString())), "角色缺所需clip拒绝");
        Reject(() => new AnimationSettings(defaults.ClipMap, 1, 2, float.NaN, .1f, .2f, .2f, .1f, [.2f]), "程序配置NaN拒绝");
        float[] sourceMarkers = [.25f];
        var copied = new AnimationSettings(defaults.ClipMap, 1, 2, 12, .1f, .2f, .2f, .1f, sourceMarkers); sourceMarkers[0] = .9f;
        Require(copied.FootstepMarkers[0] == .25f, "共享定义不暴露调用者原数组");
    }
    private static bool Different(NodePose[] a, NodePose[] b)
    {
        for (int i = 0; i < a.Length; i++)
        {
            Quaternion qa = a[i].Rotation, qb = b[i].Rotation;
            float dot = MathF.Abs(qa.X * qb.X + qa.Y * qb.Y + qa.Z * qb.Z + qa.W * qb.W);
            if ((a[i].Translation - b[i].Translation).LengthSquared > 1e-8f || (a[i].Scale - b[i].Scale).LengthSquared > 1e-8f || dot < .99999f) return true;
        }
        return false;
    }
    private static void Reject(Action action, string message)
    {
        try { action(); }
        catch (Exception error) when (error is System.Text.Json.JsonException or InvalidDataException or ArgumentException) { return; }
        throw new InvalidOperationException("Animation verification expected rejection: " + message);
    }
    private static bool Finite(Matrix4 m) => new[] { m.Row0, m.Row1, m.Row2, m.Row3 }.All(r => float.IsFinite(r.X) && float.IsFinite(r.Y) && float.IsFinite(r.Z) && float.IsFinite(r.W));
    private static void Require(bool condition, string message) { if (!condition) throw new InvalidOperationException("Animation verification failed: " + message); }
}
