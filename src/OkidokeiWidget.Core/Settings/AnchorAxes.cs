namespace OkidokeiWidget.Core.Settings;

public enum AnchorHorizontal
{
    Left,
    Center,
    Right,
}

public enum AnchorVertical
{
    Top,
    Center,
    Bottom,
}

/// <summary>
/// 保存形式の <see cref="AnchorPosition"/> (9 値) と、右クリックメニューで別々に選ぶ横位置・縦位置を
/// 相互に変換する。保存は 9 値のままにし、「横だけ決まっていて縦が未設定」の状態を作らない
/// (research.md #15)。
/// </summary>
public static class AnchorAxes
{
    public static AnchorHorizontal HorizontalOf(AnchorPosition anchor) => anchor switch
    {
        AnchorPosition.TopLeft or AnchorPosition.Left or AnchorPosition.BottomLeft => AnchorHorizontal.Left,
        AnchorPosition.TopRight or AnchorPosition.Right or AnchorPosition.BottomRight => AnchorHorizontal.Right,
        _ => AnchorHorizontal.Center,
    };

    public static AnchorVertical VerticalOf(AnchorPosition anchor) => anchor switch
    {
        AnchorPosition.TopLeft or AnchorPosition.Top or AnchorPosition.TopRight => AnchorVertical.Top,
        AnchorPosition.BottomLeft or AnchorPosition.Bottom or AnchorPosition.BottomRight => AnchorVertical.Bottom,
        _ => AnchorVertical.Center,
    };

    public static AnchorPosition Compose(AnchorHorizontal horizontal, AnchorVertical vertical) => (vertical, horizontal) switch
    {
        (AnchorVertical.Top, AnchorHorizontal.Left) => AnchorPosition.TopLeft,
        (AnchorVertical.Top, AnchorHorizontal.Center) => AnchorPosition.Top,
        (AnchorVertical.Top, AnchorHorizontal.Right) => AnchorPosition.TopRight,
        (AnchorVertical.Center, AnchorHorizontal.Left) => AnchorPosition.Left,
        (AnchorVertical.Center, AnchorHorizontal.Right) => AnchorPosition.Right,
        (AnchorVertical.Bottom, AnchorHorizontal.Left) => AnchorPosition.BottomLeft,
        (AnchorVertical.Bottom, AnchorHorizontal.Center) => AnchorPosition.Bottom,
        (AnchorVertical.Bottom, AnchorHorizontal.Right) => AnchorPosition.BottomRight,
        _ => AnchorPosition.Center,
    };
}
