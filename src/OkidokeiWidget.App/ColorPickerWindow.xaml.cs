using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.App;

/// <summary>
/// <see cref="System.Windows.Forms.ColorDialog"/>(Windows 2000 時代から見た目が変わっていない
/// 共通ダイアログ)の代わりに、WPF だけで実装した簡易な色選択ダイアログ(パレット + 16 進コード
/// 入力)。追加の NuGet パッケージには依存しない
/// </summary>
public partial class ColorPickerWindow : Window
{
    private sealed record PaletteEntry(string Hex, SolidColorBrush Brush);

    private static readonly PaletteEntry[] Palette =
    [
        Entry("#FFFFFFFF"), Entry("#FFD9D9D9"), Entry("#FFA6A6A6"), Entry("#FF404040"), Entry("#FF000000"),
        Entry("#FFE81123"), Entry("#FFFF8C00"), Entry("#FFFFD700"),
        Entry("#FF107C10"), Entry("#FF00B7C3"), Entry("#FF0078D7"),
        Entry("#FF6B69D6"), Entry("#FF8E5CD9"), Entry("#FFE3008C"), Entry("#FFFF97C7"), Entry("#FF8B5A2B"),
    ];

    public string? SelectedHex { get; private set; }

    public ColorPickerWindow(string initialHex)
    {
        InitializeComponent();
        WindowThemeHelper.ApplyImmersiveDarkMode(this);

        PaletteItemsControl.ItemsSource = Palette;

        HexTextBox.Text = ToRgbHex(ColorHexResolver.Resolve(initialHex));
        UpdatePreview();
    }

    private static PaletteEntry Entry(string argbHex) => new(argbHex, new SolidColorBrush(ParseArgb(argbHex)));

    private void PaletteButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: string hex })
        {
            HexTextBox.Text = ToRgbHex(hex);
        }
    }

    private void HexTextBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        UpdatePreview();
    }

    private void UpdatePreview()
    {
        var resolved = ColorHexResolver.Resolve(FromRgbHexToArgb(HexTextBox.Text));
        PreviewSwatch.Background = new SolidColorBrush(ParseArgb(resolved));
    }

    private void OkButton_Click(object sender, RoutedEventArgs e)
    {
        SelectedHex = ColorHexResolver.Resolve(FromRgbHexToArgb(HexTextBox.Text));
        DialogResult = true;
    }

    private void CancelButton_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }

    private static string ToRgbHex(string argbHex) => "#" + argbHex.Substring(3);

    private static string FromRgbHexToArgb(string rgbHex) =>
        rgbHex.Length == 7 && rgbHex[0] == '#' ? "#FF" + rgbHex.Substring(1) : rgbHex;

    private static Color ParseArgb(string argbHex)
    {
        var a = Convert.ToByte(argbHex.Substring(1, 2), 16);
        var r = Convert.ToByte(argbHex.Substring(3, 2), 16);
        var g = Convert.ToByte(argbHex.Substring(5, 2), 16);
        var b = Convert.ToByte(argbHex.Substring(7, 2), 16);
        return Color.FromArgb(a, r, g, b);
    }
}
