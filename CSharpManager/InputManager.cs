using System;
using System.Collections.Generic;
using System.Diagnostics;
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
            var modifiers = KeyUtils.Modifiers;
            foreach (var item in items)
            {
                if (item.Modifiers == modifiers && KeyUtils.IsKeyDown(item.Key) ||
                    CurrentGamePadButton != GamePadButton.None && item.GamePadButton != GamePadButton.None && CurrentGamePadButton.HasFlag(item.GamePadButton))
                {
                    if (item.IsPressed && (item.RepeatMs == 0 || item.LastTriggerMs > 0 && now - item.LastTriggerMs < item.RepeatMs))
                    {
                        continue;
                    }
                    item.IsPressed = true;
                    if (item.RunOnGameThread)
                    {
                        Utils.TryRunOnGameThread(item.Action);
                    }
                    else
                    {
                        Utils.TryRun(item.Action);
                    }
                }
                else if (item.IsPressed)
                {
                    item.IsPressed = false;
                }
            }
        }

        public void Update()
        {
            if (!IsProgramFocused()) return;
            if (EnableGamePad)
            {
                GamePadUtils.GetGamePadButtons(out var buttons);
                // if (buttons != GamePadButton.None)
                // {
                //     GamePadButtonDown?.Invoke(new GamePadButtonEvent { Button = buttons });
                // }
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
            return item;
        }

        public HotKeyItem RegisterBuiltinKeyBind(ModifierKeys modifiers, Key key, Action action)
        {
            var item = new HotKeyItem(modifiers, key, action);
            BuiltinHotKeyItems.Add(item);
            return item;
        }

        public void RegisterKeyBind(HotKeyItem item)
        {
            lock (HotKeyItems)
            {
                HotKeyItems.Add(item);
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
