using System;
using System.Windows;
using System.Windows.Threading;

namespace KeyboardCleaner
{
    internal static class App
    {
        [STAThread]
        public static void Main()
        {
            // Prevent multiple instances
            bool createdNew;
            using (var mutex = new System.Threading.Mutex(true, "KeyboardCleaner_SingleInstance", out createdNew))
            {
                if (!createdNew)
                {
                    System.Windows.Forms.MessageBox.Show(
                        "键盘清洁助手已经在运行中。\n请查看系统托盘图标。",
                        "键盘清洁助手",
                        System.Windows.Forms.MessageBoxButtons.OK,
                        System.Windows.Forms.MessageBoxIcon.Information);
                    return;
                }

                var app = new Application();
                app.ShutdownMode = ShutdownMode.OnExplicitShutdown;

                var window = new MainWindow();
                window.Show();

                app.Run();
            }
        }
    }
}
