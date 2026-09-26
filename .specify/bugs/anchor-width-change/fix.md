# Bug Fix: 表示文字列の変化でウィジェット幅が変わると、右上アンカーの位置がずれる

- **Slug**: anchor-width-change
- **Fixed**: 2026-09-25
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`SizeChanged` のハンドラで `ApplyPlacement()` を同期的に呼ぶのをやめ、`Dispatcher.BeginInvoke` で
UI スレッドのキューの後ろに積むようにした。Win32 側のウィンドウのリサイズが終わってから
`GetWindowRect` で幅を取るので、右寄せ・下寄せのアンカーでも新しいサイズで配置される。

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/OkidokeiWidget.App/ClockWindow.xaml.cs` | modified | `SizeChanged` での再配置を `Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ApplyPlacement)` に変更し、理由をコメントに記載 |

## Diff Highlights (optional)

```csharp
// 変更前
SizeChanged += (_, _) => ApplyPlacement();

// 変更後
SizeChanged += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ApplyPlacement);
```

## Tests Added or Updated

- なし
  - assessment のとおり、WPF のイベント順序を扱う自動テストの仕組みがないため
  - 配置計算 (`WidgetPlacementCalculator`) は変更しておらず、既存のテストで足りる

## Local Verification

- Commands run:
  - `dotnet build` → 成功 (警告 0、エラー 0)
  - `dotnet test` → 70 件すべて合格
- Manual checks:
  - 修正前の調査で、最小の WPF アプリにより `DispatcherPriority.Loaded` の `BeginInvoke` 後なら
    `GetWindowRect` が新しい幅を返すことを実測済み (assessment.md の表)
  - 本アプリでの実機確認は `/speckit-bug-test` で行う

## Deviations from Assessment

なし

## Follow-ups

- 実機で右上アンカーのままフォントサイズや日付表示を変え、右端の余白が保たれることを確認する (`/speckit-bug-test`)
