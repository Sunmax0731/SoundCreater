# Torus Edison v0.2.0

Unity Editor 上で短いゲーム向け効果音やループ素材を試作し、`.gats.json` と WAV で扱えるようにするエディタ拡張です。

## 含まれるもの

- `TorusEdison-0.2.0.unitypackage`
- 日本語 / 英語マニュアル
- 利用規約
- リリースノート
- 検証チェックリスト
- サンプル `.gats.json` 10 本

## 0.2.0 の主な更新

- Edit 画面にプレビュー操作と波形表示を統合
- タイムライン下部からの `+ Add Track` 追加
- Edit / Settings の両方から `Total Bars` を変更可能
- Unity に取り込んだ `AudioClip` の 8-bit WAV 変換
- 変換した WAV と同じフォルダへの `.gats.json` 自動生成
- `Version & License` 画面の追加
- 言語切替時に Settings が空になる不具合の修正
- 編集ショートカットと add-track footer まわりの不具合修正
- README、マニュアル、商品紹介文の更新

## 現在利用できる主な機能

- `.gats.json` ベースの音声プロジェクト保存 / 読み込み
- タイムライン上でのノート配置、移動、長さ変更、複製、削除
- note / track / project Inspector 編集
- Render Preview / Play / Pause / Stop / Rewind / Loop
- preview 波形表示
- 16-bit WAV 書き出し
- 8-bit WAV 変換
- 日本語 / 英語 / 中国語 UI
- Debug Mode と Log Level による診断ログ

## 同梱サンプル

- `Basic SE`
- `Simple Loop`

## 既知の制限

- マウス主体の操作感は、引き続き実際の Unity Editor 上での確認を推奨します
- `Assets/` 配下への書き出しは、実際の Unity Editor 上での確認を推奨します
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
