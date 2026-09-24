using System;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Threading;
using OkidokeiWidget.Core.Monitors;
using OkidokeiWidget.Core.Persistence;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.App;

public partial class ClockWindow : Window
{
    private const double DefaultTimeFontSize = 24.0;
    private const double DefaultDateFontSize = 14.0;

    private readonly WidgetSettings _settings;
    private readonly MonitorPlacement _placement;
    private ConnectedMonitor _monitor;
    private readonly Action _openSettingsWindow;
    private readonly Action _onWindowBehaviorChanged;
    private readonly Action _onDpiChanged;
    private readonly DispatcherTimer _timer;
    private bool _isDragging;
    private Point _dragLastPointerPosition;

    public ClockWindow(
        WidgetSettings settings,
        ConnectedMonitor monitor,
        MonitorPlacement placement,
        Action openSettingsWindow,
        Action onWindowBehaviorChanged,
        Action onDpiChanged)
    {
        InitializeComponent();

        _settings = settings;
        _placement = placement;
        _monitor = monitor;
        _openSettingsWindow = openSettingsWindow;
        _onWindowBehaviorChanged = onWindowBehaviorChanged;
        _onDpiChanged = onDpiChanged;

        // MonitorPlacement.X/Y はモニタの作業領域左上を基準とした相対座標のため、仮想
        // デスクトップ上の絶対座標 (物理ピクセル) へ変換してから配置する (data-model.md)。
        // ウィンドウハンドルの生成後でないと配置できないため SourceInitialized で行い、
        // ウィンドウサイズが確定する ContentRendered で作業領域内へ収め直す (issue #10)
        SourceInitialized += (_, _) => ApplyPlacement();
        ContentRendered += (_, _) => ApplyPlacement();

        // フォントサイズや日付/曜日表示の変更でサイズが変わっても、アンカーからの位置を保つ (SC-007)
        SizeChanged += (_, _) => ApplyPlacement();

        // DPI スケールのみの変更 (WM_DISPLAYCHANGE は飛ばない) では、既定では WPF が Windows の
        // 提案する矩形へウィンドウを自動移動させてしまい、位置ロック中でも保存済み座標からずれる。
        // DpiChanged は WPF のその自動移動が完了した後に発火するため、ここで最新のモニタ情報へ
        // 差し替えて保存済みの座標から配置し直すことでロック位置を維持する (issue #25)
        DpiChanged += (_, _) => _onDpiChanged();

        ApplyAppearance();
        ApplyWindowBehavior();
        UpdateClockText();

        _timer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(1) };
        _timer.Tick += (_, _) => UpdateClockText();
        _timer.Start();
    }

    /// <summary>
    /// 稼働中のモニタ構成の変化 (取り外し・再接続・解像度変更) でモニタの作業領域が動いた場合に、
    /// 最新のモニタ情報へ差し替えて配置し直す (issue #10)。
    /// </summary>
    public void UpdateMonitor(ConnectedMonitor monitor)
    {
        _monitor = monitor;
        ApplyPlacement();
    }

    /// <summary>
    /// 保存された配置 (相対座標またはアンカー指定) に従ってウィンドウを配置する。座標は WPF の
    /// 論理単位ではなく物理ピクセルで扱う (issue #10、<see cref="WindowPositionHelper"/>)。
    /// </summary>
    private void ApplyPlacement()
    {
        var bounds = WindowPositionHelper.TryGetBounds(this);
        var dpiScale = VisualTreeHelper.GetDpi(this).DpiScaleX;
        var (x, y) = WidgetPlacementCalculator.ToAbsolutePosition(
            _monitor, _placement, bounds?.Width ?? 0, bounds?.Height ?? 0, dpiScale);

        WindowPositionHelper.MoveTo(this, x, y);
    }

    /// <summary>
    /// このモニタの横位置を変更してアンカー指定にする。縦位置は、アンカー指定中ならそのまま、
    /// 自由配置中なら今の位置から一番近いものにする (research.md #15)。本体とタスクトレイの
    /// どちらのメニューからもこの経路で変更する (research.md #17)。位置ロック中は何もしない (FR-010)。
    /// </summary>
    public void SetAnchorHorizontal(AnchorHorizontal horizontal)
    {
        if (_settings.WindowBehavior.PositionLocked || WindowPositionHelper.TryGetBounds(this) is not { } bounds)
        {
            return;
        }

        SetAnchor(WidgetPlacementCalculator.WithHorizontal(_monitor, _placement, horizontal, bounds.Y, bounds.Height));
    }

    /// <summary>
    /// このモニタの縦位置を変更してアンカー指定にする。横位置の決め方は
    /// <see cref="SetAnchorHorizontal"/> と同じ。
    /// </summary>
    public void SetAnchorVertical(AnchorVertical vertical)
    {
        if (_settings.WindowBehavior.PositionLocked || WindowPositionHelper.TryGetBounds(this) is not { } bounds)
        {
            return;
        }

        SetAnchor(WidgetPlacementCalculator.WithVertical(_monitor, _placement, vertical, bounds.X, bounds.Width));
    }

    private void SetAnchor(AnchorPosition anchor)
    {
        _placement.Anchor = anchor;
        ApplyPlacement();
        SettingsRepository.Save(_settings);
    }

    /// <summary>
    /// このモニタのアンカー指定時の余白を変更する。自由配置中は値を保存するだけで、位置は
    /// 変わらない (research.md #16)。位置ロック中は何もしない (FR-010)。
    /// </summary>
    public void SetAnchorMargin(AnchorMargin margin)
    {
        if (_settings.WindowBehavior.PositionLocked)
        {
            return;
        }

        _placement.AnchorMargin = margin;
        ApplyPlacement();
        SettingsRepository.Save(_settings);
    }

    private void BackgroundBorder_ContextMenuOpening(object sender, ContextMenuEventArgs e)
    {
        PlacementMenuBuilder.Populate(
            PlacementMenuItem,
            _placement,
            _settings.WindowBehavior.PositionLocked,
            SetAnchorHorizontal,
            SetAnchorVertical,
            SetAnchorMargin);
    }

    public void ApplyWindowBehavior()
    {
        var behavior = _settings.WindowBehavior;

        Topmost = behavior.TopMost;
        PositionLockMenuItem.IsChecked = behavior.PositionLocked;
        TopMostMenuItem.IsChecked = behavior.TopMost;
    }

    public void ApplyAppearance()
    {
        var appearance = _settings.Appearance;

        DateText.Visibility = appearance.ShowDate ? Visibility.Visible : Visibility.Collapsed;
        DayOfWeekText.Visibility = appearance.ShowDayOfWeek ? Visibility.Visible : Visibility.Collapsed;
        DateDayOfWeekSpacer.Visibility = appearance.ShowDate && appearance.ShowDayOfWeek ? Visibility.Visible : Visibility.Collapsed;
        DateRow.Visibility = appearance.ShowDate || appearance.ShowDayOfWeek ? Visibility.Visible : Visibility.Collapsed;

        // FR-029: 時刻表示に対する日付/曜日表示の相対位置 (上/下/左/右)
        var (dock, margin) = appearance.DateDayOfWeekPosition switch
        {
            RelativePosition.Above => (Dock.Top, new Thickness(0, 0, 0, 2)),
            RelativePosition.Left => (Dock.Left, new Thickness(0, 0, 8, 0)),
            RelativePosition.Right => (Dock.Right, new Thickness(8, 0, 0, 0)),
            _ => (Dock.Bottom, new Thickness(0, 2, 0, 0)),
        };
        DockPanel.SetDock(DateRow, dock);
        DateRow.Margin = margin;

        var installedFontNames = Fonts.SystemFontFamilies.Select(f => f.Source).ToList();
        var resolvedFontName = FontResolver.Resolve(appearance.FontFamily, installedFontNames);
        var fontFamily = resolvedFontName is null ? SystemFonts.MessageFontFamily : new FontFamily(resolvedFontName);

        var timeFontSize = appearance.TimeFontSize > 0 ? appearance.TimeFontSize : DefaultTimeFontSize;
        var dateFontSize = appearance.DateFontSize > 0 ? appearance.DateFontSize : DefaultDateFontSize;
        var timeForeground = new SolidColorBrush(ParseColor(ColorHexResolver.Resolve(appearance.TimeFontColor)));
        var dateForeground = new SolidColorBrush(ParseColor(ColorHexResolver.Resolve(appearance.DateFontColor)));

        TimeText.FontFamily = fontFamily;
        TimeText.FontSize = timeFontSize;
        TimeText.Foreground = timeForeground;

        foreach (var textBlock in new[] { DateText, DateDayOfWeekSpacer, DayOfWeekText })
        {
            textBlock.FontFamily = fontFamily;
            textBlock.FontSize = dateFontSize;
            textBlock.Foreground = dateForeground;
        }

        // BackgroundOpacity: 0 = 完全に不透明、100 = 完全に透明 (FR-008)。
        // BackgroundColor の A 成分は無視し、実際のアルファ値は BackgroundOpacity から算出する (FR-030)
        var alpha = BackgroundAlphaResolver.Resolve(appearance.BackgroundOpacity);
        var backgroundRgb = ParseColor(ColorHexResolver.Resolve(appearance.BackgroundColor));
        BackgroundBorder.Background = new SolidColorBrush(
            System.Windows.Media.Color.FromArgb(alpha, backgroundRgb.R, backgroundRgb.G, backgroundRgb.B));
    }

    private static System.Windows.Media.Color ParseColor(string argbHex)
    {
        var a = Convert.ToByte(argbHex.Substring(1, 2), 16);
        var r = Convert.ToByte(argbHex.Substring(3, 2), 16);
        var g = Convert.ToByte(argbHex.Substring(5, 2), 16);
        var b = Convert.ToByte(argbHex.Substring(7, 2), 16);
        return System.Windows.Media.Color.FromArgb(a, r, g, b);
    }

    private void UpdateClockText()
    {
        var now = DateTime.Now;
        var appearance = _settings.Appearance;

        TimeText.Text = appearance.ShowSeconds ? now.ToString("HH:mm:ss") : now.ToString("HH:mm");

        var separator = DateSeparatorResolver.Resolve(appearance.DateSeparator);
        DateText.Text = now.ToString($"yyyy{separator}MM{separator}dd");
        DayOfWeekText.Text = DayOfWeekFormatter.Format(now.DayOfWeek, appearance.DayOfWeekFormat);
    }

    private void OpenSettingsMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _openSettingsWindow();
    }

    private void PositionLockMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _settings.WindowBehavior.PositionLocked = PositionLockMenuItem.IsChecked;
        _onWindowBehaviorChanged();
    }

    private void TopMostMenuItem_Click(object sender, RoutedEventArgs e)
    {
        _settings.WindowBehavior.TopMost = TopMostMenuItem.IsChecked;
        _onWindowBehaviorChanged();
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e)
    {
        System.Windows.Application.Current.Shutdown();
    }

    private void BackgroundBorder_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (_settings.WindowBehavior.PositionLocked)
        {
            return;
        }

        _isDragging = true;
        _dragLastPointerPosition = e.GetPosition(this);
        BackgroundBorder.CaptureMouse();
    }

    private void BackgroundBorder_MouseMove(object sender, MouseEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        var position = e.GetPosition(this);
        Left += position.X - _dragLastPointerPosition.X;
        Top += position.Y - _dragLastPointerPosition.Y;
    }

    private void BackgroundBorder_MouseLeftButtonUp(object sender, MouseButtonEventArgs e)
    {
        if (!_isDragging)
        {
            return;
        }

        _isDragging = false;
        BackgroundBorder.ReleaseMouseCapture();

        // FR-015: ドラッグ終了時の位置をこのモニタの MonitorPlacement へ反映して永続化する。
        // WPF の Left/Top は論理単位なので、物理ピクセルでウィンドウ位置を取得する (issue #10)
        var windowBounds = WindowPositionHelper.TryGetBounds(this);
        if (windowBounds is not { } bounds)
        {
            return;
        }

        var (relativeX, relativeY) = WidgetPlacementCalculator.ToRelativePosition(_monitor, bounds.X, bounds.Y);
        _placement.X = relativeX;
        _placement.Y = relativeY;

        // ドラッグで動かしたら、以後はアンカーではなくドラッグ後の座標を使う (FR-036)
        _placement.Anchor = null;
        SettingsRepository.Save(_settings);
    }
}
