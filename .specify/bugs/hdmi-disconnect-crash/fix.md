# Bug Fix: 表示中のモニタとの HDMI 接続が切れると、約 19 秒後にアプリが落ちる

- **Slug**: hdmi-disconnect-crash
- **Fixed**: 2026-09-26 (同日の再修正。初回のソフトウェア描画への切り替えの fix.md はコミット `fb9ae3c` に残っている)
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`ClockWindow` の `DpiChanged` ハンドラで同期処理 (`App.OnDisplaySettingsChanged`) を直接呼ばず、
`Dispatcher.BeginInvoke` で `WM_DPICHANGED` の処理が終わった後に実行するようにした。モニタの
取り外しで DPI の変更を処理している最中のウィンドウ自身を閉じてしまい、描画スレッドが異常終了する
のを防ぐ。あわせて、原因と無関係だった初回の修正 (`RenderMode.SoftwareOnly`) を元に戻した。

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/OkidokeiWidget.App/ClockWindow.xaml.cs` | modified | `DpiChanged` の処理を `Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ...)` で後回しにした。理由のコメントを追加 |
| `src/OkidokeiWidget.App/App.xaml.cs` | reverted | 初回の修正で入れた `RenderMode.SoftwareOnly` と `using` 2 行を削除。`main` と同じ内容に戻った |

## Diff Highlights (optional)

```csharp
// 差し替えて保存済みの座標から配置し直すことでロック位置を維持する (issue #25)。
// ただし DpiChanged の中で同期的に処理してはいけない。モニタの取り外しで Windows がこの
// ウィンドウを別の DPI のモニタへ移すと、ここでモニタを列挙し直した結果このウィンドウ自身が
// 閉じられ、WPF が DPI の処理を続ける途中で破棄されて描画スレッドが異常終了する (issue #41)。
// WM_DPICHANGED の処理が終わってから実行する
DpiChanged += (_, _) => Dispatcher.BeginInvoke(DispatcherPriority.Loaded, _onDpiChanged);
```

- 優先度 `DispatcherPriority.Loaded` は、issue #36 の対応 (`SizeChanged`) と同じ
- 診断用ビルドの実験 (assessment の回 3・4) で入れた次のものは含めていない
  - `WM_DISPLAYCHANGE` の処理を 10 秒遅らせる
  - 確認用の 1×1 ウィンドウ
  - ログ出力

## Tests Added or Updated

- なし
  - assessment のとおり、WPF のメッセージ処理や描画スレッドを扱う自動テストは作れない
  - 検証は `/speckit-bug-test` での実機の手動確認で行う

## Local Verification

- Commands run: `dotnet build -c Release` → 成功 (警告 0、エラー 0)
- Commands run: `dotnet test -c Release --no-build` → 70 件すべて合格
- Commands run: `git diff main -- src/OkidokeiWidget.App/App.xaml.cs` → 差分なし (元に戻ったことを確認)
- Manual checks: 未実施 (`/speckit-bug-test` で行う)
  - 同じ修正を含む診断用ビルド (実験の回 4) では、TV の Android OS の再起動で落ちないことを確認済み
  - ただし回 4 は `WM_DISPLAYCHANGE` の 10 秒遅延も入った状態だった

## Deviations from Assessment

なし

## Follow-ups

- `/speckit-bug-test` で次を確認する
  - TV の Android OS を再起動しても落ちないこと (`WM_DISPLAYCHANGE` を遅らせない状態で)
  - TV の再接続後、TV 側のウィジェットが元の位置に再表示されること
  - issue #25 の回帰確認: 位置ロック中にディスプレイ設定で拡大率を変えても、ウィジェットの位置がずれないこと
- issue #39 に、同じ `DpiChanged` → `OnDisplaySettingsChanged` の経路を通ることをコメントで記録する
- 診断用の git worktree (スクラッチパッド内) を片付ける
