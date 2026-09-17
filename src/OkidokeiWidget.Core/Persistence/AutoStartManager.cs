using System.Runtime.InteropServices;
using System.Text;

namespace OkidokeiWidget.Core.Persistence;

/// <summary>
/// スタートアップフォルダ (shell:startup) へのショートカット (.lnk) 作成/削除で自動起動を切り替える
/// (レジストリの Run キーは使用しない。research.md #3)。ショートカット生成は Shell32 の
/// IShellLinkW/IPersistFile を COM 経由で呼び出す (追加の NuGet パッケージは不要)。
/// </summary>
public static class AutoStartManager
{
    private const string ShortcutFileName = "OkidokeiWidget.lnk";

    public static string DefaultStartupFolderPath { get; } = Environment.GetFolderPath(Environment.SpecialFolder.Startup);

    public static void SetEnabled(bool enabled, string? executablePath = null, string? startupFolderPath = null)
    {
        var folder = startupFolderPath ?? DefaultStartupFolderPath;
        var shortcutPath = Path.Combine(folder, ShortcutFileName);

        if (enabled)
        {
            var target = executablePath ?? Environment.ProcessPath
                ?? throw new InvalidOperationException("実行ファイルのパスを取得できません");
            CreateShortcut(shortcutPath, target);
        }
        else if (File.Exists(shortcutPath))
        {
            File.Delete(shortcutPath);
        }
    }

    private static void CreateShortcut(string shortcutPath, string targetPath)
    {
        var directory = Path.GetDirectoryName(shortcutPath);
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        var shellLinkType = Type.GetTypeFromCLSID(new Guid("00021401-0000-0000-C000-000000000046"))!;
        var shellLink = (IShellLinkW)Activator.CreateInstance(shellLinkType)!;

        shellLink.SetPath(targetPath);
        shellLink.SetWorkingDirectory(Path.GetDirectoryName(targetPath) ?? string.Empty);

        ((IPersistFile)shellLink).Save(shortcutPath, false);
    }

    [ComImport]
    [Guid("000214F9-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IShellLinkW
    {
        void GetPath([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszFile, int cchMaxPath, IntPtr pfd, uint fFlags);
        void GetIDList(out IntPtr ppidl);
        void SetIDList(IntPtr pidl);
        void GetDescription([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszName, int cchMaxName);
        void SetDescription([MarshalAs(UnmanagedType.LPWStr)] string pszName);
        void GetWorkingDirectory([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszDir, int cchMaxPath);
        void SetWorkingDirectory([MarshalAs(UnmanagedType.LPWStr)] string pszDir);
        void SetArguments([MarshalAs(UnmanagedType.LPWStr)] string pszArgs);
        void GetArguments([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszArgs, int cchMaxPath);
        void GetHotkey(out short pwHotkey);
        void SetHotkey(short wHotkey);
        void GetShowCmd(out int piShowCmd);
        void SetShowCmd(int iShowCmd);
        void GetIconLocation([Out, MarshalAs(UnmanagedType.LPWStr)] StringBuilder pszIconPath, int cchIconPath, out int piIcon);
        void SetIconLocation([MarshalAs(UnmanagedType.LPWStr)] string pszIconPath, int iIcon);
        void SetRelativePath([MarshalAs(UnmanagedType.LPWStr)] string pszPathRel, uint dwReserved);
        void Resolve(IntPtr hwnd, uint fFlags);
        void SetPath([MarshalAs(UnmanagedType.LPWStr)] string pszFile);
    }

    [ComImport]
    [Guid("0000010b-0000-0000-C000-000000000046")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    private interface IPersistFile
    {
        void GetClassID(out Guid pClassID);
        [PreserveSig] int IsDirty();
        void Load([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, uint dwMode);
        void Save([MarshalAs(UnmanagedType.LPWStr)] string pszFileName, [MarshalAs(UnmanagedType.Bool)] bool fRemember);
        void SaveCompleted([MarshalAs(UnmanagedType.LPWStr)] string pszFileName);
        void GetCurFile([MarshalAs(UnmanagedType.LPWStr)] out string ppszFileName);
    }
}
