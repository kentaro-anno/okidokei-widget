# Bug Verification: 位置ロック中でも DPI 変更でウィジェットの表示位置がズレる

- **Slug**: dpi-position-drift
- **Tested**: 2026-09-24
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: partial (2026-09-24 訂正。当初は verified。末尾の「訂正」を参照)

## Summary

ビルド・既存の Core テスト (39 件) はいずれも成功し、今回の変更によるリグレッションは確認されなかった。
その後、人間 (issue 報告者) がこのブランチのビルドを実機で起動し、位置ロックを有効にした状態で
Windows の表示スケールを変更したところ、ウィジェットが追随して位置を調整し直す動作が確認できた
(`DpiChanged` → `App.OnDisplaySettingsChanged` の経路が実際に発火していることの確認)。ズレ幅の
定量比較 (px 単位の diff) までは行っていないが、修正前は位置ロック中でも何もフィードバックなく
ズレたままだった挙動が、修正後は DPI 変更に反応して再配置される挙動に変わったことは実機で確認できた。

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (post-fix) | 実機の DPI スケール変更 / モニタ抜き差し | pass | 人間 (issue 報告者) がこのブランチをビルドして起動し、位置ロック中に表示スケールを変更 → ウィジェットが追随して再配置される動作を確認。px 単位の diff 再計測は未実施 |
| Build | `dotnet build` | pass | 0 エラー、0 警告 |
| Regression suite (Core) | `dotnet test` | pass | 39 件全て合格 (失敗 0、スキップ 0) |
| Lint / type-check | (プロジェクトに専用の lint 構成なし。ビルドの警告 0 件で代替) | pass | - |
| コードレビュー: 配線の妥当性 | `ClockWindow.DpiChanged` → `_onDpiChanged()` → `App.OnDisplaySettingsChanged` (`RefreshConnectedMonitors` → `SyncClockWindows` → `SettingsRepository.Save`) の呼び出し経路を目視確認 | pass | `SyncClockWindows` は表示中のウィンドウに対し `UpdateMonitor` → `ApplyPlacement` を呼ぶため、保存済み座標からの再配置が行われる経路になっている |

## Output Excerpts

```
ビルドに成功しました。
    0 個の警告
    0 エラー

成功!   -失敗:     0、合格:    39、スキップ:     0、合計:    39
```

## Residual Risks

- `Window.DpiChanged` が実機でいつ・どの程度の頻度で発火するかは未検証。issue #25 で報告された
  「画面ロック解除後」等のタイミングで確実に発火するかは、実際に DPI 再ネゴシエーションが
  起きる環境でのみ確認できる
- モニター 2 の保存済み相対座標 (X=2881) が実際の作業領域を超えている状態そのものは、今回の
  コード変更だけでは自動的には直らない。`DpiChanged` (または次回の `WM_DISPLAYCHANGE`) が
  発火して初めて `MonitorSettingsReconciler` 経由で追随・保存し直される
- 複数モニタで `DpiChanged` がほぼ同時に発火した場合の冪等性はコードレビューでは妥当と判断したが、
  実機での同時発火は未検証

## Recommendation

Close — ビルド・既存テストに加え、実機で位置ロック中に表示スケールを変更してウィジェットが
追随して再配置される挙動を人間が確認した。issue #25 の症状 (DPI 変更でロック位置がズレたまま
放置される) に対する修正として妥当と判断する。

なお、動作確認の過程で新たに「アンカー (右上/右下/左上/左下など) 基準での位置指定」という
関連の機能要望が挙がっている。これは今回のバグ修正の範囲外の新機能であり、`spec.md` の変更を
伴う可能性が高いため、別途 GitHub Issue として切り出し、対応するなら通常の
`/speckit-specify` からのフェーズで検討する。

問題なければこの bug を close してよい。ズレが再現する場合は新しい観測結果を添えて
`/speckit-bug-assess` からやり直す。

## 訂正 (2026-09-24)

assessment.md の「訂正」のとおり、元の症状 (位置ロック中のずれ) を示すとした実測値は計測の
誤りだった。人間が確認したのは「表示スケールを変えると保存済みの位置へ再配置される」ことで、
修正した経路が動くことの確認にはなっている。一方で、報告されたずれがこの修正で直ったことは
確認できていない。

このため Result を verified から partial に改める。同じ症状が再び起きた場合は、Per-Monitor V2 で
座標を測った上で `/speckit-bug-assess` からやり直す。
