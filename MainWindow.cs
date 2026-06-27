using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Media.Effects;
using System.Windows.Shapes;
using System.Windows.Interop;
using System.Windows.Threading;
using System.ComponentModel;
// using System.Drawing removed — use fully qualified names to avoid
// ambiguity with System.Windows.Media (Color, Brushes, Point, FontFamily)

namespace KeyboardCleaner
{
    internal sealed class MainWindow : Window
    {
        // ── State ──────────────────────────────────────────────────
        private bool _isLocked;
        private KeyboardHook _hook;
        private System.Windows.Forms.NotifyIcon _trayIcon;

        // ── UI elements ────────────────────────────────────────────
        private Ellipse           _statusCircle;
        private ScaleTransform    _statusCircleScale;
        private TextBlock         _statusTitle;
        private TextBlock         _statusSubtitle;
        private Button            _toggleButton;
        private TextBlock         _hintText;

        // ── Brushes ────────────────────────────────────────────────
        private static readonly SolidColorBrush BrushBg        = new SolidColorBrush(Color.FromRgb(0x0D, 0x11, 0x17));
        private static readonly SolidColorBrush BrushSurface   = new SolidColorBrush(Color.FromRgb(0x16, 0x1B, 0x22));
        private static readonly SolidColorBrush BrushBorder    = new SolidColorBrush(Color.FromRgb(0x30, 0x36, 0x3D));
        private static readonly SolidColorBrush BrushTextPri   = new SolidColorBrush(Color.FromRgb(0xE6, 0xED, 0xF3));
        private static readonly SolidColorBrush BrushTextSec   = new SolidColorBrush(Color.FromRgb(0x8B, 0x94, 0x9E));
        private static readonly SolidColorBrush BrushLocked    = new SolidColorBrush(Color.FromRgb(0xE9, 0x45, 0x60)); // coral red
        private static readonly SolidColorBrush BrushUnlocked  = new SolidColorBrush(Color.FromRgb(0x2E, 0xCC, 0x71)); // emerald green
        private static readonly SolidColorBrush BrushBtnLocked = new SolidColorBrush(Color.FromRgb(0xC0, 0x39, 0x4B)); // darker red
        private static readonly SolidColorBrush BrushBtnUnlock = new SolidColorBrush(Color.FromRgb(0x27, 0xAE, 0x60)); // darker green

        // ── Constructor ────────────────────────────────────────────
        public MainWindow()
        {
            _hook = new KeyboardHook();
            _hook.EmergencyUnlockRequested += OnEmergencyUnlock;

            InitializeWindow();
            BuildUI();
            SetupTrayIcon();

            // Default state: unlocked — user clicks button to lock
            UpdateUIState();
        }

        // ── Window setup ───────────────────────────────────────────
        private void InitializeWindow()
        {
            Title = "键盘清洁助手";
            Width = 320;
            Height = 260;
            ResizeMode = ResizeMode.NoResize;
            WindowStyle = WindowStyle.None;
            AllowsTransparency = true;
            Background = Brushes.Transparent;
            Topmost = true;
            ShowInTaskbar = true;

            // Center on screen
            WindowStartupLocation = WindowStartupLocation.CenterScreen;

            // Handle window events
            Loaded          += OnLoaded;
            Closing         += OnClosing;
            MouseLeftButtonDown += (s, e) => { if (e.LeftButton == System.Windows.Input.MouseButtonState.Pressed) DragMove(); };
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // ── Apply Windows 11 rounded corners ──
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                // Windows 11 rounded corners (ignored on Win10)
                int cornerPref = NativeMethods.DWMWCP_ROUND;
                NativeMethods.DwmSetWindowAttribute(hwnd,
                    NativeMethods.DWMWA_WINDOW_CORNER_PREFERENCE,
                    ref cornerPref, sizeof(int));

                // Dark window title bar
                bool useDark = true;
                NativeMethods.DwmSetWindowAttribute(hwnd,
                    NativeMethods.DWMWA_USE_IMMERSIVE_DARK_MODE,
                    ref useDark, sizeof(bool));
            }
        }

        // ── Build all UI programmatically ──────────────────────────
        private void BuildUI()
        {
            // Outer border with rounded clip
            var rootBorder = new Border
            {
                CornerRadius = new CornerRadius(12),
                Background = BrushBg,
                BorderBrush = BrushBorder,
                BorderThickness = new Thickness(1),
            };

            var rootGrid = new Grid();
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                        // row 0: title bar
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) }); // row 1: status
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                        // row 2: button
            rootGrid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });                        // row 3: hint
            rootGrid.Margin = new Thickness(0);

            // ── Row 0: Title bar (buttons top-right) ─────────────────
            var titleBar = new Grid { Height = 30 };
            titleBar.ColumnDefinitions.Add(new ColumnDefinition());             // left: drag area
            titleBar.ColumnDefinitions.Add(new ColumnDefinition { Width = GridLength.Auto }); // right: buttons

            var chromeBtns = new StackPanel { Orientation = Orientation.Horizontal };
            Grid.SetColumn(chromeBtns, 1);

            var minBtn = CreateChromeButton("‒", "最小化到托盘");
            minBtn.Width = 32; minBtn.Height = 30;
            minBtn.Click += (s, e) => HideToTray();
            chromeBtns.Children.Add(minBtn);

            var clsBtn = CreateChromeButton("✕", "退出程序");
            clsBtn.Width = 32; clsBtn.Height = 30;
            clsBtn.Click += (s, e) => ExitApplication();
            chromeBtns.Children.Add(clsBtn);

            titleBar.Children.Add(chromeBtns);
            Grid.SetRow(titleBar, 0);
            rootGrid.Children.Add(titleBar);

            // ── Row 1: Status area ─────────────────────────────────
            var statusPanel = new StackPanel
            {
                VerticalAlignment = VerticalAlignment.Center,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 20, 0, 8)
            };

            // Pulsing circle
            _statusCircleScale = new ScaleTransform(1.0, 1.0);
            _statusCircle = new Ellipse
            {
                Width = 48,
                Height = 48,
                Fill = BrushUnlocked,
                RenderTransformOrigin = new Point(0.5, 0.5),
                RenderTransform = _statusCircleScale,
                Effect = new DropShadowEffect
                {
                    Color = Color.FromRgb(0x2E, 0xCC, 0x71),
                    BlurRadius = 16,
                    ShadowDepth = 0,
                    Opacity = 0.6,
                }
            };
            statusPanel.Children.Add(_statusCircle);

            _statusTitle = new TextBlock
            {
                Text = "键盘已解锁",
                FontSize = 16,
                FontWeight = FontWeights.SemiBold,
                Foreground = BrushTextPri,
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 12, 0, 2),
            };
            statusPanel.Children.Add(_statusTitle);

            _statusSubtitle = new TextBlock
            {
                Text = "可以正常使用键盘",
                FontSize = 12,
                Foreground = BrushTextSec,
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            statusPanel.Children.Add(_statusSubtitle);

            Grid.SetRow(statusPanel, 1);
            rootGrid.Children.Add(statusPanel);

            // ── Row 1: Toggle button ───────────────────────────────
            _toggleButton = new Button
            {
                Width = 200,
                Height = 42,
                FontSize = 14,
                FontWeight = FontWeights.SemiBold,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = Brushes.White,
                Background = BrushUnlocked,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Margin = new Thickness(0, 8, 0, 0),
                HorizontalAlignment = HorizontalAlignment.Center,
            };
            // Apply template for rounded corners
            var btnTemplate = new ControlTemplate(typeof(Button));
            var btnBorderFactory = new FrameworkElementFactory(typeof(Border));
            btnBorderFactory.SetValue(Border.CornerRadiusProperty, new CornerRadius(8));
            btnBorderFactory.SetValue(Border.BackgroundProperty, new TemplateBindingExtension(Button.BackgroundProperty));
            btnBorderFactory.SetValue(Border.PaddingProperty, new Thickness(0));
            var btnContentFactory = new FrameworkElementFactory(typeof(ContentPresenter));
            btnContentFactory.SetValue(ContentPresenter.HorizontalAlignmentProperty, HorizontalAlignment.Center);
            btnContentFactory.SetValue(ContentPresenter.VerticalAlignmentProperty, VerticalAlignment.Center);
            btnBorderFactory.AppendChild(btnContentFactory);
            btnTemplate.VisualTree = btnBorderFactory;
            _toggleButton.Template = btnTemplate;
            _toggleButton.Click += OnToggleClick;
            _toggleButton.MouseEnter += (s, e) =>
            {
                _toggleButton.Background = _isLocked
                    ? new SolidColorBrush(Color.FromRgb(0xD0, 0x50, 0x60))
                    : new SolidColorBrush(Color.FromRgb(0x3A, 0xD8, 0x80));
            };
            _toggleButton.MouseLeave += (s, e) =>
            {
                _toggleButton.Background = _isLocked ? BrushBtnLocked : BrushBtnUnlock;
            };

            Grid.SetRow(_toggleButton, 2);
            rootGrid.Children.Add(_toggleButton);

            // ── Row 2: Hint ────────────────────────────────────────
            _hintText = new TextBlock
            {
                Text = "紧急快捷键解锁：Ctrl + Shift + F12",
                FontSize = 10,
                Foreground = BrushTextSec,
                FontFamily = new FontFamily("Segoe UI"),
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(0, 10, 0, 14),
            };
            Grid.SetRow(_hintText, 3);
            rootGrid.Children.Add(_hintText);

            rootBorder.Child = rootGrid;
            Content = rootBorder;
        }

        /// <summary>Small text-only button for window chrome (─ / ✕).</summary>
        private Button CreateChromeButton(string text, string tooltip)
        {
            var blueBrush = new SolidColorBrush(Color.FromRgb(0x58, 0xA6, 0xFF));
            var btn = new Button
            {
                Content = text,
                ToolTip = tooltip,
                FontSize = 13,
                FontFamily = new FontFamily("Segoe UI"),
                Foreground = BrushTextSec,
                Background = Brushes.Transparent,
                BorderThickness = new Thickness(0),
                Cursor = System.Windows.Input.Cursors.Hand,
                Padding = new Thickness(0),
                VerticalContentAlignment = VerticalAlignment.Center,
                HorizontalContentAlignment = HorizontalAlignment.Center,
            };
            btn.MouseEnter += (s, e) => btn.Foreground = blueBrush;
            btn.MouseLeave += (s, e) => btn.Foreground = BrushTextSec;
            return btn;
        }

        // ── System tray ────────────────────────────────────────────
        private void SetupTrayIcon()
        {
            _trayIcon = new System.Windows.Forms.NotifyIcon
            {
                Icon = System.Drawing.SystemIcons.Shield,
                Text = "键盘清洁助手 - 已解锁",
                Visible = true,
            };

            _trayIcon.DoubleClick += (s, e) => ShowFromTray();

            var menu = new System.Windows.Forms.ContextMenuStrip();

            var lockItem = new System.Windows.Forms.ToolStripMenuItem("🔒 锁定键盘");
            lockItem.Click += (s, e) => DoLock();
            menu.Items.Add(lockItem);

            var unlockItem = new System.Windows.Forms.ToolStripMenuItem("🔓 解锁键盘");
            unlockItem.Click += (s, e) => DoUnlock();
            menu.Items.Add(unlockItem);

            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var showItem = new System.Windows.Forms.ToolStripMenuItem("显示窗口");
            showItem.Click += (s, e) => ShowFromTray();
            menu.Items.Add(showItem);

            menu.Items.Add(new System.Windows.Forms.ToolStripSeparator());

            var exitItem = new System.Windows.Forms.ToolStripMenuItem("退出");
            exitItem.Click += (s, e) => ExitApplication();
            menu.Items.Add(exitItem);

            _trayIcon.ContextMenuStrip = menu;
        }

        // ── Lock toggle ────────────────────────────────────────────
        private void OnToggleClick(object sender, RoutedEventArgs e)
        {
            if (_isLocked) DoUnlock();
            else DoLock();
        }

        private void DoLock()
        {
            if (_isLocked) return;
            try { _hook.Install(); }
            catch (Exception ex)
            {
                System.Windows.Forms.MessageBox.Show(
                    "无法安装键盘钩子：\n" + ex.Message + "\n\n请尝试以管理员身份运行此程序。",
                    "错误", System.Windows.Forms.MessageBoxButtons.OK,
                    System.Windows.Forms.MessageBoxIcon.Error);
                return;
            }
            _isLocked = true;
            UpdateUIState();
        }

        private void DoUnlock()
        {
            if (!_isLocked) return;
            _hook.Uninstall();
            _isLocked = false;
            UpdateUIState();
        }

        private void OnEmergencyUnlock()
        {
            if (_isLocked)
            {
                DoUnlock();
                FlashWindow();
            }
        }

        // ── UI state updates ───────────────────────────────────────
        private void UpdateUIState()
        {
            if (_isLocked)
            {
                _statusCircle.Fill = BrushLocked;
                ((DropShadowEffect)_statusCircle.Effect).Color = Color.FromRgb(0xE9, 0x45, 0x60);
                _statusTitle.Text = "键盘已锁定";
                _statusSubtitle.Text = "所有按键已被拦截，放心清洁吧 ✨";
                _toggleButton.Content = "🔓  点 击 解 锁";
                _toggleButton.Background = BrushBtnLocked;
                _trayIcon.Text = "键盘清洁助手 - 🔒 已锁定";

                StartPulseAnimation();
            }
            else
            {
                _statusCircle.Fill = BrushUnlocked;
                ((DropShadowEffect)_statusCircle.Effect).Color = Color.FromRgb(0x2E, 0xCC, 0x71);
                _statusTitle.Text = "键盘已解锁";
                _statusSubtitle.Text = "可以正常使用键盘";
                _toggleButton.Content = "🔒  点 击 锁 定";
                _toggleButton.Background = BrushBtnUnlock;
                _trayIcon.Text = "键盘清洁助手 - 🔓 已解锁";

                StopPulseAnimation();
            }
        }

        // ── Pulse animation for locked state ───────────────────────
        private void StartPulseAnimation()
        {
            var da = new DoubleAnimation
            {
                From = 1.0,
                To = 1.15,
                Duration = TimeSpan.FromMilliseconds(1000),
                AutoReverse = true,
                RepeatBehavior = RepeatBehavior.Forever,
                EasingFunction = new SineEase { EasingMode = EasingMode.EaseInOut },
            };
            _statusCircleScale.BeginAnimation(ScaleTransform.ScaleXProperty, da);
            _statusCircleScale.BeginAnimation(ScaleTransform.ScaleYProperty, da);
        }

        private void StopPulseAnimation()
        {
            _statusCircleScale.BeginAnimation(ScaleTransform.ScaleXProperty, null);
            _statusCircleScale.BeginAnimation(ScaleTransform.ScaleYProperty, null);
            _statusCircleScale.ScaleX = 1.0;
            _statusCircleScale.ScaleY = 1.0;
        }

        // ── Tray management ────────────────────────────────────────
        private void HideToTray()
        {
            Hide();
            ShowInTaskbar = false;
        }

        private void ShowFromTray()
        {
            Show();
            ShowInTaskbar = true;
            WindowState = WindowState.Normal;

            // Bring to front
            var hwnd = new WindowInteropHelper(this).Handle;
            if (hwnd != IntPtr.Zero)
            {
                NativeMethods.ShowWindow(hwnd, NativeMethods.SW_RESTORE);
                NativeMethods.SetForegroundWindow(hwnd);
            }
        }

        // ── Visual feedback ────────────────────────────────────────
        private void FlashWindow()
        {
            // Brief opacity flash to signal the emergency unlock succeeded
            var da = new DoubleAnimation
            {
                From = 1.0,
                To = 0.4,
                Duration = TimeSpan.FromMilliseconds(120),
                AutoReverse = true,
                RepeatBehavior = new RepeatBehavior(3),
            };
            this.BeginAnimation(OpacityProperty, da);

            // Also flash the tray with a balloon tip
            _trayIcon.ShowBalloonTip(2000, "键盘清洁助手", "键盘已通过快捷键解锁！", System.Windows.Forms.ToolTipIcon.Info);
        }

        // ── Window events ──────────────────────────────────────────
        private void OnClosing(object sender, CancelEventArgs e)
        {
            // Close button → minimize to tray, don't exit
            e.Cancel = true;
            HideToTray();
        }

        // ── Clean exit ─────────────────────────────────────────────
        private void ExitApplication()
        {
            _hook.Uninstall();
            _hook.Dispose();

            _trayIcon.Visible = false;
            _trayIcon.Dispose();

            Application.Current.Shutdown();
        }
    }
}
