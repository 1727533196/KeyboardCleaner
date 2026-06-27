# 🧹 键盘清洁助手

一个 Windows 小工具，清洁键盘时防止误触导致按键乱触发。

## 使用场景

擦键盘、清理键帽缝隙时，抹布一碰就按出一堆字符，切窗口、关页面、甚至误发消息。打开这个工具，鼠标点一下"锁定"，所有键盘输入被拦截，放心擦。

## 功能

- **一键锁定/解锁键盘** — 点击按钮切换，绿色解锁 / 红色锁定
- **系统托盘运行** — 关窗口不退出，最小化到托盘右下角，随时右键操作
- **紧急快捷键解锁** — `Ctrl + Shift + F12` 强制解锁，鼠标失灵也不怕
- **呼吸灯提示** — 锁定状态红色圆圈脉冲动画，一眼就知道键盘处于保护状态
- **防多开** — 不会重复启动多个实例
- **始终置顶** — 窗口保持在最前，不会被其他窗口遮挡

## 运行

双击 `KeyboardCleaner.exe`，点击 **🔒 点击锁定** 即可。如果按键未被完全拦截，请右键 → **以管理员身份运行**。

## 效果演示

```
┌──────────────────────────────┐
│                        ‒  ✕ │
│                              │
│          🔴 / 🟢             │  ← 锁定红 / 解锁绿
│        键盘已锁定             │
│     所有按键已被拦截          │
│                              │
│    ┌──────────────────┐      │
│    │   🔓 点击解锁     │      │
│    └──────────────────┘      │
│                              │
│   紧急快捷键：Ctrl+Shift+F12  │
└──────────────────────────────┘
```

## 技术

- C# + WPF（.NET Framework 4.x，Windows 10/11 自带运行时）
- `WH_KEYBOARD_LL` 低层键盘钩子，在系统处理按键之前拦截
- 纯代码构建 UI，无 XAML 依赖，`csc.exe` 直接编译

## 编译

```bash
csc.exe /target:winexe /out:KeyboardCleaner.exe ^
  /reference:PresentationCore.dll ^
  /reference:PresentationFramework.dll ^
  /reference:WindowsBase.dll ^
  /reference:System.Xaml.dll ^
  /reference:System.dll ^
  /reference:System.Windows.Forms.dll ^
  /reference:System.Drawing.dll ^
  NativeMethods.cs KeyboardHook.cs MainWindow.cs App.cs
```

> 无需 Visual Studio，Windows 自带的 .NET Framework 编译器即可编译。

## 许可证

MIT
