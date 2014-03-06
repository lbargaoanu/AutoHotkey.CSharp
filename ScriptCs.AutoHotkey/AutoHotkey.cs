using System;
using System.ComponentModel;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using ScriptCs.Contracts;

namespace ScriptCs.AutoHotkey
{
    public interface IService : IDisposable
    {
    }

    [Export(typeof(IScriptHostFactory))]
    public sealed class AutoHotkey : IScriptPack, IScriptPackContext, IDisposable, IScriptHost, IScriptHostFactory
    {
        private bool disposed;
        
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IKeyboard Keyboard { get; set; }

        [Import]
        public IProcesses Processes { get; set; }

        [Import]
        public IRegistry Registry { get; set; }

        public AutoHotkey()
        {
            Console.TreatControlCAsInput = true;
            Trace.Listeners.Add(new ConsoleTraceListener());
            var threadId = Helpers.GetCurrentThreadId();
            AppDomain.CurrentDomain.DomainUnload += delegate
            {
                Helpers.TraceResult(Helpers.PostThreadMessage((uint)threadId, 0, 0, 0), "PostThreadMessage");
            };
        }

        IScriptPackContext IScriptPack.GetContext()
        {
            return this;
        }

        void IScriptPack.Initialize(IScriptPackSession session)
        {
            session.AddReference("System.Windows.Forms");
            Array.ForEach(new[] { "System.Windows.Forms", "System.Diagnostics", "System.Threading", "Microsoft.Win32" }, session.ImportNamespace);
        }

        void IScriptPack.Terminate()
        {
        }

        void IDisposable.Dispose()
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
            using(this)
            {
                Application.Run();
            }
        }

        IScriptHost IScriptHostFactory.CreateScriptHost(IScriptPackManager scriptPackManager, string[] scriptArgs)
        {
            return this;
        }
    }
}