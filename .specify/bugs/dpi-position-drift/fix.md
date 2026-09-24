# Bug Fix: 位置ロック中でも DPI 変更でウィジェットの表示位置がズレる

- **Slug**: dpi-position-drift
- **Fixed**: 2026-09-24
- **Assessment**: ./assessment.md
- **Status**: applied

## Summary

`ClockWindow` に WPF の `Window.DpiChanged` イベントを購読させ、発火時に既存の
`App.OnDisplaySettingsChanged` (モニタ情報の再取得 → 全 `ClockWindow` の再配置) を呼び直すように
した。DPI スケールのみが変わり `WM_DISPLAYCHANGE` が飛ばないケースでも、WPF/Windows がウィンドウを
自動移動させた直後に保存済みの座標から位置を計算し直して上書きするため、位置ロックが維持される。

## Changes

| File | Change | Notes |
|------|--------|-------|
| `src/OkidokeiWidget.App/ClockWindow.xaml.cs` | modified | コンストラクタに `Action onDpiChanged` を追加し、`DpiChanged` イベントで呼び出すよう配線 |
| `src/OkidokeiWidget.App/App.xaml.cs` | modified | `CreateClockWindow` から `ClockWindow` を生成する際、既存の `OnDisplaySettingsChanged` を `onDpiChanged` としてそのまま渡すよう変更 |

新規ファイルの追加や依存関係の変更は無し。アセスメントで挙げた 2 ファイルのみを変更。

## Diff Highlights

```csharp
// ClockWindow.xaml.cs (コンストラクタ)
private readonly Action _onDpiChanged;

public ClockWindow(
    WidgetSettings settings,
    ConnectedMonitor monitor,
    MonitorPlacement placement,
    Action openSettingsWindow,
    Action onWindowBehaviorChanged,
    Action onDpiChanged)
{
    ...
    _onDpiChanged = onDpiChanged;
    ...
    // DPI スケールのみの変更 (WM_DISPLAYCHANGE は飛ばない) では、既定では WPF が Windows の
    // 提案する矩形へウィンドウを自動移動させてしまい、位置ロック中でも保存済み座標からずれる。
    // DpiChanged は WPF のその自動移動が完了した後に発火するため、ここで最新のモニタ情報へ
    // 差し替えて保存済みの座標から配置し直すことでロック位置を維持する (issue #25)
    DpiChanged += (_, _) => _onDpiChanged();
}
```

```csharp
// App.xaml.cs
private void CreateClockWindow(ConnectedMonitor monitor, MonitorPlacement placement)
{
    var window = new ClockWindow(_settings, monitor, placement, OpenSettingsWindow, OnWindowBehaviorChanged, OnDisplaySettingsChanged);
    ...
}
```

`OnDisplaySettingsChanged` は既存の実装 (`RefreshConnectedMonitors(); SyncClockWindows(); SettingsRepository.Save(_settings);`) をそのまま再利用しており、`DpiChanged` 専用の新しい経路は追加していない。

## Tests Added or Updated

なし。アセスメント時点の判断どおり、`WidgetPlacementCalculator` のクランプ挙動は既存の
`tests/OkidokeiWidget.Core.Tests/Monitors/WidgetPlacementCalculatorTests.cs` で保存座標が作業領域を
超えるケースも含め検証済みで今回の変更では触っていない。`ClockWindow`/`App` は WPF の実ウィンドウに
依存し、このリポジトリには App 層の自動テストが存在しないため、`DpiChanged` の配線自体に対する
自動テストは追加していない。

## Local Verification

- Commands run: `dotnet build` → 成功 (0 エラー、0 警告)
- Commands run: `dotnet test` → 成功 (39 件全て合格、既存テストに影響なし)
- Manual checks: 実機で DPI スケールを変更する再現手順が確立できていないため、今回のセッションでは
  未実施。`/speckit-bug-test` で手動確認の範囲を検討する

## Deviations from Assessment

なし。アセスメントの Preferred Remediation (`Window.DpiChanged` イベントを使い、既存の
`OnDisplaySettingsChanged` 相当の処理を再実行する) をそのまま適用した。

## Follow-ups

- 実機で DPI スケールを変更する / 外部モニタを抜き差しする等で実際に `DpiChanged` が発火し、
  位置ロックが維持されることを手動確認する (`/speckit-bug-test` で実施)
- モニター 2 の保存済み相対座標 (X=2881) が現在の作業領域幅を超えていた件は、今回の修正で
  `RefreshConnectedMonitors`/`MonitorSettingsReconciler` が再実行されるようになったことで
  今後は自動的に追随されるはずだが、次回起動時やモニタ再接続時に実際の座標が妥当な範囲に
  収まっているかも合わせて確認する

## 訂正 (2026-09-24)

Follow-ups の 2 つ目 (モニター 2 の保存値 X=2881 が作業領域の幅を超えていた件) は、計測の誤りに
基づく記述だった。実際の作業領域の幅は 3840px で、X=2881 は範囲内に収まっている。対応は不要
(詳細は assessment.md の「訂正」を参照)。
