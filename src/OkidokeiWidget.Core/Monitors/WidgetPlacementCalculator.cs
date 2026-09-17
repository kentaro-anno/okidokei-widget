using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Monitors;

/// <summary>
/// <see cref="MonitorPlacement"/> (モニタの作業領域の左上を基準とした相対座標) と、仮想
/// デスクトップ上の絶対座標を相互に変換する。座標はいずれも物理ピクセルで扱う。
/// WPF の <c>Window.Left/Top</c> は論理単位 (DIP) であり、拡大率が 100% でないモニタでは
/// 物理座標とずれるため、配置と保存は Win32 API 経由の物理ピクセルで行う (issue #10)。
/// </summary>
public static class WidgetPlacementCalculator
{
    /// <summary>
    /// 保存された相対座標を、仮想デスクトップ上の絶対座標へ変換する。
    /// モニタの解像度変更や取り外しで座標が作業領域の外を指すことがあるため、ウィジェットが
    /// 画面外に出て操作できなくなることのないよう作業領域内へ収める。
    /// <paramref name="widgetWidth"/> / <paramref name="widgetHeight"/> にウィンドウサイズ
    /// 確定前の 0 を渡した場合は、左上が作業領域内に入ることだけを保証する。
    /// </summary>
    public static (int X, int Y) ToAbsolutePosition(
        ConnectedMonitor monitor,
        MonitorPlacement placement,
        int widgetWidth,
        int widgetHeight)
    {
        // サイズ未確定 (0) でも左上が作業領域の内側に収まるように、最低 1 px を確保する
        var effectiveWidth = Math.Max(widgetWidth, 1);
        var effectiveHeight = Math.Max(widgetHeight, 1);

        var x = Clamp(
            monitor.WorkAreaX + placement.X,
            monitor.WorkAreaX,
            monitor.WorkAreaX + monitor.WorkAreaWidth - effectiveWidth);
        var y = Clamp(
            monitor.WorkAreaY + placement.Y,
            monitor.WorkAreaY,
            monitor.WorkAreaY + monitor.WorkAreaHeight - effectiveHeight);

        return (x, y);
    }

    /// <summary>
    /// 仮想デスクトップ上の絶対座標を、そのモニタの作業領域を基準とした相対座標へ変換する
    /// (ドラッグ移動後の位置の保存に使う。FR-015)。
    /// </summary>
    public static (int X, int Y) ToRelativePosition(ConnectedMonitor monitor, int absoluteX, int absoluteY)
        => (absoluteX - monitor.WorkAreaX, absoluteY - monitor.WorkAreaY);

    // ウィジェットが作業領域より大きい場合は max < min となるため、左上を優先して収める
    private static int Clamp(int value, int min, int max) => Math.Clamp(value, min, Math.Max(min, max));
}
