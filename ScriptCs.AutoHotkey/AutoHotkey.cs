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

    public sealed class AutoHotkey : IScriptPack, IScriptPackContext, IDisposable
    {
        private bool disposed;
        
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IKeyboard Keyboard { get; set; }

        [Import]
        public IProcesses Processes { get; set; }

        [Import]
        public IRegistry Registry { get; set; }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool PostThreadMessage(uint threadId, uint msg, UIntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll")]
        static extern uint GetCurrentThreadId();

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

        IScriptPackContext IScriptPack.GetContext()
        {
            Console.TreatControlCAsInput = true;
            Trace.Listeners.Add(new ConsoleTraceListener());
            var threadId = GetCurrentThreadId();
            AppDomain.CurrentDomain.DomainUnload += delegate
            {
                TraceResult(PostThreadMessage((uint)threadId, 0, UIntPtr.Zero, IntPtr.Zero), "PostThreadMessage");
            };
            return this;
        }

        void IScriptPack.Initialize(IScriptPackSession session)
        {
            session.AddReference("System.Windows.Forms");
            Array.ForEach(new[] { "System.Windows.Forms", "System.Diagnostics", "System.Threading", "Microsoft.Win32" }, session.ImportNamespace);
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
            using(this)
            {
                Application.Run();
            }
        }
    }
}