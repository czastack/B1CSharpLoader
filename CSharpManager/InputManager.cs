using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading;
using CSharpModBase;
using CSharpModBase.Input;

namespace CSharpManager
{
    public class InputManager : IInputManager
    {
        public List<HotKeyItem> BuiltinHotKeyItems { get; } = new();
        public List<HotKeyItem> HotKeyItems { get; } = new();
        public bool EnableGamePad { get; set; } = true;
        public static GamePadButton CurrentGamePadButton { get; set; }
        // public static GamePadButtonEventHandler? GamePadButtonDown { get; set; }

        private IntPtr HWnd;

        public InputManager()
        {
        }

        private void FindMainWindow()
        {
            uint currentProcessId = GetCurrentProcessId();
            StringBuilder stringBuilder = new(64);
            EnumWindows(new EnumWindowsProc((hWnd, lParam) =>
            {
                GetWindowThreadProcessId(hWnd, out uint processId);
                if (processId == GetCurrentProcessId() &&
                    GetClassName(hWnd, stringBuilder, stringBuilder.Capacity) > 0 &&
                    stringBuilder.ToString() == "UnrealWindow")
                {
                    HWnd = hWnd;
                    return false;
                }
                return true; // 继续枚举
            }), IntPtr.Zero);
        }

        private void HandleKeys(List<HotKeyItem> items)
        {
            var now = DateTime.Now.Ticks / 10000;
            var currentModifiers = KeyUtils.Modifiers;

            var matchedItems = new List<HotKeyItem>();

            // 统计可能按下的按键
            foreach (var item in items)
            {
                // 情况：组合键单独按下，多个组合键按下，多个组合键+普通键按下
                bool keyboardMatch = item.Modifiers == currentModifiers && ((item.Key == Key.None && currentModifiers != ModifierKeys.None) || KeyUtils.IsKeyDown(item.Key));
                // 任意多个手柄按键按下
                bool gamepadMatch = CurrentGamePadButton != GamePadButton.None && item.GamePadButton != GamePadButton.None && (CurrentGamePadButton & item.GamePadButton) == item.GamePadButton;

                if (keyboardMatch || gamepadMatch)
                {
                    matchedItems.Add(item);
                }
                else if (item.IsPressed)
                {
                    item.IsPressed = false;
                }
            }

            if (matchedItems.Count > 0)
            {
                // 获取最高复杂度
                int maxComplexity = KeyUtils.CountModifiers(matchedItems[0].Modifiers) +
                              KeyUtils.CountGamePadButtons(matchedItems[0].GamePadButton);

                foreach (var item in matchedItems)
                {
                    // 获取当前复杂度
                    int currentComplexity = KeyUtils.CountModifiers(item.Modifiers) +
                                      KeyUtils.CountGamePadButtons(item.GamePadButton);

                    // 比最高复杂度低的，忽略掉
                    if (currentComplexity < maxComplexity)
                    {
                        break;
                    }

                    bool shouldTrigger = false;
                    if (!item.IsPressed ||
                       (item.RepeatMs > 0 && item.LastTriggerMs > 0 &&
                        now - item.LastTriggerMs >= item.RepeatMs))
                    {
                        shouldTrigger = true;
                        item.IsPressed = true;
                        item.LastTriggerMs = now;
                    }

                    if (shouldTrigger)
                    {
                        if (item.RunOnGameThread)
                        {
                            Utils.TryRunOnGameThread(item.Action);
                        }
                        else
                        {
                            Utils.TryRun(item.Action);
                        }
                    }
                }
            }
        }

        public void Update()
        {
            if (!IsProgramFocused()) return;
            if (EnableGamePad)
            {
                GamePadUtils.GetGamePadButtons(out var buttons);
                CurrentGamePadButton = buttons;
            }
            HandleKeys(BuiltinHotKeyItems);
            lock (HotKeyItems)
            {
                HandleKeys(HotKeyItems);
            }
        }

        public void Clear()
        {
            lock (HotKeyItems)
            {
                HotKeyItems.Clear();
            }
        }

        public bool IsProgramFocused()
        {
            if (HWnd == IntPtr.Zero)
            {
                Thread.Sleep(1000);
                FindMainWindow();
            }
            var hwnd = GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;
            return hwnd == HWnd;
        }

        [DllImport("user32.dll")]
        private extern static IntPtr GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(IntPtr hWnd, out uint lpdwProcessId);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentProcessId();

        private delegate bool EnumWindowsProc(IntPtr hWnd, IntPtr lParam);

        // 导入Win32 API函数
        [DllImport("user32.dll", SetLastError = true, CharSet = CharSet.Auto, CallingConvention = CallingConvention.StdCall)]
        private static extern bool EnumWindows(EnumWindowsProc lpEnumFunc, IntPtr lParam);

        [DllImport("user32.dll", CharSet = CharSet.Unicode)]
        public static extern int GetClassName(IntPtr hWnd, StringBuilder lpClassName, int nMaxCount);

        public HotKeyItem RegisterBuiltinKeyBind(Key key, Action action)
        {
            var item = new HotKeyItem(ModifierKeys.None, key, action);
            BuiltinHotKeyItems.Add(item);
            SortHotKeys(BuiltinHotKeyItems);
            return item;
        }

        public HotKeyItem RegisterBuiltinKeyBind(ModifierKeys modifiers, Key key, Action action)
        {
            var item = new HotKeyItem(modifiers, key, action);
            BuiltinHotKeyItems.Add(item);
            SortHotKeys(BuiltinHotKeyItems);
            return item;
        }

        private void SortHotKeys(List<HotKeyItem> items)
        {
            items.Sort((a, b) =>
            {
                int aComplexity = KeyUtils.CountModifiers(a.Modifiers) + KeyUtils.CountGamePadButtons(a.GamePadButton);
                int bComplexity = KeyUtils.CountModifiers(b.Modifiers) + KeyUtils.CountGamePadButtons(b.GamePadButton);
                return bComplexity.CompareTo(aComplexity);
            });
        }

        public void RegisterKeyBind(HotKeyItem item)
        {
            lock (HotKeyItems)
            {
                HotKeyItems.Add(item);
                SortHotKeys(HotKeyItems);
            }
        }

        public HotKeyItem RegisterKeyBind(Key key, Action action)
        {
            var item = new HotKeyItem(ModifierKeys.None, key, action);
            RegisterKeyBind(item);
            return item;
        }

        public HotKeyItem RegisterKeyBind(ModifierKeys modifiers, Key key, Action action)
        {
            var item = new HotKeyItem(modifiers, key, action);
            RegisterKeyBind(item);
            return item;
        }

        public HotKeyItem RegisterGamePadBind(GamePadButton button, Action action)
        {
            var item = new HotKeyItem(ModifierKeys.None, Key.None, action)
            {
                GamePadButton = button
            };
            RegisterKeyBind(item);
            return item;
        }
    }
}
