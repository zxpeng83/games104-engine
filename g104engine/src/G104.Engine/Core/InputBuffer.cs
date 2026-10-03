using OpenTK.Mathematics;

namespace G104.Engine.Core;

public readonly record struct InputState(Vector2 Move, Vector2 LookDelta, bool JumpPressed = false, bool InteractPressed = false,
    bool Focused = true, bool CaptureKeyboard = false, bool CaptureMouse = false, bool Sprint = false);

public readonly record struct SimulationInput(Vector2 Move, bool JumpPressed, bool InteractPressed, bool Sprint);

// Push是每显示帧采集；按住态持续，边沿由一次有效模拟步消费，Look由显示帧单独消费。
public sealed class InputBuffer
{
    private Vector2 _move;
    private Vector2 _look;
    private bool _jump;
    private bool _interact;
    private bool _sprint;

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
            var moveSquared = (double)state.Move.X * state.Move.X + (double)state.Move.Y * state.Move.Y;
            _move = moveSquared > 1 ? new Vector2((float)(state.Move.X / Math.Sqrt(moveSquared)), (float)(state.Move.Y / Math.Sqrt(moveSquared))) : state.Move;
            _sprint = state.Sprint;
            _jump |= state.JumpPressed;
            _interact |= state.InteractPressed;
        }
        if (state.CaptureMouse) _look = Vector2.Zero;
        else _look = accumulatedLook;
    }

    public SimulationInput Consume()
    {
        var result = new SimulationInput(_move, _jump, _interact, _sprint);
        _jump = _interact = false;
        return result;
    }

    public Vector2 ConsumeLook()
    {
        var result = _look;
        _look = Vector2.Zero;
        return result;
    }

    public void Clear()
    {
        _move = _look = Vector2.Zero;
        _jump = _interact = _sprint = false;
    }
}
