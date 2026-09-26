# Implementation Plan: 常駐デスクトップ時計ウィジェット

**Branch**: `001-clock-widget` | **Date**: 2026-09-17 (2026-09-24 更新: アンカー指定・右クリックメニュー統一、issue #26。2026-09-26 更新: ドラッグ範囲の制限、issue #39。同日更新: 自由配置のときの余白、issue #43) | **Spec**: [spec.md](./spec.md)

**Input**: Feature specification from `/specs/001-clock-widget/spec.md`

**Note**: This template is filled in by the `/speckit-plan` command; its definition describes the execution workflow.

## Summary

Windows 11 上で常駐する WPF デスクトップ時計ウィジェット。時刻/日付/曜日を表示し、フォント・
文字色・背景透過度をカスタマイズ可能、ドラッグ配置と位置ロック・最前面表示 ON/OFF、モニタごと
の表示/非表示・位置の独立管理を行う。設定は `%APPDATA%\OkidokeiWidget\settings.json` に保存し
(レジストリ不使用)、Windows 起動時にスタートアップフォルダのショートカット経由で自動起動する。
見た目・配置に関するロジックはテスト可能な `OkidokeiWidget.Core` ライブラリに切り出し、WPF 本体
(`OkidokeiWidget.App`) から利用する構成とする(詳細は research.md 参照)。

2026-09-24 の更新では、実装済みのアプリに対して以下を追加する (FR-034〜FR-038、FR-010 等の改訂)。

- モニタごとに、9 つの配置 (右上など) と余白 (狭め/広め) でウィジェットの位置を指定できる
  「アンカー指定」を追加する
- アンカー指定中は、フォントサイズや DPI が変わってウィジェットのサイズが変わっても、隅からの
  位置関係を保つ
- 本体とタスクトレイの右クリックメニューを同じ構成に揃える。トレイ側は、配置を選ぶ前に
  モニタを選ぶ階層が 1 段入る
- 位置ロック中は、メニューからの配置の変更もグレーアウトで禁止する

設計の詳細は research.md #15〜#17、data-model.md の `MonitorPlacement`、
contracts/context-menus.md を参照。

2026-09-26 の更新では、FR-009 の改訂 (issue #39) に合わせて、ドラッグで動かせる範囲を
表示中のモニタの作業領域内に限る。

- ドラッグ中の位置を物理ピクセルで計算し、作業領域内に収めてから動かす
- ウィンドウが別のモニタへ出なくなるので、`DpiChanged` による引き戻しと揺れが起きなくなる
- 設定ファイルの形式、画面、メニューは変えない (constitution v1.5.0 の小さな変更として、
  specify から implement までを 1 ブランチ・1PR で進める)

設計の詳細は research.md #18 を参照。

同じく 2026-09-26 の更新で、FR-035 の改訂と FR-039 の新設 (issue #43) に合わせて、自由配置の
ときの余白の扱いを変える。

- 自由配置中は、右クリックメニューの余白にチェックを付けない
- 自由配置中に余白を選ぶと、範囲内の縁から余白ぶん内側へ動かす。範囲内の縁がない余白は
  グレーアウトする。動かした後も自由配置のまま
- グレーアウトの判定と移動先は、Core の 1 つの関数で求めて食い違わないようにする
- 設定ファイルの形式と画面は変えない (issue #39 と同じく、小さな変更として 1 ブランチ・1PR で進める)

設計の詳細は research.md #19 を参照。

## Technical Context

**Language/Version**: C# (最新言語バージョン) / .NET 10 (LTS)、`net10.0-windows` ターゲット

**Primary Dependencies**: WPF (Microsoft.WindowsDesktop.App)、`System.Text.Json` (BCL)、
`System.Windows.Forms.Screen`(モニタ列挙)、Win32 `EnumDisplayDevices`(P/Invoke、モニタの
安定した識別子取得用)。`OkidokeiWidget.Core` / `OkidokeiWidget.App` の本体コードには外部 NuGet
パッケージを導入しない(テストプロジェクトの xUnit 関連パッケージは対象外。research.md #10 参照)

**Storage**: ローカル JSON ファイル 1 つ (`%APPDATA%\OkidokeiWidget\settings.json`)。DB は使用しない

**Testing**: xUnit による `OkidokeiWidget.Core` の単体テスト(設定の読み書き・デフォルトへの
フォールバック・モニタ識別子マッチング・アンカー指定の位置計算・既存設定ファイルとの互換性)。
UI/視覚的な確認は `quickstart.md` の手動シナリオで行い、UI 自動化フレームワークは導入しない
(research.md #7)

**Target Platform**: Windows 11 デスクトップ

**Project Type**: デスクトップアプリ(単一 WPF 実行ファイル + テスト可能なコアライブラリ)

**Performance Goals**: ログインからウィジェット表示まで 5 秒以内 (SC-001)。表示更新は 1 秒
周期で十分であり、過剰なポーリングは行わない (Core Principle II)

**Constraints**: アイドル時 CPU 使用率 1%未満・メモリ使用量 100MB 未満 (SC-006)。設定は
JSON のみで保存しレジストリを使用しない (Core Principle III)。設定ファイルが存在しない/
不正でもクラッシュせずデフォルト設定で起動する (FR-018)

**Scale/Scope**: 個人利用(1 PC・1 ユーザー)、4 ユーザーストーリー・39 の機能要件
(2026-09-24 の追加分 FR-034〜FR-038 はすべて User Story 3「配置とロック」に属する。
2026-09-26 の FR-009 の改訂と SC-008 の新設、FR-035 の改訂と FR-039 の新設も User Story 3 に属する)

## Constitution Check

*GATE: Must pass before Phase 0 research. Re-check after Phase 1 design.*

| 原則 | 判定 | 根拠 |
|---|---|---|
| I. シンプルさ優先 (YAGNI) | PASS | `docs/requirements.md` にない機能(トレイアイコン、クリックスルー、設定の多重フォーマット対応等)は導入しない。依存パッケージも BCL/WPF 標準の範囲に留める (research.md #6, #8) |
| II. 軽量な常駐動作 | PASS | 表示更新は 1 秒周期のタイマーのみ。自動起動はショートカット配置のみで起動時の重い処理を行わない |
| III. 設定は JSON・非破壊 | PASS | レジストリ不使用。設定ファイル欠損/不正時はデフォルトにフォールバックしクラッシュしない (data-model.md, contracts/settings-file.md) |
| IV. 誤操作防止 | PASS | 位置ロック中はドラッグ操作を無効化 (FR-010)。破壊的操作(終了等)は右クリックメニューの通常導線に置くのみで、誤操作しやすい配置にはしない |
| V. マルチモニタ・DPI 対応 | PASS | EDID 由来の安定したモニタ識別子で配置を保持し、再接続構成の変化にも対応 (research.md #2)。Per-Monitor V2 DPI 宣言でモニタ間 DPI 差異に対応 (research.md #5) |

**結果**: 違反なし。Complexity Tracking への記載は不要

### 再チェック (2026-09-24、アンカー指定・右クリックメニュー統一の設計後)

| 原則 | 判定 | 根拠 |
|---|---|---|
| I. シンプルさ優先 (YAGNI) | PASS | 追加する設定は spec で要求された `Anchor`・`AnchorMargin` の 2 つのみ。余白は 2 段階に留め、px 指定などの細かい設定は設けない。新しい依存パッケージは追加しない。メニューの共通化は、本体とトレイの 2 か所で実際に使うために行う (research.md #17) |
| II. 軽量な常駐動作 | PASS | 再配置はサイズ変更・DPI 変更・メニュー操作の時だけ行い、定期的な監視はしない |
| III. 設定は JSON・非破壊 | PASS | 追加フィールドがない既存の設定ファイルもそのまま読める。未知の値は既存と同じくデフォルトへフォールバックする (contracts/settings-file.md) |
| IV. 誤操作防止 | PASS | 位置ロック中はドラッグに加えて、メニューからの配置変更もグレーアウトで禁止する (FR-010) |
| V. マルチモニタ・DPI 対応 | PASS | アンカー指定はモニタごとに保持する。余白は DIP で定義してモニタの DPI で物理ピクセルに換算するため、拡大率が変わっても見た目が保たれる (research.md #15) |

**結果**: 違反なし。Complexity Tracking への記載は不要

補足: 初回チェックの原則 I の根拠にある「トレイアイコンは導入しない」は、FR-031 (2026-09-17) の
時点で見直し済み (research.md #13)。今回はさらにトレイのメニューを本体と揃える (research.md #17)

### 再チェック (2026-09-26、ドラッグ範囲の制限の設計後)

| 原則 | 判定 | 根拠 |
|---|---|---|
| I. シンプルさ優先 (YAGNI) | PASS | ドラッグでモニタを移す機能は作らない (spec.md の Clarifications)。作業領域内へ収める計算は既存の処理を共通化して使い、新しい設定・依存パッケージは追加しない |
| II. 軽量な常駐動作 | PASS | 計算はドラッグ中の `MouseMove` の時だけ行う。常駐中の処理は増えない |
| III. 設定は JSON・非破壊 | PASS | 設定ファイルの形式は変えない。保存するのは今と同じ、作業領域の左上からの相対座標 |
| IV. 誤操作防止 | PASS | ドラッグでウィジェットが作業領域の外やタスクバーの下へ出なくなり、見失う・操作できなくなることがない。位置ロック中の扱いは変えない |
| V. マルチモニタ・DPI 対応 | PASS | ドラッグ中の位置を物理ピクセルで計算し、拡大率の違うモニタの境界でも単位がずれない (research.md #18) |
| Development Workflow 7 (UI フレームワークの挙動の事前確認) | PASS | 前提にした 4 つの挙動を最小のアプリで確かめ、方法と結果を research.md #18 に記録した。未確認の前提は残っていない |

**結果**: 違反なし。Complexity Tracking への記載は不要

### 再チェック (2026-09-26、自由配置のときの余白の設計後)

| 原則 | 判定 | 根拠 |
|---|---|---|
| I. シンプルさ優先 (YAGNI) | PASS | 新しい設定・メニュー項目・依存パッケージは追加しない。向かい合う縁が両方とも範囲内になるケースは作り込まず、spec で「定めない」とした (issue #45) |
| II. 軽量な常駐動作 | PASS | 判定はメニューを開いたときと余白を選んだときだけ行う。常駐中の処理は増えない |
| III. 設定は JSON・非破壊 | PASS | 設定ファイルの形式は変えない。保存するのは今と同じ `X`/`Y` と `AnchorMargin` |
| IV. 誤操作防止 | PASS | 位置ロック中は今までどおりすべてグレーアウトする (FR-010)。範囲内の縁がない余白もグレーアウトし、選んでも何も起きない項目を残さない |
| V. マルチモニタ・DPI 対応 | PASS | 縁までの距離と余白は、アンカー指定と同じ換算で物理ピクセルにそろえて比べる (research.md #19) |
| Development Workflow 7 (UI フレームワークの挙動の事前確認) | PASS | 前提にしている挙動 3 つを research.md #19 に列挙した。これまで確かめていなかった「有効・無効が混ざったサブメニューの表示とクリック」は、`/speckit-analyze` の指摘 C1 を受けて最小のアプリで確かめ、方法と結果を記録した。未確認の前提は残っていない |

**結果**: 違反なし。Complexity Tracking への記載は不要

## Project Structure

### Documentation (this feature)

```text
specs/001-clock-widget/
├── plan.md              # This file (/speckit-plan command output)
├── research.md          # Phase 0 output (/speckit-plan command)
├── data-model.md         # Phase 1 output (/speckit-plan command)
├── quickstart.md         # Phase 1 output (/speckit-plan command)
├── contracts/
│   ├── settings-file.md  # Phase 1 output: 設定ファイルのスキーマ契約
│   └── context-menus.md  # Phase 1 output: 右クリックメニューの項目構成 (2026-09-24 追加)
└── tasks.md              # Phase 2 output (/speckit-tasks command - NOT created by /speckit-plan)
```

### Source Code (repository root)

```text
src/
├── OkidokeiWidget.Core/          # WPF に依存しない、テスト可能なロジック
│   ├── Settings/                # WidgetSettings, AppearanceSettings,
│   │                             # WindowBehaviorSettings, MonitorPlacement
│   ├── Persistence/              # 設定ファイルの読み書き・デフォルトへのフォールバック
│   └── Monitors/                  # モニタ列挙・EDID 由来の識別子解決
│
└── OkidokeiWidget.App/           # WPF 実行ファイル
    ├── App.xaml(.cs)              # エントリポイント、多重起動防止、自動起動連携
    ├── ClockWindow.xaml(.cs)       # 時計本体ウィンドウ(ドラッグ・右クリックメニュー・最前面表示)
    └── SettingsWindow.xaml(.cs)    # 詳細設定ウィンドウ

tests/
└── OkidokeiWidget.Core.Tests/    # xUnit: 設定の読み書き・モニタ識別ロジックの単体テスト
```

**Structure Decision**: Single-project 系のシンプルな構成を採用しつつ、WPF に依存しない設定・
モニタロジックのみ `OkidokeiWidget.Core` として分離した 2 プロジェクト構成とする。UI 全体を含む
単一プロジェクトにしなかった理由は、FR-018(壊れた設定ファイルでもクラッシュしない)等の
ロジックを WPF の UI テストハーネストなしに単体テストできるようにするためであり、将来の
仮説的な拡張のためではない(Core Principle I への準拠)。Web/モバイル向けのテンプレート構成
(Option 2/3)は対象外(バックエンド/フロントエンド分離やモバイルプラットフォームが存在しない
ネイティブデスクトップアプリのため)

## 既存実装に対する変更計画 (2026-09-24)

実装済みのコードに対して、どのファイルをどう変えるかの一覧。作業の順序と粒度は `/speckit-tasks`
で決める。

### OkidokeiWidget.Core

| ファイル | 変更 | 内容 |
|---|---|---|
| `Settings/AnchorPosition.cs` | 新規 | 9 つの配置を表す enum (data-model.md) |
| `Settings/AnchorMargin.cs` | 新規 | 余白 `Narrow`/`Wide` の enum |
| `Settings/MonitorPlacement.cs` | 変更 | `Anchor` (null 許容)・`AnchorMargin` を追加 |
| `Monitors/WidgetPlacementCalculator.cs` | 変更 | アンカー指定の位置計算を追加。DPI スケールを引数で受け取り、余白の DIP 値を物理ピクセルへ換算する (research.md #15)。自由配置の計算は変更しない |

### OkidokeiWidget.App

| ファイル | 変更 | 内容 |
|---|---|---|
| `PlacementMenuBuilder.cs` | 新規 | 1 つのモニタ分の「配置」サブメニュー (9 項目 + 余白 2 項目) を作る。本体とトレイの両方から使う (research.md #17) |
| `ClockWindow.xaml` | 変更 | 右クリックメニューに「配置」を追加する (中身は開くたびに `PlacementMenuBuilder` で作る) |
| `ClockWindow.xaml.cs` | 変更 | `ApplyPlacement` でアンカーと DPI スケールを計算に渡す。`SizeChanged` で再配置する。ドラッグ終了時に `Anchor` を null に戻す (FR-036)。メニューを開く時にロック状態でグレーアウトを切り替える |
| `TrayIconManager.cs` | 変更 | 右クリック時に、`App` から受け取ったメニューを表示するだけにする。「終了」だけを持つ現在のメニュー組み立ては削除する |
| `App.xaml.cs` | 変更 | トレイのメニュー (詳細設定・位置ロック・最前面表示・モニタ一覧付きの配置・終了) を組み立てる。配置の変更を該当モニタのウィンドウへ反映して保存する |

### テスト (OkidokeiWidget.Core.Tests)

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/WidgetPlacementCalculatorTests.cs` | 変更 | 9 つの配置 × 余白 × DPI スケールの位置計算、作業領域より大きいときの収め方 |
| `Persistence/SettingsRepositoryTests.cs` | 変更 | `Anchor` のない既存の設定ファイルを読むと自由配置になること、`Anchor` を含む設定の保存・読み込み |

### 変更しないもの

- 詳細設定画面 (`SettingsWindow`): アンカー指定は右クリックメニューからのみ操作する
  (spec.md の Clarifications)
- `MonitorSettingsReconciler`: 新規モニタの既定値は従来どおり自由配置 (中央寄せ) のまま
- `DisplayChangeNotifier` と issue #25 の `DpiChanged` 対応: そのまま使い、アンカー指定の
  再配置もこの経路で行う

## 既存実装に対する変更計画 (2026-09-26、issue #39)

### OkidokeiWidget.Core

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/WidgetPlacementCalculator.cs` | 変更 | 作業領域内へ収める計算を、ドラッグからも呼べる公開メソッドとして切り出す。`ToAbsolutePosition` もこれを使うようにし、結果は変えない |

### OkidokeiWidget.App

| ファイル | 変更 | 内容 |
|---|---|---|
| `WindowPositionHelper.cs` | 変更 | マウスの画面座標を物理ピクセルで取る処理 (`GetCursorPos`) を追加する |
| `ClockWindow.xaml.cs` | 変更 | ドラッグ開始時に、つかんだ位置 (マウスとウィンドウ左上の差、物理ピクセル) を覚える。`MouseMove` では `Left`/`Top` を使わず、作業領域内に収めた位置へ `WindowPositionHelper.MoveTo` で動かす。ドラッグ終了時の処理は変えない |

### テスト (OkidokeiWidget.Core.Tests)

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/WidgetPlacementCalculatorTests.cs` | 変更 | 作業領域内へ収める計算の単体テスト (内側ならそのまま、4 辺の外なら端に止まる、負の座標のモニタ、作業領域より大きいとき) |

### 変更しないもの

- 設定ファイルの形式 (`contracts/settings-file.md`) と右クリックメニュー (`contracts/context-menus.md`)
- `DpiChanged` の後回し処理 (issue #41 の修正): ドラッグ以外の経路 (拡大率の変更、モニタの取り外し) で
  引き続き必要
- ドラッグ終了時の保存とアンカー指定の解除 (FR-036)

## 既存実装に対する変更計画 (2026-09-26、issue #43)

### OkidokeiWidget.Core

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/WidgetPlacementCalculator.cs` | 変更 | 自由配置中に余白を選んだときの移動先を求める関数を追加する。範囲内の縁がなければ null を返す (research.md #19)。余白を物理ピクセルに換算する処理は、アンカー指定の計算と共通にする |

### OkidokeiWidget.App

| ファイル | 変更 | 内容 |
|---|---|---|
| `PlacementMenuBuilder.cs` | 変更 | 余白のチェックは、アンカー指定中だけ付ける。余白の項目ごとに選べるかどうかを、呼び出し側から受け取って反映する (位置ロック中は今までどおりすべて無効) |
| `ClockWindow.xaml.cs` | 変更 | 余白の項目が選べるかを返すメソッドを追加する (アンカー指定中は常に選べる、自由配置中は Core の関数で判定)。`SetAnchorMargin` は、自由配置中なら Core の関数で求めた位置を `X`/`Y` に保存してから配置し直す。移動先がなければ何もしない |
| `App.xaml.cs` | 変更 | トレイのメニューを作るときに、各モニタのウィジェットの「余白が選べるか」を `PlacementMenuBuilder` へ渡す |

### テスト (OkidokeiWidget.Core.Tests)

| ファイル | 変更 | 内容 |
|---|---|---|
| `Monitors/WidgetPlacementCalculatorTests.cs` | 変更 | 追加する関数の単体テスト (縁に接している、距離 = 余白、余白 + 1 px で null、隅で両方の縁から離れる、1 つの縁だけで他の軸は動かない、拡大率 150% での換算、負の座標のモニタ) |

### 変更しないもの

- 設定ファイルの形式 (`contracts/settings-file.md`)
- アンカー指定中の余白の動き、ドラッグ、横位置・縦位置の選び方
- 位置ロック中のグレーアウト (FR-010)

## Complexity Tracking

*本セクションに記載すべき Constitution 違反はない(Constitution Check はすべて PASS)。*
