using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Monitors;

/// <summary>
/// <see cref="MonitorPlacement"/> (モニタの作業領域の左上を基準とした相対座標、またはアンカー指定) と、
/// 仮想デスクトップ上の絶対座標を相互に変換する。座標はいずれも物理ピクセルで扱う。
/// WPF の <c>Window.Left/Top</c> は論理単位 (DIP) であり、拡大率が 100% でないモニタでは
/// 物理座標とずれるため、配置と保存は Win32 API 経由の物理ピクセルで行う (issue #10)。
/// </summary>
public static class WidgetPlacementCalculator
{
    // アンカー指定時の画面端からの余白 (DIP)。実機で見た目を確認して調整してよい (research.md #15)
    private const double NarrowMarginDip = 8;
    private const double WideMarginDip = 24;

    /// <summary>
    /// 保存された配置を、仮想デスクトップ上の絶対座標へ変換する。
    /// <see cref="MonitorPlacement.Anchor"/> が null なら保存済みの相対座標を、値があれば
    /// アンカーと余白から位置を計算する。
    /// モニタの解像度変更や取り外しで座標が作業領域の外を指すことがあるため、ウィジェットが
    /// 画面外に出て操作できなくなることのないよう作業領域内へ収める。
    /// <paramref name="widgetWidth"/> / <paramref name="widgetHeight"/> にウィンドウサイズ
    /// 確定前の 0 を渡した場合は、左上が作業領域内に入ることだけを保証する。
    /// <paramref name="dpiScale"/> はウィンドウが乗っているモニタの拡大率 (100% なら 1.0) で、
    /// 余白を物理ピクセルへ換算するために使う。
    /// </summary>
    public static (int X, int Y) ToAbsolutePosition(
        ConnectedMonitor monitor,
        MonitorPlacement placement,
        int widgetWidth,
        int widgetHeight,
        double dpiScale)
    {
        // サイズ未確定 (0) でも左上が作業領域の内側に収まるように、最低 1 px を確保する
        var effectiveWidth = Math.Max(widgetWidth, 1);
        var effectiveHeight = Math.Max(widgetHeight, 1);

        var (desiredX, desiredY) = placement.Anchor is { } anchor
            ? ToAnchoredPosition(monitor, anchor, placement.AnchorMargin, effectiveWidth, effectiveHeight, dpiScale)
            : (monitor.WorkAreaX + placement.X, monitor.WorkAreaY + placement.Y);

        var x = Clamp(
            desiredX,
            monitor.WorkAreaX,
            monitor.WorkAreaX + monitor.WorkAreaWidth - effectiveWidth);
        var y = Clamp(
            desiredY,
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

    /// <summary>
    /// 右クリックメニューで横位置だけを選んだときの、新しいアンカーを返す。縦位置は、アンカー
    /// 指定中ならその縦位置をそのまま使い、自由配置中なら今の位置から一番近いもの (作業領域を
    /// 上・中央・下に 3 等分し、ウィジェットの中心がある所) にする (research.md #15)。
    /// </summary>
    public static AnchorPosition WithHorizontal(
        ConnectedMonitor monitor,
        MonitorPlacement placement,
        AnchorHorizontal horizontal,
        int currentY,
        int widgetHeight)
    {
        var vertical = placement.Anchor is { } anchor
            ? AnchorAxes.VerticalOf(anchor)
            : NearestVertical(monitor, currentY, widgetHeight);
        return AnchorAxes.Compose(horizontal, vertical);
    }

    /// <summary>
    /// 右クリックメニューで縦位置だけを選んだときの、新しいアンカーを返す。横位置の決め方は
    /// <see cref="WithHorizontal"/> と同じ。
    /// </summary>
    public static AnchorPosition WithVertical(
        ConnectedMonitor monitor,
        MonitorPlacement placement,
        AnchorVertical vertical,
        int currentX,
        int widgetWidth)
    {
        var horizontal = placement.Anchor is { } anchor
            ? AnchorAxes.HorizontalOf(anchor)
            : NearestHorizontal(monitor, currentX, widgetWidth);
        return AnchorAxes.Compose(horizontal, vertical);
    }

    private static AnchorHorizontal NearestHorizontal(ConnectedMonitor monitor, int currentX, int widgetWidth)
    {
        var center = currentX + widgetWidth / 2 - monitor.WorkAreaX;
        if (center < monitor.WorkAreaWidth / 3)
        {
            return AnchorHorizontal.Left;
        }

        return center < monitor.WorkAreaWidth * 2 / 3 ? AnchorHorizontal.Center : AnchorHorizontal.Right;
    }

    private static AnchorVertical NearestVertical(ConnectedMonitor monitor, int currentY, int widgetHeight)
    {
        var center = currentY + widgetHeight / 2 - monitor.WorkAreaY;
        if (center < monitor.WorkAreaHeight / 3)
        {
            return AnchorVertical.Top;
        }

        return center < monitor.WorkAreaHeight * 2 / 3 ? AnchorVertical.Center : AnchorVertical.Bottom;
    }

    private static (int X, int Y) ToAnchoredPosition(
        ConnectedMonitor monitor,
        AnchorPosition anchor,
        AnchorMargin margin,
        int widgetWidth,
        int widgetHeight,
        double dpiScale)
    {
        var marginPx = (int)Math.Round((margin == AnchorMargin.Wide ? WideMarginDip : NarrowMarginDip) * dpiScale);

        // 中央の軸では余白を使わない
        var x = anchor switch
        {
            AnchorPosition.TopLeft or AnchorPosition.Left or AnchorPosition.BottomLeft
                => monitor.WorkAreaX + marginPx,
            AnchorPosition.TopRight or AnchorPosition.Right or AnchorPosition.BottomRight
                => monitor.WorkAreaX + monitor.WorkAreaWidth - widgetWidth - marginPx,
            _ => monitor.WorkAreaX + (monitor.WorkAreaWidth - widgetWidth) / 2,
        };
        var y = anchor switch
        {
            AnchorPosition.TopLeft or AnchorPosition.Top or AnchorPosition.TopRight
                => monitor.WorkAreaY + marginPx,
            AnchorPosition.BottomLeft or AnchorPosition.Bottom or AnchorPosition.BottomRight
                => monitor.WorkAreaY + monitor.WorkAreaHeight - widgetHeight - marginPx,
            _ => monitor.WorkAreaY + (monitor.WorkAreaHeight - widgetHeight) / 2,
        };

        return (x, y);
    }

    // ウィジェットが作業領域より大きい場合は max < min となるため、左上を優先して収める
    private static int Clamp(int value, int min, int max) => Math.Clamp(value, min, Math.Max(min, max));
}
