# Phase 1 Data Model: 常駐デスクトップ時計ウィジェット

**Feature**: `001-clock-widget` | **Date**: 2026-09-17

`spec.md` の Key Entities を、`OkidokeiWidget.Core` 内の設定モデルとして具体化する。すべて
`%APPDATA%\OkidokeiWidget\settings.json` へ 1 つの JSON ドキュメントとしてシリアライズされる
(詳細な JSON 構造は [`contracts/settings-file.md`](./contracts/settings-file.md) を参照)。

## WidgetSettings (ルート集約)

設定ファイル全体を表すルートオブジェクト。

| フィールド | 型 | 説明 |
|---|---|---|
| `Appearance` | `AppearanceSettings` | 表示設定 |
| `WindowBehavior` | `WindowBehaviorSettings` | ウィンドウ挙動設定 |
| `Monitors` | `Dictionary<string, MonitorPlacement>` | モニタ識別子をキーとしたモニタ別配置 |
| `AutoStartEnabled` | `bool` | 自動起動 ON/OFF (FR-019) |

- 設定ファイルの保存場所自体(`ApplicationSettings` の一部)は固定パスのため、モデルの
  フィールドとしては持たない(research.md #4)
- spec.md の Key Entities における「アプリケーション設定 (Application Settings)」は、本モデル
  では独立したクラスを持たない。自動起動設定は `WidgetSettings.AutoStartEnabled` に、保存場所は
  前述のとおり固定パスとして扱う

## AppearanceSettings

表示内容・見た目に関する、ウィジェット全体で共有される設定 (FR-003〜FR-008, FR-024〜FR-030)。
`DateDayOfWeekPosition` の型 `RelativePosition` は本アプリ専用の enum で、
`src/OkidokeiWidget.Core/Settings/RelativePosition.cs` に定義する。

| フィールド | 型 | 説明 | バリデーション |
|---|---|---|---|
| `ShowDate` | `bool` | 日付表示 ON/OFF | - |
| `ShowDayOfWeek` | `bool` | 曜日表示 ON/OFF | - |
| `ShowSeconds` | `bool` | 秒表示 ON/OFF (FR-024) | - |
| `FontFamily` | `string` | フォント名(時刻・日付/曜日で共有) | 空文字/未インストールフォントの場合はシステムデフォルトにフォールバック |
| `TimeFontSize` | `double` | 時刻表示のフォントサイズ (pt) (FR-025) | `> 0`。範囲外・不正値はデフォルト値にフォールバック |
| `TimeFontColor` | `string` | 時刻表示の文字色 (`#AARRGGBB` 形式の 16 進文字列) (FR-025) | パース不可の場合はデフォルト色にフォールバック |
| `DateFontSize` | `double` | 日付/曜日表示のフォントサイズ (pt) (FR-025) | `> 0`。範囲外・不正値はデフォルト値にフォールバック |
| `DateFontColor` | `string` | 日付/曜日表示の文字色 (`#AARRGGBB` 形式の 16 進文字列) (FR-025) | パース不可の場合はデフォルト色にフォールバック |
| `DateSeparator` | `string` | 日付の区切り文字 (FR-026) | `/` または `-` のいずれか。それ以外はデフォルト (`/`) にフォールバック |
| `DayOfWeekFormat` | `string` (enum 名) | 曜日の表示形式 (FR-027): `ShortKanjiParen`(例:「(月)」)・`LongKanji`(例:「月曜日」)・`ShortEnglish`(例:「Mon.」)・`LongEnglish`(例:「Monday」) | 未知の値はデフォルト (`LongKanji`) にフォールバック |
| `DateDayOfWeekPosition` | `string` (`RelativePosition` enum 名) | 時刻表示に対する日付/曜日表示の相対位置 (FR-029): `Above`(上)・`Below`(下)・`Left`(左)・`Right`(右) | 未知の値は JSON 全体のパース失敗として扱われ、`WidgetSettings` 全体がデフォルトへフォールバックする(既存の `DayOfWeekFormat` と同じ扱い。個別フィールド用のフォールバック処理は設けない) |
| `BackgroundColor` | `string` | 背景色 (`#AARRGGBB` 形式の 16 進文字列、アルファ成分は無視し `BackgroundOpacity` から都度算出する) (FR-030) | パース不可の場合はデフォルト色 (黒) にフォールバック |
| `BackgroundOpacity` | `double` | 背景透過度 (0=不透明 〜 100=完全透明) | `0〜100` の範囲にクランプ (FR-008) |

- 日付表示・曜日表示がともに ON の場合、両者は常に同一行に表示され改行されない (FR-028、選択式にはしない)

## WindowBehaviorSettings

ウィンドウの振る舞いに関する設定 (FR-010, FR-011)。

| フィールド | 型 | 説明 |
|---|---|---|
| `TopMost` | `bool` | 常に最前面に表示するか |
| `PositionLocked` | `bool` | 位置ロック中かどうか(ロック中はドラッグ移動を無効化) |

## MonitorPlacement

モニタごとに独立して保持される表示設定 (FR-014, FR-015)。`WidgetSettings.Monitors` の値として、
モニタ識別子をキーに保持される。

| フィールド | 型 | 説明 |
|---|---|---|
| `IsVisible` | `bool` | このモニタにウィジェットを表示するか |
| `X` | `int` | モニタの作業領域左上を基準としたウィンドウ左端の位置 |
| `Y` | `int` | モニタの作業領域左上を基準としたウィンドウ上端の位置 |

- **キー(モニタ識別子)**: `EnumDisplayDevices` から取得する EDID 由来のデバイス ID
  (research.md #2)。実際の書式は `EDD_GET_DEVICE_INTERFACE_NAME` 付きで取得する
  デバイスインターフェース名 (`\\?\DISPLAY#<モデル ID>#<インスタンス ID>#{GUID}`) で、
  実例は contracts/settings-file.md を参照。現在接続されていないモニタのエントリも削除せず保持する
  (Edge Case: モニタ取り外し時に設定を保持し、再接続時のために備える)
- 未知のモニタ(現在の接続構成に一致するエントリが存在しない新規モニタ)が検出された場合は、
  そのモニタに対してデフォルト値(`IsVisible = true`, 作業領域中央寄せの位置)のエントリを
  新規作成する

## タスクトレイアイコン・アプリケーションアイコン (FR-031〜FR-033)

タスクトレイの常駐アイコン、実行ファイル・ウィンドウのアイコンは、いずれもユーザーが変更可能な
設定値を持たない(ON/OFF の切替や永続化対象のフィールドはない)ため、`WidgetSettings` には
対応するフィールドを追加しない。実装は `src/OkidokeiWidget.App/app.ico`(静的なアセット)と、
それを利用するコード側の振る舞いのみで完結する(research.md #13, #14)。

## 状態遷移・関係性

- `WidgetSettings` は 1 つの JSON ファイルに対応する単一の集約であり、内部エンティティ間に
  独立したライフサイクルはない(すべて設定ファイルの読み込み/保存と一蓋に扱われる)
- 読み込み時に JSON が存在しない、またはパースに失敗した場合は、`WidgetSettings` 全体を
  デフォルト値で構築し、正常に起動する(FR-018)。既存の壊れたファイルは上書き保存時に
  正しい内容で置き換えられる(自動修復は行わず、次回保存時に正しい状態が書き込まれる)
- `Monitors` 内の各エントリは、対応する物理モニタが接続されている間のみ実際の表示に影響する。
  接続されていないモニタのエントリは保持されるだけで、表示ロジックからは無視される
