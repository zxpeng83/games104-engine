namespace G104.Engine.Core;

/// <summary>本次显示更新的模拟步与丢时统计。</summary>
/// <param name="Steps">本次实际调用模拟回调的次数。</param>
/// <param name="Alpha">余量占一步的比例，用于前后姿态的显示插值。</param>
/// <param name="DroppedSteps">本次丢弃时间折算的完整步数，采用饱和计数。</param>
/// <param name="DroppedSeconds">本次丢弃的总秒数，包含帧时间上限外的部分。</param>
public readonly record struct FixedStepResult(int Steps, float Alpha, long DroppedSteps, double DroppedSeconds);

/// <summary>累计显示帧时间，以固定秒数推进模拟并限制单帧追赶次数。</summary>
public sealed class FixedStepClock
{
    /// <summary>尚未模拟的累计秒数；每次更新后只保留不足一步的余量。</summary>
    private double _accumulator;
    /// <summary>设置步长与帧时间上限（秒），以及单帧补步次数上限。</summary>
    public FixedStepClock(double fixedSeconds = 1.0 / 60.0, int maximumSteps = 5, double maximumFrameSeconds = 0.25)
    {
        if (!double.IsFinite(fixedSeconds) || fixedSeconds <= 0 || maximumSteps <= 0 || !double.IsFinite(maximumFrameSeconds) || maximumFrameSeconds < fixedSeconds)
            throw new ArgumentOutOfRangeException(nameof(fixedSeconds));
        FixedSeconds = fixedSeconds;
        MaximumSteps = maximumSteps;
        MaximumFrameSeconds = maximumFrameSeconds;
    }

    /// <summary>每个模拟步的固定秒数，默认1/60秒。</summary>
    public double FixedSeconds { get; }
    /// <summary>一次Advance最多执行的模拟步数，默认五步。</summary>
    public int MaximumSteps { get; }
    /// <summary>一次Advance允许进入累计器的最大秒数。</summary>
    public double MaximumFrameSeconds { get; }
    /// <summary>自创建以来丢弃的完整步数；达到long上限后保持饱和值。</summary>
    public long TotalDroppedSteps { get; private set; }
    /// <summary>自创建以来丢弃的秒数；Reset不会清除此统计。</summary>
    public double TotalDroppedSeconds { get; private set; }
    /// <summary>累计余量与固定步长的比值，供显示姿态插值使用。</summary>
    public float Alpha => (float)Math.Clamp(_accumulator / FixedSeconds, 0, 1);

    /// <summary>接收真实帧秒数并调用固定步回调，返回本次步数与丢时。</summary>
    public FixedStepResult Advance(double frameSeconds, Action<float> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (!double.IsFinite(frameSeconds) || frameSeconds < 0) throw new ArgumentOutOfRangeException(nameof(frameSeconds));
        // 帧时间截断和补步上限分别控制大跳时与积压；丢弃时间不进入模拟。
        var accepted = Math.Min(frameSeconds, MaximumFrameSeconds);
        var discarded = frameSeconds - accepted;
        _accumulator += accepted;
        int steps = 0;
        // 小容差只抵消累计浮点误差；每次回调始终收到相同dt。
        while (steps < MaximumSteps && _accumulator + FixedSeconds * 1e-10 >= FixedSeconds)
        {
            _accumulator = Math.Max(0, _accumulator - FixedSeconds);
            step((float)FixedSeconds);
            steps++;
        }
        // 慢帧仅保留不足一步的余数，防止长期追赶产生“死亡螺旋”。
        var wholeBacklog = (long)Math.Floor((_accumulator + FixedSeconds * 1e-10) / FixedSeconds);
        if (wholeBacklog > 0)
        {
            _accumulator = Math.Max(0, _accumulator - wholeBacklog * FixedSeconds);
            discarded += wholeBacklog * FixedSeconds;
        }
        var droppedSteps = SaturatingSteps(wholeBacklog + Math.Floor((frameSeconds - accepted) / FixedSeconds));
        TotalDroppedSteps = SaturatingSteps((double)TotalDroppedSteps + droppedSteps);
        TotalDroppedSeconds = Math.Min(double.MaxValue, TotalDroppedSeconds + discarded);
        return new FixedStepResult(steps, Alpha, droppedSteps, discarded);
    }

    // 暂停、失焦、加载、Play/Stop后调用；暂停时长不进入下次Advance。
    /// <summary>仅清空时间余量，避免恢复时补算暂停或同步加载耗时。</summary>
    public void Reset() => _accumulator = 0;

    /// <summary>将非负步数限制在long可表示范围，防止长期统计溢出。</summary>
    private static long SaturatingSteps(double value) => value >= long.MaxValue ? long.MaxValue : (long)value;
}
