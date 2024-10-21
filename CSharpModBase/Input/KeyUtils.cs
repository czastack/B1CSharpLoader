using System.Runtime.InteropServices;

namespace CSharpModBase.Input;

public static class KeyUtils
{
    public static ModifierKeys Modifiers
    {
        get
        {
            var modifiers = ModifierKeys.None;
            if (GetKeyState((uint)Key.MENU) < 0)
            {
                modifiers |= ModifierKeys.Alt;
            }

            if (GetKeyState((uint)Key.CONTROL) < 0)
            {
                modifiers |= ModifierKeys.Control;
            }

            if (GetKeyState((uint)Key.SHIFT) < 0)
            {
                modifiers |= ModifierKeys.Shift;
            }

            return modifiers;
        }
    }

    /// <summary>
    ///     Keys转字符串，支持组合键
    /// </summary>
    /// <param name="modifiers"></param>
    /// <param name="key"></param>
    /// <param name="buf"></param>
    /// <returns></returns>
    public static string KeyToString(ModifierKeys modifiers, Key key, List<string> buf)
    {
        if (modifiers.HasFlag(ModifierKeys.Control))
        {
            buf.Add("Ctrl");
        }

        if (modifiers.HasFlag(ModifierKeys.Shift))
        {
            buf.Add("Shift");
        }

        if (modifiers.HasFlag(ModifierKeys.Alt))
        {
            buf.Add("Alt");
        }

        string? keyCodeStr;
        var oemKeyChar = OemKeyChar(key);
        if (oemKeyChar != default)
        {
            keyCodeStr = oemKeyChar.ToString();
        }
        else
        {
            keyCodeStr = key switch
            {
                Key.None => "",
                _ => key.ToString()
            };
        }

        if (keyCodeStr != null)
        {
            buf.Add(keyCodeStr);
        }

        return string.Join("+", buf);
    }

    public static string KeyToString(ModifierKeys modifiers, Key key) => KeyToString(modifiers, key, new List<string>());

    public static char OemKeyChar(Key key)
    {
        return key switch
        {
            Key.OEM_PLUS => '+',
            Key.OEM_COMMA => ',',
            Key.OEM_MINUS => '-',
            Key.OEM_PERIOD => '.',
            Key.OEM_1 => ';',
            Key.OEM_2 => '/',
            Key.OEM_3 => '`',
            Key.OEM_4 => '[',
            Key.OEM_5 => '\\',
            Key.OEM_6 => ']',
            Key.OEM_7 => '\'',
            _ => default
        };
    }

    [DllImport("user32.dll")]
    private static extern short GetKeyState(uint vk);

    public static bool IsKeyDown(Key key) => GetKeyState((uint)key) < 0;
}