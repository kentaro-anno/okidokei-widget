using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Settings;

public class ColorHexResolverTests
{
    [Theory]
    [InlineData("#FFFFFFFF")]
    [InlineData("#80112233")]
    public void Resolve_有効な16進ARGB文字列はそのまま返す(string hex)
    {
        Assert.Equal(hex, ColorHexResolver.Resolve(hex));
    }

    [Theory]
    [InlineData("")]
    [InlineData("not-a-color")]
    [InlineData("#FFFFFF")]
    [InlineData("#GGFFFFFF")]
    public void Resolve_パース不可な文字列はデフォルト色を返す(string hex)
    {
        Assert.Equal(ColorHexResolver.DefaultColor, ColorHexResolver.Resolve(hex));
    }
}
