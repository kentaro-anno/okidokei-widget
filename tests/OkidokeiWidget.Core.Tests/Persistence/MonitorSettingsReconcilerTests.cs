using OkidokeiWidget.Core.Monitors;
using OkidokeiWidget.Core.Persistence;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Persistence;

public class MonitorSettingsReconcilerTests
{
    [Fact]
    public void Reconcile_現在接続されていないモニタのエントリは削除せず保持する()
    {
        var settings = WidgetSettings.CreateDefault();
        settings.Monitors["DISCONNECTED-MONITOR"] = new MonitorPlacement { IsVisible = false, X = 10, Y = 20 };

        MonitorSettingsReconciler.Reconcile(settings, connectedMonitors: []);

        Assert.True(settings.Monitors.ContainsKey("DISCONNECTED-MONITOR"));
        Assert.Equal(10, settings.Monitors["DISCONNECTED-MONITOR"].X);
    }

    [Fact]
    public void Reconcile_新規接続モニタにはデフォルト値のエントリを補完する()
    {
        var settings = WidgetSettings.CreateDefault();
        var newMonitor = new ConnectedMonitor("NEW-MONITOR", IsPrimary: true, DisplayNumber: 1, WorkAreaX: 0, WorkAreaY: 0, WorkAreaWidth: 1920, WorkAreaHeight: 1040);

        MonitorSettingsReconciler.Reconcile(settings, [newMonitor]);

        Assert.True(settings.Monitors.TryGetValue("NEW-MONITOR", out var placement));
        Assert.True(placement!.IsVisible);
    }

    [Fact]
    public void Reconcile_既存エントリのあるモニタは上書きしない()
    {
        var settings = WidgetSettings.CreateDefault();
        settings.Monitors["EXISTING-MONITOR"] = new MonitorPlacement { IsVisible = false, X = 500, Y = 600 };
        var existingMonitor = new ConnectedMonitor("EXISTING-MONITOR", IsPrimary: true, DisplayNumber: 1, WorkAreaX: 0, WorkAreaY: 0, WorkAreaWidth: 1920, WorkAreaHeight: 1040);

        MonitorSettingsReconciler.Reconcile(settings, [existingMonitor]);

        var placement = settings.Monitors["EXISTING-MONITOR"];
        Assert.False(placement.IsVisible);
        Assert.Equal(500, placement.X);
    }
}
