using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Settings;

public class DayOfWeekFormatterTests
{
    [Theory]
    [InlineData(DayOfWeekFormat.ShortKanjiParen, "(月)")]
    [InlineData(DayOfWeekFormat.LongKanji, "月曜日")]
    [InlineData(DayOfWeekFormat.ShortEnglish, "Mon.")]
    [InlineData(DayOfWeekFormat.LongEnglish, "Monday")]
    public void Format_指定した形式で曜日を表示する(DayOfWeekFormat format, string expected)
    {
        Assert.Equal(expected, DayOfWeekFormatter.Format(DayOfWeek.Monday, format));
    }

    [Fact]
    public void Format_日曜日もShortKanjiParenで正しく表示する()
    {
        Assert.Equal("(日)", DayOfWeekFormatter.Format(DayOfWeek.Sunday, DayOfWeekFormat.ShortKanjiParen));
    }
}
