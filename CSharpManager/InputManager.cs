using System;
using System.Collections.Generic;
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
        public bool IsProgramFocused { get; private set; }
        // public static GamePadButtonEventHandler? GamePadButtonDown { get; set; }

        public IntPtr HWnd { get; private set; }

        private InputManager()
        {
        }

        public static InputManager Instance { get; } = new();

        private void FindMainWindow()
        {
            uint currentProcessId = User32.GetCurrentProcessId();
            StringBuilder stringBuilder = new(64);
            User32.EnumWindows(new User32.EnumWindowsProc((hWnd, lParam) =>
            {
                User32.GetWindowThreadProcessId(hWnd, out uint processId);
                if (processId == User32.GetCurrentProcessId() &&
                    User32.GetClassName(hWnd, stringBuilder, stringBuilder.Capacity) > 0 &&
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
            IsProgramFocused = CheckProgramFocused();
            if (!IsProgramFocused) return;
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

        public bool CheckProgramFocused()
        {
            if (HWnd == IntPtr.Zero)
            {
                Thread.Sleep(1000);
                FindMainWindow();
            }
            var hwnd = User32.GetForegroundWindow();
            if (hwnd == IntPtr.Zero) return false;
            return hwnd == HWnd;
        }

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
