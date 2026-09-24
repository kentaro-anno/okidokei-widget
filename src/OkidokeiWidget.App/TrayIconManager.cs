using System;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Interop;

namespace OkidokeiWidget.App;

/// <summary>
/// タスクトレイの常駐アイコンを、Shell_NotifyIcon への P/Invoke + 非表示の HwndSource で実装する
/// (FR-031, FR-032、research.md #13。System.Windows.Forms.NotifyIcon は使わず、UseWindowsForms の
/// 再有効化を避ける)。クリックで <see cref="ActivateAllRequested"/> を発火し、右クリックでは
/// コンストラクタで受け取った関数が返すメニューを表示する。メニューの中身はウィジェット本体と
/// 揃えるため <c>App</c> 側で組み立てる (FR-038、research.md #17)。
/// </summary>
public sealed class TrayIconManager : IDisposable
{
    private const int WM_APP = 0x8000;
    private const int WM_TRAYICON = WM_APP + 1;
    private const int WM_LBUTTONUP = 0x0202;
    private const int WM_RBUTTONUP = 0x0205;

    private const uint NIM_ADD = 0x00000000;
    private const uint NIM_DELETE = 0x00000002;
    private const uint NIF_MESSAGE = 0x00000001;
    private const uint NIF_ICON = 0x00000002;
    private const uint NIF_TIP = 0x00000004;

    [StructLayout(LayoutKind.Sequential, CharSet = CharSet.Unicode)]
    private struct NOTIFYICONDATA
    {
        public uint cbSize;
        public IntPtr hWnd;
        public uint uID;
        public uint uFlags;
        public uint uCallbackMessage;
        public IntPtr hIcon;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 128)]
        public string szTip;

        public uint dwState;
        public uint dwStateMask;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 256)]
        public string szInfo;

        public uint uTimeoutOrVersion;

        [MarshalAs(UnmanagedType.ByValTStr, SizeConst = 64)]
        public string szInfoTitle;

        public uint dwInfoFlags;
        public Guid guidItem;
        public IntPtr hBalloonIcon;
    }

    [DllImport("shell32.dll", CharSet = CharSet.Unicode)]
    private static extern bool Shell_NotifyIcon(uint dwMessage, ref NOTIFYICONDATA lpData);

    private readonly HwndSource _hwndSource;
    private readonly Icon _icon;
    private readonly Func<ContextMenu> _buildContextMenu;
    private NOTIFYICONDATA _iconData;
    private bool _disposed;

    public event Action? ActivateAllRequested;

    public TrayIconManager(Func<ContextMenu> buildContextMenu)
    {
        _buildContextMenu = buildContextMenu;

        var parameters = new HwndSourceParameters("OkidokeiWidgetTrayHost")
        {
            Width = 0,
            Height = 0,
            WindowStyle = unchecked((int)0x80000000), // WS_POPUP (非表示のメッセージ受信専用ウィンドウ)
        };
        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);

        _icon = Icon.ExtractAssociatedIcon(Environment.ProcessPath!) ?? SystemIcons.Application;

        _iconData = new NOTIFYICONDATA
        {
            cbSize = (uint)Marshal.SizeOf<NOTIFYICONDATA>(),
            hWnd = _hwndSource.Handle,
            uID = 1,
            uFlags = NIF_MESSAGE | NIF_ICON | NIF_TIP,
            uCallbackMessage = WM_TRAYICON,
            hIcon = _icon.Handle,
            szTip = "OkidokeiWidget",
        };
        Shell_NotifyIcon(NIM_ADD, ref _iconData);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg != WM_TRAYICON)
        {
            return IntPtr.Zero;
        }

        var mouseMessage = lParam.ToInt32();
        if (mouseMessage == WM_LBUTTONUP)
        {
            ActivateAllRequested?.Invoke();
            handled = true;
        }
        else if (mouseMessage == WM_RBUTTONUP)
        {
            ShowContextMenu();
            handled = true;
        }

        return IntPtr.Zero;
    }

    private void ShowContextMenu()
    {
        // 設定値を反映したチェック状態にするため、開くたびに作り直す
        var menu = _buildContextMenu();

        // 通常のコントロールに紐付かないメニューのため、非表示のホストウィンドウを
        // PlacementTarget にしてマウス位置に表示する
        menu.Placement = System.Windows.Controls.Primitives.PlacementMode.MousePoint;
        menu.IsOpen = true;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        Shell_NotifyIcon(NIM_DELETE, ref _iconData);
        _icon.Dispose();
        _hwndSource.Dispose();
    }
}
