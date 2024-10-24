using CSharpModBase.Input;
using UnrealEngine.Runtime;

namespace CSharpModBase;

public static class Utils
{
    private static IInputManager? _inputManager;

    public static void InitInputManager(IInputManager inputManager)
    {
        _inputManager = inputManager;
    }

    public static HotKeyItem RegisterKeyBind(Key key, Action action) => _inputManager!.RegisterKeyBind(key, action);

    public static HotKeyItem RegisterKeyBind(ModifierKeys modifiers, Key key, Action action) => _inputManager!.RegisterKeyBind(modifiers, key, action);

    public static HotKeyItem RegisterGamePadBind(GamePadButton button, Action action) => _inputManager!.RegisterGamePadBind(button, action);

    public static void TryRun(Action action)
    {
        try
        {
            action();
        }
        catch (Exception e)
        {
            Log.Error(e);
        }
    }

    public static void TryRunOnGameThread(Action action)
    {
        FThreading.RunOnGameThread(() =>
        {
            try
            {
                action();
            }
            catch (Exception e)
            {
                Log.Error(e);
            }
        });
    }
}