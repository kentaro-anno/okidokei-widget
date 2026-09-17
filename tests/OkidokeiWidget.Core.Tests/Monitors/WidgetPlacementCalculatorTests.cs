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

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(SecondaryMonitor, placement, 400, 200);

        Assert.Equal(-2840, x);
        Assert.Equal(-571, y);
    }

    [Fact]
    public void ToAbsolutePosition_作業領域からはみ出す位置は内側へ収める()
    {
        // モニタの取り外しで作業領域の原点や大きさが変わった場合に、保存済みの座標のままでは
        // 画面外へ出てしまうケース (issue #10)
        var placement = new MonitorPlacement { X = 3700, Y = 2000 };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(PrimaryMonitor, placement, 400, 200);

        Assert.Equal(1520, x);
        Assert.Equal(832, y);
    }

    [Fact]
    public void ToAbsolutePosition_負の相対座標は作業領域の左上へ収める()
    {
        var placement = new MonitorPlacement { X = -500, Y = -300 };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(SecondaryMonitor, placement, 400, 200);

        Assert.Equal(-3840, x);
        Assert.Equal(-1071, y);
    }

    [Fact]
    public void ToAbsolutePosition_サイズ未確定の0を渡しても左上は作業領域内に収まる()
    {
        var placement = new MonitorPlacement { X = 5000, Y = 3000 };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(PrimaryMonitor, placement, 0, 0);

        Assert.Equal(1919, x);
        Assert.Equal(1031, y);
    }

    [Fact]
    public void ToAbsolutePosition_作業領域より大きいウィジェットは左上に合わせる()
    {
        var placement = new MonitorPlacement { X = 100, Y = 100 };

        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(PrimaryMonitor, placement, 4000, 3000);

        Assert.Equal(0, x);
        Assert.Equal(0, y);
    }

    [Fact]
    public void ToRelativePosition_作業領域の原点を引いた相対座標を返す()
    {
        var (x, y) = WidgetPlacementCalculator.ToRelativePosition(SecondaryMonitor, -887, -1042);

        Assert.Equal(2953, x);
        Assert.Equal(29, y);
    }
}
