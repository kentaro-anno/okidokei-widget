using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OkidokeiWidget.Core.Monitors;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.App;

public partial class SettingsWindow : Window
{
    private sealed record DayOfWeekFormatOption(DayOfWeekFormat Value, string Label);

    private static readonly DayOfWeekFormatOption[] DayOfWeekFormatOptions =
    [
        new(DayOfWeekFormat.ShortKanjiParen, "(月)"),
        new(DayOfWeekFormat.LongKanji, "月曜日"),
        new(DayOfWeekFormat.ShortEnglish, "Mon."),
        new(DayOfWeekFormat.LongEnglish, "Monday"),
    ];

    private static readonly string[] DateSeparatorOptions = ["/", "-"];

    private sealed record DateDayOfWeekPositionOption(RelativePosition Value, string Label);

    private static readonly DateDayOfWeekPositionOption[] DateDayOfWeekPositionOptions =
    [
        new(RelativePosition.Above, "上"),
        new(RelativePosition.Below, "下"),
        new(RelativePosition.Left, "左"),
        new(RelativePosition.Right, "右"),
    ];

    private readonly WidgetSettings _settings;
    private readonly Action _onChanged;
    private readonly Action _onMonitorVisibilityChanged;
    private readonly Action _onAutoStartChanged;
    private bool _isInitializing;

    public SettingsWindow(
        WidgetSettings settings,
        IReadOnlyList<ConnectedMonitor> connectedMonitors,
        Action onChanged,
        Action onMonitorVisibilityChanged,
        Action onAutoStartChanged)
    {
        _settings = settings;
        _onChanged = onChanged;
        _onMonitorVisibilityChanged = onMonitorVisibilityChanged;
        _onAutoStartChanged = onAutoStartChanged;
        // InitializeComponent() が Slider/ComboBox の初期値設定時に対応する ValueChanged/
        // SelectionChanged を同期的に発火させるため、_settings 代入・_isInitializing のガードは
        // InitializeComponent() より前に済ませておく必要がある
        _isInitializing = true;

        InitializeComponent();
        WindowThemeHelper.ApplyImmersiveDarkMode(this);

        var appearance = _settings.Appearance;

        FontFamilyCombo.ItemsSource = Fonts.SystemFontFamilies
            .Select(f => f.Source)
            .OrderBy(name => name, StringComparer.OrdinalIgnoreCase)
            .ToList();
        FontFamilyCombo.SelectedItem = appearance.FontFamily;

        TimeFontSizeSlider.Value = appearance.TimeFontSize;
        DateFontSizeSlider.Value = appearance.DateFontSize;
        OpacitySlider.Value = appearance.BackgroundOpacity;

        ShowDateCheckBox.IsChecked = appearance.ShowDate;
        ShowDayOfWeekCheckBox.IsChecked = appearance.ShowDayOfWeek;
        ShowSecondsCheckBox.IsChecked = appearance.ShowSeconds;

        DateDayOfWeekPositionCombo.ItemsSource = DateDayOfWeekPositionOptions;
        DateDayOfWeekPositionCombo.SelectedItem = DateDayOfWeekPositionOptions.First(o => o.Value == appearance.DateDayOfWeekPosition);

        DateSeparatorCombo.ItemsSource = DateSeparatorOptions;
        DateSeparatorCombo.SelectedItem = DateSeparatorResolver.Resolve(appearance.DateSeparator);

        DayOfWeekFormatCombo.ItemsSource = DayOfWeekFormatOptions;
        DayOfWeekFormatCombo.SelectedItem = DayOfWeekFormatOptions.First(o => o.Value == appearance.DayOfWeekFormat);

        UpdateColorButtonLabel(TimeFontColorButton, "文字色を選択", appearance.TimeFontColor);
        UpdateColorButtonLabel(DateFontColorButton, "文字色を選択", appearance.DateFontColor);
        UpdateColorButtonLabel(BackgroundColorButton, "背景色を選択", appearance.BackgroundColor);

        BuildMonitorCheckBoxes(connectedMonitors);
        AutoStartCheckBox.IsChecked = settings.AutoStartEnabled;

        _isInitializing = false;
    }

    private void BuildMonitorCheckBoxes(IReadOnlyList<ConnectedMonitor> connectedMonitors)
    {
        foreach (var monitor in connectedMonitors)
        {
            // DisplayNumber は Windows の「設定 > システム > ディスプレイ」の識別番号と一致する
            var label = monitor.IsPrimary ? $"モニター {monitor.DisplayNumber} (プライマリ)" : $"モニター {monitor.DisplayNumber}";
            var checkBox = new CheckBox
            {
                Content = label,
                Tag = monitor.Identifier,
                IsChecked = _settings.Monitors.TryGetValue(monitor.Identifier, out var placement) && placement.IsVisible,
            };
            checkBox.Checked += MonitorVisibilityCheckBox_Changed;
            checkBox.Unchecked += MonitorVisibilityCheckBox_Changed;
            MonitorsPanel.Children.Add(checkBox);
        }
    }

    private void MonitorVisibilityCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        // FR-014: モニタごとの表示/非表示は詳細設定画面からのみ切り替える
        if (sender is CheckBox { Tag: string identifier } checkBox
            && _settings.Monitors.TryGetValue(identifier, out var placement))
        {
            placement.IsVisible = checkBox.IsChecked == true;
            _onMonitorVisibilityChanged();
        }
    }

    private void AutoStartCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.AutoStartEnabled = AutoStartCheckBox.IsChecked == true;
        _onAutoStartChanged();
    }

    private void FontFamilyCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.Appearance.FontFamily = FontFamilyCombo.SelectedItem as string ?? string.Empty;
        _onChanged();
    }

    private void TimeFontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.Appearance.TimeFontSize = TimeFontSizeSlider.Value;
        _onChanged();
    }

    private void DateFontSizeSlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.Appearance.DateFontSize = DateFontSizeSlider.Value;
        _onChanged();
    }

    private void OpacitySlider_ValueChanged(object sender, RoutedPropertyChangedEventArgs<double> e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.Appearance.BackgroundOpacity = OpacitySlider.Value;
        _onChanged();
    }

    private void ShowDateCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.Appearance.ShowDate = ShowDateCheckBox.IsChecked == true;
        _onChanged();
    }

    private void ShowDayOfWeekCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.Appearance.ShowDayOfWeek = ShowDayOfWeekCheckBox.IsChecked == true;
        _onChanged();
    }

    private void ShowSecondsCheckBox_Changed(object sender, RoutedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.Appearance.ShowSeconds = ShowSecondsCheckBox.IsChecked == true;
        _onChanged();
    }

    private void DateDayOfWeekPositionCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (DateDayOfWeekPositionCombo.SelectedItem is DateDayOfWeekPositionOption option)
        {
            _settings.Appearance.DateDayOfWeekPosition = option.Value;
            _onChanged();
        }
    }

    private void DateSeparatorCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        _settings.Appearance.DateSeparator = DateSeparatorCombo.SelectedItem as string ?? DateSeparatorResolver.DefaultSeparator;
        _onChanged();
    }

    private void DayOfWeekFormatCombo_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (_isInitializing)
        {
            return;
        }

        if (DayOfWeekFormatCombo.SelectedItem is DayOfWeekFormatOption option)
        {
            _settings.Appearance.DayOfWeekFormat = option.Value;
            _onChanged();
        }
    }

    private void TimeFontColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(TimeFontColorButton, "文字色を選択", _settings.Appearance.TimeFontColor, hex =>
        {
            _settings.Appearance.TimeFontColor = hex;
        });
    }

    private void DateFontColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(DateFontColorButton, "文字色を選択", _settings.Appearance.DateFontColor, hex =>
        {
            _settings.Appearance.DateFontColor = hex;
        });
    }

    private void BackgroundColorButton_Click(object sender, RoutedEventArgs e)
    {
        PickColor(BackgroundColorButton, "背景色を選択", _settings.Appearance.BackgroundColor, hex =>
        {
            _settings.Appearance.BackgroundColor = hex;
        });
    }

    private void PickColor(Button button, string label, string currentColor, Action<string> applyHex)
    {
        var picker = new ColorPickerWindow(currentColor) { Owner = this };
        if (picker.ShowDialog() != true || picker.SelectedHex is not { } hex)
        {
            return;
        }

        applyHex(hex);
        UpdateColorButtonLabel(button, label, hex);
        _onChanged();
    }

    private static void UpdateColorButtonLabel(Button button, string label, string color)
    {
        button.Content = $"{label}…({ColorHexResolver.Resolve(color)})";
    }
}
