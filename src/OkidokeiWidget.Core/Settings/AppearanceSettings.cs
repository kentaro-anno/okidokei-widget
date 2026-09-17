namespace OkidokeiWidget.Core.Settings;

public sealed class AppearanceSettings
{
    public bool ShowDate { get; set; } = true;

    public bool ShowDayOfWeek { get; set; } = true;

    public bool ShowSeconds { get; set; } = true;

    public string FontFamily { get; set; } = string.Empty;

    public double TimeFontSize { get; set; } = 24.0;

    public string TimeFontColor { get; set; } = "#FFFFFFFF";

    public double DateFontSize { get; set; } = 14.0;

    public string DateFontColor { get; set; } = "#FFFFFFFF";

    public string DateSeparator { get; set; } = "/";

    public DayOfWeekFormat DayOfWeekFormat { get; set; } = DayOfWeekFormat.LongKanji;

    public RelativePosition DateDayOfWeekPosition { get; set; } = RelativePosition.Below;

    public string BackgroundColor { get; set; } = "#FF000000";

    public double BackgroundOpacity { get; set; } = 30.0;
}
