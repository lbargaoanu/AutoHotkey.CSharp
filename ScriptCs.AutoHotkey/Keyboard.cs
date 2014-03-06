using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace ScriptCs.AutoHotkey
{
    public interface IKeyboard : IService
    {
        void RegisterHotkeys(Hotkeys hotkeys);
        void RegisterHotkey(Keys hotkey, Action<object> handler);
        void Send(params Keys[] keys);
        void SendModified(Keys modifiers, params Keys[] keys);
    }
    
    [Export(typeof(IKeyboard))]
    public sealed class Keyboard : IKeyboard
    {
        private KeyboardHook keyboardHook = new KeyboardHook();
        private InputSimulator inputSimulator = new InputSimulator();

        void IDisposable.Dispose()
        {
            keyboardHook.Dispose();
        }

        public void RegisterHotkeys(Hotkeys hotkeys)
        {
            foreach(var hotkey in hotkeys)
            {
                RegisterHotkey(hotkey.Key, hotkey.Value);
            }
        }

        public void RegisterHotkey(Keys hotkey, Action<object> handler)
        {
            keyboardHook.RegisterHotkey(hotkey, handler);
        }

        public void SendModified(Keys modifiers, params Keys[] keys)
        {
            var modifersVK = GetModifiersVK(modifiers).ToArray();
            var keysVK = keys.AsVirtualKeyCodes();
            inputSimulator.Keyboard.ModifiedKeyStroke(modifersVK, keysVK);
        }

        private IEnumerable<VirtualKeyCode> GetModifiersVK(Keys modifiers)
        {
            if((modifiers & Keys.Control) != 0)
            {
                yield return VirtualKeyCode.CONTROL;
            }
            if((modifiers & Keys.Shift) != 0)
            {
                yield return VirtualKeyCode.SHIFT;
            }
            if((modifiers & Keys.Alt) != 0)
            {
                yield return VirtualKeyCode.MENU;
            }
        }

        public void Send(params Keys[] keys)
        {
            if(keys.Length == 1)
            {
                SendModified(keys[0], keys[0] & Keys.KeyCode);
                return;
            }
            inputSimulator.Keyboard.KeyPress(keys.AsVirtualKeyCodes());
        }
    }

    public static class Helpers
    {
        [DllImport("user32.dll")]
        static extern uint GetForegroundWindow();

        [DllImport("user32.dll", SetLastError = true)]
        static extern uint GetWindowThreadProcessId(uint hWnd, int processId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool AttachThreadInput(uint idAttach, uint idAttachTo, bool attach);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool GetKeyboardState([Out]byte[] keyboardState);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool BlockInput([In, MarshalAs(UnmanagedType.Bool)] bool block);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool GetGUIThreadInfo(uint idThread, ref GuiThreadInfo lpgui);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", EntryPoint = "PostMessage", SetLastError = true)]
        private static extern bool PostMessageNative(uint hWnd, uint Msg, uint wParam, uint lParam);

        [DllImport("user32.dll", SetLastError = true)]
        public static extern int SendMessage(uint hWnd, uint Msg, uint wParam, uint lParam);

        [DllImport("kernel32.dll")]
        public static extern uint GetCurrentThreadId();

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostThreadMessage(uint threadId, uint msg, uint wParam, uint lParam);

        public static void PostMessage(uint hWnd, uint Msg, uint wParam, uint lParam)
        {
            CheckResult(PostMessageNative(hWnd, Msg, wParam, lParam), "PostMessage");
        }

        public static VirtualKeyCode[] AsVirtualKeyCodes(this Keys[] keys)
        {
            return Array.ConvertAll(keys, key => (VirtualKeyCode) key);
        }

        public static void CheckResult(IntPtr result, string message)
        {
            CheckResult(result != IntPtr.Zero, message);
        }

        public static void TraceResult(IntPtr result, string message)
        {
            TraceResult(result != IntPtr.Zero, message);
        }

        public static void TraceResult(bool result, string message)
        {
            if(result)
            {
                return;
            }
            Trace.WriteLine(message);
            Trace.WriteLine(new Win32Exception());
        }

        public static void CheckResult(bool result, string message)
        {
            if(result)
            {
                return;
            }
            Trace.WriteLine(message);
            throw new Win32Exception();
        }
    }

    public class Hotkeys : Dictionary<Keys, Action<object>>
    {
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct Rect
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal struct GuiThreadInfo
    {
        public int cbSize;
        public uint flags;
        public uint hwndActive;
        public uint hwndFocus;
        public uint hwndCapture;
        public uint hwndMenuOwner;
        public uint hwndMoveSize;
        public uint hwndCaret;
        public Rect rcCaret;
    }
}
//var foregroundWindow = GetForegroundWindow();
//if(foregroundWindow == 0)
//{
//    Trace.WriteLine("No focused window.");
//    action();
//    return;
//}
//var threadId = GetWindowThreadProcessId(foregroundWindow, 0);
//Helpers.CheckResult(0 != threadId, "GetWindowThreadProcessId");
//var guiTheadInfo = new GuiThreadInfo { cbSize = Marshal.SizeOf(typeof(GuiThreadInfo)) };
//Helpers.CheckResult(GetGUIThreadInfo(threadId, ref guiTheadInfo), "GetGUIThreadInfo");
//var currentThreadId = Helpers.GetCurrentThreadId();                //Helpers.CheckResult(AttachThreadInput(currentThreadId, threadId, attach: true), "AttachThreadInput(true)");
//Helpers.CheckResult(BlockInput(true), "BlockInput(true)");
//Helpers.SendMessage(guiTheadInfo.hwndFocus, KeyboardHook.WM_KEYDOWN, (uint)VirtualKeyCode.CONTROL, 0x001D0001);
//Helpers.SendMessage(guiTheadInfo.hwndFocus, KeyboardHook.WM_KEYDOWN, (uint)VirtualKeyCode.VK_C, 0x002E0001);
//Helpers.SendMessage(guiTheadInfo.hwndFocus, KeyboardHook.WM_KEYUP, (uint)VirtualKeyCode.CONTROL, 0xC01D0001);
//Helpers.SendMessage(guiTheadInfo.hwndFocus, KeyboardHook.WM_KEYUP, (uint)VirtualKeyCode.VK_C, 0xC02E0001);
//Helpers.TraceResult(BlockInput(false), "BlockInput(false)");
//Helpers.TraceResult(AttachThreadInput(currentThreadId, threadId, attach: false), "AttachThreadInput(false)");

//inputSimulator.Keyboard.Sleep(50);
//private static readonly VirtualKeyCode[] Modifiers = new[] { VirtualKeyCode.CONTROL, VirtualKeyCode.SHIFT, VirtualKeyCode.MENU/*, VirtualKeyCode.LMENU, VirtualKeyCode.LSHIFT, VirtualKeyCode.LCONTROL, VirtualKeyCode.RMENU, VirtualKeyCode.RSHIFT, VirtualKeyCode.RCONTROL*/ };