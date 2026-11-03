using OpenTK.Mathematics;

namespace G104.Engine.Core;

/// <summary>显示帧采集的原始输入；焦点和UI捕获决定哪些设备数据可进入模拟。</summary>
/// <param name="Move">相机相对移动意图，X为右、Y为前；长度在缓存时限制到1。</param>
/// <param name="LookDelta">本显示帧鼠标相对位移，供相机单独消费。</param>
/// <param name="JumpPressed">本帧新按下跳跃键的边沿。</param>
/// <param name="InteractPressed">本帧新按下交互键的边沿。</param>
/// <param name="Focused">窗口是否允许接收输入；失焦清空全部缓存。</param>
/// <param name="CaptureKeyboard">UI是否占用键盘；占用时清空角色键盘输入。</param>
/// <param name="CaptureMouse">UI是否占用鼠标；占用时清空视角增量。</param>
/// <param name="Sprint">冲刺键当前是否按住。</param>
public readonly record struct InputState(Vector2 Move, Vector2 LookDelta, bool JumpPressed = false, bool InteractPressed = false,
    bool Focused = true, bool CaptureKeyboard = false, bool CaptureMouse = false, bool Sprint = false);

/// <summary>交给一个固定模拟步的输入；边沿只在首次消费时有效。</summary>
/// <param name="Move">持续移动意图，长度不超过1。</param>
/// <param name="JumpPressed">本步可消费的一次跳跃请求。</param>
/// <param name="InteractPressed">本步可消费的一次交互请求。</param>
/// <param name="Sprint">当前持续的冲刺按住态。</param>
public readonly record struct SimulationInput(Vector2 Move, bool JumpPressed, bool InteractPressed, bool Sprint);

// Push是每显示帧采集；按住态持续，边沿由一次有效模拟步消费，Look由显示帧单独消费。
/// <summary>连接显示帧采集与固定步模拟，避免零步丢键和补步重复触发。</summary>
public sealed class InputBuffer
{
    /// <summary>最近一次有效采集的持续移动意图。</summary>
    private Vector2 _move;
    /// <summary>尚未由显示帧消费的鼠标位移总量。</summary>
    private Vector2 _look;
    /// <summary>等待下一有效模拟步消费的跳跃边沿。</summary>
    private bool _jump;
    /// <summary>等待下一有效模拟步消费的交互边沿。</summary>
    private bool _interact;
    /// <summary>最近一次有效采集的冲刺按住态。</summary>
    private bool _sprint;

    /// <summary>每显示帧写入输入，保留未消费边沿并处理焦点与UI捕获。</summary>
    public void Push(InputState state)
    {
        if (!float.IsFinite(state.Move.X) || !float.IsFinite(state.Move.Y) || !float.IsFinite(state.LookDelta.X) || !float.IsFinite(state.LookDelta.Y))
            throw new ArgumentOutOfRangeException(nameof(state));
        if (!state.Focused) { Clear(); return; }
        var accumulatedLook = _look + state.LookDelta;
        if (!state.CaptureMouse && (!float.IsFinite(accumulatedLook.X) || !float.IsFinite(accumulatedLook.Y))) throw new ArgumentOutOfRangeException(nameof(state), "Accumulated mouse delta overflowed.");
        if (state.CaptureKeyboard)
        {
            _move = Vector2.Zero;
            _sprint = _jump = _interact = false;
        }
        else
        {
            // 用double计算长度，避免有限但极大的float输入在平方时溢出。
            var moveSquared = (double)state.Move.X * state.Move.X + (double)state.Move.Y * state.Move.Y;
            _move = moveSquared > 1 ? new Vector2((float)(state.Move.X / Math.Sqrt(moveSquared)), (float)(state.Move.Y / Math.Sqrt(moveSquared))) : state.Move;
            _sprint = state.Sprint;
            // 零模拟步期间边沿仍保留；后续补步只会消费一次。
            _jump |= state.JumpPressed;
            _interact |= state.InteractPressed;
        }
        if (state.CaptureMouse) _look = Vector2.Zero;
        else _look = accumulatedLook;
    }

    /// <summary>取出本模拟步输入，仅清除跳跃与交互边沿，保留按住态。</summary>
    public SimulationInput Consume()
    {
        var result = new SimulationInput(_move, _jump, _interact, _sprint);
        _jump = _interact = false;
        return result;
    }

    /// <summary>每显示帧取出并清零视角增量，避免补步使鼠标转动倍增。</summary>
    public Vector2 ConsumeLook()
    {
        var result = _look;
        _look = Vector2.Zero;
        return result;
    }

    /// <summary>清除所有持续态与待消费量，用于失焦、暂停和场景切换。</summary>
    public void Clear()
    {
        _move = _look = Vector2.Zero;
        _jump = _interact = _sprint = false;
    }
}
