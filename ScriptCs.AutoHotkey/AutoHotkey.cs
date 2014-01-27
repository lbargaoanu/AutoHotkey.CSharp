using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Forms;

namespace ScriptCs.AutoHotkey
{
    public interface IService : IDisposable
    {
    }

    public sealed class AutoHotkey
    {
        public IKeyboard Keyboard { get; set; }

        public void Run()
        {
            Application.Run();
        }
    }
}