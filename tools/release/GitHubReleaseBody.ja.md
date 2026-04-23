# Torus Edison v0.1.1

Unity Editor 上で簡単なゲーム音を作成し、`.gats.json` と WAV で扱えるようにするエディタ拡張ツールです。

## 含まれるもの

- `TorusEdison-0.1.1.unitypackage`
- 日本語 / 英語マニュアル
- 利用規約
- リリースノート
- 検証チェックリスト
- サンプル `.gats.json`

## 0.1.1 の主な更新

- 日本語 / 英語 / 中国語の UI 表示切替
- `Auto + Override` の言語モード
- Debug Mode と Log Level 切替による Unity Console 向け診断ログ
- release 文面と validation docs の現状反映

## 現在利用できる主な機能

- `.gats.json` ベースの音声プロジェクト保存 / 読込
- タイムライン上でのノート配置、移動、長さ変更
- note / track / project inspector 編集
- Render Preview / Play / Pause / Stop / Rewind / Loop
- WAV 書き出し
- Undo / Redo

## 同梱サンプル

- `Basic SE`
- `Simple Loop`

## 既知の制限

- マウス主体の editor workflow は引き続き MVP 段階です
- `Assets/` 配下への書き出しは live editor での spot check を推奨します
- MP3 書き出し、オンライン連携、人声生成は対象外です

## ドキュメント

- 日本語マニュアル: `Manual.ja.md`
- English manual: `Manual.md`
- 利用規約: `TermsOfUse.md`
- リリースノート: `ReleaseNotes.md`
- 検証チェックリスト: `ValidationChecklist.md`

## 備考

- GitHub リポジトリ名は `SoundCreater` ですが、公開名は `Torus Edison` です
- BOOTH 配布物も同一成果物を前提にしています
