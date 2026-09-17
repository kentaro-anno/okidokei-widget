using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Settings;

public class FontResolverTests
{
    [Fact]
    public void Resolve_空文字の場合はnullを返す()
    {
        Assert.Null(FontResolver.Resolve(string.Empty, ["Yu Gothic UI"]));
    }

    [Fact]
    public void Resolve_未インストールのフォント名の場合はnullを返す()
    {
        Assert.Null(FontResolver.Resolve("NotInstalledFont", ["Yu Gothic UI"]));
    }

    [Fact]
    public void Resolve_インストール済みのフォント名の場合はそのまま返す()
    {
        Assert.Equal("Yu Gothic UI", FontResolver.Resolve("Yu Gothic UI", ["Yu Gothic UI", "Meiryo"]));
    }
}
