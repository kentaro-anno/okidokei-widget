# Bug Assessment: 位置ロック中でも DPI 変更でウィジェットの表示位置がズレる

- **Slug**: dpi-position-drift
- **Created**: 2026-09-24
- **Source**: https://github.com/kentaro-anno/desktop-clock-widget/issues/25 (host: github.com, allowlisted, fetched via `gh issue view`)
- **Verdict**: valid
- **Severity**: medium

## Report (要約)

issue #25 より:

- モニター位置ロック (`WindowBehavior.PositionLocked = true`) を有効にしていても、しばらく使っていると
  ウィジェットの表示位置が保存済みの座標から微妙にズレる。明確な操作のきっかけは不明。ユーザーの
  体感では「画面ロックしてしばらく放置した後」に気づくことが多い
- 実測 (2026-09-24、モニター 1: DELF16C、モニター 2: SNYAE04):
  - モニター 1: 保存値 X=1480,Y=9 → 計算結果 (1480,9) → 実際の表示位置 (1480,9) で完全一致
  - モニター 2: 保存値 X=2881,Y=20。現在の作業領域幅 (1707px) を超えており、クランプにより
    本来 (-2556,-1051) になるはずが、実際は (-2560,-1062) と **X 4px、Y 11px ズレていた**

## Symptom

位置ロックを有効にしているウィンドウが、ユーザーの操作を介さずに保存済み座標からズレる。特に
DPI スケールが 100% でないモニタ (実測ではモニター 2) で発生し、ズレ幅は数 px 〜十数 px 程度。

## Reproduction

1. マルチモニタ環境 (うち 1 台は DPI スケールが 100% でない) でウィジェットを起動し、位置ロックを有効にする
2. [NEEDS CLARIFICATION: 直接の引き金となる操作は未確定。スリープ復帰・外部モニタの抜き差し・RDP
   セッションの接続/切断など、DPI の再ネゴシエーションが起きるタイミングで発生している可能性]
3. 再度ウィジェットの位置を確認すると、保存済みの座標から数 px 〜十数 px ズレている

## Suspected Code Paths

- `src/OkidokeiWidget.App/DisplayChangeNotifier.cs:13,34` — `WM_DISPLAYCHANGE` (`0x007E`) のみを
  購読しており、DPI スケールのみの変更では発火しない
- `src/OkidokeiWidget.App/App.xaml.cs:79-84` (`OnDisplaySettingsChanged`) — `DisplaySettingsChanged`
  イベントでのみ `RefreshConnectedMonitors` → `SyncClockWindows` を実行しており、この経路以外で
  モニタの作業領域が変わった場合には追随しない
- `src/OkidokeiWidget.App/ClockWindow.xaml.cs:73-80` (`ApplyPlacement`) — 保存済みの相対座標から
  絶対座標を再計算して `WindowPositionHelper.MoveTo` で配置し直す、位置の「正」となる処理。
  `WM_DPICHANGED` 発生時にはこれが一切呼ばれない
- `src/OkidokeiWidget.App/app.manifest:22` — `PerMonitorV2` DPI awareness を宣言しているため、
  各 `ClockWindow` は自身に届く `WM_DPICHANGED` を実際に受信する対象になっている
- `src/OkidokeiWidget.Core/Monitors/WidgetPlacementCalculator.cs` — クランプ計算自体は
  `WidgetPlacementCalculatorTests.cs` で作業領域より保存座標が大きいケースも含め検証済みであり、
  ここにバグは無い (原因はこの計算が「呼ばれないこと」「呼ばれる前に Windows 側が先に動かして
  しまうこと」の 2 点)

## Root Cause Hypothesis

**確信度: 中〜高**

1. `WM_DISPLAYCHANGE` は解像度・色深度の変更時にのみ送出され、DPI スケールだけが変わるケースでは
   送出されない。そのためモニタの作業領域 (≒ DPI 設定) が変化しても `App` 側の再配置ロジック
   (`RefreshConnectedMonitors` → `SyncClockWindows` → `ApplyPlacement`) が一切トリガーされない
2. `PerMonitorV2` 宣言により各 `ClockWindow` は `WM_DPICHANGED` を直接受け取るが、このメッセージを
   アプリ側でハンドルしていない。未処理の場合、WPF (`HwndSource`) は既定で Windows が提案する
   矩形へウィンドウを自動的に移動・リサイズする。この自動移動は保存済みの相対座標を経由しない
   ため、`ApplyPlacement` が計算する「あるべき位置」と一致する保証がない
3. モニター 2 の保存済み相対座標 (X=2881) が現在の作業領域幅 (1707px) を超えていた事実から、
   このモニタの作業領域は過去のどこかで実際に縮小しており (1 と 2 の一因)、かつ
   `MonitorSettingsReconciler` による再保存も発生していない (再配置ロジックが呼ばれていない
   証拠でもある)
4. 実測でのズレ (4px, 11px) は「本来のクランプ計算結果」と「実際の表示位置」の差であり、
   これは 2 の「Windows 独自の自動移動」が着地させた位置とみて矛盾がない

## Proposed Remediation

**Preferred**: `ClockWindow` で WPF の `Window.DpiChanged` イベント (.NET Framework 4.6.2 以降で
per-monitor DPI 対応のために追加された、公式にサポートされたイベント。生の `WM_DPICHANGED` を
フックして既定処理を横取りするより安全) を購読し、発火時に `App` 側の
`RefreshConnectedMonitors` → `SyncClockWindows` 相当の処理 (=既存の `OnDisplaySettingsChanged` と
同じもの) を再実行する。`DpiChanged` は WPF がウィンドウを自動移動・リサイズし終えた後に発火する
ため、ハンドラ内で `ApplyPlacement` (経由の再配置) を行えば、Windows 側の自動移動を上書きして
常に保存済みの座標から計算した正しい位置へ戻せる。

併せて `RefreshConnectedMonitors` を呼ぶことで、`MonitorEnumerationService` が最新の作業領域を
再取得し、`MonitorSettingsReconciler` が古い相対座標との整合を取り直す(= issue 中の「保存値が
現在の作業領域を超えている」状態も解消される)。

実装イメージ:

- `App.xaml.cs`: 既存の `OnDisplaySettingsChanged` をそのまま `onDpiChanged` としても
  `ClockWindow` に渡せるよう、`ClockWindow` のコンストラクタへコールバック引数を追加する
  (既存の `onWindowBehaviorChanged` 等と同じパターン)
- `ClockWindow.xaml.cs`: コンストラクタで `DpiChanged += (_, _) => _onDpiChanged();` を購読する
- 複数モニタで同時に `DpiChanged` が発火した場合でも `RefreshConnectedMonitors` /
  `SyncClockWindows` は冪等なので、重複実行それ自体は安全 (パフォーマンス上も許容範囲)

**Alternatives** (optional):
- 生の `WM_DPICHANGED` を `HwndSource.AddHook` でフックし `handled = true` にして既定処理を
  完全に止め、自前で新 DPI に基づく矩形を計算して配置する方法もあるが、WPF 内部の DPI 処理
  フックとの実行順序が非公開の実装詳細に依存し壊れやすいため不採用。公式にサポートされた
  `Window.DpiChanged` イベントを使う方が保守性が高い

**Files likely to change**:
- `src/OkidokeiWidget.App/ClockWindow.xaml.cs`
- `src/OkidokeiWidget.App/App.xaml.cs`

**Tests to add or update**:
- `WidgetPlacementCalculator` 自体のクランプ挙動は既存の
  `tests/OkidokeiWidget.Core.Tests/Monitors/WidgetPlacementCalculatorTests.cs` で保存座標が
  作業領域を超えるケースを含め検証済みであり、追加不要
- `ClockWindow`/`App` は WPF の実ウィンドウに依存し、現状このリポジトリに App 層の自動テストは
  存在しない (Core 層のみ)。`DpiChanged` の配線自体は自動テストでの再現が困難なため、
  修正後は実機での手動確認 (DPI スケールを変更するか、外部モニタの抜き差し・スリープ復帰を
  試す) で検証する

## Risks & Considerations

- `DpiChanged` はモニタ間をまたいでウィンドウが移動した場合にも発火する。位置ロック中は
  ドラッグ自体ができないため、ウィンドウが別モニタへ移動すること自体は起こらない想定だが、
  念のため `_monitor` の更新は `RefreshConnectedMonitors`/`SyncClockWindows` 経由の
  `UpdateMonitor` に一本化し、`ClockWindow` 内で個別に作業領域を再計算しない
- `DpiChanged` イベントハンドラ内から `App` の非公開メンバへコールバックする設計になるため、
  既存の `onWindowBehaviorChanged` などと同様のコールバック注入パターンを踏襲し、責務の境界を
  崩さない
- 実機で DPI 変更を意図的に再現するのが難しく、修正後の検証は不完全になりやすい。
  `/speckit-bug-test` の段階で手動確認の範囲を明記する

## Open Questions

- [NEEDS CLARIFICATION: ズレが発生する直接の引き金 (スリープ復帰か、外部モニタの抜き差しか、
  それ以外か) は未確定。今回の修正はどのトリガーであっても `WM_DPICHANGED`/`WM_DISPLAYCHANGE`
  のいずれかが飛ぶ前提でカバーする設計とする]

## 訂正 (2026-09-24、公開リポジトリへの反映作業中に判明)

本文の「Report」と「Root Cause Hypothesis」の 3・4 で根拠にした実測値は、計測スクリプトの
誤りによるものだった。本文は当時の記録として残し、ここで訂正する。

- **誤りの原因**: 計測に使った PowerShell スクリプトが `SetProcessDPIAware` (システム DPI 対応)
  のままだった。拡大率 225% のモニター 2 では、Windows が座標を 1/2.25 に換算した値 (DPI 仮想化)
  を返していた
- **正しい値**: Per-Monitor V2 (`SetThreadDpiAwarenessContext(-4)`) で測り直した
  - モニター 2 の作業領域の幅は 3840px。本文の 1707px は 3840 ÷ 2.25 の仮想化された値
  - したがって保存値 X=2881 は作業領域の範囲内であり、「作業領域の幅を超えている」は誤り
  - 当時のウィンドウ位置 (-2560, -1062) を物理ピクセルに戻すと約 (-960, -1051)。保存値から
    計算した位置 (-959, -1051) とほぼ一致しており、**計測した時点ではずれは起きていなかった**
- **原因の仮説への影響**
  - 1・2 (DPI だけの変更 = `WM_DPICHANGED` に追随していない) は、コード上の事実として有効
  - 3・4 (作業領域の縮小と、Windows の自動移動によるずれの痕跡) は根拠を失う
  - 人間が観測したずれが本当にこの原因によるものかは、実測では確認できていない。確信度は
    「中〜高」から「中」に下げる
- **修正の扱い**: 表示スケールの変更時に保存済みの位置へ再配置されることは、人間が実機で確認
  済み (test.md)。コード上の欠落を埋める修正として妥当なので、修正自体は維持する
