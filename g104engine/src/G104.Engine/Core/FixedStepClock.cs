namespace G104.Engine.Core;

public readonly record struct FixedStepResult(int Steps, float Alpha, long DroppedSteps, double DroppedSeconds);

public sealed class FixedStepClock
{
    private double _accumulator;
    public FixedStepClock(double fixedSeconds = 1.0 / 60.0, int maximumSteps = 5, double maximumFrameSeconds = 0.25)
    {
        if (!double.IsFinite(fixedSeconds) || fixedSeconds <= 0 || maximumSteps <= 0 || !double.IsFinite(maximumFrameSeconds) || maximumFrameSeconds < fixedSeconds)
            throw new ArgumentOutOfRangeException(nameof(fixedSeconds));
        FixedSeconds = fixedSeconds;
        MaximumSteps = maximumSteps;
        MaximumFrameSeconds = maximumFrameSeconds;
    }

    public double FixedSeconds { get; }
    public int MaximumSteps { get; }
    public double MaximumFrameSeconds { get; }
    public long TotalDroppedSteps { get; private set; }
    public double TotalDroppedSeconds { get; private set; }
    public float Alpha => (float)Math.Clamp(_accumulator / FixedSeconds, 0, 1);

    public FixedStepResult Advance(double frameSeconds, Action<float> step)
    {
        ArgumentNullException.ThrowIfNull(step);
        if (!double.IsFinite(frameSeconds) || frameSeconds < 0) throw new ArgumentOutOfRangeException(nameof(frameSeconds));
        var accepted = Math.Min(frameSeconds, MaximumFrameSeconds);
        var discarded = frameSeconds - accepted;
        _accumulator += accepted;
        int steps = 0;
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
    public void Reset() => _accumulator = 0;

    private static long SaturatingSteps(double value) => value >= long.MaxValue ? long.MaxValue : (long)value;
}
