using CSharpModBase.Input;

namespace CSharpModBase
{
    public static class CSharpLoaderInternal
    {
        internal static ICSharpModManager? CSharpModManager;
        internal static IInputManager? InputManager;

        public static void InitModManager(ICSharpModManager modManager)
        {
            CSharpModManager = modManager;
        }

        public static void InitInputManager(IInputManager inputManager)
        {
            InputManager = inputManager;
        }
    }
}
