# Bug Verification: 表示文字列の変化でウィジェット幅が変わると、右上アンカーの位置がずれる

- **Slug**: anchor-width-change
- **Tested**: 2026-09-25
- **Assessment**: ./assessment.md
- **Fix**: ./fix.md
- **Result**: verified

## Summary

修正前のビルドで、秒表示の ON/OFF により幅を変えると位置がずれることを人間が実機で再現した。
修正後のビルドで同じ操作をしてもずれないことを人間が確認し、既存のテストもすべて合格した。

## Checks Performed

| Check | Command / Action | Result | Notes |
|-------|------------------|--------|-------|
| Reproduction (pre-fix) | 修正前の Release ビルドで、右上アンカー (余白: 狭い) のまま詳細設定の「秒を表示する」を ON → OFF | fail (再現) | [人間] OFF にした時点で左にずれた。幅が縮んだのに、古い (広い) 幅で配置した結果と整合する |
| Reproduction (post-fix) | Claude が修正版の Release ビルドを作り直して起動 (2026-09-25 01:45:34)、同じ操作を実施 | pass | [人間] 「直ってる」と確認 |
| New / updated tests | なし | not-run | WPF のイベント順序を扱う自動テストの仕組みがないため、テストは追加していない (fix.md) |
| Regression suite | `dotnet test` | pass | 70 件すべて合格 |
| Build | `dotnet build` / `dotnet build -c Release` | pass | 警告 0、エラー 0 |
| Lint / type-check | - | skipped | 本プロジェクトに lint の設定はない。型チェックはビルドで兼ねる |

## Output Excerpts

```text
成功!   -失敗:     0、合格:    70、スキップ:     0、合計:    70、期間: 152 ms - OkidokeiWidget.Core.Tests.dll (net10.0)
```

## Residual Risks

- 実機で確認したのは、右上アンカーでの秒表示 ON/OFF による幅の変化のみ
  - 報告時のトリガーと推定した曜日の変化 (深夜 0 時) は、実機では待って確認していない
  - 同じ `SizeChanged` の経路を通るため、秒表示の確認で代替できると判断した
- 中央寄せ・下寄せアンカーと、高さの変化 (フォントサイズ変更、日付表示の ON/OFF) は実機で確認していない
  - いずれも同じ経路で、修正で直るはずだが未確認
- WPF のイベント順序に依存する修正のため、自動テストで回帰を検知できない

## Recommendation

issue #36 をクローズしてよい。報告された症状 (右寄せアンカーで幅が変わると位置がずれる) は、
修正前に実機で再現し、修正後に同じ操作で解消したことを確認した。中央寄せ・下寄せや高さの変化は
未確認だが、同じ経路の修正であり、今後気づいた時点で追加で確認すればよい。
