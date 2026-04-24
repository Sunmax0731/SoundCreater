# Torus Edison

`Torus Edison` は、Unity Editor 上でゲーム向けの短い効果音やループ素材を試作するためのツールです。
音声プロジェクトを `.gats.json` 形式で保存し、Unity 上で内容を確認しながらプレビュー再生や WAV 書き出しを行えます。

GitHub 上のリポジトリ名は `SoundCreater` ですが、BOOTH や配布物での公開名は `Torus Edison` です。

## BOOTH ご購入者の方へ

この README は概要案内です。実際の導入手順や画面ごとの操作方法は、以下のドキュメントをご確認ください。

- GitHub Release 配布ページ: https://github.com/Sunmax0731/SoundCreater/releases
- 日本語マニュアル: [Manual.ja.md](Packages/com.sunmax.trusedison/Documentation~/Manual.ja.md)
- English manual: [Manual.md](Packages/com.sunmax.trusedison/Documentation~/Manual.md)
- 利用規約: [TermsOfUse.md](Packages/com.sunmax.trusedison/Documentation~/TermsOfUse.md)
- リリースノート: [ReleaseNotes.md](Packages/com.sunmax.trusedison/Documentation~/ReleaseNotes.md)
- 検証チェックリスト: [ValidationChecklist.md](Packages/com.sunmax.trusedison/Documentation~/ValidationChecklist.md)

## 主な機能

- `New / Open / Save / Save As` によるプロジェクト管理
- `.gats.json` 形式での保存と読み込み
- 起動ガイドと前回プロジェクト復元
- Edit 画面でのプレビュー生成、再生、波形確認
- タイムライン上でのノート作成、移動、長さ変更、複製、削除
- タイムラインの grid division 直接選択と `Grid` ボタンによる段階切り替え
- タイムライン下部の `+ Add Track` ボタンによるトラック追加
- Edit 画面と Settings 画面からの `Total Bars` 変更
- note / track / project 単位の Inspector 編集
- voice preset の import / export と built-in preset 適用
- Stereo project 向けの `Stereo Detune` / `Stereo Delay` による左右チャンネル差分
- 16-bit PCM WAV 書き出し
- `Project Bars` / `Seconds` / `Auto Trim` の書き出し長モード
- release / delay tail の include/cut と Export Quality 表示
- Unity に取り込んだ `AudioClip` の 8-bit WAV 変換
- 変換した WAV と同じフォルダへの変換用 `.gats.json` 自動生成
- `Tools/Torus Edison/Version & License` から開けるバージョン / ライセンス画面
- 日本語 / 英語 / 中国語 UI
- Debug Mode と Log Level による診断ログ出力

## 対応環境

- Unity `6000.0` 以降
- Windows 11 上の Unity Editor 利用
- オフライン利用

## 対象外機能

- 人声や歌声の生成
- MP3 書き出し
- オンライン連携
- ランタイム向け音声プレイヤー機能
- DAW のような本格的な音楽制作機能

## 最初に確認する資料

導入後は、まず次の順番で確認するのがおすすめです。

1. [日本語マニュアル](Packages/com.sunmax.trusedison/Documentation~/Manual.ja.md)
2. [English manual](Packages/com.sunmax.trusedison/Documentation~/Manual.md)
3. [利用規約](Packages/com.sunmax.trusedison/Documentation~/TermsOfUse.md)
4. [リリースノート](Packages/com.sunmax.trusedison/Documentation~/ReleaseNotes.md)
5. [検証チェックリスト](Packages/com.sunmax.trusedison/Documentation~/ValidationChecklist.md)

## ツールの起動

Unity プロジェクトを開いたあと、次のメニューから起動します。

- `Tools/Torus Edison/Open Editor`
- `Tools/Torus Edison/Version & License`

## サポート情報

問い合わせや不具合報告の際は、以下の情報があると切り分けしやすくなります。

- 使用している Unity バージョン
- Torus Edison のバージョン
- 読み込んだ `.gats.json` ファイル名
- 再現手順
- Unity Console に表示されたエラーや警告

補足:

- GitHub Release と BOOTH では、同一成果物を配布する前提です
- 同梱サンプルは `Basic SE`、`Simple Loop`、`UI Click`、`UI Confirm`、`UI Cancel`、`Coin Pickup`、`Power Up Rise`、`Laser Shot`、`Explosion Burst`、`Alarm Loop` の 10 本です

## 開発者向け情報

このリポジトリには、配布対象の Unity パッケージだけでなく、ローカル検証用の Unity ホストプロジェクトも含まれています。

- `Packages/com.sunmax.trusedison`
  Torus Edison 本体の Unity パッケージです。
- `Assets`, `Packages/manifest.json`, `ProjectSettings`
  ローカル検証用の Unity プロジェクトです。
- `game-audio-tool-docs`
  要件、仕様、実装ガイドの保管場所です。

参考ドキュメント:

- [requirements-definition-v0.3.md](game-audio-tool-docs/requirements-definition-v0.3.md)
- [specification-v0.1.md](game-audio-tool-docs/specification-v0.1.md)
- [Skill.md](game-audio-tool-docs/Skill.md)
- [Agents.md](game-audio-tool-docs/Agents.md)

自動検証:

```powershell
powershell -ExecutionPolicy Bypass -File tools\validation\run-editmode-tests.ps1
```

リリース成果物を作る場合は、`tools\release\build-release.ps1` が EditMode テストの結果 XML を確認してから梱包します。
