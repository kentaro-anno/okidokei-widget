# Bug Assessment: 表示中のモニタとの HDMI 接続が切れると、約 19 秒後にアプリが落ちる

- **Slug**: hdmi-disconnect-crash
- **Created**: 2026-09-26 (同日に再 assessment。初回の assessment はコミット `fb9ae3c` に残っている)
- **Source**: https://github.com/kentaro-anno/desktop-clock-widget/issues/41
  - Host: `github.com` (allowlisted)
  - issue は本セッションで Claude が人間の報告と再現結果をもとに起票したもので、取得した内容に指示的な記述はない
- **Verdict**: valid
- **Severity**: high

## Report (verbatim or summarized)

issue #41「表示中のモニタとの HDMI 接続が切れると、約 19 秒後にアプリが落ちる (UCEERR_RENDERTHREADFAILURE)」の要約:

- 画面ロックから小一時間後に戻ると、アプリが落ちていた (2026-09-26 15:58:52)
- その 19 秒前に、Sony の TV (`DISPLAY\SNYAE04`) の取り外しがイベントログに記録されていた
- 人間が TV の Android OS を再起動して再現した。TV の電源 OFF/ON では再現しない

例外 (.NET Runtime 1026、毎回同一):

```text
System.Runtime.InteropServices.COMException (0x88980406): UCEERR_RENDERTHREADFAILURE (0x88980406)
   at System.Windows.Media.Composition.DUCE.Channel.SyncFlush()
   at System.Windows.Interop.HwndTarget.UpdateWindowSettings(Boolean enableRenderTarget, Nullable`1 channelSet)
   at System.Windows.Interop.HwndTarget.UpdateWindowPos(IntPtr lParam)
   ...
```

### 初回の assessment からの経緯

- 初回は「HDMI の接続断で GPU の描画デバイスが失われる」と見て、ソフトウェア描画に切り替えた
- しかし修正後も同じ例外で落ちた (test.md、Result: failed)
- その際のイベントログから、落ちるのは TV が外れた時点ではなく、再接続された直後だと分かった
  - 取り外しから「19 秒後」というのは、TV の Android が起動して HDMI の出力が戻るまでの時間だった
- そこで本 assessment では、推測で修正に進まず、診断用ビルドによる実験で原因を特定した (次節)

## Symptom

ウィジェットを表示しているモニタとの HDMI 接続が切れ、Windows がそのディスプレイを取り外したと
判断すると、その後に最初にウィンドウを作ったタイミングで、アプリが未処理例外で終了する。通常は
モニタの再接続時に、そのモニタのウィジェットを作り直す処理で落ちる。期待される動作は、取り外された
モニタのウィジェットだけが非表示になり、再接続されたら元の位置に再表示されること (spec の Edge Case
「稼働中のモニタ切断時は自動非表示、設定は保持」)。

## Reproduction

1. 拡大率の異なる 2 台のモニタの両方にウィジェットを表示する
   - 本環境: DELL (`DELF16C`、プライマリ、100%) と Sony の TV (`SNYAE04`、3840x2160、225%)
2. TV の Android OS を再起動する (HDMI の接続が一時的に切れる)
3. TV が再接続された瞬間 (取り外しの約 19 秒後) に、アプリが落ちる

2026-09-26 に人間が実機で、修正前のビルドで 2 回、ソフトウェア描画のビルドで 1 回、診断用ビルドで 3 回再現した。

## 診断用ビルドによる実験

作業フォルダとは別の git worktree で、ログ出力を加えたビルドを作って調べた。ログの内容は次のとおり。

- ウィンドウのメッセージ、`DpiChanged`、同期処理の開始と終了
- 未処理例外が起きた時点の呼び出し履歴の全体 (`Environment.StackTrace`)

| 回 | 変更内容 | 結果 |
|---|---|---|
| 1 | ログ出力のみ | 再接続時に落ちた。`WM_DISPLAYCHANGE` の処理の中で TV 用 `ClockWindow` を作る `Show()` の中だった |
| 2 | `WM_DISPLAYCHANGE` の処理を `BeginInvoke` で後回しにする | 後回しにした処理の `Show()` で、同じように落ちた |
| 3 | `WM_DISPLAYCHANGE` の処理を 10 秒遅らせ、その直前に DELL 側で 1×1 の確認用ウィンドウを表示する | 取り外しの 10 秒後 (再接続より前)、確認用ウィンドウの `Show()` で落ちた |
| 4 | 3 に加え、`DpiChanged` の処理を `BeginInvoke` で後回しにする | 落ちなかった。確認用ウィンドウも通り、再接続後に TV のウィジェットが元の位置に再表示された |

- 3 の結果から、描画スレッドは TV が**外れた時点で**すでに異常終了していると分かった
  - 外れた後は新しいウィンドウを作らないので表に出ず、再接続で TV のウィンドウを作ったときに初めて例外として表に出ていた
- 4 の結果から、外れたときの `DpiChanged` の処理が原因と特定できた

TV が外れたとき (回 1〜3) のログ:

```text
WM_DPICHANGED win=TV                  ← Windows が、行き場のなくなった TV のウィンドウを DELL へ移す
> DpiChanged TV 2.25->1
  > OnDisplaySettingsChanged
    monitors: DELL
    Close (removed) TV                ← TV のウィンドウ自身の DpiChanged の処理の中で、そのウィンドウを閉じている
    WM_CLOSE win=TV
    ...
    WM_DESTROY win=TV
  < OnDisplaySettingsChanged
< DpiChanged TV 2.25->1
```

## Suspected Code Paths

- `src/OkidokeiWidget.App/ClockWindow.xaml.cs:63` — `DpiChanged += (_, _) => _onDpiChanged();`
  - `DpiChanged` の中で、同期的に `App.OnDisplaySettingsChanged` を呼んでいる (issue #25 の対応で追加)
- `src/OkidokeiWidget.App/App.xaml.cs` (`OnDisplaySettingsChanged` → `SyncClockWindows`)
  - モニタを列挙し直し、取り外されたモニタの `ClockWindow` を `Close()` する
  - `DpiChanged` から呼ばれると、DPI の変更を処理している最中のウィンドウ自身を閉じることになる
- `src/OkidokeiWidget.App/App.xaml.cs` (`OnStartup`) — 初回の修正で入れた `RenderMode.SoftwareOnly`
  - 原因とは無関係だったので、元に戻す対象

## Root Cause Hypothesis

モニタが取り外されると、Windows はそのモニタ上にあったウィンドウを残ったモニタ (DELL) へ移す。
TV (225%) から DELL (100%) への移動なので、TV の `ClockWindow` に `WM_DPICHANGED` が届く。WPF は
その処理の途中で `DpiChanged` イベントを発火し、イベントから戻った後も、そのウィンドウの描画先を
新しい DPI に合わせる処理を続ける。ところが本アプリは `DpiChanged` の中で同期的に
`OnDisplaySettingsChanged` を呼んでおり、取り外されたモニタのウィンドウとして、その `ClockWindow`
自身を `Close()` (破棄) してしまう。WPF は破棄済みのウィンドウに対して描画の処理を続けることになり、
描画スレッドが異常終了する。その後、最初に描画スレッドと同期する操作 (通常は再接続時のウィンドウの
作成) で `UCEERR_RENDERTHREADFAILURE` が投げられ、アプリが落ちる。

確度: **high**。`DpiChanged` の処理を後回しにするだけで、描画スレッドが生き残り、落ちなくなることを
実機で確認した (実験の回 4)。ただし WPF の内部のどの処理が破棄済みのウィンドウで失敗しているかまでは
確認していない。

- ハードウェア描画でもソフトウェア描画でも落ちたこととも矛盾しない
- TV の電源 OFF/ON で落ちないのは、取り外しが起きず、`WM_DPICHANGED` も届かないため

## Proposed Remediation

**Preferred**: `ClockWindow` の `DpiChanged` ハンドラで `_onDpiChanged()` を直接呼ばず、
`Dispatcher.BeginInvoke(DispatcherPriority.Loaded, ...)` で後回しにする。WPF が `WM_DPICHANGED` の
処理を終えてから、モニタの列挙し直し・ウィンドウを閉じる・配置し直す処理が走るようになる。

- 変更は `ClockWindow.xaml.cs` の 1 か所で済む
- `DispatcherPriority.Loaded` は issue #36 の対応 (`SizeChanged`) で使っているものと同じ優先度
- あわせて、初回の修正で入れた `RenderMode.SoftwareOnly` を元に戻す
  - 原因と無関係で、CPU 使用率が 0.007% から 0.037% に増えるだけのため

実験の回 3・4 で入れた「`WM_DISPLAYCHANGE` の処理を 10 秒遅らせる」「確認用ウィンドウ」は、原因を切り分けるための診断用で、修正には含めない。

- 回 1・2 で再接続時に落ちたのは、描画スレッドが取り外しの時点で死んでいたため
  - なので、`DpiChanged` を直せば、`WM_DISPLAYCHANGE` の処理は今のまま (同期) でよいはず
- ただし「遅らせない」組み合わせは実機で未確認。`/speckit-bug-test` で確かめる

**Alternatives**:

- `SyncClockWindows` の中で `Close()` だけを後回しにする
  - `DpiChanged` 以外の経路 (トレイからの表示切替等) の動きまで変わる
  - 問題は「`DpiChanged` の最中に同期処理を走らせること」なので、呼び出し元で後回しにする方が的確。採らない
- `WM_DISPLAYCHANGE` の処理も遅らせる
  - 実験の回 4 はこの状態で成功しているが、原因の説明には不要で、再表示が遅れるだけ
  - `/speckit-bug-test` で、遅らせない場合に落ちると分かったときに改めて検討する

**Files likely to change**:

- `src/OkidokeiWidget.App/ClockWindow.xaml.cs`
- `src/OkidokeiWidget.App/App.xaml.cs` (`RenderMode.SoftwareOnly` を元に戻す)

**Tests to add or update**:

- WPF のメッセージ処理や描画スレッドを扱う自動テストは作れない
  - 既存のテストプロジェクトは `OkidokeiWidget.Core` の純粋なロジックが対象
- 検証は実機の手動確認で行う (`/speckit-bug-test`)
  - 両モニタにウィジェットを表示し、TV の Android OS を再起動する。落ちないこと、再接続後に TV 側のウィジェットが元の位置に再表示されることを確認する
  - issue #25 の回帰確認: 位置ロック中に、ディスプレイ設定で拡大率を変えてもウィジェットの位置がずれないこと

## Risks & Considerations

- 後回しにすることで、DPI が変わってから配置し直すまでの一瞬、WPF が自動で動かした位置に描画される可能性がある
  - issue #25 の「位置ロック中にずれない」は、最終的な位置が保たれればよいので、見た目の問題にはならないと考える
- issue #39 (DPI の違うモニタの境界をドラッグでまたぐと「ウィンドウ ハンドルが無効です」で落ちる) も、同じ `DpiChanged` → `OnDisplaySettingsChanged` の経路を通る
  - 根が同じ可能性があるが、#39 は FR の改訂で対応する方針になっており、本件では扱わない
  - 本件の修正後に #39 が再現しなくなるかは、参考として #39 側に記録する
- WPF の内部で何が失敗しているかは、実験で確かめた範囲の推定にとどまる
  - `DpiChanged` を後回しにすると直ることは確認済み

## Open Questions

- なし
