# Torus Edison マニュアル

英語版: [Manual.md](Manual.md)

関連ドキュメント:

- [TermsOfUse.md](TermsOfUse.md)
- [ReleaseNotes.md](ReleaseNotes.md)
- [ValidationChecklist.md](ValidationChecklist.md)

## 概要

`Torus Edison` は、Unity Editor 上でゲーム向けの短い効果音やループ素材を試作するためのエディタ拡張です。
プロジェクトデータを `.gats.json` で保存し、編集、試聴、書き出しまでを Unity 内で完結できます。

現在の実装範囲:

- `New / Open / Save / Save As`
- `.gats.json` の保存と読み込み
- タイムライン上でのノート編集
- note / track / project の Inspector 編集
- プレビュー生成と再生
- 16-bit WAV 書き出し
- 8-bit WAV 変換
- Undo / Redo
- 検証用サンプル同梱

## 対応環境

- Unity `6000.0` 以降
- Windows 上の Unity Editor 利用
- オフライン利用

## 起動方法

次のメニューから起動します。

- `Tools/Torus Edison/Open Editor`
- `Tools/Torus Edison/Version & License`

## 画面構成

現在のエディタウィンドウは、上部タブで 4 つの画面に分かれています。

### File

この画面では次の操作を行います。

- 現在のプロジェクト状態と保存先の確認
- 新規作成
- 読み込み
- 保存
- ローカルサンプルの作成
- `Basic SE` と `Simple Loop` の読み込み

### Edit

この画面では、編集とプレビュー確認をまとめて行います。

- プレビュー生成と再生操作
- 生成済みプレビューの波形表示
- `128 bars` までのプロジェクト長設定
- タイムライン上でのノート作成
- ノート移動
- ノートの長さ変更
- ノート複製と削除
- タイムライン下部の `+ Add Track` ボタンからのトラック追加
- note / track / project の Inspector 編集
- `?` ボタンから開く操作ヘルプウィンドウ

主なショートカット:

- `Ctrl+D` 選択ノート複製
- `Delete` 選択ノート削除
- `Ctrl+Z` Undo
- `Ctrl+Y` Redo

### Export

この画面では WAV 書き出しと、Unity に取り込んだ `AudioClip` の 8-bit 変換を行います。

利用できる操作:

- `Export WAV`
- Unity に取り込んだ `AudioClip` の 8-bit WAV 変換
- 変換した WAV と同じフォルダへの変換用 `.gats.json` 自動生成
- `Open Export Folder`
- 共通既定フォルダ設定
- プロジェクト上書きフォルダ設定
- `Assets/` 配下書き出し時の自動リフレッシュ設定

### Settings

この画面では、プロジェクト設定とツール設定を扱います。

- BPM
- Total Bars
- Sample Rate
- Channel Mode
- 表示言語
- Debug Mode / Log Level
- Foundation Status
- 読み込み時の警告確認

## 保存と読み込み

- `New`
  新しいプロジェクトを作成します。
- `Open`
  `.gats.json` プロジェクトファイルを読み込みます。形式が不正な場合は読み込みを拒否します。
- `Save`
  現在のプロジェクトを保存します。
- `Save As`
  別名保存します。保存時は `.gats.json` 拡張子に正規化されます。

標準のセッション形式は `.gats.json` です。

## タイムライン編集

現在対応している主な編集操作:

- 空レーン上のドラッグによるノート作成
- ノートドラッグによる移動
- ノート端ドラッグによる長さ変更
- 複数選択を前提にした編集
- Inspector からの pitch や velocity の変更
- タイムライン下部ボタンからのトラック追加
- Edit 画面の `Bars` 入力欄からのプロジェクト長変更
- 主要編集操作に対する Undo / Redo

## プレビュー再生

プレビュー再生では、現在のプロジェクト内容からオフライン音声バッファを生成し、Unity Editor の preview 経路で再生します。

現在の実装内容:

- 波形生成
- White Noise
- ADSR
- Delay
- トラック / プロジェクトの mixdown
- プレビュー波形表示
- `Render Preview / Play / Pause / Stop / Rewind / Loop`

## WAV 書き出し

現在の書き出し仕様:

- 16-bit PCM `.wav`
- `48000` / `44100` Hz
- Mono / Stereo
- ファイル名禁止文字の補正
- 出力先フォルダ自動作成
- `Assets/` 配下書き出し時の `AssetDatabase.Refresh()`

## 8-bit WAV 変換

Export 画面には、Unity に取り込んだ `AudioClip` アセットを 8-bit PCM `.wav` に変換する機能があります。

- 変換元 `AudioClip` を選択
- 出力名を指定
- 変換後サンプルレートを指定
- `Preserve Source` / `Mono` / `Stereo` を選択
- 8-bit PCM `.wav` として書き出し
- 同じフォルダへ変換用 `.gats.json` を自動生成

この機能は、すでに Unity に取り込んだ音声を変換するためのものです。YouTube など外部サービスから音源を取得する機能は含みません。

## バージョン / ライセンス画面

`Tools/Torus Edison/Version & License` から、現在のバージョン、利用規約への導線、配布元情報を確認できます。

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

- マウス操作の感触は、実際の Unity Editor 上での確認を推奨します。
- `Assets/` 配下書き出し時の挙動は、実際の Unity Editor 上での確認を推奨します。
- UI 表示言語は日本語 / 英語 / 中国語に対応しています。
- 診断ログは Unity Console を使ったローカルサポート用途を前提にしています。

## 補足

このマニュアルは、現在の実装状態に合わせて更新しています。今後の機能追加に応じて内容も追従します。
