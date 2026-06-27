using System;
using System.Runtime.InteropServices;

namespace KeyboardCleaner
{
    internal static class NativeMethods
    {
        // ── Keyboard Hook ──────────────────────────────────────────
        public const int WH_KEYBOARD_LL = 13;

        public const int WM_KEYDOWN     = 0x0100;
        public const int WM_KEYUP       = 0x0101;
        public const int WM_SYSKEYDOWN  = 0x0104;
        public const int WM_SYSKEYUP    = 0x0105;

        // Virtual key codes
        public const int VK_LCONTROL = 0xA2;
        public const int VK_RCONTROL = 0xA3;
        public const int VK_LSHIFT   = 0xA0;
        public const int VK_RSHIFT   = 0xA1;
        public const int VK_LWIN     = 0x5B;
        public const int VK_RWIN     = 0x5C;
        public const int VK_F12      = 0x7B;

        [StructLayout(LayoutKind.Sequential)]
        internal struct KBDLLHOOKSTRUCT
        {
            public uint   vkCode;
            public uint   scanCode;
            public uint   flags;
            public uint   time;
            public IntPtr dwExtraInfo;
        }

        internal delegate IntPtr LowLevelKeyboardProc(
            int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr SetWindowsHookEx(
            int idHook, LowLevelKeyboardProc lpfn,
            IntPtr hMod, uint dwThreadId);

        [DllImport("user32.dll", SetLastError = true)]
        [return: MarshalAs(UnmanagedType.Bool)]
        internal static extern bool UnhookWindowsHookEx(IntPtr hhk);

        [DllImport("user32.dll", SetLastError = true)]
        internal static extern IntPtr CallNextHookEx(
            IntPtr hhk, int nCode, IntPtr wParam, IntPtr lParam);

        [DllImport("kernel32.dll", CharSet = CharSet.Auto)]
        internal static extern IntPtr GetModuleHandle(string lpModuleName);

        // ── DWM rounded corners (Windows 11) ──────────────────────
        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int attr, ref int attrValue, int attrSize);

        internal const int DWMWA_WINDOW_CORNER_PREFERENCE = 33;
        internal const int DWMWCP_ROUND    = 2;
        internal const int DWMWCP_DONOTROUND = 1;

        // ── Dark title bar (Windows 10 1809+) ─────────────────────
        [DllImport("dwmapi.dll")]
        internal static extern int DwmSetWindowAttribute(
            IntPtr hwnd, int dwAttribute, ref bool pvAttribute, int cbAttribute);

        internal const int DWMWA_USE_IMMERSIVE_DARK_MODE = 20;

        // ── Window styles ─────────────────────────────────────────
        internal const int GWL_EXSTYLE       = -20;
        internal const int WS_EX_TOOLWINDOW  = 0x80;
        internal const int WS_EX_APPWINDOW   = 0x40000;
        internal const int WS_EX_NOACTIVATE  = 0x08000000;

        [DllImport("user32.dll")]
        internal static extern int GetWindowLong(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll")]
        internal static extern int SetWindowLong(IntPtr hWnd, int nIndex, int dwNewLong);

        [DllImport("user32.dll")]
        internal static extern bool SetForegroundWindow(IntPtr hWnd);

        [DllImport("user32.dll")]
        internal static extern bool ShowWindow(IntPtr hWnd, int nCmdShow);

        internal const int SW_RESTORE = 9;
    }
}
