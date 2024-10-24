using SharpDX.XInput;

namespace CSharpModBase.Input;

[Flags]
public enum GamePadButton
{
    None = GamepadButtonFlags.None,
    DPadUp = GamepadButtonFlags.DPadUp,
    DPadDown = GamepadButtonFlags.DPadDown,
    DPadLeft = GamepadButtonFlags.DPadLeft,
    DPadRight = GamepadButtonFlags.DPadRight,
    Start = GamepadButtonFlags.Start,
    Back = GamepadButtonFlags.Back,
    LeftThumb = GamepadButtonFlags.LeftThumb,
    RightThumb = GamepadButtonFlags.RightThumb,
    LeftShoulder = GamepadButtonFlags.LeftShoulder,
    RightShoulder = GamepadButtonFlags.RightShoulder,
    A = GamepadButtonFlags.A,
    B = GamepadButtonFlags.B,
    X = GamepadButtonFlags.X,
    Y = GamepadButtonFlags.Y,
    LeftTrigger = 0x10000,
    RightTrigger = 0x20000,
    DPadRightUp = DPadRight | DPadUp,
    DPadLeftDown = DPadLeft | DPadDown,
    DPadRightDown = DPadRight | DPadDown,
    DPadLeftUp = DPadLeft | DPadUp
}

public sealed class GamePadButtonEvent : EventArgs
{
    public GamePadButton Button { get; set; }
}

public delegate void GamePadButtonEventHandler(GamePadButtonEvent e);