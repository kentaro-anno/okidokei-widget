using OkidokeiWidget.Core.Persistence;

namespace OkidokeiWidget.Core.Tests.Persistence;

public class AutoStartManagerTests : IDisposable
{
    private readonly string _tempStartupFolder;

    public AutoStartManagerTests()
    {
        _tempStartupFolder = Path.Combine(Path.GetTempPath(), "OkidokeiWidgetStartupTests_" + Guid.NewGuid());
        Directory.CreateDirectory(_tempStartupFolder);
    }

    public void Dispose()
    {
        if (Directory.Exists(_tempStartupFolder))
        {
            Directory.Delete(_tempStartupFolder, recursive: true);
        }
    }

    [Fact]
    public void SetEnabled_trueの場合はスタートアップフォルダにショートカットを作成する()
    {
        AutoStartManager.SetEnabled(true, executablePath: @"C:\dummy\OkidokeiWidget.App.exe", startupFolderPath: _tempStartupFolder);

        Assert.True(File.Exists(Path.Combine(_tempStartupFolder, "OkidokeiWidget.lnk")));
    }

    [Fact]
    public void SetEnabled_falseの場合は既存のショートカットを削除する()
    {
        AutoStartManager.SetEnabled(true, executablePath: @"C:\dummy\OkidokeiWidget.App.exe", startupFolderPath: _tempStartupFolder);
        AutoStartManager.SetEnabled(false, startupFolderPath: _tempStartupFolder);

        Assert.False(File.Exists(Path.Combine(_tempStartupFolder, "OkidokeiWidget.lnk")));
    }
}
