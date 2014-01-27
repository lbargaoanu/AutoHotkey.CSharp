using System;
using System.Windows.Forms;

namespace ScriptCs.AutoHotkey
{
    public interface IKeyboard : IService
    {
        void RegisterHotkey(Keys hotkey, Action handler);
    }
    
    public class Keyboard : IKeyboard
    {
        private KeyboardHook keyboardHook = new KeyboardHook();

        public void RegisterHotkey(Keys hotkey, Action handler)
        {
            keyboardHook.RegisterHotkey(hotkey, handler);
        }

        public void Dispose()
        {
            keyboardHook.Dispose();
        }
    }
}