using System;

namespace CSharpModBase.Input
{
    public interface IInputManager
    {
        HotKeyItem RegisterKeyBind(Key key, Action action);

        HotKeyItem RegisterKeyBind(ModifierKeys modifiers, Key key, Action action);
        HotKeyItem RegisterGamePadBind(GamePadButton button, Action action);
    }
}
