using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Windows;
using OkidokeiWidget.Core.Monitors;
using OkidokeiWidget.Core.Persistence;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.App;

public partial class App : System.Windows.Application
{
    private const string SingleInstanceMutexName = "OkidokeiWidget.SingleInstance";

    private Mutex? _singleInstanceMutex;
    private readonly Dictionary<string, ClockWindow> _clockWindowsByMonitor = new();
    private IReadOnlyList<ConnectedMonitor> _connectedMonitors = Array.Empty<ConnectedMonitor>();
    private WidgetSettings _settings = null!;
    private SettingsWindow? _settingsWindow;
    private TrayIconManager? _trayIconManager;
    private DisplayChangeNotifier? _displayChangeNotifier;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 二重起動時、2 つ目のプロセスは UI を作らずそのまま終了する (research.md #6)
        _singleInstanceMutex = new Mutex(initiallyOwned: true, SingleInstanceMutexName, out var createdNew);
        if (!createdNew)
        {
            Shutdown();
            return;
        }

        // 個々の ClockWindow の表示/非表示に関わらずアプリを常駐させ続けるため、
        // 終了は右クリックメニューの「終了」から明示的にのみ行う (FR-023)
        ShutdownMode = ShutdownMode.OnExplicitShutdown;

        var (settings, fellBackToDefaults) = SettingsRepository.Load();
        _settings = settings;

        RefreshConnectedMonitors();
        SettingsRepository.Save(settings);

        AutoStartManager.SetEnabled(settings.AutoStartEnabled);
        SyncClockWindows();

        if (fellBackToDefaults)
        {
            MessageBox.Show(
                "設定ファイルの読み込みに失敗したため、デフォルト設定で起動しました。",
                "OkidokeiWidget",
                MessageBoxButton.OK,
                MessageBoxImage.Warning);
        }

        // タスクトレイの常駐アイコン: 最前面表示が無効な場合でもウィジェットを前面に呼び戻す
        // 手段、および終了メニューを提供する (FR-031, FR-032、research.md #13)
        _trayIconManager = new TrayIconManager();
        _trayIconManager.ActivateAllRequested += ActivateAllClockWindows;
        _trayIconManager.ExitRequested += Shutdown;

        // 稼働中のモニタ接続/取り外しを検知し、表示を追随させる (T044、Edge Case: 稼働中の
        // モニタ切断時は自動非表示、設定は保持)
        _displayChangeNotifier = new DisplayChangeNotifier();
        _displayChangeNotifier.DisplaySettingsChanged += OnDisplaySettingsChanged;
    }

    private void RefreshConnectedMonitors()
    {
        _connectedMonitors = MonitorEnumerationService.GetConnectedMonitors();
        MonitorSettingsReconciler.Reconcile(_settings, _connectedMonitors);
    }

    private void OnDisplaySettingsChanged()
    {
        RefreshConnectedMonitors();
        SyncClockWindows();
        SettingsRepository.Save(_settings);
    }

    /// <summary>
    /// 現在接続中のモニタ・<see cref="WidgetSettings.Monitors"/> の表示/非表示設定と、実際に
    /// 開いている <see cref="ClockWindow"/> の集合が一致するように生成/破棄する (FR-014, T033, T045)。
    /// </summary>
    private void SyncClockWindows()
    {
        var currentIds = new HashSet<string>(_connectedMonitors.Select(m => m.Identifier));

        // 稼働中に取り外されたモニタのウィンドウを閉じる。Monitors エントリ自体は保持する
        foreach (var identifier in _clockWindowsByMonitor.Keys.Where(id => !currentIds.Contains(id)).ToList())
        {
            _clockWindowsByMonitor[identifier].Close();
            _clockWindowsByMonitor.Remove(identifier);
        }

        foreach (var monitor in _connectedMonitors)
        {
            var isVisible = _settings.Monitors.TryGetValue(monitor.Identifier, out var placement) && placement.IsVisible;
            var hasWindow = _clockWindowsByMonitor.TryGetValue(monitor.Identifier, out var window);

            if (isVisible && !hasWindow)
            {
                CreateClockWindow(monitor, _settings.Monitors[monitor.Identifier]);
            }
            else if (!isVisible && hasWindow)
            {
                window!.Close();
                _clockWindowsByMonitor.Remove(monitor.Identifier);
            }
            else if (isVisible)
            {
                // モニタの取り外し・再接続・解像度変更で作業領域の原点が動くため、表示中の
                // ウィンドウにも最新のモニタ情報を渡し、保存された位置へ配置し直す (issue #10)
                window!.UpdateMonitor(monitor);
            }
        }
    }

    private void CreateClockWindow(ConnectedMonitor monitor, MonitorPlacement placement)
    {
        var window = new ClockWindow(_settings, monitor, placement, OpenSettingsWindow, OnWindowBehaviorChanged);
        _clockWindowsByMonitor[monitor.Identifier] = window;
        window.Show();
    }

    private void ActivateAllClockWindows()
    {
        foreach (var window in _clockWindowsByMonitor.Values)
        {
            // Topmost を一時的に true にしてから元の値へ戻すことで、「最前面表示」が
            // OFF の場合でも確実に他のウィンドウより前面へ引き上げる
            var originalTopMost = window.Topmost;
            window.Topmost = true;
            window.Activate();
            window.Topmost = originalTopMost;
        }
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _displayChangeNotifier?.Dispose();
        _trayIconManager?.Dispose();
        _singleInstanceMutex?.Dispose();
        base.OnExit(e);
    }

    private void OpenSettingsWindow()
    {
        if (_settingsWindow is not null)
        {
            _settingsWindow.Activate();
            return;
        }

        _settingsWindow = new SettingsWindow(_settings, _connectedMonitors, OnAppearanceChanged, OnMonitorVisibilityChanged, OnAutoStartChanged);
        _settingsWindow.Closed += (_, _) => _settingsWindow = null;
        _settingsWindow.Show();
    }

    private void OnAppearanceChanged()
    {
        foreach (var window in _clockWindowsByMonitor.Values)
        {
            window.ApplyAppearance();
        }

        SettingsRepository.Save(_settings);
    }

    private void OnWindowBehaviorChanged()
    {
        foreach (var window in _clockWindowsByMonitor.Values)
        {
            window.ApplyWindowBehavior();
        }

        SettingsRepository.Save(_settings);
    }

    private void OnMonitorVisibilityChanged()
    {
        SyncClockWindows();
        SettingsRepository.Save(_settings);
    }

    private void OnAutoStartChanged()
    {
        AutoStartManager.SetEnabled(_settings.AutoStartEnabled);
        SettingsRepository.Save(_settings);
    }
}
