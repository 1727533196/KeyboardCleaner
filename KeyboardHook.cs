using System;
using System.Runtime.InteropServices;

namespace KeyboardCleaner
{
    /// <summary>
    /// Low-level keyboard hook that intercepts ALL keyboard input
    /// before any application can receive it.
    /// </summary>
    internal class KeyboardHook : IDisposable
    {
        private IntPtr _hookId = IntPtr.Zero;
        private NativeMethods.LowLevelKeyboardProc _proc;
        private bool _ctrlHeld;
        private bool _shiftHeld;

        public event Action EmergencyUnlockRequested;
        public bool IsInstalled { get { return _hookId != IntPtr.Zero; } }

        /// <summary>Install the global keyboard hook.</summary>
        public void Install()
        {
            if (IsInstalled) return;

            _proc = HookCallback;

            // GetModuleHandle(null) returns this EXE's module handle —
            // the simplest and most reliable way for WH_KEYBOARD_LL.
            IntPtr hMod = NativeMethods.GetModuleHandle(null);

            _hookId = NativeMethods.SetWindowsHookEx(
                NativeMethods.WH_KEYBOARD_LL,
                _proc,
                hMod,
                0);

            if (_hookId == IntPtr.Zero)
            {
                int err = Marshal.GetLastWin32Error();
                throw new System.ComponentModel.Win32Exception(err,
                    "无法安装键盘钩子。错误码: " + err +
                    "\n请尝试以管理员身份运行此程序。");
            }
        }

        /// <summary>Remove the global keyboard hook.</summary>
        public void Uninstall()
        {
            if (!IsInstalled) return;
            NativeMethods.UnhookWindowsHookEx(_hookId);
            _hookId = IntPtr.Zero;
            _ctrlHeld = false;
            _shiftHeld = false;
        }

        // ── Hook callback (called on the UI thread's message pump) ──
        private IntPtr HookCallback(int nCode, IntPtr wParam, IntPtr lParam)
        {
            if (nCode < 0)
                return NativeMethods.CallNextHookEx(_hookId, nCode, wParam, lParam);

            int vkCode = Marshal.ReadInt32(lParam);
            bool isDown = (wParam == (IntPtr)NativeMethods.WM_KEYDOWN ||
                           wParam == (IntPtr)NativeMethods.WM_SYSKEYDOWN);
            bool isUp   = (wParam == (IntPtr)NativeMethods.WM_KEYUP ||
                           wParam == (IntPtr)NativeMethods.WM_SYSKEYUP);

            // Track modifier key state (we must track ourselves since
            // all keys are blocked and GetAsyncKeyState would be stale).
            if (isDown)
            {
                if (vkCode == NativeMethods.VK_LCONTROL || vkCode == NativeMethods.VK_RCONTROL)
                    _ctrlHeld = true;
                if (vkCode == NativeMethods.VK_LSHIFT || vkCode == NativeMethods.VK_RSHIFT)
                    _shiftHeld = true;

                // ── Emergency unlock: Ctrl + Shift + F12 ──
                if (vkCode == NativeMethods.VK_F12 && _ctrlHeld && _shiftHeld)
                {
                    // Fire on UI thread via dispatcher
                    var app = System.Windows.Application.Current;
                    if (app != null)
                    {
                        app.Dispatcher.BeginInvoke(new Action(() =>
                        {
                            var handler = EmergencyUnlockRequested;
                            if (handler != null) handler();
                        }));
                    }
                    return (IntPtr)1; // swallow F12 as well
                }
            }
            else if (isUp)
            {
                if (vkCode == NativeMethods.VK_LCONTROL || vkCode == NativeMethods.VK_RCONTROL)
                    _ctrlHeld = false;
                if (vkCode == NativeMethods.VK_LSHIFT || vkCode == NativeMethods.VK_RSHIFT)
                    _shiftHeld = false;
            }

            // Block everything — return non-zero so Windows discards the message
            return (IntPtr)1;
        }

        public void Dispose() { Uninstall(); }
    }
}
