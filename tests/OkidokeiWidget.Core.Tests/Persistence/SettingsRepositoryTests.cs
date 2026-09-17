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
}
