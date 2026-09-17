namespace OkidokeiWidget.Core.Settings;

/// <summary>
/// <see cref="AppearanceSettings.FontFamily"/> が空文字、またはシステムに未インストールの
/// フォント名を指す場合にシステムデフォルトへフォールバックする (data-model.md)。
/// </summary>
public static class FontResolver
{
    /// <summary>
    /// 有効なフォント名を返す。フォールバックが必要な場合は null を返す(呼び出し側でシステム
    /// デフォルトフォントを適用する)。
    /// </summary>
    public static string? Resolve(string fontFamily, IReadOnlyCollection<string> installedFontNames)
    {
        if (string.IsNullOrWhiteSpace(fontFamily) || !installedFontNames.Contains(fontFamily))
        {
            return null;
        }

        return fontFamily;
    }
}
