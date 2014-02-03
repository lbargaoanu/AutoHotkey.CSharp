using System;
using System.Collections.Generic;
using System.ComponentModel.Composition;
using System.Linq;
using System.Windows.Forms;
using WindowsInput;
using WindowsInput.Native;

namespace ScriptCs.AutoHotkey
{
    public interface IKeyboard : IService
    {
        void RegisterHotkey(Keys hotkey, Action handler);
        void Send(params Keys[] keys);
        void SendModified(Keys modifiers, params Keys[] keys);
    }
    
    [Export(typeof(IKeyboard))]
    public sealed class Keyboard : IKeyboard
    {
        private KeyboardHook keyboardHook = new KeyboardHook();
        private InputSimulator inputSimulator = new InputSimulator();

        public void Dispose()
        {
            keyboardHook.Dispose();
        }

        public void RegisterHotkey(Keys hotkey, Action handler)
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

    public static class Extensions
    {
        public static VirtualKeyCode[] AsVirtualKeyCodes(this Keys[] keys)
        {
            return Array.ConvertAll(keys, key => (VirtualKeyCode) key);
        }
    }
}