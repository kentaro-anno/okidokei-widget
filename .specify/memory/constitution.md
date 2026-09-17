# 常駐時計ウィジェット (desktop-clock-widget) Constitution

## Core Principles

### I. シンプルさ優先 (YAGNI)
本プロジェクトは個人利用を目的としたデスクトップツールである。`docs/requirements.md`
に定義されていない機能を先回りして実装してはならない。抽象化・設定項目・拡張ポイントは、
現時点で要求されている具体的なユースケースを満たす最小限にとどめる。汎用フレームワーク化や
将来の仮説的な要件への備えは行わない。

### II. 軽量な常駐動作
常駐アプリケーションとして、アイドル時の CPU・メモリ消費を最小限に保つこと。時刻表示の更新は
必要最小限の頻度(秒単位の表示更新に対して過剰なポーリングをしない等)で行う。Windows 起動時の
自動起動によって、ユーザーが体感できるレベルの起動遅延を発生させてはならない。

### III. 設定は JSON・非破壊
設定は Windows レジストリを使用せず、人間が読める JSON ファイルとして保存する
(アプリと同じフォルダ、または `%APPDATA%` 配下)。設定ファイルの読み書きは、破損時にも
アプリがクラッシュせずデフォルト値にフォールバックできること。

### IV. 誤操作防止
ユーザーが意図しない状態変化(特にウィンドウ位置のズレ)を防ぐ UI を優先する。位置ロック機能は
既定の防御線とし、ロック中はドラッグ操作を受け付けない。破壊的・見た目が大きく変わる設定変更は、
右クリックメニューのような誤操作しやすい導線に置かない。

### V. マルチモニタ・DPI 対応を前提とした設計
モニタごとの表示/非表示および表示位置は、モニタの増減・接続構成の変化に対しても矛盾なく
復元できるように保持すること。High DPI 環境やモニタごとに異なる DPI スケーリングが混在する環境
でも、フォントサイズやウィンドウ位置が崩れないよう設計する。

## Additional Constraints

- 技術スタック: WPF / C#(.NET)。対象.NET バージョンは実装時に妥当な LTS 等を選定してよい
- 対象 OS: Windows 11
- 設定保存にレジストリを使用しない(Core Principle III)
- 自動起動は Windows 標準の仕組み(スタートアップフォルダ、またはレジストリ Run キー等)を
  実装時に選定して用いる。ただしユーザー設定自体は Core Principle III に従い JSON で管理する

## Development Workflow

本プロジェクトは GitHub Spec Kit による spec-driven development で進める。

1. `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` → `/speckit-implement` の順で
   フェーズを進める。必要に応じて `/speckit-clarify`(plan 前)、`/speckit-analyze`・
   `/speckit-checklist`(implement 前)を挟む
2. 開発の意思決定の顛末を後から追跡できるように、各フェーズの成果物
   (`spec.md`, `plan.md`, `tasks.md` およびそれらの更新)はリポジトリにコミットし、
   開発記録として残す。生成物を破棄・非公開のまま次フェーズに進めない
3. コミットメッセージや PR 相当の記録には、そのフェーズで何を決定し、なぜそう決めたかが
   後から追える情報を残すことを推奨する
4. **ブランチ運用**: `/speckit-plan`・`/speckit-tasks` はそれぞれ専用ブランチ
   (`plan/<feature>`, `tasks/<feature>` 等)を切って作業し、完了したら PR を作成する。
   `/speckit-implement` は `spec.md` のユーザーストーリー(P1, P2, ...)単位でブランチ・PR を
   分割する(1 ユーザーストーリー = 1 ブランチ = 1PR)
5. **バグ修正の扱い**: 実装済みの機能で発覚したバグは、まず GitHub Issue として現象・再現手順・
   原因を記録し、`fix/<短い名前>` ブランチで修正する。修正作業自体は `bug` 拡張
   (`specify extension add bug`)の `/speckit.bug.assess` → `/speckit.bug.fix` →
   `/speckit.bug.test` の順で行い、`.specify/bugs/<slug>/` の各レポート
   (`assessment.md`, `fix.md`, `test.md`)をコミットして記録として残す
   (`spec.md`・`plan.md`・`tasks.md` と同様、Governance の記録要件に従う)。既存の
   機能要件 (FR) の実装バグであれば `spec.md` は変更せず、`tasks.md` に新しい Phase を
   追記する。FR 自体の追加・変更が必要な場合は `/speckit-specify` からの通常のフェーズに戻る
6. **レビューとマージのゲート**: PR は人間が GitHub 上でレビューし、マージを判断する。Claude は
   マージ状態を自発的に確認しない。人間から次のフェーズ/ストーリーに進めるよう明示的な指示が
   あった時点で、ローカルの `main` を最新化(`checkout main` → `pull`)してから次の作業用
   ブランチを作成する

## Governance

本 Constitution は、本プロジェクトにおける他のすべての開発方針に優先する。原則からの逸脱は、
`plan.md` の Complexity Tracking 等で明示的に正当化されない限り認めない。

改定はセマンティックバージョニングに従う:
- MAJOR: 原則の後方互換性のない削除・再定義
- MINOR: 新しい原則・セクションの追加、既存ガイダンスの実質的な拡張
- PATCH: 文言修正・明確化等の非意味論的変更

**Version**: 1.3.0 | **Ratified**: 2026-09-17 | **Last Amended**: 2026-09-17
