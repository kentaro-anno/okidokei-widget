# Bug Assessment: 自動起動が有効なのに再起動時にウィジェットが起動しないことがある

- **Slug**: autostart-shortcut-overwrite
- **Created**: 2026-09-18
- **Source**: https://github.com/kentaro-anno/desktop-clock-widget/issues/20 (host: github.com, allowlisted, fetched via `gh issue view 20`)
- **Verdict**: likely valid, needs reproduction
- **Severity**: medium

## Report (要約)

Issue #20 より:

> 自動起動 (AutoStartEnabled) を有効にしているにもかかわらず、PC 再起動後にログインしても
> ウィジェットが自動的に起動しなかった。手動で Release ビルドの exe をダブルクリックすると
> 問題なく起動する。

再現手順として、Release exe 以外の方法(`dotnet run`、IDE のデバッグ実行等)でアプリを
起動した後 PC を再起動すると、Startup フォルダの `.lnk` の対象が書き換わってしまい
自動起動が無言で失敗する、という仮説が記載されている。ただし issue 本文にも明記されている
通り、「手順 2 で実際に Release exe 以外の方法で起動したか」自体は確定していない
(当日のイベントログ・ファイルタイムスタンプからの推測)。

## Symptom

自動起動が有効な状態で PC を再起動しても、時計ウィジェットが自動的に表示されないことがある。
エラーダイアログやクラッシュログは残らず、手動で exe を起動すれば問題なく動作する。

## Reproduction

1. 詳細設定で自動起動を ON にする
2. `dotnet run` または IDE のデバッグ実行で、ビルド済みの Release exe とは別プロセスとして
   アプリを起動する(この時点で `App.OnStartup` が実行され、Startup フォルダの `.lnk` の
   対象が `dotnet.exe` 等、実行中プロセスのパスに書き換わる)
3. アプリを終了し、Release exe を起動し直さないまま PC を再起動する
4. ログイン後、`.lnk` の対象(`dotnet.exe` 等)は実行されるが引数がないため何も表示されない

[NEEDS CLARIFICATION: 手順 2 の発生自体が未確定。次回同じ状況(Release ビルド後に
`dotnet run`/デバッグ実行を行う)を意図的に再現し、Startup フォルダの `.lnk` の対象が
実際に書き換わることを確認できると verdict を valid に引き上げられる]

## Suspected Code Paths

- `src/OkidokeiWidget.Core/Persistence/AutoStartManager.cs:24` — `SetEnabled` は
  `executablePath` 省略時に `Environment.ProcessPath`(現在実行中のプロセスのパス)を
  無条件にショートカットの対象にする
- `src/OkidokeiWidget.App/App.xaml.cs:46` — `OnStartup` 内で、アプリ起動のたびに無条件で
  `AutoStartManager.SetEnabled(settings.AutoStartEnabled)` を呼んでいる。呼び出し元は
  `executablePath` を明示しないため、その時点で動いているプロセスがそのまま Startup
  ショートカットの対象になる
- `src/OkidokeiWidget.App/App.xaml.cs:190` — `OnAutoStartChanged`(詳細設定のトグル操作時)
  でも同じ `SetEnabled(_settings.AutoStartEnabled)` を呼んでいるが、こちらはユーザーが
  明示的に ON/OFF を切り替えた結果として妥当な呼び出しである

## Root Cause Hypothesis

`App.OnStartup` (App.xaml.cs:46) が、ユーザー操作を経ずにアプリ起動のたびに Startup
フォルダの `.lnk` を「その時実行中のプロセスのパス」で無条件に上書きしている
(セルフヒーリング目的の実装と推測されるが、そのような要求は spec.md の FR-019 には
存在しない)。この結果、Release exe 以外の方法(`dotnet run` や IDE のデバッグ実行)で
アプリを起動すると、意図せず Startup ショートカットが破壊される。`dotnet run` の場合、
実際に起動するプロセスは `dotnet.exe` であり、これを引数なしで自動実行しても何も起きない
上にエラーも出ないため、次回の自動起動が無言で失敗する。

確信度: **medium**。コード上この経路が存在し得ることは確実だが、今回の具体的な発生を
100% 再現・確定できたわけではない(イベントログにはクラッシュ・ブロックの痕跡がなく、
「何も起きずに終了した」という状況とは整合するが、他の要因を完全には排除できていない)。

## Proposed Remediation

**Preferred**: `App.OnStartup` 内の起動時自動書き換え呼び出し(App.xaml.cs:46)を削除する。
Startup フォルダの `.lnk` 作成/削除は、詳細設定画面での ON/OFF トグル
(`OnAutoStartChanged`、App.xaml.cs:190)時のみ行うようにする。FR-019 が要求しているのは
「ユーザーが自動起動の ON/OFF を設定できること」であり、起動毎のセルフヒーリングは
要求されていない。この変更により、Release exe 以外の方法で起動した際に Startup
ショートカットが意図せず上書きされる経路自体がなくなる。

**Alternatives** (検討したが不採用):
- `Environment.ProcessPath` が `dotnet.exe` 等、想定外の実行ファイルである場合のみ
  書き換えをスキップするガードを追加する案。今回の preferred 案(呼び出し自体の削除)の
  方がコードが増えず、YAGNI に沿う

**Files likely to change**:
- `src/OkidokeiWidget.App/App.xaml.cs`(46 行目の呼び出しを削除)

**Tests to add or update**:
- 既存の `tests/OkidokeiWidget.Core.Tests/Persistence/AutoStartManagerTests.cs` は
  `AutoStartManager` 単体のテストであり、今回の修正(呼び出し元である `App.xaml.cs` 側の
  変更)には直接対応するテストがない。WPF `Application` の起動シーケンスを単体テストで
  検証するのは大掛かりになるため、自動テストの追加は見送り、`/speckit-bug-test` での
  手動確認(設定ファイルの `AutoStartEnabled` を変更しない限り Startup フォルダの `.lnk`
  が変化しないこと)で代替する

## Risks & Considerations

- 起動毎のセルフヒーリングを削除することで、ユーザーが Startup フォルダの `.lnk` を
  手動で削除してしまった場合、次回起動時には自動で復元されなくなる(トグルを OFF→ON
  し直すまで自動起動は直らない)。ただし元々 `.lnk` を手動で触るような操作は想定外であり、
  影響は軽微と判断
- 実行ファイルを別の場所に移動した場合も同様に、次回起動時の自動追随がなくなる。ただし
  これも現状の FR-019 の要求範囲外

## Open Questions

- [NEEDS CLARIFICATION: 今回の発生時、実際に Startup フォルダの `.lnk` が `dotnet.exe`
  等を指していたかどうかは、書き換え後の状態しか確認できておらず直接確認はできていない]
