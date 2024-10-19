using CSharpModBase.Input;
using SharpDX.XInput;

namespace CSharpModBase.Utils;

public static class GamePadUtils
{
    private static readonly Controller Controller = new(UserIndex.One);

    public static bool GetGamePadButtons(out GamePadButton flags)
    {
        flags = GamePadButton.None;
        if (Controller.GetState(out var state))
        {
            var gamepad = state.Gamepad;
            flags = (GamePadButton)gamepad.Buttons;
            if (gamepad.LeftTrigger > 100)
            {
                flags |= GamePadButton.LeftTrigger;
            }

            if (gamepad.RightTrigger > 100)
            {
                flags |= GamePadButton.RightTrigger;
            }

            return true;
        }

        return false;
    }
}