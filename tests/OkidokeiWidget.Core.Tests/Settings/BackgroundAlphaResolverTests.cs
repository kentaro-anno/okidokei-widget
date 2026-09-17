using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Tests.Settings;

public class BackgroundAlphaResolverTests
{
    [Theory]
    [InlineData(0.0, 255)]
    [InlineData(50.0, 128)]
    [InlineData(66.0, 87)]
    public void Resolve_透過度に応じたアルファ値を返す(double opacity, byte expected)
    {
        Assert.Equal(expected, BackgroundAlphaResolver.Resolve(opacity));
    }

    [Theory]
    [InlineData(100.0)]
    [InlineData(150.0)]
    public void Resolve_透過度100パーセントでもアルファ値は0にしない(double opacity)
    {
        // アルファ値 0 の領域はレイヤードウィンドウのヒットテストが成立せず、クリックが
        // 背後へ抜けてウィジェットを操作できなくなる (issue #11)
        Assert.Equal(BackgroundAlphaResolver.MinimumAlpha, BackgroundAlphaResolver.Resolve(opacity));
    }

    [Fact]
    public void Resolve_負の透過度は不透明として扱う()
    {
        Assert.Equal(255, BackgroundAlphaResolver.Resolve(-10.0));
    }
}
