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

        public static void RegisterKeyBind(Key key, Action action)
        {
            InputManager?.RegisterKeyBind(key, action);
        }

        public static void RegisterKeyBind(ModifierKeys modifiers, Key key, Action action)
        {
            InputManager?.RegisterKeyBind(modifiers, key, action);
        }
    }
}
