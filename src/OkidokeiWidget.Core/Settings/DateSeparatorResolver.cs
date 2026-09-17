using System.Linq;

namespace OkidokeiWidget.Core.Settings;

/// <summary>
/// <see cref="AppearanceSettings.DateSeparator"/> が `/` または `-` 以外の場合にデフォルトへ
/// フォールバックする (FR-026、data-model.md のバリデーション)。
/// </summary>
public static class DateSeparatorResolver
{
    public const string DefaultSeparator = "/";

    private static readonly string[] AllowedSeparators = ["/", "-"];

    public static string Resolve(string separator) =>
        AllowedSeparators.Contains(separator) ? separator : DefaultSeparator;
}
