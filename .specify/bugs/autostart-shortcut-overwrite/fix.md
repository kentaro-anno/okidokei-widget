# Bug Fix: 自動起動が有効なのに再起動時にウィジェットが起動しないことがある

- **Slug**: autostart-shortcut-overwrite
- **Fixed**: 2026-09-18
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`App.OnStartup` がアプリ起動のたびに無条件で `AutoStartManager.SetEnabled` を呼び、
Startup フォルダの `.lnk` をその時実行中のプロセスのパスで上書きしていた処理を削除した。
これにより、Release exe 以外の方法(`dotnet run`、IDE のデバッグ実行等)でアプリを起動
しても、Startup ショートカットが意図せず書き換わらなくなる。

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/OkidokeiWidget.App/App.xaml.cs` | modified | `OnStartup` 内の `AutoStartManager.SetEnabled(settings.AutoStartEnabled)` 呼び出し(旧 46 行目)を削除し、理由を説明するコメントに置き換えた |

`OnAutoStartChanged`(詳細設定のトグル時、190 行目付近)の呼び出しは変更していない。
自動起動の ON/OFF はこちらのみで行われるようになる。

## Diff Highlights

```csharp
RefreshConnectedMonitors();
SettingsRepository.Save(settings);

// Startup フォルダのショートカット作成/削除は詳細設定での ON/OFF トグル時
// (OnAutoStartChanged) のみ行う。起動のたびにここで書き換えると、Release exe
// 以外の方法 (dotnet run 等) で起動した際にショートカットの対象が意図せず
// 上書きされてしまうため (issue #20)
SyncClockWindows();
```

## Tests Added or Updated

- なし。assessment.md の判断通り、`App.xaml.cs` の起動シーケンスに対する自動テストの
  追加は見送った(WPF `Application` の起動を単体テストで検証するコストが見合わないため)。
  代わりに `/speckit-bug-test` で手動確認を行う

## Local Verification

- `dotnet build` → 成功、0 警告 0 エラー
- `dotnet test` → 成功、39 件全て合格(既存の `AutoStartManagerTests` を含む。今回の
  変更は `App.xaml.cs` 側のみで `AutoStartManager` 自体は変更していないため、既存テストは
  そのまま通る)
- 手動確認は `/speckit-bug-test` で実施予定(Release exe を直接起動しても Startup フォルダの
  `.lnk` が変化しないこと、`dotnet run` で起動しても `.lnk` が上書きされないこと)

## Deviations from Assessment

なし。assessment.md の Preferred Remediation をそのまま適用した。

## Follow-ups

- `.specify/bugs/autostart-shortcut-overwrite/assessment.md` の Risks & Considerations
  に記載の通り、Startup フォルダの `.lnk` を手動で削除した場合やアプリの実行ファイルを
  移動した場合、次回起動時の自動追随はなくなる(トグルを OFF→ON し直すまで自動起動は
  直らない)。現状の FR-019 の要求範囲外だが、実際に困るようなら別途 Issue 化する
- issue #18(`AutoStartManager.SetEnabled` が例外を投げるとアプリがクラッシュする)は
  本修正で 46 行目側の懸念のみ解消される。190 行目(トグル時)の try/catch 不足は
  引き続き issue #18 側で対応が必要
