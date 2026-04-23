# Torus Edison

`Torus Edison` は、Unity Editor 上でゲーム向けの効果音やループ素材を試作し、音声プロジェクトとして保存、試聴、書き出しできるツールです。

この GitHub リポジトリ名は `SoundCreater` ですが、BOOTH や配布物での公開名は `Torus Edison` です。

## BOOTH ご購入者の方へ

この README は概要案内です。詳しい使い方や画面ごとの説明は、以下のドキュメントをご確認ください。

- 使い方マニュアル 日本語版: [Manual.ja.md](Packages/com.sunmax.trusedison/Documentation~/Manual.ja.md)
- 使い方マニュアル 英語版: [Manual.md](Packages/com.sunmax.trusedison/Documentation~/Manual.md)
- 利用規約: [TermsOfUse.md](Packages/com.sunmax.trusedison/Documentation~/TermsOfUse.md)
- リリースノート: [ReleaseNotes.md](Packages/com.sunmax.trusedison/Documentation~/ReleaseNotes.md)
- 検証チェックリスト: [ValidationChecklist.md](Packages/com.sunmax.trusedison/Documentation~/ValidationChecklist.md)

## Torus Edison でできること

- 音声プロジェクトの新規作成、保存、読み込み
- `.gats.json` 形式でのセッション管理
- タイムライン上でのノート作成、移動、リサイズ、複製、削除
- note / track / project の Inspector 編集
- `Render Preview / Play / Pause / Stop / Rewind / Loop` によるプレビュー再生
- WAV 書き出し
- Undo / Redo
- サンプルプロジェクトを使った動作確認

## 対応環境

- Unity `6000.0` 以降
- Windows 11 を前提とした Unity Editor 利用
- オフライン利用

## 現在の対象外機能

- 人声や歌声の生成
- MP3 書き出し
- オンライン連携
- ランタイム向け再生プレイヤー機能
- DAW 級の高度なミキサー機能

## 最初に確認する場所

導入後にまず確認したい情報は、以下の順番がおすすめです。

1. [使い方マニュアル 日本語版](Packages/com.sunmax.trusedison/Documentation~/Manual.ja.md)
2. [使い方マニュアル 英語版](Packages/com.sunmax.trusedison/Documentation~/Manual.md)
3. [利用規約](Packages/com.sunmax.trusedison/Documentation~/TermsOfUse.md)
4. [リリースノート](Packages/com.sunmax.trusedison/Documentation~/ReleaseNotes.md)
5. [検証チェックリスト](Packages/com.sunmax.trusedison/Documentation~/ValidationChecklist.md)

## ツールの起動

Unity プロジェクトを開き、パッケージの読み込みが終わったあとに次のメニューから起動します。

- `Tools/Torus Edison/Open Editor`

## サポートと補足情報

- リポジトリ名: `SoundCreater`
- ツール名: `Torus Edison`
- 公開や更新は GitHub Release / BOOTH を前提に進めています
- 同梱サンプルは `Basic SE` と `Simple Loop` です

不具合確認や問い合わせ時は、以下が分かると切り分けしやすくなります。

- 使用している Unity バージョン
- Torus Edison のバージョン
- 読み込んだ `.gats.json` ファイル名
- 再現手順
- Console に出たエラーや警告

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
