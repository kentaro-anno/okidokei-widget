# Bug Fix: 右クリックメニューで、今の配置や位置ロックのチェックマークが表示されない

- **Slug**: menu-check-marks
- **Fixed**: 2026-09-24
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

コードで組み立てている右クリックメニューの項目 (配置サブメニューの選択肢と、タスクトレイの
「位置ロック」「最前面表示」) に `IsCheckable = true` を付けた。Fluent テーマは `IsCheckable` が
true の項目にしかチェックの枠を表示しないため。

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/OkidokeiWidget.App/PlacementMenuBuilder.cs` | modified | 横位置・縦位置・余白の選択肢に `IsCheckable = true` を追加。「反転してしまうので付けない」という旧コメントを、付けても問題ない理由に置き換えた |
| `src/OkidokeiWidget.App/App.xaml.cs` | modified | `BuildTrayContextMenu` の「位置ロック」「最前面表示」に `IsCheckable = true` を追加 |

## Diff Highlights

```csharp
// PlacementMenuBuilder.BuildGroup
// Fluent テーマは IsCheckable が true の項目にしかチェックを描かない (issue #34)。クリックで
// IsChecked が反転しても、メニューは開くたびに設定値から作り直すので表示と食い違わない
var item = new MenuItem
{
    Header = label,
    IsCheckable = true,
    IsChecked = current is { } c && c.Equals(value),
    IsEnabled = !isLocked,
};
```

## Tests Added or Updated

なし。App 層には自動テストがない (research.md #7)。修正後の表示は `/speckit-bug-test` で実機を
使って確認する。

## Local Verification

- `dotnet build` → 成功 (警告 0、エラー 0)
- `dotnet test` → 成功 (70 件すべて合格。既存テストへの影響なし)
- 実機での表示確認は `/speckit-bug-test` で行う

## Deviations from Assessment

なし

## Follow-ups

- assessment の「範囲外の見た目の問題」(サブメニューを持つ「配置」などの項目が、隣の項目より
  文字が左に寄って見える) は、本件では扱っていない
