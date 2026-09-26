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

    [Fact]
    public void ClampToWorkArea_作業領域の内側ならそのまま返す()
    {
        var (x, y) = WidgetPlacementCalculator.ClampToWorkArea(PrimaryMonitor, 500, 300, 400, 200);

        Assert.Equal(500, x);
        Assert.Equal(300, y);
    }

    // ドラッグで作業領域の外へ出ようとしても、端で止まる (FR-009、issue #39)
    [Theory]
    [InlineData(-50, 300, 0, 300)] // 左
    [InlineData(1600, 300, 1520, 300)] // 右
    [InlineData(500, -30, 500, 0)] // 上
    [InlineData(500, 900, 500, 832)] // 下 (タスクバーの方向)
    [InlineData(-50, 900, 0, 832)] // 左下の角
    public void ClampToWorkArea_作業領域の外なら端に止める(int inputX, int inputY, int expectedX, int expectedY)
    {
        var (x, y) = WidgetPlacementCalculator.ClampToWorkArea(PrimaryMonitor, inputX, inputY, 400, 200);

        Assert.Equal(expectedX, x);
        Assert.Equal(expectedY, y);
    }

    [Fact]
    public void ClampToWorkArea_原点が負のモニタでも右隣のモニタへはみ出さない()
    {
        // プライマリの左にあるモニタから、右の境界 (x = 0) を越えてドラッグしたケース
        var (x, y) = WidgetPlacementCalculator.ClampToWorkArea(SecondaryMonitor, 100, -1200, 400, 200);

        Assert.Equal(-400, x);
        Assert.Equal(-1071, y);
    }

    [Fact]
    public void ClampToWorkArea_作業領域より大きいウィジェットは左上に合わせる()
    {
        var (x, y) = WidgetPlacementCalculator.ClampToWorkArea(PrimaryMonitor, 100, 100, 4000, 3000);

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
    public void ApplyMarginToNearEdges_右の縁に接していれば余白ぶん内側へ離し縦は動かさない()
    {
        var result = WidgetPlacementCalculator.ApplyMarginToNearEdges(
            PrimaryMonitor, 1520, 400, 400, 200, AnchorMargin.Narrow, dpiScale: 1.0);

        Assert.Equal((1512, 400), result);
    }

    [Fact]
    public void ApplyMarginToNearEdges_縁までの距離がちょうど余白と同じなら位置は変わらない()
    {
        var result = WidgetPlacementCalculator.ApplyMarginToNearEdges(
            PrimaryMonitor, 1512, 400, 400, 200, AnchorMargin.Narrow, dpiScale: 1.0);

        Assert.Equal((1512, 400), result);
    }

    [Fact]
    public void ApplyMarginToNearEdges_どの縁からも余白より離れていればnullを返す()
    {
        // 右の縁から 9 px (狭めの 8 px + 1 px)。他の縁からも十分に離れている
        var result = WidgetPlacementCalculator.ApplyMarginToNearEdges(
            PrimaryMonitor, 1511, 400, 400, 200, AnchorMargin.Narrow, dpiScale: 1.0);

        Assert.Null(result);
    }

    [Fact]
    public void ApplyMarginToNearEdges_狭めより離れていても広めの範囲内なら広めでは動かせる()
    {
        // 右の縁から 16 px
        var narrow = WidgetPlacementCalculator.ApplyMarginToNearEdges(
            PrimaryMonitor, 1504, 400, 400, 200, AnchorMargin.Narrow, dpiScale: 1.0);
        var wide = WidgetPlacementCalculator.ApplyMarginToNearEdges(
            PrimaryMonitor, 1504, 400, 400, 200, AnchorMargin.Wide, dpiScale: 1.0);

        Assert.Null(narrow);
        Assert.Equal((1496, 400), wide);
    }

    [Fact]
    public void ApplyMarginToNearEdges_右下の隅に接していれば右と下の両方から離す()
    {
        var result = WidgetPlacementCalculator.ApplyMarginToNearEdges(
            PrimaryMonitor, 1520, 832, 400, 200, AnchorMargin.Wide, dpiScale: 1.0);

        Assert.Equal((1496, 808), result);
    }

    [Theory]
    [InlineData(0, 0, 8, 8)]
    [InlineData(3, 400, 8, 400)]
    [InlineData(700, 5, 700, 8)]
    public void ApplyMarginToNearEdges_左や上の縁でも余白ぶん内側へ離す(int x, int y, int expectedX, int expectedY)
    {
        var result = WidgetPlacementCalculator.ApplyMarginToNearEdges(
            PrimaryMonitor, x, y, 400, 200, AnchorMargin.Narrow, dpiScale: 1.0);

        Assert.Equal((expectedX, expectedY), result);
    }

    [Theory]
    [InlineData(1520, AnchorMargin.Narrow, 1508)]
    [InlineData(1520, AnchorMargin.Wide, 1484)]
    [InlineData(1508, AnchorMargin.Narrow, 1508)]
    public void ApplyMarginToNearEdges_拡大率150パーセントでは余白を物理ピクセルに換算して判定する(
        int x, AnchorMargin margin, int expectedX)
    {
        // 狭め = 12 px、広め = 36 px
        var result = WidgetPlacementCalculator.ApplyMarginToNearEdges(
            PrimaryMonitor, x, 400, 400, 200, margin, dpiScale: 1.5);

        Assert.Equal((expectedX, 400), result);
    }

    [Fact]
    public void ApplyMarginToNearEdges_拡大率150パーセントで狭めより1ピクセル離れていればnullを返す()
    {
        var result = WidgetPlacementCalculator.ApplyMarginToNearEdges(
            PrimaryMonitor, 1507, 400, 400, 200, AnchorMargin.Narrow, dpiScale: 1.5);

        Assert.Null(result);
    }

    [Theory]
    [InlineData(-3840, -1071, AnchorMargin.Wide, -3816, -1047)]
    [InlineData(-400, 781, AnchorMargin.Narrow, -408, 773)]
    public void ApplyMarginToNearEdges_作業領域の原点が負のモニタでも縁から離す(
        int x, int y, AnchorMargin margin, int expectedX, int expectedY)
    {
        var result = WidgetPlacementCalculator.ApplyMarginToNearEdges(
            SecondaryMonitor, x, y, 400, 200, margin, dpiScale: 1.0);

        Assert.Equal((expectedX, expectedY), result);
    }

    [Fact]
    public void ToRelativePosition_作業領域の原点を引いた相対座標を返す()
    {
        var (x, y) = WidgetPlacementCalculator.ToRelativePosition(SecondaryMonitor, -887, -1042);

        Assert.Equal(2953, x);
        Assert.Equal(29, y);
    }
}
