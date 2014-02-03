using System;
using System.Threading;
using System.ComponentModel.Composition;
using System.Windows.Forms;
using ScriptCs.Contracts;
using System.Runtime.InteropServices;
using System.ComponentModel;

namespace ScriptCs.AutoHotkey
{
    public interface IService : IDisposable
    {
    }

    public sealed class AutoHotkey : IScriptPack, IScriptPackContext, IDisposable
    {
        private readonly object sync = new object();
        private bool disposed;
        [Import(RequiredCreationPolicy = CreationPolicy.NonShared)]
        public IKeyboard Keyboard { get; set; }

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        public static extern bool PostThreadMessage(uint threadId, uint msg, UIntPtr wParam, IntPtr lParam);

        IScriptPackContext IScriptPack.GetContext()
        {
            AppDomain.CurrentDomain.UnhandledException += (sender, e) =>
            {
                Console.WriteLine("UNHANDLEDEXCEPTION " + e.ExceptionObject);
            };
            var threadId = AppDomain.GetCurrentThreadId();
            AppDomain.CurrentDomain.DomainUnload += delegate
            {
                var result = PostThreadMessage((uint) threadId, 0, UIntPtr.Zero, IntPtr.Zero);
                if(!result)
                {
                    throw new Win32Exception();
                }
                Dispose();
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
            try
            {
                lock(sync)
                {
                    if(disposed)
                    {
                        return;
                    }
                    disposed = true;
                    Keyboard.Dispose();
                }
            }
            catch(Exception ex)
            {
                Log(ex);
            }
        }

        private static void Log(Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }

        public void Run()
        {
            try
            {
                Console.WriteLine("APPLICATION.RUN");
                Application.AddMessageFilter(new MyMessageFilter());
                Application.Run();
            }
            catch(Exception ex)
            {
                Log(ex);
            }
            finally
            {
                Console.WriteLine("DISPOSE");
                Dispose();
            }
        }

        class MyMessageFilter : IMessageFilter
        {
            public bool PreFilterMessage(ref Message m)
            {
                Console.WriteLine(m.ToString());
                return m.HWnd == IntPtr.Zero;
            }
        }
    }
}