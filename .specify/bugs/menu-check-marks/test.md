# Bug Verification: 右クリックメニューで、今の配置や位置ロックのチェックマークが表示されない

- **Slug**: menu-check-marks
- **Tested**: 2026-09-24
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

修正版 (Debug ビルド) を実機で起動し、assessment の再現手順と同じ画面を撮影して確認した。
本体の「配置 ▶ 横位置」では今の配置 (右) に、タスクトレイのメニューでは「位置ロック」に、
チェックが表示されるようになった。既存テストへの影響もない。

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix): 本体の配置サブメニュー | 起動中の Release 版 (修正前) を終了して Debug 版 (修正後) を起動。拡大率 225% のモニター 2 のウィジェットを右クリックし、UI Automation で「配置 ▶ 横位置」を展開して PrintWindow で撮影 | pass | モニター 2 は「右上」の設定。修正前は「右」にチェックがなかったが、修正後は付いた。撮影の間だけ位置ロックを外し、撮影後に元の ON に戻したことを `settings.json` で確認 |
| Reproduction (post-fix): タスクトレイのメニュー | トレイの非表示ウィンドウ (`OkidokeiWidgetTrayHost`) に WM_TRAYICON + WM_RBUTTONUP を送ってメニューを開き、撮影 | pass | 位置ロック ON の状態で「位置ロック」にチェックが付いた。修正前の同じ画面は撮影していない (assessment の [NEEDS CLARIFICATION] のとおり) |
| Build | `dotnet build` | pass | 警告 0、エラー 0 |
| Regression suite | `dotnet test` | pass | 70 件すべて合格 |

## Output Excerpts

```
ビルドに成功しました。 0 個の警告 0 エラー
成功!   -失敗:     0、合格:    70、スキップ:     0、合計:    70
```

撮影したウィンドウ (物理ピクセル、Per-Monitor V2 で取得):

```
ウィジェット          (-969,-1053) 951x210
右クリックメニュー    (-494,-948)  315x436
配置サブメニュー      (-705,-715)  243x315
横位置サブメニュー    (-885,-710)  239x324   ← 「右」にチェック
```

## Residual Risks

- 縦位置・余白のサブメニューと、トレイの「最前面表示」のチェックは画像では確認していない。
  いずれも同じ `BuildGroup` / 同じ書き方で作っているため、同じ修正で直っていると判断した
- 拡大率 100% のモニターでの表示は確認していない (トレイのメニューは 100% で描かれており、
  そちらではチェックの表示を確認できた)

## Recommendation

Close — 修正前に再現していた画面 (本体の「配置 ▶ 横位置」) で、修正後にチェックが表示される
ことを実機で確認した。
