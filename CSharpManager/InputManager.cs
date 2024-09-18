using System.Diagnostics;
using System.Runtime.InteropServices;
using CSharpModBase.Input;

namespace CSharpManager
{
    public class InputManager : IInputManager
    {
        public List<HotKeyItem> BuiltinHotKeyItems { get; } = new();
        public List<HotKeyItem> HotKeyItems { get; } = new();

        private IntPtr HWnd = IntPtr.Zero;

        public InputManager()
        {
            var process = Process.GetCurrentProcess();
            HWnd = process.MainWindowHandle;
        }

        public void Update()
        {
            if (!IsProgramFocused()) return;
            var now = DateTime.Now.Ticks / 10000;
            var modifiers = KeyUtils.Modifiers;
            foreach (var item in BuiltinHotKeyItems)
            {
                if (item.Modifiers == modifiers && KeyUtils.IsKeyDown(item.Key))
                {
                    if (item.IsPressed && (item.RepeatMs == 0 || item.LastTriggerMs > 0 && now - item.LastTriggerMs < item.RepeatMs))
                    {
                        continue;
                    }
                    item.IsPressed = true;
                    item.Action();
                }
                else if (item.IsPressed)
                {
                    item.IsPressed = false;
                }
            }
            foreach (var item in HotKeyItems)
            {
                if (item.Modifiers == modifiers && KeyUtils.IsKeyDown(item.Key))
                {
                    if (item.IsPressed && (item.RepeatMs == 0 || item.LastTriggerMs > 0 && now - item.LastTriggerMs < item.RepeatMs))
                    {
                        continue;
                    }
                    item.IsPressed = true;
                    item.Action();
                }
                else if (item.IsPressed)
                {
                    item.IsPressed = false;
                }
            }
        }

        public void Clear()
        {
            HotKeyItems.Clear();
        }

        public bool IsProgramFocused()
        {
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;
            return hwnd == HWnd;
        }

        [DllImport("user32.dll")]
        private extern static IntPtr GetForegroundWindow();

        public HotKeyItem RegisterBuiltinKeyBind(Key key, Action action)
        {
            var item = new HotKeyItem(ModifierKeys.None, key, action);
            BuiltinHotKeyItems.Add(item);
            return item;
        }

        public HotKeyItem RegisterBuiltinKeyBind(ModifierKeys modifiers, Key key, Action action)
        {
            var item = new HotKeyItem(modifiers, key, action);
            BuiltinHotKeyItems.Add(item);
            return item;
        }

        public HotKeyItem RegisterKeyBind(Key key, Action action)
        {
            var item = new HotKeyItem(ModifierKeys.None, key, action);
            HotKeyItems.Add(item);
            return item;
        }

        public HotKeyItem RegisterKeyBind(ModifierKeys modifiers, Key key, Action action)
        {
            var item = new HotKeyItem(modifiers, key, action);
            HotKeyItems.Add(item);
            return item;
        }
    }
}
