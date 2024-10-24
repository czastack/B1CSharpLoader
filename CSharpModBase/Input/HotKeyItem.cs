namespace CSharpModBase.Input;

public class HotKeyItem : HotKeyData
{
    public string Label { get; set; }
    public Action Action { get; set; }

    /// <summary>
    ///     0为不支持连按，大于0为连按触发毫秒间隔
    /// </summary>
    public int RepeatMs { get; set; }

    /// <summary>
    ///     上一次触发的时间，毫秒
    /// </summary>
    public long LastTriggerMs { get; set; }

    /// <summary>
    ///     正在被按下
    /// </summary>
    public bool IsPressed { get; set; }

    public bool RunOnGameThread { get; set; } = true;

    public HotKeyItem(ModifierKeys modifiers, Key key, Action action) : this("", modifiers, key, action)
    {
    }

    public HotKeyItem(string label, ModifierKeys modifiers, Key key, Action action) : base(modifiers, key)
    {
        Label = label;
        Action = action;
    }

    public HotKeyItem() : this("", ModifierKeys.None, Key.None, EmptyAction)
    {
    }

    public HotKeyItem WithKey(ModifierKeys modifiers, Key key) => new(Label, modifiers, key, Action);

    private static void EmptyAction()
    {
    }
}