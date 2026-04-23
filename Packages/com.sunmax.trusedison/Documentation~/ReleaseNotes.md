# Release Notes

## 0.1.0

`Torus Edison` の初回配布候補版です。

主な内容:

- `.gats.json` によるプロジェクト保存と読込
- タイムライン上でのノート作成、移動、リサイズ、複製、削除
- note / track / project の Inspector 編集
- `Render Preview / Play / Pause / Stop / Rewind / Loop` のプレビュー再生
- WAV 書き出し
- Undo / Redo
- `Basic SE` と `Simple Loop` のサンプル同梱
- 日本語 / 英語マニュアル
- 利用規約、検証チェックリスト、CHANGELOG の整備

## 既知の制約

- 実 editor 上でのマウス操作感と `Assets/` 配下書き出しは、手動確認を推奨します
- UI の多言語切替はまだ未実装です
- デバッグモードとログレベル切替はまだ未実装です
- Unity batch `-runTests -testResults ...` は、この環境では exit code `0` でも XML を出力しない場合があります

## 関連ドキュメント

- [Manual.ja.md](Manual.ja.md)
- [Manual.md](Manual.md)
- [TermsOfUse.md](TermsOfUse.md)
- [ValidationChecklist.md](ValidationChecklist.md)
