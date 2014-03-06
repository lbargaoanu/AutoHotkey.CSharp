using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;
using WindowsInput.Native;

namespace ScriptCs.AutoHotkey
{
    public sealed class KeyboardHook : IDisposable, IMessageFilter
    {
        private Queue<Action<object>> delayedHandlers = new Queue<Action<object>>();
        private Dictionary<Keys, Action<object>> handlers = new Dictionary<Keys, Action<object>>();
        private readonly KeyboardHookHandler hookHandler;
        private int hookId;
        private int currentHotkeyId;

        public KeyboardHook()
        {
            Application.AddMessageFilter(this);
            hookHandler = HookFunc;
        }

        private void Install()
        {
            if(hookId != 0)
            {
                return;
            }
            using(var process = Process.GetCurrentProcess())
            {
                using(var module = process.MainModule)
                {
                    hookId = SetWindowsHookEx(WH_KEYBOARD_LL, hookHandler, GetModuleHandle(module.ModuleName), 0);
                }
            }
        }

        private void Uninstall()
        {
            if(hookId == 0)
            {
                return;
            }
            Helpers.TraceResult(UnhookWindowsHookEx(hookId), "UnhookWindowsHookEx " + hookId);
            hookId = 0;
        }

        /// <summary>
        /// Default hook call, which analyses pressed keys
        /// </summary>
        private int HookFunc(int nCode, int wParam, KBDLLHOOKSTRUCT keyInfo)
        {
            Trace.WriteLine(keyInfo.vkCode + " " + new Message { Msg = wParam });
            if(nCode >= 0)
            {
                if(IsAltUp(wParam, keyInfo.vkCode))
                {
                    Trace.WriteLine("ALT UP");
                    Uninstall();
                    SynchronizationContext.Current.Post(ExecuteDelayedHandlers, null);
                }
            }
            return CallNextHookEx(0, nCode, wParam, keyInfo);
        }

        private bool IsAltUp(int wParam, VirtualKeyCode vkCode)
        {
            return (wParam == WM_KEYUP || wParam == WM_SYSKEYUP) && (vkCode == VirtualKeyCode.MENU || vkCode == VirtualKeyCode.LMENU || vkCode == VirtualKeyCode.RMENU);
        }

        public bool PreFilterMessage(ref Message m)
        {
            Trace.WriteLine("PreFilterMessage " + m);
            if(m.Msg != WM_HOTKEY)
            {
                if(m.Msg == 0)
                {
                    Trace.WriteLine("Domain unloading...");
                    Thread.CurrentThread.Join();
                }
                return false;
            }            
            OnHotkey(m.LParam);
            return true;
        }

        private static uint TranslateModifiers(IntPtr lParam)
        {
            var inputModifiers = (uint)lParam & Lower16BitsMask;
            uint outputModifers = 0;
            if((inputModifiers & MOD_ALT) == MOD_ALT)
            {
                outputModifers |= (uint)Keys.Alt;
            }
            if((inputModifiers & MOD_CONTROL) == MOD_CONTROL)
            {
                outputModifers |= (uint)Keys.Control;
            }
            if((inputModifiers & MOD_SHIFT) == MOD_SHIFT)
            {
                outputModifers |= (uint)Keys.Shift;
            }
            return outputModifers;
        }

        private void OnHotkey(IntPtr lParam)
        {
            var modifiers = TranslateModifiers(lParam);
            var keys = modifiers + (((int)lParam >> 16) & Lower16BitsMask);
            Action<object> handler;
            if(!handlers.TryGetValue((Keys) keys, out handler))
            {
                return;
            }
            if((modifiers & (uint)Keys.Alt) != 0)
            {
                delayedHandlers.Enqueue(handler);
                Install();
                return;
            }
            Execute(handler);
        }

        private void ExecuteDelayedHandlers(object _)
        {
            while(delayedHandlers.Count > 0)
            {
                Execute(delayedHandlers.Dequeue());
            }
        }

        private static void Execute(Action<object> handler)
        {
            try
            {
                handler(null);
            }
            catch(Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        public void RegisterHotkey(Keys hotkey, Action<object> handler)
        {
            uint modifiers = TranslateModifiers(hotkey) | MOD_NOREPEAT;
            Helpers.CheckResult(RegisterHotKey(IntPtr.Zero, currentHotkeyId + 1, modifiers, Lower16BitsMask & (uint)hotkey), "RegisterHotKey " + hotkey);
            currentHotkeyId++;
            handlers.Add(hotkey, handler);
        }

        private static uint TranslateModifiers(Keys hotkey)
        {
            uint modifiers = 0;
            if((hotkey & Keys.Alt) == Keys.Alt)
            {
                modifiers |= MOD_ALT;
            }
            if((hotkey & Keys.Control) == Keys.Control)
            {
                modifiers |= MOD_CONTROL;
            }
            if((hotkey & Keys.Shift) == Keys.Shift)
            {
                modifiers |= MOD_SHIFT;
            }
            return modifiers;
        }

        public void Dispose()
        {
            Uninstall();
            Application.RemoveMessageFilter(this);
            for(int index = currentHotkeyId; index > 0; index--)
            {
                Helpers.TraceResult(UnregisterHotKey(IntPtr.Zero, index), "UnregisterHotKey");
            }
        }

        /// <summary>
        /// Low-Level function declarations
        /// </summary>
        private const int WM_KEYDOWN = 0x100;
        private const int WM_SYSKEYDOWN = 0x104;
        private const int WM_KEYUP = 0x101;
        private const int WM_SYSKEYUP = 0x105;
        private const int WH_KEYBOARD_LL = 13;
        private const uint Lower16BitsMask = 0xFFFF;
        private const uint MOD_ALT = 0x1;
        private const uint MOD_CONTROL = 0x2;
        private const uint MOD_SHIFT = 0x4;
        private const uint MOD_NOREPEAT = 0x4000;
        private const int WM_HOTKEY = 0x0312;

        [StructLayout(LayoutKind.Sequential)]
        public class KBDLLHOOKSTRUCT
        {
            public VirtualKeyCode vkCode;
            public uint scanCode;
            public HookFlags flags;
            public uint time;
            public UIntPtr dwExtraInfo;
        }

        [Flags]
        public enum HookFlags : uint
        {
            LLKHF_EXTENDED = 0x01,
            LLKHF_INJECTED = 0x10,
            LLKHF_ALTDOWN = 0x20,
            LLKHF_UP = 0x80,
        }

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool SetKeyboardState([In]byte[] keyboardState);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int SetWindowsHookEx(int idHook, KeyboardHookHandler lpfn, IntPtr hMod, uint dwThreadId);
        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern bool UnhookWindowsHookEx(int hhk);
        [DllImport("user32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern int CallNextHookEx(int hhk, int nCode, int wParam, KBDLLHOOKSTRUCT lParam);
        [DllImport("kernel32.dll", CharSet = CharSet.Auto, SetLastError = true)]
        private static extern IntPtr GetModuleHandle(string lpModuleName);
        private delegate int KeyboardHookHandler(int nCode, int wParam, KBDLLHOOKSTRUCT lParam);
    }
}