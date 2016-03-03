using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using Microsoft.CodeAnalysis.Scripting;
using Microsoft.CodeAnalysis.Scripting.CSharp;

namespace ScriptCs.AutoHotkey
{
    public interface IService : IDisposable
    {
    }

    public sealed class AutoHotkey : IDisposable
    {
        private bool disposed;

        public IKeyboard Keyboard { get; } = new Keyboard();

        public IProcesses Processes { get; } = new Processes();

        public IRegistry Registry { get; } = new Registry();

        public AutoHotkey()
        {
            AppDomain.CurrentDomain.UnhandledException += OnUnhandledException;
            var threadId = Helpers.GetCurrentThreadId();
            AppDomain.CurrentDomain.DomainUnload += delegate
            {
                Helpers.TraceResult(Helpers.PostThreadMessage((uint)threadId, 0, 0, 0), "PostThreadMessage");
            };
        }

        private void OnUnhandledException(object sender, UnhandledExceptionEventArgs e)
        {
            var message = e.ExceptionObject.ToString();
            Trace.WriteLine(message);
            MessageBox.Show(message, "AutoHotkeyError");
        }

        [STAThread]
        static void Main(string[] args)
        {
            var path = args.FirstOrDefault() ?? Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "AutoHotkey.csx");
            var options = ScriptOptions.Default
                                    .WithReferences("System", "System.Windows.Forms")
                                    .WithNamespaces("System", "System.Windows.Forms", "System.Diagnostics", "System.Threading", "Microsoft.Win32", "ScriptCs.AutoHotkey");

            var autoHotkey = new AutoHotkey();

            CSharpScript.RunAsync(File.ReadAllText(path), options, autoHotkey).Wait();

            autoHotkey.Run();
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

        private void Run()
        {
            using(this)
            {
                Application.Run();
            }
        }
    }
}