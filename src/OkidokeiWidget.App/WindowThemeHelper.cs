using System.Runtime.InteropServices;
using System.Windows;
using System.Windows.Interop;
using Microsoft.Win32;

namespace OkidokeiWidget.App;

/// <summary>
/// <see cref="Application.ThemeMode"/> は WPF コントロールの見た目はダーク/ライトに切り替えるが、
/// ウィンドウのタイトルバー(非クライアント領域)は別扱いで自動追随しないため、DWM API で
/// 個別に切り替える。タイトルバーを持つウィンドウ(<see cref="SettingsWindow"/>・
/// <see cref="ColorPickerWindow"/>)にのみ適用する(<see cref="ClockWindow"/> はタイトルバー
/// 自体を持たないため対象外)。
/// </summary>
internal static class WindowThemeHelper
{
    private const int DwmwaUseImmersiveDarkMode = 20;

    [DllImport("dwmapi.dll")]
    private static extern int DwmSetWindowAttribute(nint hwnd, int attribute, ref int value, int size);

    public static void ApplyImmersiveDarkMode(Window window)
    {
        window.SourceInitialized += (_, _) =>
        {
            var hwnd = new WindowInteropHelper(window).Handle;
            var useDarkMode = IsSystemInDarkMode() ? 1 : 0;
            DwmSetWindowAttribute(hwnd, DwmwaUseImmersiveDarkMode, ref useDarkMode, sizeof(int));
        };
    }

    private static bool IsSystemInDarkMode()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");
            return key?.GetValue("AppsUseLightTheme") is int lightTheme && lightTheme == 0;
        }
        catch (Exception ex) when (ex is System.Security.SecurityException or System.IO.IOException)
        {
            return false;
        }
    }
}
