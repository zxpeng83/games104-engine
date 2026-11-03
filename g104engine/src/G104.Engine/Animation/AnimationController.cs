using G104.Engine.Assets;
using G104.Engine.Rendering;
using OpenTK.Mathematics;

namespace G104.Engine.Animation;

/// <summary>动画推进时生成的实例事件，显示求值不会生成事件。</summary>
/// <param name="Name">脚步、起跳、下落或落地等事件名称。</param>
/// <param name="Sequence">实例内单调递增序号，用于区分循环中的同名事件。</param>
public readonly record struct AnimationEvent(string Name, long Sequence);
/// <summary>内置状态：Locomotion混合移动；JumpStart起跳；JumpLoop腾空循环；JumpLand落地。</summary>
public enum CharacterAnimationState { Locomotion, JumpStart, JumpLoop, JumpLand }

// 时间和事件只在Update推进；Render可以重复消费同一姿态而不会重发事件。
/// <summary>持有单实例的逻辑姿态、有限状态、显示插值缓存和一次消费事件。</summary>
public sealed class AnimationController
{
    /// <summary>实例引用的共享CPU模型定义，不在控制器中创建GPU资源。</summary>
    private readonly GltfModel model;
    /// <summary>经校验的共享只读配置，运行时游标仍属于本实例。</summary>
    private readonly AnimationSettings settings;
    /// <summary>是否启用角色FSM；静态模型保持默认绑定姿态。</summary>
    private readonly bool character;
    /// <summary>切状态时保存的局部姿态，作为跨状态混合起点。</summary>
    private readonly NodePose[] transitionFrom;
    /// <summary>第二路片段采样的可复用暂存数组，避免每步分配。</summary>
    private readonly NodePose[] temporary;
    /// <summary>上一逻辑步局部姿态，供显示插值使用。</summary>
    private readonly NodePose[] previousPose;
    /// <summary>独立显示局部姿态缓冲，不反写当前逻辑姿态。</summary>
    private readonly NodePose[] displayPose;
    /// <summary>显示姿态对应的模型空间矩阵缓存。</summary>
    private readonly Matrix4[] displayWorld;
    /// <summary>逻辑姿态版本，用于多个渲染Pass复用显示结果。</summary>
    private int poseRevision;
    /// <summary>已缓存显示结果对应的逻辑版本；-1表示无效。</summary>
    private int displayRevision = -1;
    /// <summary>已缓存显示结果使用的相邻逻辑步插值比例。</summary>
    private float displayAlpha;
    /// <summary>等待消费的事件队列，最多保留64项。</summary>
    private readonly Queue<AnimationEvent> events = new();
    /// <summary>Walk/Run同步步态相位，循环范围[0,1)。</summary>
    private float phase;
    /// <summary>实例累计逻辑时间，单位秒，用于独立推进待机片段。</summary>
    private float elapsed;
    /// <summary>当前状态已停留的逻辑秒数。</summary>
    private float stateTime;
    /// <summary>当前跨状态混合已推进的秒数。</summary>
    private float transitionTime;
    /// <summary>指数平滑后的非负水平速度，单位米/秒。</summary>
    private float smoothedSpeed;
    /// <summary>上一逻辑步接地事实，用于检测起跳、下落和落地边沿。</summary>
    private bool wasGrounded = true;
    /// <summary>本实例发出事件的累计序号，不随步态循环重置。</summary>
    private long eventSequence;
    /// <summary>当前逻辑步的节点局部姿态；显示插值使用独立数组。</summary>
    public NodePose[] Pose { get; }
    /// <summary>当前逻辑姿态求得的模型空间矩阵。</summary>
    public Matrix4[] World { get; }
    /// <summary>当前角色动画状态，不决定角色物理位移。</summary>
    public CharacterAnimationState State { get; private set; }
    /// <summary>当前行走贡献权重，剩余贡献由待机与跑步补齐。</summary>
    public float WalkWeight { get; private set; }
    /// <summary>当前跑步贡献权重，默认来源是Jog片段。</summary>
    public float JogWeight { get; private set; }
    /// <summary>以Run角色名称公开跑步权重，与JogWeight保持同一数值。</summary>
    public float RunWeight => JogWeight;
    /// <summary>公开实例所引用的共享只读动画定义。</summary>
    public AnimationSettings Settings => settings;
    /// <summary>当前状态累计时间，单位秒，供调试和验证读取。</summary>
    public float StateTime => stateTime;
    /// <summary>公开同步步态相位，供调试和事件数量核对。</summary>
    public float LocomotionPhase => phase;
    /// <summary>将过渡时间归一化并限制到[0,1]的姿态混合权重。</summary>
    public float TransitionWeight => Math.Clamp(transitionTime / settings.TransitionSeconds, 0, 1);
    /// <summary>给出主导片段的调试名称；移动状态实际仍可能混合多个片段。</summary>
    public string ActiveClip => !character ? "BindPose" : State switch
    {
        CharacterAnimationState.JumpStart => settings.ClipMap.JumpStart,
        CharacterAnimationState.JumpLoop => settings.ClipMap.JumpLoop,
        CharacterAnimationState.JumpLand => settings.ClipMap.JumpLand,
        _ => RunWeight >= .5f ? settings.ClipMap.Run : WalkWeight >= .5f ? settings.ClipMap.Walk : settings.ClipMap.Idle
    };
    /// <summary>汇总状态、映射片段、时间、权重、相位和骨骼数量。</summary>
    public string DebugText => !character ? "BindPose | no character FSM" :
        $"{State} | {ActiveClip} {stateTime:F2}s | speed {smoothedSpeed:F2} | " +
        (State == CharacterAnimationState.Locomotion ? $"Idle {1 - WalkWeight - RunWeight:F2} Walk {WalkWeight:F2} Run {RunWeight:F2} | phase {phase:F2}" : $"transition {TransitionWeight:F2}") +
        $" | joints {model.Skins.FirstOrDefault()?.Joints.Length ?? 0}";

    /// <summary>根据模型是否含片段选择角色模式；省略配置时使用内存默认定义。</summary>
    public AnimationController(GltfModel model, AnimationSettings? settings = null) : this(model, settings, model.Clips.Count > 0) { }

    /// <summary>显式选择角色或静态模式，分配独立姿态缓冲并初始化模型空间结果。</summary>
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

    /// <summary>以秒推进逻辑采样、状态混合及事件；输入仅消费运动事实，不反写物理。</summary>
    public void Update(float dt, AnimationInputs inputs)
    {
        if (!float.IsFinite(dt) || dt < 0) throw new ArgumentOutOfRangeException(nameof(dt));
        if (!float.IsFinite(inputs.Speed) || !float.IsFinite(inputs.VerticalVelocity)) throw new ArgumentException("动画输入速度必须为有限值", nameof(inputs));
        if (dt == 0 || !character) return;
        Array.Copy(Pose, previousPose, Pose.Length);
        elapsed += dt;
        smoothedSpeed += (MathF.Max(inputs.Speed, 0) - smoothedSpeed) * (1 - MathF.Exp(-settings.SpeedResponse * dt));
        // 依据真实接地边沿及竖直速度区分主动起跳和走出边缘后的下落。
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
        // 先获得目标状态姿态，再从切换瞬间的旧姿态渐入，避免状态间跳变。
        float blend = TransitionWeight;
        if (blend < 1) for (int i = 0; i < Pose.Length; i++) Pose[i] = NodePose.Blend(transitionFrom[i], Pose[i], blend);
        model.EvaluateWorld(Pose, World);
        poseRevision++;
    }

    /// <summary>按相邻局部TRS及统一alpha求显示矩阵，可重复调用且不推进时间或事件。</summary>
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

    /// <summary>将上一显示历史贴到当前逻辑姿态，保留状态、游标及待消费事件。</summary>
    public void ResetInterpolation()
    {
        Array.Copy(Pose, previousPose, Pose.Length);
        displayRevision = -1; // 失焦/暂停的显示历史重置不重启动作或重发事件。
    }

    /// <summary>返回当前队列快照并清空，实现每个消费者批次的一次取出。</summary>
    public AnimationEvent[] DrainEvents()
    {
        var result = events.ToArray();
        events.Clear();
        return result;
    }

    /// <summary>切换状态并冻结旧姿态，重置状态和过渡时间；同状态请求忽略。</summary>
    private void ChangeState(CharacterAnimationState state)
    {
        if (State == state) return;
        Array.Copy(Pose, transitionFrom, Pose.Length);
        State = state;
        stateTime = 0;
        transitionTime = 0;
    }

    /// <summary>计算本次相位推进跨过marker的次数，跨多个周期也逐次产生脚步事件。</summary>
    private void Crossed(float from, float advance, float marker)
    {
        // 一次更新越过多少个循环就产生多少个事件；序列号不会因循环回零重复。
        int count = (int)MathF.Floor(from + advance - marker) - (int)MathF.Floor(from - marker);
        for (int i = 0; i < count; i++) Emit("Footstep");
    }

    /// <summary>发出带新序号的事件，并丢弃超出64项容量的最旧记录。</summary>
    private void Emit(string name)
    {
        events.Enqueue(new(name, ++eventSequence));
        // 可视化使用者若不消费事件，也不会造成无限增长。
        while (events.Count > 64) events.Dequeue();
    }

    /// <summary>读取已验证映射片段的秒时长。</summary>
    private float ClipDuration(string name) => model.Clips[name].Duration;
    /// <summary>先恢复默认局部TRS，再覆盖片段轨道，保留未动画节点的根变换。</summary>
    private void Sample(string name, float time, NodePose[] result)
    {
        for (int i = 0; i < result.Length; i++) result[i] = model.Nodes[i].DefaultPose;
        AnimationSampler.Sample(model.Clips[name], time, result);
    }
}

/// <summary>对已导入的节点TRS关键帧执行无状态CPU采样。</summary>
public static class AnimationSampler
{
    /// <summary>在给定秒时间覆盖目标姿态的轨道分量，支持STEP和线性/最短弧插值。</summary>
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
