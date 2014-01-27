using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Threading;
using System.Windows.Forms;

namespace ScriptCs.AutoHotkey
{
    public sealed class KeyboardHook : IDisposable
    {
        [DllImport("user32.dll")]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll")]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint Lower16BitsMask = 0xFFFF;
        public static uint MOD_ALT = 0x1;
        public static uint MOD_CONTROL = 0x2;
        public static uint MOD_SHIFT = 0x4;
        public static uint MOD_WIN = 0x8;

        /// <summary>
        /// Represents the window that is used internally to get the messages.
        /// </summary>
        private class Window : NativeWindow, IDisposable
        {
            private static int WM_HOTKEY = 0x0312;

            public event EventHandler<KeyPressedEventArgs> KeyPressed;

            public Window()
            {
                this.CreateHandle(new CreateParams());
            }

            protected override void WndProc(ref Message m)
            {
                base.WndProc(ref m);
                if(m.Msg != WM_HOTKEY || KeyPressed == null)
                {
                    return;
                }
                var modifiers = TranslateModifiers(m.LParam);
                var key = modifiers + (((int)m.LParam >> 16) & Lower16BitsMask);
                KeyPressed(this, new KeyPressedEventArgs((Keys)key));
            }

            private static uint TranslateModifiers(IntPtr lParam)
            {
                var inputModifiers = (uint) lParam & Lower16BitsMask;
                uint outputModifers = 0;
                if((inputModifiers & MOD_ALT) == MOD_ALT)
                {
                    outputModifers |= (uint) Keys.Alt;
                }
                if((inputModifiers & MOD_CONTROL) == MOD_CONTROL)
                {
                    outputModifers |= (uint) Keys.Control;
                }
                if((inputModifiers & MOD_SHIFT) == MOD_SHIFT)
                {
                    outputModifers |= (uint) Keys.Shift;
                }
                return outputModifers;
            }

            public void Dispose()
            {
                this.DestroyHandle();
            }
        }

        private Window _window = new Window();
        private Dictionary<Keys, Action> _handlers = new Dictionary<Keys, Action>();
        private int _currentId;

        public KeyboardHook()
        {
            _window.KeyPressed += OnKeyPressed;
        }

        private void OnKeyPressed(object sender, KeyPressedEventArgs args)
        {
            Action handler;
            if(_handlers.TryGetValue(args.Keys, out handler))
            {
                handler();
            }
        }

        public void RegisterHotkey(Keys hotkey, Action handler)
        {
            _handlers.Add(hotkey, handler);
            _currentId = _currentId + 1;
            uint modifiers = TranslateModifiers(hotkey);
            if(!RegisterHotKey(_window.Handle, _currentId, modifiers, Lower16BitsMask & (uint) hotkey))
            {
                throw new Win32Exception();
            }
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
            for(int index = _currentId; index > 0; index--)
            {
                UnregisterHotKey(_window.Handle, index);
            }
            _window.Dispose();
        }
    }

    public class KeyPressedEventArgs : EventArgs
    {
        public KeyPressedEventArgs(Keys keys)
        {
            Keys = keys;
        }

        public Keys Keys { get; private set; }
    }
}