using System;
using System.Collections;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace ScriptCs.AutoHotkey
{
    public interface IProcesses : IService, IEnumerable
    {
        void CloseWindows(string name);
        void CloseWindows(params Process[] processes);
    }

    public sealed class Processes : IProcesses
    {
        const uint WM_CLOSE = 0x0010;
        private const short OneSecond = 1000;

        delegate bool EnumWindowsDelegate(uint hWnd, IntPtr lParam);

        [return: MarshalAs(UnmanagedType.Bool)]
        [DllImport("user32.dll", SetLastError = true)]
        static extern bool EnumThreadWindows(int dwThreadId, EnumWindowsDelegate lpfn, IntPtr lParam);

        [DllImport("user32", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        static extern bool EnumChildWindows(uint window, EnumWindowsDelegate callback, IntPtr lParam);

        void IDisposable.Dispose()
        {
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return Process.GetProcesses().GetEnumerator();
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
                try
                {
                    CloseWindows(process);
                }
                catch(Exception ex)
                {
                    Trace.WriteLine(ex);
                }
            }
        }

        private static void CloseWindows(Process process)
        {
            foreach(ProcessThread thread in process.Threads)
            {
                Helpers.CheckResult(EnumThreadWindows(thread.Id, (hWnd, lParam) =>
                {
                    EnumChildWindows(hWnd, (hChildWnd, _) =>
                    {
                        CloseWindow(hChildWnd);
                        return true;
                    }, IntPtr.Zero);
                    CloseWindow(hWnd);
                    return true;
                }, IntPtr.Zero), "EnumThreadWindows");
            }
            process.WaitForExit(OneSecond);
            process.Kill();
        }

        private static void CloseWindow(uint hWnd)
        {
            Helpers.TraceResult(0 == Helpers.SendMessage(hWnd, WM_CLOSE, 0, 0), "CloseWindow");
        }
    }
}