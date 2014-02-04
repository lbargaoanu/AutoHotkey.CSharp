using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;

namespace ScriptCs.AutoHotkey
{
    public sealed class KeyboardHook : IDisposable, IMessageFilter
    {
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool RegisterHotKey(IntPtr hWnd, int id, uint fsModifiers, uint vk);
        [DllImport("user32.dll", SetLastError = true)]
        private static extern bool UnregisterHotKey(IntPtr hWnd, int id);

        private const uint Lower16BitsMask = 0xFFFF;
        private const uint MOD_ALT = 0x1;
        private const uint MOD_CONTROL = 0x2;
        private const uint MOD_SHIFT = 0x4;
        private const int WM_HOTKEY = 0x0312;

        private Dictionary<Keys, Action> _handlers = new Dictionary<Keys, Action>();
        private int _currentId;
        
        public KeyboardHook()
        {
            Application.AddMessageFilter(this);
        }

        public bool PreFilterMessage(ref Message m)
        {
            Trace.WriteLine(m.ToString());
            if(m.Msg != WM_HOTKEY)
            {
                return false;
            }            
            OnKeyPressed(m.LParam);
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

        private void OnKeyPressed(IntPtr lParam)
        {
            var modifiers = TranslateModifiers(lParam);
            var keys = modifiers + (((int)lParam >> 16) & Lower16BitsMask);
            Action handler;
            if(!_handlers.TryGetValue((Keys) keys, out handler))
            {
                return;
            }
            try
            {
                handler();
            }
            catch(Exception ex)
            {
                Trace.WriteLine(ex);
            }
        }

        public void RegisterHotkey(Keys hotkey, Action handler)
        {
            uint modifiers = TranslateModifiers(hotkey);
            AutoHotkey.CheckResult(RegisterHotKey(IntPtr.Zero, _currentId + 1, modifiers, Lower16BitsMask & (uint)hotkey), "RegisterHotKey");
            _currentId++;
            _handlers.Add(hotkey, handler);
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
            Application.RemoveMessageFilter(this);
            for(int index = _currentId; index > 0; index--)
            {
                AutoHotkey.TraceResult(UnregisterHotKey(IntPtr.Zero, index), "UnregisterHotKey");
            }
        }
    }
}