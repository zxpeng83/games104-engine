using G104.Engine.Assets;
using G104.Engine.Rendering;
using OpenTK.Mathematics;

namespace G104.Engine.Animation;

public readonly record struct AnimationEvent(string Name, long Sequence);
public enum CharacterAnimationState { Locomotion, JumpStart, JumpLoop, JumpLand }

// 时间和事件只在Update推进；Render可以重复消费同一姿态而不会重发事件。
public sealed class AnimationController
{
    private readonly GltfModel model;
    private readonly AnimationSettings settings;
    private readonly bool character;
    private readonly NodePose[] transitionFrom;
    private readonly NodePose[] temporary;
    private readonly NodePose[] previousPose;
    private readonly NodePose[] displayPose;
    private readonly Matrix4[] displayWorld;
    private int poseRevision;
    private int displayRevision = -1;
    private float displayAlpha;
    private readonly Queue<AnimationEvent> events = new();
    private float phase;
    private float elapsed;
    private float stateTime;
    private float transitionTime;
    private float smoothedSpeed;
    private bool wasGrounded = true;
    private long eventSequence;
    public NodePose[] Pose { get; }
    public Matrix4[] World { get; }
    public CharacterAnimationState State { get; private set; }
    public float WalkWeight { get; private set; }
    public float JogWeight { get; private set; }
    public float RunWeight => JogWeight;
    public AnimationSettings Settings => settings;
    public float StateTime => stateTime;
    public float LocomotionPhase => phase;
    public float TransitionWeight => Math.Clamp(transitionTime / settings.TransitionSeconds, 0, 1);
    public string ActiveClip => !character ? "BindPose" : State switch
    {
        CharacterAnimationState.JumpStart => settings.ClipMap.JumpStart,
        CharacterAnimationState.JumpLoop => settings.ClipMap.JumpLoop,
        CharacterAnimationState.JumpLand => settings.ClipMap.JumpLand,
        _ => RunWeight >= .5f ? settings.ClipMap.Run : WalkWeight >= .5f ? settings.ClipMap.Walk : settings.ClipMap.Idle
    };
    public string DebugText => !character ? "BindPose | no character FSM" :
        $"{State} | {ActiveClip} {stateTime:F2}s | speed {smoothedSpeed:F2} | " +
        (State == CharacterAnimationState.Locomotion ? $"Idle {1 - WalkWeight - RunWeight:F2} Walk {WalkWeight:F2} Run {RunWeight:F2} | phase {phase:F2}" : $"transition {TransitionWeight:F2}") +
        $" | joints {model.Skins.FirstOrDefault()?.Joints.Length ?? 0}";

    public AnimationController(GltfModel model, AnimationSettings? settings = null) : this(model, settings, model.Clips.Count > 0) { }

    internal AnimationController(GltfModel model, AnimationSettings? settings, bool character)
    {
        this.model = model;
        this.settings = settings ?? AnimationSettings.Default;
        this.character = character;
        if (character) this.settings.ValidateCharacterModel(model);
        transitionTime = this.settings.TransitionSeconds;
        Pose = model.CreateDefaultPose();
        temporary = model.CreateDefaultPose();
        transitionFrom = model.CreateDefaultPose();
        World = new Matrix4[model.Nodes.Length];
        if (character) Sample(this.settings.ClipMap.Idle, 0, Pose);
        model.EvaluateWorld(Pose, World);
        previousPose = Pose.ToArray(); displayPose = Pose.ToArray(); displayWorld = new Matrix4[World.Length];
    }

    public void Update(float dt, AnimationInputs inputs)
    {
        if (!float.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
        if (!float.IsFinite(inputs.Speed) || !float.IsFinite(inputs.VerticalVelocity)) throw new ArgumentException("动画输入速度必须为有限值", nameof(inputs));
        if (dt == 0 || !character) return;
        Array.Copy(Pose, previousPose, Pose.Length);
        elapsed += dt;
        smoothedSpeed += (MathF.Max(inputs.Speed, 0) - smoothedSpeed) * (1 - MathF.Exp(-settings.SpeedResponse * dt));
        if (wasGrounded && !inputs.Grounded)
        {
            bool jumping = inputs.VerticalVelocity > settings.MinJumpVelocity;
            ChangeState(jumping ? CharacterAnimationState.JumpStart : CharacterAnimationState.JumpLoop);
            Emit(jumping ? "Jump" : "Fall");
        }
        if (!wasGrounded && inputs.Grounded) { ChangeState(CharacterAnimationState.JumpLand); Emit("Land"); }
        wasGrounded = inputs.Grounded;
        stateTime += dt;
        transitionTime += dt;
        if (State == CharacterAnimationState.JumpStart && stateTime >= settings.JumpStartSeconds) ChangeState(CharacterAnimationState.JumpLoop);
        if (State == CharacterAnimationState.JumpLand && stateTime >= settings.JumpLandSeconds) ChangeState(inputs.Grounded ? CharacterAnimationState.Locomotion : CharacterAnimationState.JumpLoop);

        if (State == CharacterAnimationState.Locomotion)
        {
            // 同步步态相位，避免不同长度Walk/Jog混合时双脚周期不断漂移。
            float normalizedSpeed = Math.Clamp(smoothedSpeed / settings.WalkReferenceSpeed, 0, 1);
            JogWeight = Math.Clamp((smoothedSpeed - settings.WalkReferenceSpeed) / (settings.RunReferenceSpeed - settings.WalkReferenceSpeed), 0, 1);
            WalkWeight = normalizedSpeed * (1 - JogWeight);
            float previous = phase;
            float duration = MathHelper.Lerp(ClipDuration(settings.ClipMap.Walk), ClipDuration(settings.ClipMap.Run), JogWeight);
            float advance = dt / duration * Math.Clamp(smoothedSpeed / MathHelper.Lerp(settings.WalkReferenceSpeed, settings.RunReferenceSpeed, JogWeight), .2f, 1.6f);
            if (smoothedSpeed > .12f)
            {
                phase = (phase + advance) % 1;
                foreach (float marker in settings.FootstepMarkers) Crossed(previous, advance, marker);
            }
            Sample(settings.ClipMap.Idle, elapsed % ClipDuration(settings.ClipMap.Idle), Pose);
            Sample(settings.ClipMap.Walk, phase * ClipDuration(settings.ClipMap.Walk), temporary);
            for (int i = 0; i < Pose.Length; i++) Pose[i] = NodePose.Blend(Pose[i], temporary[i], normalizedSpeed);
            Sample(settings.ClipMap.Run, phase * ClipDuration(settings.ClipMap.Run), temporary);
            for (int i = 0; i < Pose.Length; i++) Pose[i] = NodePose.Blend(Pose[i], temporary[i], JogWeight);
        }
        else
        {
            string name = State switch { CharacterAnimationState.JumpStart => settings.ClipMap.JumpStart, CharacterAnimationState.JumpLand => settings.ClipMap.JumpLand, _ => settings.ClipMap.JumpLoop };
            float duration = ClipDuration(name);
            float time = State switch
            {
                CharacterAnimationState.JumpStart => Math.Min(stateTime / settings.JumpStartSeconds, 1) * duration,
                CharacterAnimationState.JumpLand => Math.Min(stateTime / settings.JumpLandSeconds, 1) * duration,
                _ => stateTime % duration
            };
            Sample(name, time, Pose);
        }
        float blend = TransitionWeight;
        if (blend < 1) for (int i = 0; i < Pose.Length; i++) Pose[i] = NodePose.Blend(transitionFrom[i], Pose[i], blend);
        model.EvaluateWorld(Pose, World);
        poseRevision++;
    }

    public Matrix4[] GetDisplayWorld(float alpha)
    {
        if (!float.IsFinite(alpha)) throw new ArgumentOutOfRangeException(nameof(alpha));
        alpha = Math.Clamp(alpha, 0, 1);
        if (displayRevision == poseRevision && displayAlpha == alpha) return displayWorld;
        // 先插值局部TRS，再按父先子后的模型层级求世界；不插值最终骨骼矩阵。
        // Clip采样/跨Clip混合属于逻辑Update，显示插值不改变逻辑姿态、状态或事件。
        for (int i = 0; i < Pose.Length; i++) displayPose[i] = alpha == 0 ? previousPose[i] : alpha == 1 ? Pose[i] : NodePose.Blend(previousPose[i], Pose[i], alpha);
        model.EvaluateWorld(displayPose, displayWorld);
        displayRevision = poseRevision; displayAlpha = alpha;
        return displayWorld;
    }

    public void ResetInterpolation()
    {
        Array.Copy(Pose, previousPose, Pose.Length);
        displayRevision = -1; // 失焦/暂停的显示历史重置不重启动作或重发事件。
    }

    public AnimationEvent[] DrainEvents()
    {
        var result = events.ToArray();
        events.Clear();
        return result;
    }

    private void ChangeState(CharacterAnimationState state)
    {
        if (State == state) return;
        Array.Copy(Pose, transitionFrom, Pose.Length);
        State = state;
        stateTime = 0;
        transitionTime = 0;
    }

    private void Crossed(float from, float advance, float marker)
    {
        // 一次更新越过多少个循环就产生多少个事件；序列号不会因循环回零重复。
        int count = (int)MathF.Floor(from + advance - marker) - (int)MathF.Floor(from - marker);
        for (int i = 0; i < count; i++) Emit("Footstep");
    }

    private void Emit(string name)
    {
        events.Enqueue(new(name, ++eventSequence));
        // 可视化使用者若不消费事件，也不会造成无限增长。
        while (events.Count > 64) events.Dequeue();
    }

    private float ClipDuration(string name) => model.Clips[name].Duration;
    private void Sample(string name, float time, NodePose[] result)
    {
        for (int i = 0; i < result.Length; i++) result[i] = model.Nodes[i].DefaultPose;
        AnimationSampler.Sample(model.Clips[name], time, result);
    }
}

public static class AnimationSampler
{
    public static void Sample(AnimationClip clip, float time, NodePose[] pose)
    {
        foreach (var track in clip.Tracks)
        {
            if (track.Times.Length == 0) continue;
            int upper = Array.BinarySearch(track.Times, time);
            if (upper < 0) upper = ~upper;
            int a = Math.Clamp(upper - 1, 0, track.Times.Length - 1);
            int b = Math.Clamp(upper, 0, track.Times.Length - 1);
            // 恰好落在关键帧时，STEP也必须取该帧，而非上一帧。
            if (track.Times[b] == time) a = b;
            float weight = track.Step || a == b ? 0 : Math.Clamp((time - track.Times[a]) / (track.Times[b] - track.Times[a]), 0, 1);
            Vector4 value;
            if (track.Property == TrackProperty.Rotation)
            {
                Quaternion q = NodePose.ShortSlerp(new(track.Values[a].Xyz, track.Values[a].W), new(track.Values[b].Xyz, track.Values[b].W), weight);
                value = new(q.X, q.Y, q.Z, q.W);
            }
            else value = Vector4.Lerp(track.Values[a], track.Values[b], weight);
            NodePose previous = pose[track.Node];
            pose[track.Node] = track.Property switch
            {
                TrackProperty.Translation => previous with { Translation = value.Xyz },
                TrackProperty.Scale => previous with { Scale = value.Xyz },
                _ => previous with { Rotation = new(value.Xyz, value.W) }
            };
        }
    }
}
