using CSharpModBase.Input;

namespace CSharpModBase
{
    public static class Utils
    {
        private static IInputManager? InputManager;

        public static void InitInputManager(IInputManager inputManager)
        {
            InputManager = inputManager;
        }

        public static HotKeyItem RegisterKeyBind(Key key, Action action)
        {
            return InputManager!.RegisterKeyBind(key, action);
        }

        public static HotKeyItem RegisterKeyBind(ModifierKeys modifiers, Key key, Action action)
        {
            return InputManager!.RegisterKeyBind(modifiers, key, action);
        }
    }
}
