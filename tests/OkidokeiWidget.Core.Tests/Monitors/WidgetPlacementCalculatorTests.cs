using OkidokeiWidget.Core.Monitors;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Monitors;

public class WidgetPlacementCalculatorTests
{
    // 仮想デスクトップ上でプライマリの左側に並ぶ 4K モニタ (実機の構成を模したケース)
    private static readonly ConnectedMonitor SecondaryMonitor =
        new("monitor-2", IsPrimary: false, DisplayNumber: 2, WorkAreaX: -3840, WorkAreaY: -1071, WorkAreaWidth: 3840, WorkAreaHeight: 2052);

    private static readonly ConnectedMonitor PrimaryMonitor =
        new("monitor-1", IsPrimary: true, DisplayNumber: 1, WorkAreaX: 0, WorkAreaY: 0, WorkAreaWidth: 1920, WorkAreaHeight: 1032);

    [Fact]
    public void ToAbsolutePosition_作業領域の原点を加えた絶対座標を返す()
    {
        var placement = new MonitorPlacement { X = 1000, Y = 500 };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(SecondaryMonitor, placement, 400, 200, dpiScale: 1.0);

        Assert.Equal(-2840, x);
        Assert.Equal(-571, y);
    }

    [Fact]
    public void ToAbsolutePosition_作業領域からはみ出す位置は内側へ収める()
    {
        // モニタの取り外しで作業領域の原点や大きさが変わった場合に、保存済みの座標のままでは
        // 画面外へ出てしまうケース (issue #10)
        var placement = new MonitorPlacement { X = 3700, Y = 2000 };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(PrimaryMonitor, placement, 400, 200, dpiScale: 1.0);

        Assert.Equal(1520, x);
        Assert.Equal(832, y);
    }

    [Fact]
    public void ToAbsolutePosition_負の相対座標は作業領域の左上へ収める()
    {
        var placement = new MonitorPlacement { X = -500, Y = -300 };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(SecondaryMonitor, placement, 400, 200, dpiScale: 1.0);

        Assert.Equal(-3840, x);
        Assert.Equal(-1071, y);
    }

    [Fact]
    public void ToAbsolutePosition_サイズ未確定の0を渡しても左上は作業領域内に収まる()
    {
        var placement = new MonitorPlacement { X = 5000, Y = 3000 };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(PrimaryMonitor, placement, 0, 0, dpiScale: 1.0);

        Assert.Equal(1919, x);
        Assert.Equal(1031, y);
    }

    [Fact]
    public void ToAbsolutePosition_作業領域より大きいウィジェットは左上に合わせる()
    {
        var placement = new MonitorPlacement { X = 100, Y = 100 };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(PrimaryMonitor, placement, 4000, 3000, dpiScale: 1.0);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Theory]
    [InlineData(AnchorPosition.TopLeft, 8, 8)]
    [InlineData(AnchorPosition.Top, 760, 8)]
    [InlineData(AnchorPosition.TopRight, 1512, 8)]
    [InlineData(AnchorPosition.Left, 8, 416)]
    [InlineData(AnchorPosition.Center, 760, 416)]
    [InlineData(AnchorPosition.Right, 1512, 416)]
    [InlineData(AnchorPosition.BottomLeft, 8, 824)]
    [InlineData(AnchorPosition.Bottom, 760, 824)]
    [InlineData(AnchorPosition.BottomRight, 1512, 824)]
    public void ToAbsolutePosition_アンカー指定では作業領域の端から余白を空けて配置する(
        AnchorPosition anchor, int expectedX, int expectedY)
    {
        var placement = new MonitorPlacement { X = 123, Y = 45, Anchor = anchor, AnchorMargin = AnchorMargin.Narrow };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(PrimaryMonitor, placement, 400, 200, dpiScale: 1.0);

        Assert.Equal(expectedX, x);
        Assert.Equal(expectedY, y);
    }

    [Fact]
    public void ToAbsolutePosition_余白は拡大率に応じて物理ピクセルへ換算する()
    {
        // 広め (24 DIP) × 拡大率 150% = 36 px
        var placement = new MonitorPlacement { Anchor = AnchorPosition.TopRight, AnchorMargin = AnchorMargin.Wide };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(PrimaryMonitor, placement, 400, 200, dpiScale: 1.5);

        Assert.Equal(1484, x);
        Assert.Equal(36, y);
    }

    [Fact]
    public void ToAbsolutePosition_アンカーの中央の軸では余白を使わない()
    {
        var placement = new MonitorPlacement { Anchor = AnchorPosition.Top, AnchorMargin = AnchorMargin.Wide };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(PrimaryMonitor, placement, 400, 200, dpiScale: 1.0);

        Assert.Equal(760, x);
        Assert.Equal(24, y);
    }

    [Fact]
    public void ToAbsolutePosition_アンカー指定でも作業領域の原点を基準にする()
    {
        var placement = new MonitorPlacement { Anchor = AnchorPosition.BottomRight, AnchorMargin = AnchorMargin.Narrow };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(SecondaryMonitor, placement, 400, 200, dpiScale: 1.0);

        Assert.Equal(-408, x);
        Assert.Equal(773, y);
    }

    [Fact]
    public void ToAbsolutePosition_アンカー指定で作業領域より大きいウィジェットは左上に合わせる()
    {
        var placement = new MonitorPlacement { Anchor = AnchorPosition.BottomRight, AnchorMargin = AnchorMargin.Wide };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(PrimaryMonitor, placement, 4000, 3000, dpiScale: 1.0);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Theory]
    [InlineData(0, AnchorPosition.TopRight)]
    [InlineData(450, AnchorPosition.Right)]
    [InlineData(800, AnchorPosition.BottomRight)]
    public void WithHorizontal_自由配置中は縦位置を今の位置から一番近いものにする(int currentY, AnchorPosition expected)
    {
        // 作業領域の高さ 1032 を 3 等分 (344 / 688) し、ウィジェット (高さ 100) の中心で判定する
        var placement = new MonitorPlacement { X = 0, Y = currentY };

        var anchor = WidgetPlacementCalculator.WithHorizontal(PrimaryMonitor, placement, AnchorHorizontal.Right, currentY, 100);

        Assert.Equal(expected, anchor);
    }

    [Fact]
    public void WithHorizontal_アンカー指定中は縦位置をそのまま使う()
    {
        var placement = new MonitorPlacement { Anchor = AnchorPosition.BottomLeft };

        var anchor = WidgetPlacementCalculator.WithHorizontal(PrimaryMonitor, placement, AnchorHorizontal.Right, 0, 100);

        Assert.Equal(AnchorPosition.BottomRight, anchor);
    }

    [Fact]
    public void WithVertical_自由配置中は横位置を今の位置から一番近いものにする()
    {
        // 幅 1920 を 3 等分 (640 / 1280)。ウィジェット (幅 200) の中心は 1800 なので右
        var placement = new MonitorPlacement { X = 1700, Y = 0 };

        var anchor = WidgetPlacementCalculator.WithVertical(PrimaryMonitor, placement, AnchorVertical.Top, 1700, 200);

        Assert.Equal(AnchorPosition.TopRight, anchor);
    }

    [Fact]
    public void WithVertical_アンカー指定中は横位置をそのまま使う()
    {
        var placement = new MonitorPlacement { Anchor = AnchorPosition.TopLeft };

        var anchor = WidgetPlacementCalculator.WithVertical(PrimaryMonitor, placement, AnchorVertical.Bottom, 1700, 200);

        Assert.Equal(AnchorPosition.BottomLeft, anchor);
    }

    [Theory]
    [InlineData(AnchorPosition.TopLeft)]
    [InlineData(AnchorPosition.Top)]
    [InlineData(AnchorPosition.TopRight)]
    [InlineData(AnchorPosition.Left)]
    [InlineData(AnchorPosition.Center)]
    [InlineData(AnchorPosition.Right)]
    [InlineData(AnchorPosition.BottomLeft)]
    [InlineData(AnchorPosition.Bottom)]
    [InlineData(AnchorPosition.BottomRight)]
    public void AnchorAxes_横位置と縦位置に分けて組み立て直すと元に戻る(AnchorPosition anchor)
    {
        var composed = AnchorAxes.Compose(AnchorAxes.HorizontalOf(anchor), AnchorAxes.VerticalOf(anchor));

        Assert.Equal(anchor, composed);
    }

    [Fact]
    public void ToRelativePosition_作業領域の原点を引いた相対座標を返す()
    {
        var (x, y) = WidgetPlacementCalculator.ToRelativePosition(SecondaryMonitor, -887, -1042);

        Assert.Equal(2953, x);
        Assert.Equal(29, y);
    }
}
