namespace OkidokeiWidget.Core.Settings;

/// <summary>
/// 曜日の表示形式 (FR-027)。
/// </summary>
public enum DayOfWeekFormat
{
    /// <summary>例: 「(月)」</summary>
    ShortKanjiParen,

    /// <summary>例: 「月曜日」</summary>
    LongKanji,

    /// <summary>例: 「Mon.」</summary>
    ShortEnglish,

    /// <summary>例: 「Monday」</summary>
    LongEnglish,
}
