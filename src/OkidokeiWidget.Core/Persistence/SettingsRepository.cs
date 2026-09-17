using System.Text.Json;
using System.Text.Json.Serialization;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.Core.Persistence;

public static class SettingsRepository
{
    public static string DefaultSettingsFilePath { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        "OkidokeiWidget",
        "settings.json");

    private static readonly JsonSerializerOptions SerializerOptions = new()
    {
        WriteIndented = true,
        Converters = { new JsonStringEnumConverter() },
    };

    /// <summary>
    /// 設定ファイルを読み込む。ファイル不在時、またはパース不可時は <see cref="WidgetSettings.CreateDefault"/>
    /// を返す (FR-018)。<c>FellBackToDefaults</c> はパース不可でフォールバックした場合のみ true になり、
    /// 単にファイルが存在しないだけの場合は false (ユーザーへの通知要否の判断に使う。contracts/settings-file.md 参照)。
    /// </summary>
    public static (WidgetSettings Settings, bool FellBackToDefaults) Load(string? path = null)
    {
        path ??= DefaultSettingsFilePath;

        if (!File.Exists(path))
        {
            return (WidgetSettings.CreateDefault(), false);
        }

        try
        {
            var json = File.ReadAllText(path);
            var settings = JsonSerializer.Deserialize<WidgetSettings>(json, SerializerOptions);
            return settings is null ? (WidgetSettings.CreateDefault(), true) : (settings, false);
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            return (WidgetSettings.CreateDefault(), true);
        }
    }

    public static void Save(WidgetSettings settings, string? path = null)
    {
        path ??= DefaultSettingsFilePath;
        settings.Appearance.BackgroundOpacity = Math.Clamp(settings.Appearance.BackgroundOpacity, 0.0, 100.0);

        try
        {
            var directory = Path.GetDirectoryName(path);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            var tempPath = path + ".tmp";
            File.WriteAllText(tempPath, JsonSerializer.Serialize(settings, SerializerOptions));
            File.Move(tempPath, path, overwrite: true);
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException)
        {
            // FR-022: 書き込みに失敗してもクラッシュせず継続する。この変更はこのセッション中は永続化されない
        }
    }
}
