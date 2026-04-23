# Release Notes

## 0.1.1

`Torus Edison` の current main を反映した patch release です。

主な更新:

- 日本語 / 英語 / 中国語の UI 表示切替を追加
- `Auto + Override` の言語モードを追加
- Debug Mode と Log Level 切替による Unity Console 向け診断ログを追加
- release 文面、manual、validation checklist を現在の実装に合わせて更新

既知の制限:

- 実 editor 上でのマウス操作感と `Assets/` 配下書き出しは、引き続き手動確認を推奨します
- MP3 書き出し、オンライン連携、人声生成は対象外です
- Unity batch `-runTests -testResults ...` の結果確認は、起動した Unity プロセスの終了待機後に行ってください

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
