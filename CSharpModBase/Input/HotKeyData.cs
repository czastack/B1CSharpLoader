namespace CSharpModBase.Input;

public class HotKeyData(ModifierKeys modifiers, Key key)
{
    public ModifierKeys Modifiers { get; set; } = modifiers;
    public Key Key { get; set; } = key;
    public string KeyString => KeyUtils.KeyToString(Modifiers, Key);
    public int Code => GetCode(Modifiers, (int)Key);

    public HotKeyData() : this(ModifierKeys.None, Key.None)
    {
    }

    public static int GetCode(ModifierKeys modifiers, int vk) => ((int)modifiers << 16) | vk;

    public static int GetCode(ModifierKeys modifiers, Key key) => ((int)modifiers << 16) | (int)key;


    public void SetFromCode(int code)
    {
        Modifiers = (ModifierKeys)(code >> 16);
        Key = (Key)(code & 0xFF);
    }

    public void SetKey(ModifierKeys modifiers, Key key)
    {
        Modifiers = modifiers;
        Key = key;
    }

    public override string ToString() => KeyString;
}