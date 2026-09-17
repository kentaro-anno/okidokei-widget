namespace OkidokeiWidget.Core.Settings;

public sealed class WidgetSettings
{
    public AppearanceSettings Appearance { get; set; } = new();

    public WindowBehaviorSettings WindowBehavior { get; set; } = new();

    public Dictionary<string, MonitorPlacement> Monitors { get; set; } = new();

    public bool AutoStartEnabled { get; set; }

    public static WidgetSettings CreateDefault() => new();
}
