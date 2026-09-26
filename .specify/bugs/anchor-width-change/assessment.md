# Bug Assessment: 表示文字列の変化でウィジェット幅が変わると、右上アンカーの位置がずれる

- **Slug**: anchor-width-change
- **Created**: 2026-09-25
- **Source**: https://github.com/kentaro-anno/desktop-clock-widget/issues/36
  - Host: `github.com` (allowlisted)
  - issue は本セッションで Claude が人間の報告をもとに起票したもので、取得した内容に指示的な記述はない
- **Verdict**: valid
- **Severity**: medium

## Report (verbatim or summarized)

issue #36「表示文字列の変化でウィジェット幅が変わると、右上アンカーの位置がずれる」の要約:

- 位置ロック中 (右上アンカー、余白: 狭い) なのに、両モニタでウィジェットの位置がずれた
- 右端の隙間が設定より広くなっていた。上端の隙間は変わっていなかった
- 2026-09-25 01:27 頃に気づいた。ずれた瞬間は見ていない
- 人間の仮説: フォントが等幅ではないため、時刻によって幅が変わることが原因ではないか

報告時の `settings.json` (該当部分):

- 両モニタとも `Anchor: TopRight` / `AnchorMargin: Narrow`
- `FontFamily: Franklin Gothic Book`、`TimeFontSize: 61`、`DateFontSize: 32`
- `ShowDate: true` / `ShowDayOfWeek: true` / `DateDayOfWeekPosition: Left` / `DayOfWeekFormat: ShortEnglish`

## Symptom

アンカー指定 (右寄せ) のウィジェットの幅が縮むと、左端の位置が変わらないまま幅だけが縮み、右端の
隙間が縮んだ分だけ広がる。期待される動作は、幅が変わっても右端からの余白が保たれること (SC-007)。

## Reproduction

1. 右上アンカー (余白: 狭い) でウィジェットを表示する
2. 表示文字列が変わってウィジェットの幅が変わるのを待つ
   - 報告時は日付が 9/24 (Thu) → 9/25 (Fri) に変わった直後で、これがトリガーと推定される
   - 詳細設定でフォントサイズを変える、日付表示を ON/OFF する等でも同じ経路を通る
3. 右端の隙間が、縮んだ幅の分だけ広がる (幅が広がった場合は、逆に右端へはみ出す方向にずれる)

## Suspected Code Paths

- `src/OkidokeiWidget.App/ClockWindow.xaml.cs:54` — `SizeChanged` で `ApplyPlacement()` を同期的に呼んでいる
- `src/OkidokeiWidget.App/ClockWindow.xaml.cs:85-93` (`ApplyPlacement`) — ウィジェット幅を `WindowPositionHelper.TryGetBounds` (`GetWindowRect`) から取得している
- `src/OkidokeiWidget.Core/Monitors/WidgetPlacementCalculator.cs:135` — 右寄せの X 座標を「作業領域の右端 − ウィジェット幅 − 余白」で計算しているため、渡された幅が古いとずれる
- `src/OkidokeiWidget.App/ClockWindow.xaml:11` — `SizeToContent="WidthAndHeight"` のため、表示文字列によってウィンドウ幅が変わる

## Root Cause Hypothesis

WPF の `SizeToContent` ウィンドウでは、レイアウトのアレンジ完了時に `SizeChanged` が発火し、
そのあとの `LayoutUpdated` で `HwndSource` が Win32 ウィンドウを新しいサイズへリサイズする。
そのため `SizeChanged` のハンドラ内で `GetWindowRect` を呼ぶと、まだ変化前の幅が返る。
`ApplyPlacement` はその古い幅で右寄せの X 座標を計算するので、幅が縮んだときは縮んだ分だけ
左に置かれたままになる。次に幅が変わるまでこの状態が続く。

確度: **high**。スクラッチパッドに最小の WPF アプリ (本アプリと同じ `net10.0-windows`、
`SizeToContent="WidthAndHeight"`、`Franklin Gothic Book` 61pt) を作り、テキストを差し替えて実測した:

| テキスト | `SizeChanged` 内の `e.NewSize` × DPI | `SizeChanged` 内の `GetWindowRect` | `Dispatcher.BeginInvoke` 後の `GetWindowRect` |
| --- | --- | --- | --- |
| `Thu 2026-09-24 23:59` (初回) | 615 | 1440 (WPF の初期サイズ) | 616 |
| `Fri 2026-09-25 00:00` | 585 | 616 (変化前の幅) | 585 |
| `Fri 2026-09-25 01:11` | (`SizeChanged` 発火せず) | - | - |
| `Wed 2026-09-30 08:48` | 634 | 585 (変化前の幅) | 634 |

- `SizeChanged` 内の `GetWindowRect` は毎回、変化前の幅を返している
- 曜日 Thu → Fri で幅が 31 px 縮んでおり、報告時のトリガーと整合する
- `00:00` → `01:11` では幅が変わらなかったので、このフォントの数字は等幅と考えられる
  - したがって、人間の仮説「時刻によって幅が変わる」は数字については当たらない
  - プロポーショナルな曜日の文字列で幅が変わる、という点では仮説どおり
- 起動時は `ContentRendered` で配置し直しているため正しい位置になり、この問題は表に出ない

## Proposed Remediation

**Preferred**: `SizeChanged` のハンドラで `ApplyPlacement()` を直接呼ばず、
`Dispatcher.BeginInvoke` で UI スレッドのキューの後ろに積み、Win32 ウィンドウのリサイズが
終わってから配置し直す。
- 時間で待つのではなく順番待ちで、リサイズを含む今の処理が終わった後に実行される
- 優先度は `DispatcherPriority.Loaded` (描画の次、ユーザー入力の処理より先) とする
- 実測で、この優先度の `BeginInvoke` 後の `GetWindowRect` は新しい幅を返すことを確認済み
変更は `ClockWindow.xaml.cs` の 1 か所で済み、配置計算 (`WidgetPlacementCalculator`) や
物理ピクセルで扱う方針 (issue #10) には手を入れない。

**Alternatives**:

- `e.NewSize` (DIP) に DPI スケールを掛けて幅を求め、`ApplyPlacement` に渡す
  - 実測で 615 と 616 のように丸め誤差で 1 px ずれることがあり、`GetWindowRect` と一致しない
  - 配置の経路が 2 つ (実サイズ / 計算サイズ) になるので採らない
- `LayoutUpdated` で配置し直す
  - レイアウトのたびに毎回呼ばれ、`HwndSource` のハンドラとの呼び出し順にも依存するので採らない

**Files likely to change**:

- `src/OkidokeiWidget.App/ClockWindow.xaml.cs`

**Tests to add or update**:

- 既存のテストプロジェクトは `OkidokeiWidget.Core` の純粋なロジックが対象で、WPF のウィンドウや
  イベント順序を扱う自動テストはない。本件の自動テストを追加するのは難しい
  - 計算側 (`WidgetPlacementCalculator`) は正しい幅を渡せば正しい位置を返しており、既存のテストで足りる
- 検証は実機の手動確認で行う (`/speckit-bug-test`)
  - 右上アンカーのまま、詳細設定で時刻のフォントサイズを変える、日付表示を ON/OFF する等で
    幅を変え、右端の余白が保たれることを確認する
  - 下寄せアンカーで高さを変えた場合も同じ経路なので、あわせて確認する

## Risks & Considerations

- 下寄せ・中央寄せのアンカーも同じ経路で、同じように 1 回分古いサイズで配置されていたはず
  - 修正でこれらもまとめて直る
- 自由配置 (`Anchor: null`) は左上の座標だけで決まるため、本件の影響を受けない
- `BeginInvoke` で後回しにするため、幅が変わってから配置し直すまでの一瞬、古い位置で描画される
  可能性がある
  - 1 分に 1 回以下の変化であり、見た目上の問題にはならないと考える
- SC-007 は quickstart の手順でフォントサイズ変更時の確認を求めていたが、ずれは変化量の分だけで、
  スライダー操作の最後の 1 段分だけなら目立たないため、見逃されていたと考えられる

## Open Questions

- なし
