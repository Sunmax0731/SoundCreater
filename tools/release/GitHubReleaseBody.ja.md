# Torus Edison v0.3.0

Unity Editor 上で短いゲーム向け効果音やループ素材を試作し、`.gats.json` と WAV で扱えるようにするエディタ拡張です。

## 含まれるもの

- `TorusEdison-0.3.0.unitypackage`
- 日本語 / 英語マニュアル
- 利用規約
- リリースノート
- 検証チェックリスト
- サンプル `.gats.json` 10 本

## 0.3.0 の主な更新

- Export 画面に `Project Bars` / `Seconds` / `Auto Trim` の書き出し長モードを追加
- release / delay tail を目標長に含めるか、目標長で切るかを選択可能
- Export Quality で peak、目標長、出力長、project length、tail length、normalize 状態を確認可能
- Stereo project で `Stereo Detune` と `Stereo Delay` による左右チャンネル差分を作成可能
- timeline toolbar で `1/4`, `1/8`, `1/16`, `1/32`, `1/64` の grid division を直接選択可能
- grid division を note snap、grid drawing、duplicate offset、common config の `defaultGridDivision` と同期
- Selection Inspector から `.gats-preset.json` の voice preset import / export を実行可能
- toolbar から built-in project template を選んで新規 project を作成可能
- README、日英マニュアル、ValidationChecklist、BOOTH 商品紹介文の更新

## 現在利用できる主な機能

- `.gats.json` ベースの音声プロジェクト保存 / 読み込み
- built-in project template からの新規作成
- タイムライン上でのノート配置、移動、長さ変更、複製、削除
- grid division の直接選択とスナップ / グリッド描画への反映
- note / track / project Inspector 編集
- Voice Preset import / export
- Stereo Detune / Stereo Delay
- Render Preview / Play / Pause / Stop / Rewind / Loop
- preview 波形表示
- 16-bit WAV 書き出しと書き出し長制御
- 8-bit WAV 変換
- 日本語 / 英語 / 中国語 UI
- Debug Mode と Log Level による診断ログ

## 同梱サンプル

- `Basic SE`
- `Simple Loop`

## 検証

- Unity `6000.4.0f1` / Windows で EditMode tests を実行し、`Total=117 Passed=117 Failed=0 Skipped=0` を確認済み

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
