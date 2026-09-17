namespace OkidokeiWidget.Core.Settings;

/// <summary>
/// spec.md の Assumptions により日本語ロケール前提のため、実行時のカルチャに依存せず
/// 曜日名を固定の表で解決する (FR-027)。
/// </summary>
public static class DayOfWeekFormatter
{
    private static readonly string[] ShortKanji = ["日", "月", "火", "水", "木", "金", "土"];
    private static readonly string[] LongKanji = ["日曜日", "月曜日", "火曜日", "水曜日", "木曜日", "金曜日", "土曜日"];
    private static readonly string[] ShortEnglish = ["Sun.", "Mon.", "Tue.", "Wed.", "Thu.", "Fri.", "Sat."];
    private static readonly string[] LongEnglish = ["Sunday", "Monday", "Tuesday", "Wednesday", "Thursday", "Friday", "Saturday"];

    public static string Format(DayOfWeek dayOfWeek, DayOfWeekFormat format)
    {
        var index = (int)dayOfWeek;

        return format switch
        {
            DayOfWeekFormat.ShortKanjiParen => $"({ShortKanji[index]})",
            DayOfWeekFormat.ShortEnglish => ShortEnglish[index],
            DayOfWeekFormat.LongEnglish => LongEnglish[index],
            _ => LongKanji[index],
        };
    }
}
