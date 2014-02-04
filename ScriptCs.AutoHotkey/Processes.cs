using System;
using System.ComponentModel.Composition;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScriptCs.AutoHotkey
{
    public interface IProcesses : IService
    {
        void CloseWindows(string name);
        void CloseWindows(params Process[] processes);
    }

    [Export(typeof(IProcesses))]
    public sealed class Processes : IProcesses
    {
        const uint WM_CLOSE = 0x0010;

        delegate bool EnumThreadDelegate(IntPtr hWnd, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool EnumThreadWindows(int dwThreadId, EnumThreadDelegate lpfn, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        static extern int SendMessage(IntPtr hWnd, uint Msg, int wParam, int lParam);

        public void Dispose()
        {
        }

        public void CloseWindows(string name)
        {
            var processes = Process.GetProcessesByName(name);
            CloseWindows(processes);
        }

        public void CloseWindows(Process[] processes)
        {
            foreach(var process in processes)
            {
                foreach(ProcessThread thread in process.Threads)
                {
                    AutoHotkey.CheckResult(EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                    {
                        AutoHotkey.TraceResult(0 == SendMessage(hWnd, WM_CLOSE, 0, 0), "CloseWindow");
                        return true;
                    }, IntPtr.Zero), "EnumThreadWindows");
                }
            }
        }
    }
}