using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ScriptCs.Contracts;

namespace ScriptCs.AutoHotkey
{
    public interface IService : IDisposable
    {
    }

    public sealed class AutoHotkey : IScriptPack, IScriptPackContext, IDisposable
    {
        private bool disposed;
        
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IKeyboard Keyboard { get; set; }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostThreadMessage(uint threadId, uint msg, UIntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

        IScriptPackContext IScriptPack.GetContext()
        {
            var threadId = GetCurrentThreadId();
            AppDomain.CurrentDomain.DomainUnload += delegate
            {
                var result = PostThreadMessage((uint) threadId, 0, UIntPtr.Zero, IntPtr.Zero);
                if(!result)
                {
                    Trace.WriteLine(new Win32Exception());
                }
            };
            return this;
        }

        void IScriptPack.Initialize(IScriptPackSession session)
        {
            session.AddReference("System.Windows.Forms");
            Array.ForEach(new[] { "System.Windows.Forms", "System.Diagnostics" }, session.ImportNamespace);
        }

        void IScriptPack.Terminate()
        {
            Dispose();
        }

        public void Dispose()
        {
            Trace.WriteLine("DISPOSE");
            if(disposed)
            {
                return;
            }
            disposed = true;
            Keyboard.Dispose();
        }

        public void Run()
        {
            try
            {
                Application.Run();
            }
            finally
            {
                Dispose();
            }
        }
    }
}