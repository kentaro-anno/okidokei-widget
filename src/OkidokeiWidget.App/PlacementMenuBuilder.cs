using System;
using System.Windows.Controls;
using OkidokeiWidget.Core.Settings;

namespace OkidokeiWidget.App;

/// <summary>
/// 1 モニタ分の「配置」サブメニュー (横位置・縦位置・余白) を作る。ウィジェット本体と
/// タスクトレイの両方の右クリックメニューから使い、項目や表示が食い違わないようにする
/// (research.md #17、contracts/context-menus.md)。
/// </summary>
internal static class PlacementMenuBuilder
{
    private static readonly (AnchorHorizontal Value, string Label)[] HorizontalItems =
    [
        (AnchorHorizontal.Left, "左"),
        (AnchorHorizontal.Center, "中央"),
        (AnchorHorizontal.Right, "右"),
    ];

    private static readonly (AnchorVertical Value, string Label)[] VerticalItems =
    [
        (AnchorVertical.Top, "上"),
        (AnchorVertical.Center, "中央"),
        (AnchorVertical.Bottom, "下"),
    ];

    private static readonly (AnchorMargin Value, string Label)[] MarginItems =
    [
        (AnchorMargin.Narrow, "狭め"),
        (AnchorMargin.Wide, "広め"),
    ];

    public static MenuItem Build(
        string header,
        MonitorPlacement placement,
        bool isLocked,
        Action<AnchorHorizontal> onHorizontalSelected,
        Action<AnchorVertical> onVerticalSelected,
        Action<AnchorMargin> onMarginSelected)
    {
        var menu = new MenuItem { Header = header };
        Populate(menu, placement, isLocked, onHorizontalSelected, onVerticalSelected, onMarginSelected);
        return menu;
    }

    /// <summary>
    /// <paramref name="menu"/> の子項目を、現在の配置を反映した内容に作り直す。
    /// 位置ロック中は末端の選択肢だけを無効にし、「横位置」などの途中の階層は有効のままにする。
    /// WPF では無効な親のサブメニューは開けず、ロック中に今の配置を確認できなくなるため (FR-010)。
    /// </summary>
    public static void Populate(
        MenuItem menu,
        MonitorPlacement placement,
        bool isLocked,
        Action<AnchorHorizontal> onHorizontalSelected,
        Action<AnchorVertical> onVerticalSelected,
        Action<AnchorMargin> onMarginSelected)
    {
        menu.Items.Clear();

        // 自由配置中 (Anchor が null) は横位置・縦位置のどれにもチェックを付けない
        AnchorHorizontal? currentHorizontal = placement.Anchor is { } h ? AnchorAxes.HorizontalOf(h) : null;
        AnchorVertical? currentVertical = placement.Anchor is { } v ? AnchorAxes.VerticalOf(v) : null;

        menu.Items.Add(BuildGroup("横位置", HorizontalItems, currentHorizontal, isLocked, onHorizontalSelected));
        menu.Items.Add(BuildGroup("縦位置", VerticalItems, currentVertical, isLocked, onVerticalSelected));
        menu.Items.Add(BuildGroup("余白", MarginItems, placement.AnchorMargin, isLocked, onMarginSelected));
    }

    private static MenuItem BuildGroup<T>(
        string header,
        (T Value, string Label)[] items,
        T? current,
        bool isLocked,
        Action<T> onSelected)
        where T : struct, Enum
    {
        var group = new MenuItem { Header = header };
        foreach (var (value, label) in items)
        {
            // Fluent テーマは IsCheckable が true の項目にしかチェックを描かない (issue #34)。クリックで
            // IsChecked が反転しても、メニューは開くたびに設定値から作り直すので表示と食い違わない
            var item = new MenuItem
            {
                Header = label,
                IsCheckable = true,
                IsChecked = current is { } c && c.Equals(value),
                IsEnabled = !isLocked,
            };
            item.Click += (_, _) => onSelected(value);
            group.Items.Add(item);
        }

        return group;
    }
}
