namespace OkidokeiWidget.Core.Settings;

public sealed class MonitorPlacement
{
    public bool IsVisible { get; set; } = true;

    public int X { get; set; }

    public int Y { get; set; }

    // null は自由配置 (X/Y を使う)。既存の settings.json にはこのフィールドがないため、
    // 読み込むと null になり、アップデートしても表示位置は変わらない (FR-034)
    public AnchorPosition? Anchor { get; set; }

    public AnchorMargin AnchorMargin { get; set; } = AnchorMargin.Narrow;
}
