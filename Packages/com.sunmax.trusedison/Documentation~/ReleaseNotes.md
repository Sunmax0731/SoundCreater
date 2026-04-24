# Release Notes

## 0.3.0

`Torus Edison` の書き出し制御、Stereo 音作り、タイムライン編集を強化する release です。

主な更新:

- Export 画面に書き出し長モード `Project Bars` / `Seconds` / `Auto Trim` を追加
- release / delay tail を目標長に含めるか、目標長で切るかを選べるように改善
- 書き出し後に peak、目標長、出力長、project length、tail length、normalize 状態などの Export Quality を表示
- voice effect に `Stereo Detune` と `Stereo Delay` を追加し、Stereo project で左右チャンネルに異なる音を配置できるように改善
- timeline toolbar に grid division の直接選択 UI を追加し、`1/4`, `1/8`, `1/16`, `1/32`, `1/64` を選べるように改善
- 選択中の grid division を note snap、grid drawing、duplicate offset、common config の `defaultGridDivision` と同期
- Selection Inspector から `.gats-preset.json` の voice preset import / export を実行できるように改善
- toolbar から built-in project template を選んで新規 project を作成できるように改善
- Unity に取り込んだ `AudioClip` の 8-bit WAV 変換を `Tools/Torus Edison/8-bit WAV Converter` へ分離
- streaming / preload 無効の import 設定でも 8-bit 変換が失敗しないように修正
- README、日英マニュアル、ValidationChecklist、release 文面、BOOTH 商品紹介文を 0.3.0 の機能状態に合わせて更新

検証:

- Unity `6000.4.0f1` / Windows で EditMode tests を実行し、`Total=126 Passed=126 Failed=0 Skipped=0` を確認

既知の制限:

- Unity Editor 上での手動 ValidationChecklist 確認は release 前チェックとして継続して実施してください
- MP3 書き出し、オンライン連携、人声生成は対象外です

## 0.2.0

`Torus Edison` の主要な編集導線と配布文書を整理した release です。

主な更新:

- Edit 画面でプレビュー生成、再生、波形確認をまとめて扱えるように改善
- タイムライン下部の `+ Add Track` ボタンでトラックを追加できるように改善
- Edit 画面と Settings 画面から `Total Bars` を変更できるように改善
- Unity に取り込んだ `AudioClip` の 8-bit WAV 変換と、変換用 `.gats.json` 自動生成を追加
- 同梱 sample AudioProject を 10 本構成へ拡充
- `Version & License` 画面を追加
- 表示言語切替時に Settings 画面が空になる不具合を修正
- `Ctrl+D`、`Delete`、`Ctrl+Z` などの編集ショートカットまわりを修正
- `+ Add Track` 押下時の footer hit-test 由来の例外を修正
- README、日英マニュアル、release 文面、BOOTH 商品紹介文を現行実装に合わせて更新

既知の制限:

- 実際の Unity Editor 上でのマウス操作感と `Assets/` 配下書き出しは、引き続き手動確認を推奨します
- MP3 書き出し、オンライン連携、人声生成は対象外です
- UI 表示言語は日本語 / 英語 / 中国語に対応しています

## 0.1.1

`Torus Edison` の実装状態を反映した patch release です。

主な更新:

- 日本語 / 英語 / 中国語の UI 表示切替を追加
- `Auto + Override` の言語モードを追加
- Debug Mode と Log Level 切替による Unity Console 向け診断ログを追加
- release 文面、manual、validation checklist を現在の実装に合わせて更新

## 0.1.0

`Torus Edison` の初回公開 release です。

主な内容:

- `.gats.json` によるプロジェクト保存と読込
- タイムライン上でのノート作成、移動、長さ変更
- note / track / project の Inspector 編集
- `Render Preview / Play / Pause / Stop / Rewind / Loop`
- WAV 書き出し
- Undo / Redo
- `Basic SE` と `Simple Loop` のサンプル同梱

## 関連ドキュメント

- [Manual.ja.md](Manual.ja.md)
- [Manual.md](Manual.md)
- [TermsOfUse.md](TermsOfUse.md)
- [ValidationChecklist.md](ValidationChecklist.md)
