using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Windows.Forms;
using ScriptCs.Contracts;

namespace ScriptCs.AutoHotkey
{
    public interface IService : IDisposable
    {
    }

    public sealed class AutoHotkey : IScriptPackContext
    {
        public IKeyboard Keyboard { get; set; }

        public void Run()
        {
            Application.Run();
        }
    }
}