using System;
using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;

namespace OkidokeiWidget.App;

/// <summary>
/// ウィンドウの位置を物理ピクセルで扱うための Win32 ラッパー。
/// WPF の <c>Window.Left/Top</c> は論理単位 (DIP) で、そのウィンドウが載っているモニタの
/// 拡大率の分だけ物理座標とずれる。本ウィジェットの位置はモニタの作業領域 (物理ピクセル) を
/// 基準に保存するため、配置と取得は <c>SetWindowPos</c> / <c>GetWindowRect</c> で行う (issue #10)。
/// </summary>
internal static class WindowPositionHelper
{
    private const uint SWP_NOSIZE = 0x0001;
    private const uint SWP_NOZORDER = 0x0004;
    private const uint SWP_NOACTIVATE = 0x0010;

    [StructLayout(LayoutKind.Sequential)]
    private struct RECT
    {
        public int Left;
        public int Top;
        public int Right;
        public int Bottom;
    }

    [StructLayout(LayoutKind.Sequential)]
    private struct POINT
    {
        public int X;
        public int Y;
    }

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetCursorPos(out POINT lpPoint);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool SetWindowPos(IntPtr hWnd, IntPtr hWndInsertAfter, int X, int Y, int cx, int cy, uint uFlags);

    [DllImport("user32.dll")]
    [return: MarshalAs(UnmanagedType.Bool)]
    private static extern bool GetWindowRect(IntPtr hWnd, out RECT lpRect);

    /// <summary>
    /// ウィンドウを仮想デスクトップ上の絶対座標 (物理ピクセル) へ移動する。
    /// ハンドル未生成 (<c>SourceInitialized</c> より前) の場合は何もしない。
    /// </summary>
    public static void MoveTo(Window window, int x, int y)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero)
        {
            return;
        }

        SetWindowPos(handle, IntPtr.Zero, x, y, 0, 0, SWP_NOSIZE | SWP_NOZORDER | SWP_NOACTIVATE);
    }

    /// <summary>
    /// ウィンドウの現在の位置・サイズを物理ピクセルで返す。取得できない場合は <c>null</c>。
    /// </summary>
    public static (int X, int Y, int Width, int Height)? TryGetBounds(Window window)
    {
        var handle = new WindowInteropHelper(window).Handle;
        if (handle == IntPtr.Zero || !GetWindowRect(handle, out var rect))
        {
            return null;
        }

        return (rect.Left, rect.Top, rect.Right - rect.Left, rect.Bottom - rect.Top);
    }

    /// <summary>
    /// マウスの画面座標を物理ピクセルで返す。取得できない場合は <c>null</c>。
    /// Per-Monitor V2 のプロセスでは <c>GetWindowRect</c> と同じ座標系になる (research.md #18)。
    /// </summary>
    public static (int X, int Y)? TryGetCursorPosition()
    {
        return GetCursorPos(out var point) ? (point.X, point.Y) : null;
    }
}
