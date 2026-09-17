using System;
using System.Windows.Interop;

namespace OkidokeiWidget.App;

/// <summary>
/// WM_DISPLAYCHANGE を受信する非表示ウィンドウ (TrayIconManager と同様、research.md #13 の
/// HwndSource パターン)。モニタの接続・取り外し・解像度変更などディスプレイ構成の変化を検知する
/// (T044、Edge Case: 稼働中のモニタ切断時は自動非表示、設定は保持)。
/// </summary>
public sealed class DisplayChangeNotifier : IDisposable
{
    private const int WM_DISPLAYCHANGE = 0x007E;

    private readonly HwndSource _hwndSource;
    private bool _disposed;

    public event Action? DisplaySettingsChanged;

    public DisplayChangeNotifier()
    {
        var parameters = new HwndSourceParameters("OkidokeiWidgetDisplayChangeHost")
        {
            Width = 0,
            Height = 0,
            WindowStyle = unchecked((int)0x80000000), // WS_POPUP (非表示のメッセージ受信専用ウィンドウ)
        };
        _hwndSource = new HwndSource(parameters);
        _hwndSource.AddHook(WndProc);
    }

    private IntPtr WndProc(IntPtr hwnd, int msg, IntPtr wParam, IntPtr lParam, ref bool handled)
    {
        if (msg == WM_DISPLAYCHANGE)
        {
            DisplaySettingsChanged?.Invoke();
        }

        return IntPtr.Zero;
    }

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        _disposed = true;
        _hwndSource.Dispose();
    }
}
