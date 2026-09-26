# Bug Verification: 表示中のモニタとの HDMI 接続が切れると、約 19 秒後にアプリが落ちる

- **Slug**: hdmi-disconnect-crash
- **Tested**: 2026-09-26 (同日の再検証。初回のソフトウェア描画の修正に対する failed の test.md はコミット `fb9ae3c` に残っている)
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

修正後のビルド (`DpiChanged` の後回し、診断用の遅延処理なし) で再現手順を実施し、TV の再接続後もアプリは
落ちず、TV 側のウィジェットも元の位置に再表示された。issue #25 (位置ロック中の DPI 変更) の回帰もなかった。

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | 修正後の Release ビルド (17:19:53 起動) で、人間が TV の Android OS を再起動 | pass | 17:20:42 に取り外し、17:21:01 に再接続。落ちずに同じプロセスが動き続けた |
| 再接続後の再表示 | 人間の目視 | pass | TV 側のウィジェットが元の位置に再表示された |
| issue #25 の回帰確認 | 位置ロック ON のまま、人間が TV の拡大率を 225% → 100% → 300% → 225% と変更 | pass | 各段階で位置がずれず、元どおり。この間もアプリは落ちていない |
| New / updated tests | なし | not-run | 自動テストは追加していない (WPF のメッセージ処理は自動テスト不可) |
| Regression suite | `dotnet test -c Release --no-build` | pass | fix 時に実施。70 件すべて合格 |
| Lint / type-check | `dotnet build -c Release` | pass | fix 時に実施。警告 0、エラー 0 |

## Output Excerpts

イベントログ (再現確認時):

| 時刻 | ログ | 出来事 |
|---|---|---|
| 17:19:53 | - | 修正後のビルドを起動 (PID 25348) |
| 17:20:42.445 | Kernel-PnP 1010 | `DISPLAY\SNYAE04` の取り外し |
| 17:21:01.0〜01.7 | Audio 65 | TV の再接続 |
| 17:23:27 時点 | - | PID 25348 のまま動作中 (応答あり)。17:19:50 以降、OkidokeiWidget の Application ログのエラーは 0 件 |

修正前は、再接続の瞬間 (取り外しの約 19 秒後) に毎回 `UCEERR_RENDERTHREADFAILURE` で落ちていた
(修正前のビルドで 2 回、ソフトウェア描画のビルドで 1 回、診断用ビルドで 3 回)。

## Residual Risks

- 再現確認は、TV の Android OS の再起動による HDMI の接続断の 1 回のみ
  - ケーブルを物理的に抜く場合も、同じ取り外しと `WM_DPICHANGED` の経路を通るはずだが、未確認
- 拡大率が同じモニタ同士の場合は、取り外しで `WM_DPICHANGED` が届かないので、もともとこの経路を通らない
  - 本環境 (100% と 225%) 以外の組み合わせは未確認
- issue #39 (DPI の違うモニタの境界をドラッグでまたぐと落ちる) は、同じ `DpiChanged` の経路を通る
  - 本修正で直るかは未確認

## Recommendation

Close the bug — verified end-to-end. 修正後のビルドで、元の再現手順を実機で実施して落ちないことを
確認した。issue #25 の回帰もない。PR をマージした後に issue #41 を閉じる。issue #39 には、同じ経路を
通ることをコメントで記録しておく。
