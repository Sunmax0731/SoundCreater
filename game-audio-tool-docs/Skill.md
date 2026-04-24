# Skill.md
## Session Learnings

- 新しい表示文言は `GameAudioLocalization` 経由で管理し、`GameAudioToolWindow` に日英中の文言を直書きしない。
- 表示言語モードは `GameAudioLanguageMode` の `Auto + Override` を前提にし、Auto では editor language を優先し、取得できない場合のみ `UnityEngine.Application.systemLanguage` に fallback する。
- `UnityEditor.LocalizationDatabase` はこの Unity 系列で直接参照できないことがある。compile-time 参照は避け、`typeof(EditorWindow).Assembly.GetType(...)` を使って localization 層の内部で reflection 解決する。
- `Application` は project namespace と衝突しうるため、system language などの取得は `UnityEngine.Application` を明示する。
- Save / Load / Preview / Export の診断は `GameAudioDiagnosticLogger` と `GameAudioDiagnosticLogLevel` を通して出す。ad-hoc な `Debug.Log` を増やさない。
- batch validation では、生成された `Unity` PID を待ってから `-logFile` と `-testResults` を読む。full suite に無関係な既知 failure がある場合は targeted fixture で今回の変更範囲を先に確認する。
- localization で `CreateGUI()` を呼び直す場合、`RefreshInspectorPanel` のような差分更新キャッシュは無効化する。表示言語が変わるのに state key が変わらないと、空の UI が「更新済み」と判定されうる。
- タイムラインの `+ Add Track` footer のような補助行は、track row と同じ hit-test に流さない。`GetTrackIndex()` は実トラック領域外で `-1` を返すようにし、`Tracks[trackCount]` を読ませない。
- release prep では、package version、`ToolVersion`、sample / spec の `toolVersion`、release body、BOOTH 商品紹介文、README、manual を同時に更新する。版番号だけ先行すると配布物の整合が崩れる。
- Save / Save As / WAV export / 8-bit conversion で作るファイル名は `GameAudioValidationUtility.SanitizeExportFileName` を唯一の入口にする。`CON`、`PRN`、`AUX`、`NUL`、`COM1`、`LPT1` などのWindows予約名と、先頭末尾のドットや空白を必ずテストする。
- ProjectSettings の `preferredSampleRate` / `preferredChannelMode` は New 作成時だけに効く既定値 override として扱う。Settings UI、Manual、仕様書、resolver の契約を揃え、既存 `.gats.json` の保存値を上書きしない。
- 共通設定の `showStartupGuide` は初回起動ガイド、`rememberLastProject` と `lastProjectPath` は最後に保存または読み込みした `.gats.json` の復元に接続する。該当ファイルが消えている場合は記憶パスをクリアして新規プロジェクトへフォールバックする。
- config JSON の部分読込では、既定値 `true` の bool を `JsonUtility.FromJson` で直接復元しない。既定値を入れた DTO に `JsonUtility.FromJsonOverwrite` し、欠落と明示 `false` の両方をテストする。


## 名称

Unity Game Audio Tool Editor Extension Skill

## このSkillの目的

このSkillは、Unity 6000以降で動作する**ゲーム内音声作成エディタ拡張**を実装・保守・拡張するための共通ガイドである。  
対象ツールは、**ボイスを除くゲーム内音声**をGUI上で作成し、再生・保存・WAV出力できるUnity Editor拡張である。

## まず最初に読むもの

実装を始める前に、以下をこの順で読むこと。

1. `requirements-definition-v0.3.md`
2. `specification-v0.1.md`
3. `Agents.md`

要件定義書は「何を満たすか」、仕様書は「どう振る舞うか」、Agents.mdは「誰が何をどう進めるか」の基準である。

## プロダクトのゴール

MVPのゴールは次の通り。

- Unity Editor上で音声作成ツールを起動できる
- 複数トラック上にノートブロックを配置できる
- 波形合成 + ノイズ + ADSR + 簡易エフェクトで音を構成できる
- 単発再生 / ループ再生 / 再生カーソル表示ができる
- JSON保存 / JSON読込ができる
- WAV書き出しができる
- Undo / Redoが機能する
- GitHub Release / BOOTH向けの配布物として、サンプル・マニュアル・利用規約を同梱できる

## ゴールではないもの

MVPでは以下を狙わない。

- DAW級の高機能ミキサー
- 人声や歌声の生成
- MP3出力
- リアルな生楽器再現
- 立体音響
- オンライン連携
- ランタイム向け音楽プレイヤー機能

## 推奨技術方針

### 1. Editor UI

- メインウィンドウは `EditorWindow`
- レイアウトは **UI Toolkit中心** を推奨
- タイムライン描画は、必要に応じて `ImmediateModeElement` 相当やカスタム描画で補完する
- ノートブロック編集は「使いやすさ優先」で、GraphView系の実験的機能に依存しすぎない

### 2. ドメインモデル

- UIから直接JSONやUnityシリアライズ形式をいじらない
- `Project`, `Track`, `Note`, `Voice`, `Envelope`, `Effect`, `Config` などの**純粋なC#モデル**を用意する
- モデルはUIと分離し、変換層を介して永続化する

### 3. 音声生成

- 音声生成は**オフラインレンダリング前提**
- リアルタイムDSP最適化より、再現性・保守性・見通しを優先する
- 浮動小数バッファで内部処理し、最後にWAV PCMへ変換する
- ノイズ生成で乱数を使う場合は**シード固定**で再現性を持たせる

### 4. 保存

- MVPの正式保存形式は JSON
- JSONには `formatVersion` を必ず持たせる
- スキーマ変更時は、必ず互換性方針を仕様書と合わせて更新する
- Save / Save As / WAV export / 8-bit conversion のファイル名生成は共通サニタイズを使い、Windows で保存できない予約名や不安定な末尾文字を残さない

### 5. 設定管理

- 共通設定とプロジェクト設定を分離する
- 優先順位は `プロジェクト設定 > 共通設定 > 既定値`
- 出力先や自動リフレッシュは設定ファイル経由で管理する
- `preferredSampleRate` / `preferredChannelMode` は Settings UI から編集できるようにし、New 作成時の既定値として `GameAudioConfigResolver` 経由で適用する
- `showStartupGuide` / `rememberLastProject` / `lastProjectPath` は共通設定のユーザー体験項目として Settings UI、起動時復元、Manual、仕様書を同期して扱う
- `autoRefreshAfterExport` など既定値 `true` の bool は、古い/手動編集済み config JSON で欠落しても既定値を維持する

## 実装アーキテクチャの基本ルール

### レイヤ分離

以下の依存方向を守る。

```text
Editor UI -> Application -> Domain -> Render / Persistence
```

逆方向依存を避ける。特に次を禁止する。

- DomainがUnity Editor APIへ直接依存すること
- PersistenceがEditorWindowの状態を直接読むこと
- Audio RenderがUI描画の責務を持つこと

### 1責務1クラスを意識する

たとえば以下を分ける。

- `TimelineView` は描画と入力だけ
- `TimelineSelectionService` は選択状態だけ
- `ProjectSerializer` は保存と読込だけ
- `WaveRenderer` は波形生成だけ
- `WavExporter` はWAV出力だけ

## フォルダ構成の方針

```text
Editor/
  Windows/
  Timeline/
  Inspector/
  Application/
  Commands/
  Domain/
  Audio/
  Persistence/
  Export/
  Config/
  Utilities/
Tests/
  Editor/
Documentation~/
Samples~/
```

- UIはUI同士でまとめる
- ドメインモデルはEditor依存を最小化する
- テスト対象クラスは分離して小さくする

## コーディング規約

### 命名

- public 型・メソッド・プロパティ: PascalCase
- private フィールド: `_camelCase`
- 定数: `PascalCase` か `UPPER_SNAKE_CASE` のどちらかに統一
- boolは `is`, `has`, `can`, `should` で始める

### nullと例外

- null許容は必要最小限にする
- 例外は握りつぶさない
- ユーザ起因の入力不正は「通知 + 安全な中断」
- 開発不備の可能性が高いものはログまたはテストで検知できるようにする

### シリアライズ

- 保存JSONのフィールド名は英語・camelCaseに統一
- enumは文字列で保存する
- 浮動小数は必要以上に桁を増やさない
- フォーマット更新時は migration 戦略を考慮する

## Undo / Redo実装ガイド

- Editor API頼みだけでなく、**ツール独自のコマンド履歴**を持つ前提で設計する
- Undo対象は「状態変更」に限定する
- 再生・停止・エクスポートなど副作用中心の操作はUndo対象外にする
- 複数ノートの一括編集は1コマンドとして扱えるようにする

## 音声レンダリング実装ガイド

### 波形生成

MVP必須波形:

- Sine
- Square
- Triangle
- Saw
- Pulse

実装上の注意:

- 位相加算で生成する
- Pulseは duty 比率を持つ
- 出力前にクリッピング対策を入れる

### ノイズ

- MVPは White Noise で十分
- ノート単位で乱数シードを保持し、同じノートから同じ結果を得られるようにする

### ADSR

- Attack / Decay / Sustain / Release は線形実装でよい
- ノート長に対して過大なA+Dは圧縮する
- Releaseぶんを必ず末尾に加算する

### ディレイ

- シンプルなフィードバックディレイで開始する
- 再帰処理より、バッファ参照で明快に実装する
- Feedback > 0.7 はMVPでは禁止する

## UI実装ガイド

- ノート編集は「置ける・動かせる・伸ばせる」が最重要
- 最初から装飾を盛りすぎない
- ノートの選択状態が常に分かるようにする
- 再生カーソルは最優先で見やすくする
- 入力補助はマウス主体 + 数値入力の両立を維持する

## 設定・保存先ガイド

### 共通設定

- `%AppData%/Local/GameAudioTool/config.json`

### プロジェクト設定

- `ProjectSettings/GameAudioToolSettings.json`

### セッション保存

- `.gats.json`

保存先や設定ファイルの仕様を変更した場合は、**必ず仕様書とマニュアルを更新**すること。

## テスト方針

最低限、以下のテストを用意する。

- JSON保存/読込の往復
- WAV出力ヘッダ
- ADSRの振る舞い
- Delayの出力長
- Undo / Redoの整合性
- 読込失敗ケース
- 複数トラックミックス結果の基本確認

バグ修正時は、原則として**再発防止テスト**を追加する。

## 配布物作成ガイド

Release成果物には少なくとも以下を含める。

- UnityPackage
- Samples
- Manual.md
- TermsOfUse.md
- LICENSE
- CHANGELOG

BOOTH向けには、GitHub Releaseと**同一成果物**を出品する。  
利用規約はBOOTHの商品説明や外部公開ページから**購入前に読める状態**にしておく。

## 外部ライブラリ方針

- 必須でなければ依存を増やさない
- 導入する場合は MIT と整合するライセンスを優先する
- 依存追加時は、用途・代替案・ライセンスを記録する
- Editor拡張に不要な巨大依存は避ける

## 作業時のチェックリスト

実装前:

- 要件定義書と仕様書の該当章を確認したか
- 既存データ形式へ影響するか確認したか
- Undo対象かどうか整理したか

実装中:

- UI・ドメイン・保存が密結合になっていないか
- 範囲外値の丸めと通知を考慮したか
- 再現性を壊す乱数や時刻依存を入れていないか

実装後:

- テストを書いたか
- サンプルで動作確認したか
- 仕様書やマニュアルの更新が必要か確認したか
- GitHub Release / BOOTH配布物に影響するか確認したか

## Definition of Done

タスク完了と見なす条件は以下。

- 仕様に沿って実装されている
- エラーケースを考慮している
- 必要なテストが追加されている
- 既存機能を壊していない
- ドキュメント更新が必要な場合は反映されている
- 配布物へ影響する場合は、サンプル・マニュアル・利用規約への影響を確認している

## よくある失敗

- UIの都合で保存形式まで歪める
- ノート編集と再生状態の責務が混ざる
- Undo / Redoを後付けにして破綻する
- WAV出力だけ通ってプレビュー再生とズレる
- 乱数の扱いで同じJSONから違う音が出る
- `Assets` 配下以外にも毎回 `AssetDatabase.Refresh()` して無駄に重くする

## 最後に

このSkillの優先順位は以下。

1. 要件を満たす
2. 使いやすいエディタ体験を守る
3. 再現性を守る
4. 拡張しやすい構造を守る
5. 実装を小さく分ける

迷ったら、**MVPを確実に成立させる方向**を選ぶこと。
