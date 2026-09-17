namespace OkidokeiWidget.Core.Settings;

/// <summary>
/// 背景の透過度 (0 = 完全に不透明、100 = 完全に透明。FR-008, FR-030) を、背景ブラシの
/// アルファ値へ変換する。
/// 透過度 100% でもアルファ値を 0 にはしない: ClockWindow は AllowsTransparency="True" の
/// レイヤードウィンドウで、ヒットテストはピクセルのアルファ値で行われるため、アルファ値 0 の
/// 領域はクリックが背後のウィンドウへ抜けてウィジェットを操作できなくなる (issue #11)。
/// アルファ値 1 は見た目には透過度 100% と区別がつかないが、ヒットテストは成立する。
/// </summary>
public static class BackgroundAlphaResolver
{
    public const byte MinimumAlpha = 1;

    public static byte Resolve(double backgroundOpacity)
    {
        var opacity = Math.Clamp(backgroundOpacity, 0.0, 100.0);
        var alpha = (int)Math.Round(255 * (1 - opacity / 100.0));

        return (byte)Math.Max(MinimumAlpha, alpha);
    }
}
