# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## プロジェクトの現状

`specs/001-clock-widget/` の実装は完了しており (v1.0.0)、`src/OkidokeiWidget.App` /
`src/OkidokeiWidget.Core` にソース一式がある。ビルドは
`dotnet build src/OkidokeiWidget.App/OkidokeiWidget.App.csproj -c Release`。
このリポジトリは、開発時に使っていた private リポジトリから、ソース・仕様一式のみを
コピーして公開用に作り直したもの(履歴・development-log 等の開発時ログは含まない)。

## Spec-driven development (GitHub Spec Kit)

本プロジェクトは [GitHub Spec Kit](https://github.com/github/spec-kit) を用い、
`/speckit-constitution` → `/speckit-specify` → `/speckit-plan` → `/speckit-tasks` →
`/speckit-implement` の順で進める(必要に応じて plan の前に `/speckit-clarify`、implement の前に
`/speckit-analyze` や `/speckit-checklist` を挟む)。

- 各コマンドは `.claude/skills/speckit-*/SKILL.md` にスキルとして実装されている
  - セッション中に  `speckit-*` スキルが呼び出し可能として認識されない場合は、該当する `SKILL.md` を開いて手順を手動でなぞること (本プロジェクトでは、導入直後のセッションでスキルがすぐに認識されず、実際にこの対応が必要になったことがある)
- プロジェクトの原則は `.specify/memory/constitution.md` にあり、その場の判断より優先される
  - **中心となる原則**
  - シンプルさ優先/YAGNI (個人用ツールであり `docs/requirements.md` にない機能を作り込まない)
  - 軽量な常駐動作、設定は JSON (レジストリは使わない)
  - 誤操作による状態変化の防止 (位置ロック等)、マルチモニタ/DPI を前提とした設計
- 機能仕様は `specs/<NNN>-<短い名前>/` に置かれる (`.specify/init-options.json` の設定により
  連番)
  - 現時点で唯一の機能は `specs/001-clock-widget/`
- `.specify/feature.json` は `/speckit-plan` や `/speckit-tasks` など後続コマンドに対象の機能
  ディレクトリを伝えるためのファイル
  - gitignore 対象(マシンローカルな状態)なので、存在しない場合は作業対象の機能ディレクトリを指すように作成し直すこと
- 各フェーズの成果物 (`spec.md`, `plan.md`, `tasks.md`) はローカルの作業メモにせず、必ずコミット
  すること
  - constitution の Governance で、何をなぜ決めたかの記録として残すことが求められている

### ブランチ・PR 運用

`.specify/memory/constitution.md` の Development Workflow に従うこと:

- `/speckit-plan`・`/speckit-tasks` はそれぞれ専用ブランチ(`plan/<feature>`,
  `tasks/<feature>` 等)を切って作業し、完了したら `gh pr create` で PR を作成する
- `/speckit-implement` は `spec.md` のユーザーストーリー(P1, P2, ...)単位でブランチ・PR を
  分割する(1 ユーザーストーリー = 1 ブランチ = 1PR)
- ただし小さな変更 (FR の追加・改訂が 1〜2 件、新しい画面やデータ構造の変更なし、1 ユーザー
  ストーリー内で完結) は、specify から implement までを 1 ブランチ・1PR で進めてよい
  - フェーズごとにコミットは分ける。小さな変更として扱うかは specify の時点で提案し、人間が決める
- 実装済み機能のバグ修正は SpecKit のフェーズ外として扱い、GitHub Issue に現象・再現手順・
  原因を記録してから `fix/<短い名前>` ブランチで修正する
  - 修正作業自体は `bug` 拡張 (`.claude/skills/speckit-bug-*`、`specify extension add bug`
    で導入済み) の `/speckit.bug.assess` → `/speckit.bug.fix` → `/speckit.bug.test` の順で
    行う。各コマンドが書く `.specify/bugs/<slug>/assessment.md`・`fix.md`・`test.md` は、
    `spec.md` 等と同様にコミットして記録として残す
  - 既存 FR の実装バグなら `spec.md` は変更せず、`tasks.md` に新しい Phase として追記する
  - FR 自体の追加・変更が必要なら `/speckit-specify` からの通常のフェーズに戻る
- PR のレビュー・マージは人間が行う。**マージ状態を自発的に確認してはならない。** 人間から
  「マージしたので次に進めて」等、明示的な指示があった時点でのみ、ローカルの `main` を
  `checkout` → `pull` して最新化し、次の作業用ブランチを新規作成すること

## ドキュメントの言語・表記ルール

プロジェクトのドキュメント(`docs/`, `specs/`, 意思決定を記述するコミットメッセージ)は、
既存ファイルおよび主な担当者の作業言語に合わせて日本語で書くこと。

### 日本語表記のルール

半角文字 (英数字・記号) と全角文字 (漢字・ひらがな・カタカナ) の間には半角スペースを入れる。

- 良い例: `GitHub を使う`、`VS Code で編集する`、`想定時間は 5 分`
- 悪い例: `GitHubを使う`、`VS Codeで編集する`、`想定時間は5分`

括弧・句読点など、直後に別の全角記号が続く場合は入れなくてよい(例: `(GitHub)`)。

### 箇条書きのルール

- 箇条書きの末尾に「。」は付けない
- 1 つの項目はブラウザの PC 画面で折り返さない程度の長さに収める
- 長くなりそうなときは、無理に 1 文にまとめず、項目を分ける
  - 前の項目の直接の帰結を書く場合のみ「なので」「したがって」で軽くつなぐ
  - 単に別の事実を並べるだけなら接続詞は不要
