# Torus Edison マニュアル

英語版: [Manual.md](Manual.md)

関連ドキュメント:

- [TermsOfUse.md](TermsOfUse.md)
- [ReleaseNotes.md](ReleaseNotes.md)
- [ValidationChecklist.md](ValidationChecklist.md)

## 概要

`Torus Edison` は、再利用しやすいゲームオーディオ用プロジェクトデータを Unity Editor 上で作成、編集、試聴、保存、書き出しするためのエディタ拡張です。

現在の実装範囲:

- `New / Open / Save / Save As` によるファイル操作
- `.gats.json` 形式での保存と読込
- タイムライン上でのノート編集
- note / track / project の Inspector 編集
- プレビュー再生
- WAV 書き出し
- Undo / Redo
- 検証用サンプル同梱

## 対応環境

- Unity `6000.0` 以降
- Windows 上の Unity Editor 利用
- オフライン利用

## 起動方法

次のメニューから起動します。

- `Tools/Torus Edison/Open Editor`

## ワークスペース構成

現在のエディタウィンドウは、上部タブで 4 つの画面に分かれています。

### File

この画面では以下を扱います。

- 現在のプロジェクト状態と保存先確認
- 新規作成
- 読み込み
- 保存
- サンプル作成
- `Basic SE` と `Simple Loop` の読込

### Edit

この画面では以下を扱います。

- 編集中のプレビュー生成と再生操作
- タイムライン上でのノート作成
- ノート移動
- ノートリサイズ
- ノート複製と削除
- note / track / project の Inspector 編集

現在の主なショートカット:

- `Render Preview` / `Play` / `Pause` / `Stop` / `Rewind` / `Loop`
- `Ctrl+D` 選択ノート複製
- `Delete` 選択ノート削除
- `Ctrl+Z` Undo
- `Ctrl+Y` Redo

### Export

この画面では WAV 書き出しを扱います。

利用できる操作:

- `Export WAV`
- `Open Export Folder`
- 共通既定フォルダ設定
- プロジェクト上書きフォルダ設定
- `Assets/` 配下書き出し時の自動リフレッシュ設定

### Settings

この画面では以下を扱います。

- BPM
- Total Bars
- sample rate
- channel mode
- 表示言語モード
- 診断モード / ログレベル
- Foundation Status
- 読み込み時の警告確認

## 保存と読込

- `New`
  新しいプロジェクトを作成します。
- `Open`
  `.gats.json` プロジェクトファイルを読み込みます。形式が不正な場合は読み込みを拒否します。
- `Save`
  現在のプロジェクトを保存します。
- `Save As`
  別名保存します。保存時は `.gats.json` 拡張子に正規化されます。

標準のセッション形式は `.gats.json` です。

## ノート編集

現在対応している編集:

- 空レーン上のドラッグによるノート作成
- ノートドラッグによる移動
- ノート端ドラッグによる長さ変更
- 複数選択を前提にした編集
- Inspector からの pitch や velocity の変更
- 主要編集操作に対する Undo / Redo

## プレビュー再生

現在のプレビュー再生では、プロジェクト内容からオフライン音声バッファを生成し、Unity Editor の preview 経路で再生します。

現在の実装内容:

- 波形生成
- White Noise
- ADSR
- Delay
- トラック / プロジェクトの mixdown
- Play / Pause / Stop / Rewind / Loop

## WAV 書き出し

現在の書き出し仕様:

- 16-bit PCM `.wav`
- `48000` / `44100` Hz
- Mono / Stereo
- ファイル名禁止文字の補正
- 出力先フォルダ自動作成
- `Assets/` 配下書き出し時の `AssetDatabase.Refresh()`

## 設定ファイル

現在の設定ファイル:

- 共通設定: `%LocalAppData%/GameAudioTool/config.json`
- プロジェクト設定: `ProjectSettings/GameAudioToolSettings.json`

プロジェクト設定は、サンプルレート、チャンネルモード、出力先、`Assets/` 自動リフレッシュ設定などで共通設定より優先されます。

## サンプル

同梱サンプル:

- `Samples~/BasicSE/basic-se.gats.json`
- `Samples~/SimpleLoop/simple-loop.gats.json`

サンプル内容と確認手順:

- [Samples~/README.md](../Samples~/README.md)
- [ValidationChecklist.md](ValidationChecklist.md)

## 既知の制約

- マウス操作の感触は実 Unity Editor 上での確認が必要です。
- `Assets/` 配下書き出し時の挙動は、実 editor 上での spot check が必要です。
- UI 表示言語は日本語 / 英語 / 中国語に対応しています。
- 診断ログは Unity Console を使ったローカルサポート用途を前提にしています。

## 現在の注意点

このマニュアルは、元の MVP 計画ではなく、現在の実装状態に合わせて更新しています。今後の機能追加に応じて内容も追従します。
