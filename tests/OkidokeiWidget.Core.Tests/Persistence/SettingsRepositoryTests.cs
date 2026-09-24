using OkidokeiWidget.Core.Persistence;

namespace OkidokeiWidget.Core.Tests.Persistence;

public class SettingsRepositoryTests : IDisposable
{
    private readonly string _tempDirectory;
    private readonly string _settingsPath;

    public SettingsRepositoryTests()
    {
        _tempDirectory = Path.Combine(Path.GetTempPath(), "OkidokeiWidgetTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempDirectory);
        _settingsPath = Path.Combine(_tempDirectory, "settings.json");
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempDirectory))
        {
            Directory.Delete(_tempDirectory, recursive: true);
        }
    }

    [Fact]
    public void Load_ファイルが存在しない場合はデフォルト値を返し通知フラグは立てない()
    {
        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.False(fellBackToDefaults);
        Assert.True(settings.Appearance.ShowDate);
        Assert.Empty(settings.Monitors);
    }

    [Fact]
    public void Load_不正なJSONの場合はデフォルト値へフォールバックし通知フラグを立てる()
    {
        File.WriteAllText(_settingsPath, "{ this is not valid json");

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.True(fellBackToDefaults);
        Assert.True(settings.Appearance.ShowDate);
    }

    [Fact]
    public void Save_BackgroundOpacityを0から100の範囲にクランプして書き込む()
    {
        var settings = Core.Settings.WidgetSettings.CreateDefault();
        settings.Appearance.BackgroundOpacity = 150.0;

        SettingsRepository.Save(settings, _settingsPath);

        var (loaded, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);
        Assert.False(fellBackToDefaults);
        Assert.Equal(100.0, loaded.Appearance.BackgroundOpacity);
    }

    [Fact]
    public void Load_アンカーを持たない既存形式の設定は自由配置として読み込む()
    {
        // 2026-09-24 より前の形式。アップデートしても既存ユーザーの位置が変わらないこと (FR-034)
        File.WriteAllText(_settingsPath, """
            {
              "Monitors": {
                "monitor-1": { "IsVisible": true, "X": 1480, "Y": 9 }
              }
            }
            """);

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.False(fellBackToDefaults);
        var placement = settings.Monitors["monitor-1"];
        Assert.Null(placement.Anchor);
        Assert.Equal(Core.Settings.AnchorMargin.Narrow, placement.AnchorMargin);
        Assert.Equal(1480, placement.X);
        Assert.Equal(9, placement.Y);
    }

    [Fact]
    public void Save_アンカー指定を保存して読み戻せる()
    {
        var settings = Core.Settings.WidgetSettings.CreateDefault();
        settings.Monitors["monitor-1"] = new Core.Settings.MonitorPlacement
        {
            Anchor = Core.Settings.AnchorPosition.TopRight,
            AnchorMargin = Core.Settings.AnchorMargin.Wide,
        };

        SettingsRepository.Save(settings, _settingsPath);

        var (loaded, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);
        Assert.False(fellBackToDefaults);
        Assert.Equal(Core.Settings.AnchorPosition.TopRight, loaded.Monitors["monitor-1"].Anchor);
        Assert.Equal(Core.Settings.AnchorMargin.Wide, loaded.Monitors["monitor-1"].AnchorMargin);
    }

    [Fact]
    public void Load_未知のアンカー値はデフォルト値へフォールバックし通知フラグを立てる()
    {
        File.WriteAllText(_settingsPath, """
            {
              "Monitors": {
                "monitor-1": { "IsVisible": true, "X": 0, "Y": 0, "Anchor": "Middle" }
              }
            }
            """);

        var (settings, fellBackToDefaults) = SettingsRepository.Load(_settingsPath);

        Assert.True(fellBackToDefaults);
        Assert.Empty(settings.Monitors);
    }
}
