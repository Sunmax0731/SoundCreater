# Torus Edison v0.1.0

Unity Editor 上で簡単なゲーム音を作成し、`.gats.json` と WAV で扱えるようにするエディタ拡張ツールです。

## 含まれるもの

- `TorusEdison-0.1.0.unitypackage`
- 日本語 / 英語マニュアル
- 利用規約
- リリースノート
- 検証チェックリスト
- サンプル `.gats.json`

## 主な機能

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

- マウス主体の editor workflow は MVP 段階です
- UI の多言語対応は未実装です
- デバッグログの詳細切替は未実装です
- Unity batch `-runTests -testResults ...` はこの環境では XML を出力しないため、最終確認は手動検証を含みます

## ドキュメント

- 日本語マニュアル: `Manual.ja.md`
- English manual: `Manual.md`
- 利用規約: `TermsOfUse.md`
- リリースノート: `ReleaseNotes.md`
- 検証チェックリスト: `ValidationChecklist.md`

## 備考

- GitHub リポジトリ名は `SoundCreater` ですが、公開名は `Torus Edison` です
- BOOTH 配布物も同一成果物を前提にしています
