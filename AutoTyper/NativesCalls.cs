#define VERBOSE_DEBUG
namespace AutoTyper
{
    #region Usings

    using System;
    using System.Runtime.InteropServices;
    #endregion

    internal class NativesCalls
    {
        public delegate IntPtr LowLevelKeyboardProc(int nCode, IntPtr wParam, IntPtr lParam);



        [DllImport("user32.dll")]
        public static extern IntPtr CallNextHookEx(IntPtr hk, int ncode, IntPtr wparam, IntPtr lparam);

        [DllImport("kernel32.dll")]
        public static extern IntPtr GetModuleHandle(string l);

        [DllImport("user32.dll")]
        public static extern IntPtr SetWindowsHookEx(int idHook, LowLevelKeyboardProc lpfn, IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll")]
        [return: MarshalAs(UnmanagedType.Bool)]
        public static extern bool UnhookWindowsHookEx(IntPtr hk);
    }
}
