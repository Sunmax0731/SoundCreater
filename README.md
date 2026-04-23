# Torus Edison

`Torus Edison` は、Unity Editor 上でゲーム向けの効果音やループ素材を試作し、音声プロジェクトとして保存しながら調整できるツールです。

この GitHub リポジトリ名は `SoundCreater` ですが、BOOTH や配布物での公開名は `Torus Edison` です。

## BOOTH ご購入者の方へ

この README は概要案内です。詳しい使い方や画面ごとの説明は、別途用意しているマニュアルをご確認ください。

- 使い方マニュアル 日本語版: [Manual.ja.md](Packages/com.sunmax.trusedison/Documentation~/Manual.ja.md)
- 使い方マニュアル 英語版: [Manual.md](Packages/com.sunmax.trusedison/Documentation~/Manual.md)

## Torus Edison でできること

- 音声プロジェクトの作成、保存、読み込み
- タイムライン上でのノート編集
- Unity Editor 上でのプレビュー再生
- `Play / Pause / Stop / Rewind / Loop` の再生操作
- WAV 書き出し
- Undo / Redo
- サンプルプロジェクトを使った動作確認

## 最初に確認する場所

導入後にまず確認したい情報は、以下の順番がおすすめです。

1. [使い方マニュアル 日本語版](Packages/com.sunmax.trusedison/Documentation~/Manual.ja.md)
2. [使い方マニュアル 英語版](Packages/com.sunmax.trusedison/Documentation~/Manual.md)
3. GitHub Releases
4. この README の「サポートと補足情報」

## ツールの起動

Unity プロジェクトを開き、パッケージの読み込みが終わったあとに次のメニューから起動します。

- `Tools/Torus Edison/Open Editor`

## サポートと補足情報

- リポジトリ名: `SoundCreater`
- ツール名: `Torus Edison`
- 公開や更新は GitHub Release / BOOTH を前提に進めています

不具合確認や問い合わせ時は、使用している Unity バージョン、Torus Edison のバージョン、再現手順が分かると切り分けしやすくなります。

## 開発者向け情報

このリポジトリには、配布対象の Unity パッケージだけでなく、ローカル検証用の Unity ホストプロジェクトも含まれています。

- `Packages/com.sunmax.trusedison`
  Torus Edison 本体の Unity パッケージです。
- `Assets`, `Packages/manifest.json`, `ProjectSettings`
  ローカル検証用の Unity プロジェクトです。
- `game-audio-tool-docs/game-audio-tool-docs`
  要件、仕様、実装ガイドの保管場所です。

参考ドキュメント:

- [requirements-definition-v0.3.md](game-audio-tool-docs/game-audio-tool-docs/requirements-definition-v0.3.md)
- [specification-v0.1.md](game-audio-tool-docs/game-audio-tool-docs/specification-v0.1.md)
- [Skill.md](game-audio-tool-docs/game-audio-tool-docs/Skill.md)
- [Agents.md](game-audio-tool-docs/game-audio-tool-docs/Agents.md)
