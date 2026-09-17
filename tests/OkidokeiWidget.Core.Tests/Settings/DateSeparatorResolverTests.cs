using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Settings;

public class DateSeparatorResolverTests
{
    [Theory]
    [InlineData("/")]
    [InlineData("-")]
    public void Resolve_許可された区切り文字はそのまま返す(string separator)
    {
        Assert.Equal(separator, DateSeparatorResolver.Resolve(separator));
    }

    [Theory]
    [InlineData("")]
    [InlineData(".")]
    [InlineData("//")]
    public void Resolve_許可されていない値はデフォルトを返す(string separator)
    {
        Assert.Equal(DateSeparatorResolver.DefaultSeparator, DateSeparatorResolver.Resolve(separator));
    }
}
