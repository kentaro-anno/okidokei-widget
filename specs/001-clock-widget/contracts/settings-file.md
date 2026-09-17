# Contract: 設定ファイル (`settings.json`)

**Feature**: `001-clock-widget`

本アプリの唯一の永続化された外部インターフェースである設定ファイルの構造を定義する。
アプリの再起動をまたいで状態を復元する際 (FR-017) の契約であり、[`data-model.md`](../data-model.md)
のモデルに対応する。

## 保存場所

`%APPDATA%\OkidokeiWidget\settings.json`

(`ClockWidget` や `DesktopClockWidget` は一般的すぎて他アプリと衝突する可能性があるため、
「置き時計」に由来する造語 `Okidokei` を使ったアプリ名 `OkidokeiWidget` を採用した。個人名を
含むベンダーフォルダ(`KentaroAnno\ClockWidget` 案)は、実名がアプリ内部のパスに埋め込まれて
しまうため不採用)

## スキーマ (例)

```json
{
  "Appearance": {
    "ShowDate": true,
    "ShowDayOfWeek": true,
    "ShowSeconds": true,
    "FontFamily": "Yu Gothic UI",
    "TimeFontSize": 24,
    "TimeFontColor": "#FFFFFFFF",
    "DateFontSize": 14,
    "DateFontColor": "#FFFFFFFF",
    "DateSeparator": "/",
    "DayOfWeekFormat": "LongKanji",
    "DateDayOfWeekPosition": "Below",
    "BackgroundColor": "#FF000000",
    "BackgroundOpacity": 30
  },
  "WindowBehavior": {
    "TopMost": true,
    "PositionLocked": false
  },
  "Monitors": {
    "\\\\?\\DISPLAY#GSM123B#5\u00263b7d6ecd\u00260\u0026UID4352#{e6f07b5f-ee97-4a90-b076-33f57bf4eaa7}": {
      "IsVisible": true,
      "X": 1600,
      "Y": 20
    }
  },
  "AutoStartEnabled": true
}
```

## スキーマに関する補足

Phase 7 (T038) で、実際に書き出された `settings.json` と上記の例を突き合わせて確認した内容:

- `Monitors` のキーは、`EnumDisplayDevices` に `EDD_GET_DEVICE_INTERFACE_NAME` を指定して
  取得するデバイスインターフェース名 (`\\?\DISPLAY#<EDID 由来のモデル ID>#<インスタンス ID>#{GUID}`)
  である (research.md #2、`MonitorIdentifier`)
- キーに含まれる `\` は JSON の文字列エスケープにより `\\` として、`&` は `System.Text.Json` の
  既定のエンコーダにより `\u0026` として書き出される
- `TimeFontSize`・`DateFontSize`・`BackgroundOpacity` は `double` だが、値が整数のときは
  `24.0` ではなく `24` として書き出される
- 上記以外のキー名・階層構造・型は、実ファイルと本契約とで一致している

## 読み込み契約

- ファイルが **存在しない** 場合: 全フィールドをデフォルト値としたオブジェクトを生成し、
  そのまま起動を継続する (FR-018)。この時点ではファイルへの書き込みは行わない
- ファイルが **存在するがパース不可**(不正な JSON、必須フィールドの型不一致等)な場合:
  ファイル全体を無視し、全フィールドをデフォルト値として起動する(部分的な復旧は行わない)。
  アプリはクラッシュしてはならない。この場合、デフォルト設定にフォールバックしたことを
  ユーザーに通知しなければならない(FR-018。通知の具体的な UI 表現は実装時に決めてよい)。
  ファイルが単に存在しない場合(初回起動等)は通知しない
- `Monitors` に含まれるキーのうち、現在接続されているモニタの識別子と一致しないものは
  読み込み時にそのまま保持し(削除しない)、表示ロジックの対象からのみ除外する
- `Monitors` に、現在接続されているが対応するキーが存在しないモニタがある場合、
  デフォルト値でエントリを補完する

## 書き込み契約

- 設定変更(見た目・配置・モニタ設定・自動起動 ON/OFF)が確定するたびに、`WidgetSettings`
  全体を都度ファイルへ上書き保存する
- 書き込みは、まず一時ファイルへ書き出してから置き換える等、書き込み途中のクラッシュで
  ファイルが破損しにくい方法で行う
- `BackgroundOpacity` は書き込み前に `0〜100` の範囲へクランプする
- 書き込み先ディレクトリへの権限不足等により書き込みに失敗した場合でも、アプリはクラッシュ
  せず動作を継続しなければならない(FR-022)。この場合、その変更内容はそのセッション中は
  永続化されない(次回、書き込みに成功した時点で最新の状態が保存される)

## 後方互換性

- 現バージョンでは単一スキーマのみを対象とし、バージョン番号フィールドは持たない
  (YAGNI: 将来のマイグレーション要件は現時点で存在しない)
- 未知の追加フィールドがファイル内に存在してもエラーにはせず無視する
  (デシリアライザの既定動作に従う)
