# Bug Assessment: 右クリックメニューで、今の配置や位置ロックのチェックマークが表示されない

- **Slug**: menu-check-marks
- **Created**: 2026-09-24
- **Source**: https://github.com/kentaro-anno/desktop-clock-widget/issues/34 (host: github.com、allowlisted、`gh issue` で作成した本人の Issue)
- **Verdict**: valid
- **Severity**: medium

## Report (要約)

issue #34 より:

- ウィジェット本体の「配置 ▶ 横位置 / 縦位置 / 余白」で、今の配置と余白にチェックが付かない
  (実機の Release 版、拡大率 225% のモニタで確認。モニター 2 は「右上」なのに「横位置 ▶ 右」に
  チェックがない)
- タスクトレイの「位置ロック」「最前面表示」も同じ作り方のため、チェックが付かないと考えられる
- 公開リポジトリの README 用にスクリーンショットを撮った際に見つかった

## Symptom

右クリックメニューのうち、コードで組み立てている項目 (配置サブメニューとタスクトレイのメニュー) で、
チェックマークが一切表示されない。contracts/context-menus.md では、今の配置・余白と、位置ロック・
最前面表示の状態にチェックを付けることになっている (FR-037, FR-038)。

## Reproduction

1. 位置ロックを OFF にし、ウィジェットを右クリック →「配置 ▶ 横位置 ▶ 右」を選ぶ
2. もう一度右クリック →「配置 ▶ 横位置」を開く
3. 「右」にチェックが付いていない (2026-09-24 に実機で確認)

タスクトレイの「位置ロック」「最前面表示」は、実機ではまだ確認していない
([NEEDS CLARIFICATION: 修正前の実機での確認は未実施。原因が同じなので同時に直す])。

## Suspected Code Paths

- `src/OkidokeiWidget.App/PlacementMenuBuilder.cs:82-86` (`BuildGroup`) — クリックでチェックが
  反転しないよう、`IsCheckable` を付けずに `IsChecked` だけを設定している
- `src/OkidokeiWidget.App/App.xaml.cs:146,154` (`BuildTrayContextMenu`) — トレイの「位置ロック」
  「最前面表示」も `IsChecked` だけで、`IsCheckable` がない
- 比較対象: `src/OkidokeiWidget.App/ClockWindow.xaml:22-23` — 本体の「位置ロック」「最前面表示」は
  XAML で `IsCheckable="True"` を付けており、2026-09-17 のスクリーンショットではチェックが出ていた

## Root Cause Hypothesis

**確信度: 高** (テンプレートのソースで確認済み)

アプリは `ThemeMode="System"` で WPF の Fluent テーマを使っている (.NET 10)。dotnet/wpf の
`release/10.0` ブランチの `PresentationFramework.Fluent/Styles/MenuItem.xaml` を確認したところ、
サブメニュー項目のテンプレート (`SubmenuItemTemplateKey`) は次の作りになっていた。

- チェックマークを入れる枠 `CheckBoxIconBorder` は既定で `Visibility="Collapsed"`
- `IsCheckable = True` のトリガーでのみ、枠が `Visible` になる
- `IsChecked = True` のトリガーは、枠の中の文字 (`CheckBoxIcon.Text`) を ✓ にするだけ

したがって `IsCheckable` が false のままだと、`IsChecked` を true にしても ✓ は枠ごと非表示のまま
になる。従来の Aero2 テーマのように「`IsChecked` だけでチェックが出る」ことを前提に書いたのが原因。

## Proposed Remediation

**Preferred**: 該当する項目すべてに `IsCheckable = true` を付ける。

- `IsCheckable` を付けると、クリックした時点で WPF が `IsChecked` を反転させる。しかし配置
  サブメニューもトレイのメニューも開くたびに作り直しており、実際の値は各コールバックが設定値
  から決める。反転した表示はメニューが閉じると同時に捨てられるので、表示と設定値が食い違う
  ことはない
- `PlacementMenuBuilder.cs` の「`IsCheckable` にするとクリックでチェックが反転してしまう」という
  コメントは、この理由に置き換える

**Alternatives**:
- `MenuItem.Icon` に ✓ の文字を自前で入れる案 (テーマの見た目と揃わず、Fluent の枠付きの
  チェック表示と異なるため不採用)

**Files likely to change**:
- `src/OkidokeiWidget.App/PlacementMenuBuilder.cs`
- `src/OkidokeiWidget.App/App.xaml.cs`

**Tests to add or update**:
- App 層に自動テストはない (research.md #7)。実機で、本体の配置サブメニューとトレイのメニューの
  チェック表示を確認する

## Risks & Considerations

- Fluent テーマの `IsCheckable` の項目は、チェックが付いていなくても枠 (四角) が表示される。
  配置サブメニューの全項目に空の枠が並ぶ見た目になるが、本体の「位置ロック」「最前面表示」と
  同じ表示なので統一感は保たれる
- 範囲外の見た目の問題: 「配置」などサブメニューを持つ項目 (`SubmenuHeaderTemplateKey`) は
  チェック用の列を持たないため、隣の項目より文字が左に寄って見える。チェックの表示とは別の
  問題なので、本件では扱わない

## Open Questions

- なし (タスクトレイ側の修正前の再現は未確認だが、同じコード上の原因なので修正後にあわせて確認する)
