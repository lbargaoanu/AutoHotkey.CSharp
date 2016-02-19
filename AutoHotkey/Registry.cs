using System;
using WindowsRegistry = Microsoft.Win32.Registry;

namespace ScriptCs.AutoHotkey
{
    public interface IRegistry : IService
    {
        T GetValue<T>(string registryKeyPath, string value = null, T defaultValue = default(T));
        string GetValue(string registryKeyPath, string value = null, string defaultValue = null);
    }

    public class Registry : IRegistry
    {
        void IDisposable.Dispose()
        {
        }

        public T GetValue<T>(string registryKeyPath, string value = null, T defaultValue = default(T))
        {
            return (T)WindowsRegistry.GetValue(registryKeyPath, value, defaultValue);
        }

        public string GetValue(string registryKeyPath, string value = null, string defaultValue = null)
        {
            return GetValue<string>(registryKeyPath, value, defaultValue);
        }
    }
}