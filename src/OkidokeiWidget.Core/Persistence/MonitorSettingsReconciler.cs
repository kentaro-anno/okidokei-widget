using OkidokeiWidget.Core.Monitors;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Persistence;

/// <summary>
/// 読み込んだ設定の <see cref="WidgetSettings.Monitors"/> と、現在接続中のモニタ構成を突き合わせる。
/// 接続されていないモニタのエントリは削除せず保持し、新規接続モニタにはデフォルト値を補完する
/// (data-model.md の状態遷移、Edge Case: モニタ取り外し時の設定保持)。
/// </summary>
public static class MonitorSettingsReconciler
{
    // モニタの作業領域中央に寄せる際の目安サイズ (実際のウィンドウサイズはフォント設定に依存するため概算)
    private const int AssumedWidgetWidth = 260;
    private const int AssumedWidgetHeight = 100;

    public static void Reconcile(WidgetSettings settings, IReadOnlyList<ConnectedMonitor> connectedMonitors)
    {
        foreach (var monitor in connectedMonitors)
        {
            if (settings.Monitors.ContainsKey(monitor.Identifier))
            {
                continue;
            }

            settings.Monitors[monitor.Identifier] = new MonitorPlacement
            {
                IsVisible = true,
                X = Math.Max(0, (monitor.WorkAreaWidth - AssumedWidgetWidth) / 2),
                Y = Math.Max(0, (monitor.WorkAreaHeight - AssumedWidgetHeight) / 2),
            };
        }
    }
}
