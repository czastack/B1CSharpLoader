using System;
using CSharpModBase.Input;
using UnrealEngine.Runtime;

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

        public static HotKeyItem RegisterGamePadBind(GamePadButton button, Action action)
        {
            return InputManager!.RegisterGamePadBind(button, action);
        }

        public static void TryRun(Action aciton)
        {
            try
            {
                aciton();
            }
            catch (Exception e)
            {
                Log.Error(e);
            };
        }

        public static void TryRunOnGameThread(Action aciton)
        {
            FThreading.RunOnGameThread(() =>
            {
                try
                {
                    aciton();
                }
                catch (Exception e)
                {
                    Log.Error(e);
                }
            });
        }
    }
}
