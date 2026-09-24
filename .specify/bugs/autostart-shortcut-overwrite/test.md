# Bug Verification: 自動起動が有効なのに再起動時にウィジェットが起動しないことがある

- **Slug**: autostart-shortcut-overwrite
- **Tested**: 2026-09-18
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial

## Summary

修正の核心(「Release exe 以外の方法で起動しても Startup フォルダの `.lnk` が書き換わらない」)
は実機で再現・確認できた。ただし、issue #20 の症状そのもの(PC 再起動後にログインしても
ウィジェットが自動起動しない)は、実際に PC を再起動して確認したわけではないため、
症状レベルでの end-to-end 検証はできていない。

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| 修正内容のメカニズム再現 | Release exe を終了 → `bin/Debug/.../OkidokeiWidget.App.exe` を起動 → Startup フォルダの `.lnk` の `LastWriteTime` / `TargetPath` を前後で比較 | pass | 修正前なら `OnStartup` のたびに書き換わっていたはずの `.lnk` が、Debug exe 起動後も `LastWriteTime` (9:38:02)・`TargetPath` (Release exe のパス) ともに不変であることを確認 |
| 新規/更新テスト | (assessment.md の判断により自動テストは追加していない) | not-run | `App.xaml.cs` の起動シーケンス自体を検証する自動テストは今回追加していないため対象なし |
| 既存回帰テスト | `dotnet test` | pass | 39 件全て合格 (`AutoStartManagerTests` 含む) |
| ビルド | `dotnet build` | pass | 0 警告 0 エラー |
| PC 再起動での end-to-end 再現 | (未実施) | not-run | 実機の PC 再起動を伴うため、今回のセッションでは実施していない。次回の実際の再起動時に、意図せず Debug ビルドや `dotnet run` を挟んだ場合でも自動起動が機能することで最終確認とする |
| トグル操作 (`OnAutoStartChanged`) の回帰確認 | (未実施、コードレビューのみ) | not-run | 190 行目付近のトグル時の呼び出し自体は変更していないため動作は変わらないはずだが、詳細設定 UI を実際に操作しての確認はしていない |

## Output Excerpts

```
=== before ===
LastWriteTime: 2026/09/18 9:38:02
TargetPath before: ...\bin\Release\net10.0-windows\OkidokeiWidget.App.exe

=== launching Debug build (simulates dotnet run / non-Release launch) ===

=== after launching Debug build ===
LastWriteTime: 2026/09/18 9:38:02   (変化なし)
TargetPath after: ...\bin\Release\net10.0-windows\OkidokeiWidget.App.exe   (変化なし)
```

```
成功!   -失敗:     0、合格:    39、スキップ:     0、合計:    39
```

## Residual Risks

- 今回の元の症状(再起動後に自動起動しない)自体は、当時のイベントログ・タイムスタンプからの
  推測に基づくものであり、確定的な再現はできていない(assessment.md 参照)。今回のテストは
  「推定した発生メカニズムが、修正後は起きなくなること」を確認したものであり、
  「実際に起きていた症状そのものが直った」ことの直接証明ではない
- 詳細設定 UI からのトグル操作 (`OnAutoStartChanged`) は今回変更しておらず、既存動作を
  維持している前提だが、実機での UI 操作による確認はしていない

## Recommendation

修正のメカニズムレベルでの検証は完了しているため、いったんクローズして差し支えないと考える。
ただし念のため、次回以降 Release exe 以外の方法(`dotnet run`、IDE のデバッグ実行)で
アプリを試した際は、その後 Release exe を一度起動し直してから PC を再起動する、という
運用上の注意は今後も有効(今回の修正はその手順を踏み忘れても壊れなくなる、という話であり、
Release exe を一度も起動しないまま自動起動だけを期待するケースはそもそも対象外)。
