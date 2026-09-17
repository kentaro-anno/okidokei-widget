---

description: "Task list template for feature implementation"
---

# Tasks: 常駐デスクトップ時計ウィジェット

**Input**: `/specs/001-clock-widget/` 配下の設計ドキュメント (plan.md, spec.md, research.md, data-model.md, contracts/settings-file.md, quickstart.md)

**Prerequisites**: plan.md、spec.md (必須)、research.md、data-model.md、contracts/settings-file.md、quickstart.md

**Tests**: plan.md の Testing 方針 (research.md #7) により、`OkidokeiWidget.Core` の設定読み書き・
デフォルトへのフォールバック・モニタ識別子マッチングのロジックは xUnit 単体テストで検証する。
UI・視覚的な確認は自動テスト化せず、quickstart.md の手動シナリオで行う (YAGNI: UI 自動化
フレームワークは導入しない)

**Organization**: タスクはユーザーストーリー (spec.md の P1〜P3) 単位でグループ化し、各ストーリーを
独立して実装・検証できるようにする

## Format: `[ID] [P?] [Story] Description`

- **[P]**: 並行実行可能 (別ファイル・未完了タスクへの依存なし)
- **[Story]**: 対応するユーザーストーリー (US1〜US4)
- 各タスクの説明には具体的なファイルパスを含める

## Path Conventions

plan.md の Project Structure に従う:

- `src/OkidokeiWidget.Core/` — WPF に依存しない設定・永続化・モニタロジック
- `src/OkidokeiWidget.App/` — WPF 実行ファイル
- `tests/OkidokeiWidget.Core.Tests/` — xUnit 単体テスト

---

## Phase 1: Setup (Shared Infrastructure)

**Purpose**: ソリューション・プロジェクトの初期構成

- [X] T001 リポジトリルートに `OkidokeiWidget.sln` を作成し、`src/OkidokeiWidget.Core/`、
      `src/OkidokeiWidget.App/`、`tests/OkidokeiWidget.Core.Tests/` の各プロジェクトを参照させる
      (plan.md の Project Structure に準拠)
- [X] T002 [P] `src/OkidokeiWidget.Core/OkidokeiWidget.Core.csproj` を作成する。ターゲットは
      `net10.0`、外部 NuGet パッケージ参照は追加しない (research.md #1, #10)
- [X] T003 [P] `src/OkidokeiWidget.App/OkidokeiWidget.App.csproj` を作成する。ターゲットは
      `net10.0-windows`、出力形式は WPF 実行ファイル (`UseWPF=true`)、
      `OkidokeiWidget.Core` プロジェクトを参照する。あわせて `app.manifest` で
      Per-Monitor V2 DPI 認識を宣言する (research.md #5)
- [X] T004 [P] `tests/OkidokeiWidget.Core.Tests/OkidokeiWidget.Core.Tests.csproj` を作成する。
      `OkidokeiWidget.Core` を参照し、xUnit のテストランナーパッケージ (`xunit`,
      `xunit.runner.visualstudio`, `Microsoft.NET.Test.Sdk`) を追加する前に、それぞれのライセンス
      (MIT 等) を確認し、個人利用の範囲で問題ないことを確かめる (research.md #10、
      CLAUDE.md の OSS ライセンス確認ルール)

**Checkpoint**: `dotnet build OkidokeiWidget.sln` が成功する状態

---

## Phase 2: Foundational (Blocking Prerequisites)

**Purpose**: 全ユーザーストーリーが依存する設定モデル・永続化・モニタ識別ロジック

**⚠️ CRITICAL**: このフェーズが完了するまで、いずれのユーザーストーリーの実装にも着手できない

- [X] T005 [P] `src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs` に `AppearanceSettings`
      モデルを実装する。フィールドは `ShowDate`(bool)、`ShowDayOfWeek`(bool)、
      `FontFamily`(string、data-model.md のバリデーション「空文字/未インストールフォントの場合は
      システムデフォルトにフォールバック」を満たす)、`FontSize`(double、data-model.md の
      バリデーション「`> 0`。範囲外・不正値はデフォルト値にフォールバック」)、
      `FontColor`(string、`#AARRGGBB` 形式、data-model.md の「パース不可の場合はデフォルト色に
      フォールバック」)、`BackgroundOpacity`(double、data-model.md の「`0〜100` の範囲に
      クランプ (FR-008)」)
- [X] T006 [P] `src/OkidokeiWidget.Core/Settings/WindowBehaviorSettings.cs` に
      `WindowBehaviorSettings` モデルを実装する。フィールドは `TopMost`(bool)、
      `PositionLocked`(bool)
- [X] T007 [P] `src/OkidokeiWidget.Core/Settings/MonitorPlacement.cs` に `MonitorPlacement`
      モデルを実装する。フィールドは `IsVisible`(bool)、`X`(int)、`Y`(int)
- [X] T008 `src/OkidokeiWidget.Core/Settings/WidgetSettings.cs` にルート集約 `WidgetSettings`
      を実装する。フィールドは `Appearance`(AppearanceSettings)、
      `WindowBehavior`(WindowBehaviorSettings)、
      `Monitors`(`Dictionary<string, MonitorPlacement>`、モニタ識別子をキーとする)、
      `AutoStartEnabled`(bool)。全フィールドのデフォルト値を持つコンストラクタ/ファクトリも
      用意する (T005, T006, T007 に依存)
- [X] T009 [P] `src/OkidokeiWidget.Core/Monitors/MonitorIdentifier.cs` に、Win32
      `EnumDisplayDevices` を P/Invoke して EDID 由来の安定したモニタデバイス ID を取得する
      ロジックを実装する (research.md #2)
- [X] T010 `src/OkidokeiWidget.Core/Monitors/MonitorEnumerationService.cs` に、
      `System.Windows.Forms.Screen` から取得したモニタの作業領域情報と、T009 の
      `MonitorIdentifier` による EDID 由来 ID を組み合わせて、接続中モニタの一覧
      (識別子・作業領域・プライマリかどうか) を返すサービスを実装する (T009 に依存)
- [X] T011 `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs` に設定ファイルの
      読み書きを実装する。`Load()` はファイルが存在しない場合、または JSON のパースに失敗
      した場合 (不正な JSON・必須フィールドの型不一致等) に、部分的な復旧を行わず
      `WidgetSettings` 全体をデフォルト値で構築して返す (FR-018、
      contracts/settings-file.md の読み込み契約)。`Save()` は一時ファイルへ書き出してから
      置き換える方式で `%APPDATA%\OkidokeiWidget\settings.json` へ書き込み、書き込み前に
      `BackgroundOpacity` を `0〜100` の範囲へクランプする (contracts/settings-file.md の
      書き込み契約) (T008 に依存)
- [X] T012 `src/OkidokeiWidget.Core/Persistence/MonitorSettingsReconciler.cs` に、読み込んだ
      `WidgetSettings.Monitors` と T010 の現在接続中モニタ一覧を突き合わせる処理を実装する。
      現在接続されていないモニタのキーはそのまま保持し (削除しない)、対応するキーが存在しない
      新規接続モニタにはデフォルト値 (`IsVisible = true`、作業領域中央寄せの位置) のエントリを
      追加する (data-model.md の状態遷移・関係性、Edge Case: モニタ取り外し時の設定保持)
      (T008, T010 に依存)
- [X] T013 `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs` に
      `SettingsRepository` の単体テストを実装する: (1) ファイルが存在しない場合に
      デフォルト値の `WidgetSettings` を返すこと、(2) 不正な JSON の場合も同様にデフォルト値
      へフォールバックし例外を投げないこと (FR-018)、(3) `Save()` が `BackgroundOpacity` を
      `0〜100` にクランプして書き込むこと、を検証する (T011 に依存)
- [X] T014 [P] `tests/OkidokeiWidget.Core.Tests/Persistence/MonitorSettingsReconcilerTests.cs`
      に `MonitorSettingsReconciler` の単体テストを実装する: 現在接続されていないモニタの
      エントリが削除されずに保持されること、新規接続モニタにデフォルト値のエントリが
      補完されることを検証する (T012 に依存)

**Checkpoint**: `dotnet test tests/OkidokeiWidget.Core.Tests` が全件成功する状態。ここから
各ユーザーストーリーの実装に着手できる

---

## Phase 3: User Story 1 - デスクトップに時計を常時表示する (Priority: P1) 🎯 MVP

**Goal**: ログイン後、操作なしにデスクトップへ時計ウィジェットが表示され、時刻が 1 秒単位で
更新され続ける

**Independent Test**: アプリをインストールし PC を再起動するだけで、ログイン後にデスクトップに
時計が表示され、時刻が 1 秒単位で正しく更新されることを確認できる

### Implementation for User Story 1

- [X] T015 [P] [US1] `src/OkidokeiWidget.App/App.xaml.cs` の `OnStartup` に、名前付き Mutex
      による多重起動チェックを実装する。2 つ目のプロセスは UI を作らずそのまま終了する
      (research.md #6、Edge Case: 二重起動時は何もしない)
- [X] T016 [US1] `src/OkidokeiWidget.App/App.xaml.cs` に起動処理を実装する:
      `SettingsRepository.Load()` で設定を読み込み、`MonitorSettingsReconciler` で現在の
      モニタ構成と突き合わせたうえで、`IsVisible = true` の各モニタについて `ClockWindow` を
      1 つずつ生成する (T015、Foundational T011/T012 に依存)
- [X] T017 [P] [US1] `src/OkidokeiWidget.App/ClockWindow.xaml` にレイアウトを実装する:
      枠なし・内容にサイズが合うウィンドウとし、時刻・日付・曜日を表示する `TextBlock` を配置する
- [X] T018 [US1] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` に 1 秒周期の `DispatcherTimer`
      による時刻更新ループを実装する (FR-002)。`AppearanceSettings.ShowDate` /
      `ShowDayOfWeek` に応じて日付・曜日表示の有無を切り替える (FR-003, FR-004) (T017 に依存)
- [X] T019 [US1] 起動時、各 `ClockWindow` をそのモニタに対応する `MonitorPlacement.X` /
      `Y` の位置に (エントリがなければ作業領域中央寄せの位置に) 配置する (T016, T018 に依存)
- [X] T020 [US1] `src/OkidokeiWidget.Core/Persistence/AutoStartManager.cs` に、
      `shell:startup` フォルダへのショートカット (`.lnk`) の作成・削除で自動起動の ON/OFF を
      切り替える処理を実装する (レジストリの Run キーは使用しない)。`App.xaml.cs` の起動処理
      から `AutoStartEnabled` の値に応じて呼び出す (FR-001, FR-019、research.md #3)
- [X] T021 [P] [US1] `tests/OkidokeiWidget.Core.Tests/Persistence/AutoStartManagerTests.cs`
      に `AutoStartManager` の単体テストを実装する: 有効化時にスタートアップフォルダへ
      ショートカットが作成され、無効化時に削除されることを検証する (T020 に依存)

**Checkpoint**: この時点で User Story 1 は単独で完全に機能し、テスト可能な状態

---

## Phase 4: User Story 2 - 見た目を自分好みにカスタマイズする (Priority: P2)

**Goal**: 詳細設定画面でフォント・サイズ・色・背景透過度を変更すると、即座にウィジェットへ
反映され、再起動後も保持される

**Independent Test**: 詳細設定画面でフォント・サイズ・色・透過度を変更し、即座にウィジェットの
見た目に反映されることを確認できる

### Implementation for User Story 2

- [X] T022 [P] [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml` にレイアウトを実装する:
      フォント種類選択 (インストール済みフォント一覧)、フォントサイズ入力、文字色選択、
      背景透過度スライダー (0〜100)、日付表示・曜日表示のチェックボックスを配置する
- [X] T023 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml.cs` に、各コントロールの変更を
      共有の `AppearanceSettings` インスタンスへ反映し、表示中のすべての `ClockWindow` へ
      即座に反映するロジックを実装する (FR-005〜FR-008、spec.md の Acceptance Scenario
      US2-1, US2-2) (T018, T022 に依存)
- [X] T024 [US2] `SettingsWindow` での変更確定のたびに `SettingsRepository.Save()` を呼び出し、
      `WidgetSettings` 全体を上書き保存する (contracts/settings-file.md の書き込み契約、
      spec.md の Acceptance Scenario US2-3: 再起動後も見た目が保持される) (T023、
      Foundational T011 に依存)
- [X] T025 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の右クリック `ContextMenu` に
      「詳細設定」項目を追加し、`SettingsWindow` を開けるようにする (T022 に依存)
- [X] T026 [P] [US2] `FontFamily` が空文字、またはシステムに未インストールのフォント名を
      指す場合に、システムデフォルトフォントへフォールバックする処理を、`ClockWindow` が
      フォントを適用する箇所に実装する (data-model.md の `FontFamily` バリデーション)
      (T018 に依存)

**Checkpoint**: この時点で User Story 1 と 2 がともに独立して動作する状態

---

## Phase 5: User Story 3 - 配置を自由に決め、誤操作から保護する (Priority: P2)

**Goal**: ウィジェットをドラッグして配置でき、ロック後は位置がずれず、最前面表示を右クリック
メニューから切り替えられる

**Independent Test**: ウィジェットをドラッグして移動できること、ロック後はドラッグしても位置が
変わらないこと、右クリックメニューから最前面表示を ON/OFF できることを個別に確認できる

### Implementation for User Story 3

- [X] T027 [US3] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` にマウスドラッグによる移動を
      実装する (`PreviewMouseLeftButtonDown` / `MouseMove` でウィンドウ位置を更新する) (FR-009)
      (T018 に依存)
- [X] T028 [US3] `WindowBehaviorSettings.PositionLocked` が `true` の間はドラッグ処理を
      無効化する (FR-010、spec.md の Acceptance Scenario US3-2) (T027 に依存)
- [X] T029 [P] [US3] `src/OkidokeiWidget.App/ClockWindow.xaml(.cs)` の右クリック
      `ContextMenu` に「位置ロック」「最前面表示」の切替項目と「終了」項目を追加し、
      `WindowBehaviorSettings` と連動させる (FR-011, FR-012、research.md #8: トレイアイコンを
      追加せず本体への右クリックメニューとして実装する)。「終了」項目の選択時は
      `Application.Current.Shutdown()` 等でアプリケーションを終了する (FR-023)
- [X] T030 [US3] ドラッグ終了時に、そのウィンドウが表示されているモニタに対応する
      `MonitorPlacement.X` / `Y` を更新し、`SettingsRepository.Save()` で永続化する (FR-015)
      (T027、Foundational T011 に依存)
- [X] T031 [US3] `ClockWindow.Topmost` を `WindowBehaviorSettings.TopMost` にバインドし、
      右クリックメニューからの切替時に `SettingsRepository.Save()` で永続化する (FR-011)。
      同様に「位置ロック」切替時も `WindowBehaviorSettings.PositionLocked` を
      `SettingsRepository.Save()` で永続化する (FR-010、spec.md の Acceptance Scenario
      US3-5: 再起動後も位置ロック・最前面表示の設定が保持される) (T029 に依存)

**Checkpoint**: この時点で User Story 1・2・3 がともに独立して動作する状態

---

## Phase 6: User Story 4 - マルチモニタ環境でモニタごとに管理する (Priority: P3)

**Goal**: モニタごとに表示/非表示を切り替え、モニタごとに異なる表示位置を独立して保持・復元する

**Independent Test**: 2 台以上のモニタ環境で、モニタ A のみ表示・モニタ B は非表示に設定し、
モニタ A での表示位置を移動した後、アプリを再起動してもモニタごとの表示/非表示と位置が
それぞれ保持されていることを確認できる

### Implementation for User Story 4

- [X] T032 [US4] `SettingsWindow.xaml(.cs)` に、`MonitorEnumerationService` から取得した
      接続中モニタの一覧を表示し、モニタごとの表示/非表示チェックボックスを配置する (FR-014)
      (Foundational T010、T022 に依存)
- [X] T033 [US4] `SettingsWindow` でモニタの表示/非表示チェックボックスが切り替えられたとき、
      対応する `ClockWindow` を実行時に生成/破棄する処理を `App.xaml.cs` (または専用の
      ウィンドウ管理ヘルパー) に実装する (FR-014、spec.md の Acceptance Scenario US4-1)
      (T016, T032 に依存)
- [X] T034 [US4] T027 のドラッグ移動と T030 の位置永続化が、各 `ClockWindow` ごとに
      それぞれのモニタ識別子をキーとした `WidgetSettings.Monitors` のエントリへ独立して
      書き込まれ、他モニタのエントリに影響しないことを実装・確認する (FR-015、spec.md の
      Acceptance Scenario US4-2) (T030, T033 に依存)
- [X] T035 [P] [US4] `SettingsWindow` に自動起動 ON/OFF のチェックボックスを追加し、
      `AutoStartManager` と `WidgetSettings.AutoStartEnabled` に連動させる (FR-019, FR-013)
      (T020, T022 に依存)

**Checkpoint**: すべてのユーザーストーリーが独立して機能する状態

---

## Phase 7: Polish & Cross-Cutting Concerns

**Purpose**: 全ストーリー共通の最終確認

- [X] T036 [P] quickstart.md の US1〜US4 の手動シナリオと DPI 混在環境の確認をすべて実施し、
      期待結果と一致することを確認する (FR-020)
      (2026-09-17 実施。プライマリ 1920x1080 /100% + セカンダリ 3840x2160 /225% の DPI 混在
      環境で確認。結果は下記「Phase 7 の確認結果」を参照)
- [X] T037 [P] 詳細設定画面を閉じている間のアイドル時 CPU 使用率が 1%未満、メモリ使用量が
      100MB 未満であることをタスクマネージャー等で確認する (SC-006、詳細設定画面を開いている
      間は対象外)。超過する場合はタイマー周期・描画処理を見直す
      (60 秒平均で CPU 0.007%、プライベートワーキングセット 36.1MB。SC-006 を満たす)
- [X] T038 [P] 実際に書き出された `settings.json` が contracts/settings-file.md のスキーマ例
      と一致すること (キー名・型・`Monitors` のキー形式) を確認する
      (キー名・階層・型は一致。`Monitors` のキー例が実装と異なっていたため
      `contracts/settings-file.md` の例と補足を実装に合わせて修正した)

### Phase 7 の確認結果 (2026-09-17)

**確認できたこと**

- `dotnet test` 27 件すべて成功
- US1: 両モニターにウィジェットが表示され、秒表示 ON で 1 秒ごとに時刻が更新される
- US1: 起動中に 2 つ目のプロセスを起動すると、新しいウィンドウを作らず終了コード 0 で終了する
- US2: 秒表示・時刻フォントサイズ・背景透過度・日付/曜日の表示位置の変更が即座に反映される
  - 変更は `settings.json` にも即座に保存され、再起動後も同じ見た目・位置で復元される
- US3: 位置ロック ON ではドラッグしても位置が変わらず、OFF にするとドラッグで移動・保存される
- US3: 最前面表示 OFF で背後に回ったウィジェットが、タスクトレイアイコンのクリックで最前面に戻る
- US3: タスクトレイアイコンの右クリックメニュー「終了」でアプリが終了する
- US3: 実行ファイル・ウィンドウ・タスクトレイのアイコンが専用の時計アイコンになっている
- US4: モニターごとの表示/非表示の切り替えが他方のモニターに影響せず、位置もモニターごとに
  独立して保存・復元される
- DPI 混在: 100% のプライマリと 225% のセカンダリの双方で、フォント・レイアウトが崩れずに
  それぞれの拡大率で表示される (FR-020)

**発見した挙動 (対応は保留)**

- 背景透過度を 100 にすると、文字が描画されているピクセル以外はクリックが下のウィンドウへ抜ける
  - 原因は、`AllowsTransparency` のレイヤードウィンドウではアルファ 0 の領域が
    ヒットテスト対象外になること
  - 文字の上を正確に右クリックすればメニューは出る
  - quickstart.md の「既知の制約の確認」にある「透過度 100% でもドラッグ・右クリックが
    できること」とは食い違うため、記録として残す (対応方針は別途判断)

**未確認**

- モニターの物理的な取り外し/再接続後の設定保持 (quickstart.md 上も任意項目)
- DirectX 排他フルスクリーンでの挙動 (既知の制約であり、不具合ではない)

---

## Phase 8: /speckit-analyze 指摘の反映 (2026-09-17)

**Purpose**: tasks.md 作成後の `/speckit-clarify` で spec.md に追加された FR-018(通知)・
FR-022(書き込み失敗耐性)・Edge Case(稼働中のモニタ切断)に対応するタスクを追加する
(`/speckit-analyze` の指摘 C1・C2・C4 への対応。C3・I1・U1 は該当タスク本文を直接修正済み)

- [X] T039 `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs` の `Load()` を変更し、
      設定ファイルの内容が不正でデフォルト値へフォールバックしたかどうかを呼び出し元へ
      伝える戻り値(例: `(WidgetSettings Settings, bool FellBackToDefaults)` のタプル)を
      追加する (FR-018) (T011 の変更)
- [X] T040 [P] `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs` に、
      不正な JSON 読み込み時に T039 のフォールバックフラグが `true` になることを検証する
      テストケースを追加する (T039 に依存)
- [X] T041 [US1] `src/OkidokeiWidget.App/App.xaml.cs` の起動処理で、T039 のフラグが `true`
      の場合に、設定が壊れていたためデフォルト設定で起動したことをユーザーに通知する
      (例: 起動直後に簡易なメッセージボックスを表示する) (FR-018) (T016, T039 に依存)
- [X] T042 `src/OkidokeiWidget.Core/Persistence/SettingsRepository.cs` の `Save()` に、
      書き込み時の例外(`UnauthorizedAccessException`、`IOException` 等)を捕捉し、
      クラッシュせず処理を継続する(保存はできないが呼び出し元には正常終了として返す)
      処理を追加する (FR-022) (T011 の変更)
- [X] T043 [P] `tests/OkidokeiWidget.Core.Tests/Persistence/SettingsRepositoryTests.cs` に、
      書き込み失敗時(例: 書き込み不可なパスを指定)でも `Save()` が例外を外部に伝播させ
      ないことを検証するテストを追加する (T042 に依存)
- [X] T044 `src/OkidokeiWidget.Core/Monitors/MonitorEnumerationService.cs` (または新規の
      ヘルパークラス)に、`SystemEvents.DisplaySettingsChanged` を購読してアプリ稼働中の
      モニタ構成変化を検知する仕組みを実装する (T010 に依存)
      (実装は `src/OkidokeiWidget.App/DisplayChangeNotifier.cs` に、TrayIconManager と同様の
      HwndSource + WM_DISPLAYCHANGE フックとして追加した。`Microsoft.Win32.SystemEvents` は
      追加の NuGet 参照が必要になるため、既存の P/Invoke パターンに合わせて置き換えた)
- [X] T045 [US4] `src/OkidokeiWidget.App/App.xaml.cs` (または専用のウィンドウ管理ヘルパー)で、
      T044 の変化通知を受けて、切断されたモニタに対応する `ClockWindow` を非表示にし、
      再接続時に同じ設定(表示/非表示・位置)で表示を再開する処理を実装する (Edge Case:
      稼働中のモニタ切断時は自動非表示、設定は保持) (T016, T033, T044 に依存)

**Checkpoint**: 上記タスク完了後、`dotnet test` が全件成功し、`quickstart.md` の既知の制約確認
セクションで DPI 混在環境の手動確認も行える状態

---

## Phase 9: User Story 2 実機確認による仕様追加の反映 (2026-09-17)

**Purpose**: User Story 2 実装後、実際にウィジェットを表示して確認した結果判明した仕様漏れ
(FR-024〜FR-029、spec.md の Clarifications 参照) に対応する

- [X] T046 [P] [US2] `src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs` を変更し、
      `ShowSeconds`(bool)、`DateSeparator`(string)、`DayOfWeekFormat`(string)、
      `TimeAndDateOnSameLine`(bool) を追加する。既存の `FontSize`/`FontColor` は
      `TimeFontSize`/`TimeFontColor` に改名し、新たに `DateFontSize`/`DateFontColor` を
      追加する(data-model.md 参照。設定ファイルは現バージョンのみが対象のため、旧フィールド名
      からの移行処理は行わない)
- [X] T047 [P] [US2] `src/OkidokeiWidget.Core/Settings/DayOfWeekFormat.cs` に、
      `ShortKanjiParen`・`LongKanji`・`ShortEnglish`・`LongEnglish` の 4 値を持つ enum を
      定義する (FR-027)
- [X] T048 [US2] `src/OkidokeiWidget.Core/Settings/DayOfWeekFormatter.cs` に、
      `DayOfWeek` と `DayOfWeekFormat` から表示文字列 (例:「(月)」「月曜日」「Mon.」「Monday」)
      を返すロジックを実装する (T047 に依存)
- [X] T049 [P] [US2] `src/OkidokeiWidget.Core/Settings/DateSeparatorResolver.cs` に、
      `DateSeparator` が `/` または `-` 以外の場合にデフォルト (`/`) へフォールバックする
      ロジックを実装する (FR-026、data-model.md のバリデーション)
- [X] T050 [P] [US2] `tests/OkidokeiWidget.Core.Tests/Settings/DayOfWeekFormatterTests.cs`、
      `tests/OkidokeiWidget.Core.Tests/Settings/DateSeparatorResolverTests.cs` に単体テストを
      実装する (T048, T049 に依存)
- [X] T051 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml(.cs)` のレイアウトを変更する:
      日付・曜日を常に同一行(改行なし)に表示し (FR-028)、`TimeAndDateOnSameLine` に応じて
      時刻行と日付/曜日行を同一行/別行に切り替える (FR-029)。`ShowSeconds` に応じて秒の
      表示有無を切り替え (FR-024)、時刻表示と日付/曜日表示にそれぞれ
      `TimeFontSize`/`TimeFontColor`・`DateFontSize`/`DateFontColor` を適用し (FR-025)、
      日付表示に `DateSeparator` を、曜日表示に `DayOfWeekFormatter` の結果を反映する
      (T046, T048, T049 に依存)
- [X] T052 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml(.cs)` に、秒表示 ON/OFF、
      時刻表示/日付・曜日表示それぞれのフォントサイズスライダー・文字色選択ボタン、
      日付区切り文字選択、曜日表示形式選択、時刻と日付/曜日を同一行に表示するかの
      チェックボックスを追加し、変更を即座に反映・保存する (T046, T048, T049 に依存)
- [X] T053 [P] [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml` の見た目を整理する
      (「見た目が古い」との指摘への対応): `GroupBox` 等でセクション分け
      (表示内容・時刻・日付/曜日・レイアウト・背景) し、余白・配置を統一する。機能追加は伴わない
      見た目の整理のみ

**Checkpoint**: 上記タスク完了後、`dotnet test` が全件成功し、詳細設定画面から秒表示・
時刻/日付/曜日それぞれのフォントサイズ・色・日付区切り文字・曜日形式・改行有無を変更すると
即座にウィジェットへ反映され、再起動後も保持されることを確認できる状態

---

## Phase 10: 背景色の追加・詳細設定画面のレイアウト再編 (2026-09-17)

**Purpose**: 実機確認で追加された FR-030(背景色)への対応と、設定項目の増加に伴い縦に伸び
続けている詳細設定画面のレイアウトをタブ化して整理する

- [X] T054 [P] [US2] `src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs` に
      `BackgroundColor`(string、`#AARRGGBB` 形式、デフォルト黒)を追加する (FR-030、
      data-model.md 参照。アルファ成分は無視し、実際の透過度は `BackgroundOpacity` から
      都度算出する)
- [X] T055 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の `ApplyAppearance()` で、
      `BackgroundColor` の RGB 成分と `BackgroundOpacity` から算出したアルファ値を組み合わせて
      `BackgroundBorder.Background` に反映する (T054 に依存)
- [X] T056 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml(.cs)` の「背景」セクションに、
      既存の `ColorPickerWindow` を再利用した背景色選択ボタンを追加する (T054、既存の
      `PickColor` ヘルパーに依存)
- [X] T057 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml` を `TabControl` で再編し、
      既存の 5 セクション(表示内容・フォント・日付/曜日の表示形式・レイアウト・背景)を
      「表示」「フォント」「レイアウト・背景」の 3 タブにまとめ、ウィンドウの縦方向の
      伸びを抑える。今後 US3/US4 で増える設定項目(位置ロック・最前面表示・モニタ別表示・
      自動起動)も同様にタブへ追加していく方針とする(機能追加は伴わない見た目の整理)

**Checkpoint**: 上記タスク完了後、`dotnet test` が全件成功し、詳細設定画面から背景色を
変更すると即座にウィジェットへ反映され、再起動後も保持されることを確認できる状態。
詳細設定画面がタブ化され、ウィンドウの高さが Phase 9 時点より抑えられていることを確認できる状態

---

## Phase 11: User Story 3 実装後の追加要望への対応 (2026-09-17)

**Purpose**: User Story 3 の実機確認後に追加された FR-029(改訂)・FR-031〜FR-033
(日付/曜日の 4 方向配置、タスクトレイ常駐アイコンによる前面復帰・終了、アプリアイコン)
に対応する

- [X] T058 [P] [US2] `src/OkidokeiWidget.Core/Settings/RelativePosition.cs` に、
      `Above`・`Below`・`Left`・`Right` の 4 値を持つ enum `RelativePosition` を定義する
      (FR-029)
- [X] T059 [US2] `src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs` の
      `TimeAndDateOnSameLine`(bool)を `DateDayOfWeekPosition`(`RelativePosition`、
      デフォルト `Below`)に置き換える(T058 に依存。設定ファイルは現バージョンのみが対象の
      ため、旧フィールド名からの移行処理は行わない)
- [X] T060 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml` の `RootPanel` を
      `StackPanel` から `DockPanel` に変更する。`ClockWindow.xaml.cs` の
      `ApplyAppearance()` で `DateDayOfWeekPosition` に応じて
      `DockPanel.SetDock(DateRow, ...)` と `DateRow.Margin` を設定し、時刻表示に対して
      上/下/左/右のいずれかに日付/曜日表示を配置する(T059 に依存)
- [X] T061 [US2] `src/OkidokeiWidget.App/SettingsWindow.xaml(.cs)` の
      `TimeAndDateOnSameLineCheckBox` を、4 方向から選択する `ComboBox` に置き換える
      (`DayOfWeekFormatCombo` と同様の Value+Label パターン)(T059 に依存)
- [X] T062 [P] `src/OkidokeiWidget.App/app.ico` を新規作成する(時計をイメージした専用の
      アイコン、複数解像度を含む)。`OkidokeiWidget.App.csproj` の `ApplicationIcon` に
      指定し、実行ファイルのアイコンとする (FR-033、research.md #14)
- [X] T063 [US3] `src/OkidokeiWidget.App/ClockWindow.xaml`・`SettingsWindow.xaml` に
      `Icon="app.ico"` を設定する(T062 に依存)
- [X] T064 [US3] `src/OkidokeiWidget.App/TrayIconManager.cs` に、`Shell_NotifyIcon`
      (shell32.dll)への P/Invoke と非表示の `HwndSource` を用いたタスクトレイ常駐アイコンを
      実装する。クリックで全 `ClockWindow` を最前面に呼び戻し(`Topmost` の一時的な
      true→元の値への切替)、右クリックで「終了」のみを持つ `ContextMenu` を表示する
      (FR-031, FR-032、research.md #13)(T062 に依存)
- [X] T065 [US3] `src/OkidokeiWidget.App/App.xaml.cs` の起動処理で `TrayIconManager` を
      初期化し、終了処理(`OnExit`)でアイコンを破棄する(T064 に依存)

**Checkpoint**: 上記タスク完了後、`dotnet test` が全件成功し、詳細設定画面から日付/曜日の
表示位置(上下左右)を変更すると即座にウィジェットへ反映され、再起動後も保持されること、
タスクトレイの常駐アイコンをクリックすると最前面表示 OFF でもウィジェットが前面に戻ること、
右クリックの「終了」でアプリが終了すること、実行ファイル・ウィンドウ・タスクトレイアイコンに
専用アイコンが表示されることを確認できる状態

---

## Phase 12: 実機確認で発覚したバグの修正 (2026-09-17)

**Purpose**: User Story 4 の実機確認 (モニタの物理的な取り外し/再接続) と Phase 7 以降の
利用で判明した 2 件のバグを修正する。いずれも既存 FR の実装バグであり、仕様の追加・変更は
伴わない

- issue #10: モニタ構成が変わるとウィジェットが画面外に配置され表示されない
  (FR-014, FR-015、Constitution 原則 V「マルチモニタ・DPI 対応を前提とした設計」に反する状態)
- issue #11: 背景の透過度を 100% にするとウィジェットのほとんどの領域をクリックできない
  (FR-008, FR-030)

- [X] T066 [P] [US2] `src/OkidokeiWidget.Core/Settings/BackgroundAlphaResolver.cs` を追加し、
      背景の透過度からアルファ値を算出する処理を切り出す。アルファ値の下限を 1 とし、透過度
      100% でも 0 にしない (issue #11)
- [X] T067 [US2] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` の `ApplyAppearance()` の
      アルファ値計算を `BackgroundAlphaResolver.Resolve()` に置き換える (T066 に依存)
- [X] T068 [P] [US4] `src/OkidokeiWidget.Core/Monitors/WidgetPlacementCalculator.cs` を追加し、
      `MonitorPlacement` (作業領域基準の相対座標) と仮想デスクトップ上の絶対座標を物理ピクセルで
      相互変換する。作業領域から外れる座標は作業領域内へ収める (issue #10)
- [X] T069 [P] [US4] `src/OkidokeiWidget.App/WindowPositionHelper.cs` を追加し、
      `SetWindowPos` / `GetWindowRect` でウィンドウ位置を物理ピクセルとして扱えるようにする。
      WPF の `Window.Left/Top` は論理単位 (DIP) のため、拡大率 100% 以外のモニタでは物理座標と
      ずれる (issue #10)
- [X] T070 [US4] `src/OkidokeiWidget.App/ClockWindow.xaml.cs` のウィンドウ配置を
      `SourceInitialized` / `ContentRendered` での `ApplyPlacement()` に移し、ドラッグ終了時の
      位置保存も物理ピクセルで行うようにする (T068、T069 に依存)
- [X] T071 [US4] `src/OkidokeiWidget.App/App.xaml.cs` の `SyncClockWindows()` で、表示中の
      `ClockWindow` にも最新の `ConnectedMonitor` を渡して配置し直す (`UpdateMonitor()`)。
      モニタの取り外し・再接続で作業領域の原点が動くため (T070 に依存)
- [X] T072 [P] `tests/OkidokeiWidget.Core.Tests/Settings/BackgroundAlphaResolverTests.cs`・
      `tests/OkidokeiWidget.Core.Tests/Monitors/WidgetPlacementCalculatorTests.cs` に単体テストを
      追加する (T066、T068 に依存)

**Checkpoint**: `dotnet test` が全件成功し、背景の透過度 100% でもウィジェットの矩形全体で
右クリック・ドラッグができること、モニタを取り外して再接続してもウィジェットが保存された位置に
表示されることを確認できる状態

**補足**: 修正前の `settings.json` の座標は論理単位と物理ピクセルが混ざった値のため、拡大率が
100% でないモニタではウィジェットの位置が一度だけずれる。そのモニタで一度ドラッグし直せば、
以後は正しい値が保存される

---

## Dependencies & Execution Order

### Phase Dependencies

- **Setup (Phase 1)**: 依存なし。即座に着手可能
- **Foundational (Phase 2)**: Setup 完了に依存。すべてのユーザーストーリーをブロックする
- **User Stories (Phase 3+)**: すべて Foundational フェーズの完了に依存
  - 各ストーリーはその後、優先度順 (P1 → P2 → P3) に進めるか、要員がいれば並行して進められる
- **Polish (最終フェーズ)**: 実施対象のユーザーストーリーすべての完了に依存
- **Phase 8 (/speckit-analyze 指摘の反映)**: T039・T042・T044 は Foundational (T011, T010) の
  後であれば着手可能。T041 は US1 (T016) に、T045 は US4 (T016, T033) に依存するため、
  それぞれ対応するユーザーストーリーの実装後が自然

### User Story Dependencies

- **User Story 1 (P1)**: Foundational (Phase 2) の後に着手可能。他ストーリーへの依存なし
- **User Story 2 (P2)**: Foundational の後に着手可能。`ClockWindow` の時刻表示ロジック (T018)
  を前提として見た目を反映するため、実装順としては US1 の後が自然
- **User Story 3 (P2)**: Foundational の後に着手可能。`ClockWindow` (T018) を前提とするため、
  実装順としては US1 の後が自然
- **User Story 4 (P3)**: Foundational の後に着手可能。モニタごとの `ClockWindow` 生成/破棄
  (T016) とドラッグ移動・位置永続化 (T027, T030) を前提とするため、実装順としては
  US1・US3 の後が自然

### Within Each User Story

- モデル/サービスの実装を先に行い、その後 UI・イベントハンドラを実装する
- 単体テストが対象とするロジック (Foundational の SettingsRepository・
  MonitorSettingsReconciler・AutoStartManager) は、実装直後にそのテストを追加する
- 各ストーリーの実装が完了してから次の優先度のストーリーに進む (または並行して着手する)

### Parallel Opportunities

- Setup の [P] マーク付きタスク (T002〜T004) は並行実行できる
- Foundational の [P] マーク付きタスク (T005〜T007、T009、T014) は並行実行できる
- Foundational 完了後は、要員がいれば各ユーザーストーリーを並行して着手できる
- 同一ストーリー内の [P] マーク付きタスクは並行実行できる

---

## Parallel Example: Foundational Phase

```bash
# データモデルを並行実装する:
Task: "AppearanceSettings モデルを src/OkidokeiWidget.Core/Settings/AppearanceSettings.cs に実装"
Task: "WindowBehaviorSettings モデルを src/OkidokeiWidget.Core/Settings/WindowBehaviorSettings.cs に実装"
Task: "MonitorPlacement モデルを src/OkidokeiWidget.Core/Settings/MonitorPlacement.cs に実装"
```

## Parallel Example: User Story 1

```bash
# 多重起動チェックと画面レイアウトは別ファイルのため並行実装できる:
Task: "App.xaml.cs に名前付き Mutex による多重起動チェックを実装"
Task: "ClockWindow.xaml に時刻/日付/曜日表示のレイアウトを実装"
```

---

## Implementation Strategy

### MVP First (User Story 1 のみ)

1. Phase 1: Setup を完了する
2. Phase 2: Foundational を完了する (すべてのストーリーをブロックするため必須)
3. Phase 3: User Story 1 を完了する
4. **一度立ち止まって検証する**: User Story 1 を独立して手動テストする (quickstart.md US1)
5. 準備ができればここでリリース/デモしてもよい (MVP)

### Incremental Delivery

1. Setup + Foundational を完了する → 基盤が整う
2. User Story 1 を追加 → 独立してテスト → デモ (MVP)
3. User Story 2 を追加 → 独立してテスト → デモ
4. User Story 3 を追加 → 独立してテスト → デモ
5. User Story 4 を追加 → 独立してテスト → デモ
6. 各ストーリーは、それ以前のストーリーを壊さずに価値を追加する

---

## Notes

- [P] タスク = 別ファイルかつ未完了タスクへの依存がない
- [Story] ラベルはタスクを対応するユーザーストーリーに紐づけ、追跡可能にする
- 各ユーザーストーリーは独立して完了・テスト可能であるべき
- Foundational フェーズの単体テストは、対象ロジックの実装直後に追加し、実装が正しいことを
  確認する
- 論理的な単位ごとにコミットする
- 各チェックポイントで、そのストーリーが独立して動作することを検証してから次に進む
- 避けるべきこと: 曖昧なタスク、同一ファイルへの並行編集競合、ストーリー間の独立性を壊す
  依存関係
