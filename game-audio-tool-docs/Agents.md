# Agents.md
## Additional Session Learnings

- `Torus Edison` では localization と diagnostic logging が基盤機能になった。新しい表示文言は `GameAudioLocalization`、新しい診断ログは `GameAudioDiagnosticLogger` を通す。
- 表示言語は日本語 / 英語 / 中国語の 3 言語対応とし、運用は `Auto + Override` を前提にする。Auto では editor language を優先し、必要なら system language に fallback する。
- Windows 上の Unity headless validation は、起動した `Unity` プロセスを待機してから `-logFile` と `-testResults` を確認する。呼び出し元が先に戻っても検証完了とはみなさない。
- full EditMode suite に無関係の既知 failure がある場合は、今回触った範囲の targeted fixture を先に使い、残りの failure は issue / PR に切り分けて記録する。
- 日本語や中国語が PowerShell 上で文字化けして見えても、それだけでファイルを書き換えない。UTF-8 ソースと Unity 上の表示が壊れていることを確認してから修正する。
- `CreateGUI()` などで UI ツリーを組み直す実装では、Inspector や表示キャッシュを明示的に無効化する。表示言語のように見た目へ影響する状態はキャッシュキーにも含める。
- タイムラインの footer や helper row のような補助領域は、通常の track hit-test から除外する。`trackCount` をそのまま配列 index として使う経路を残さない。
- release prep では、package version、`GameAudioToolInfo.ToolVersion`、sample の `toolVersion`、README / manual / release body / BOOTH 紹介文を同じ作業単位で更新する。版番号だけ先に進めない。
- Save / Save As / WAV export / 8-bit conversion でユーザー由来の名前をファイル名にする場合は、`GameAudioValidationUtility.SanitizeExportFileName` を通す。Windows 予約名、先頭末尾のドット、空白だけの名前もテスト対象に含める。
- ProjectSettings の `preferredSampleRate` / `preferredChannelMode` は New 作成時の既定値 override として扱う。既存 `.gats.json` 読み込み時はファイル内の `sampleRate` / `channelMode` を優先し、UI・Manual・仕様書の説明をこの契約に合わせる。
- 共通設定の `showStartupGuide` / `rememberLastProject` / `lastProjectPath` は起動時 UX の実動作契約として扱う。保存・読み込み・New・Settings UI を変更した場合は、復元対象のクリア条件と Manual / 仕様書も合わせて更新する。


## 目的

このファイルは、ゲーム内音声作成ツールの実装を進める際に、**人間の開発者やAIコーディングエージェントが役割分担しやすいようにするための運用ガイド**である。  
対象は Unity 6000 以降の Editor 拡張であり、MVPの範囲は `requirements-definition-v0.3.md` と `specification-v0.1.md` を正とする。

## まず守るべき前提

- 対象は **ボイス以外のゲーム内音声**
- 実行環境は **Unity Editor / Windows 11 / オフライン完結**
- MVPの正式保存形式は **JSON**
- 出力形式は **WAVのみ**
- **Undo / Redo は必須**
- 配布物には **UnityPackage / Samples / Manual / Terms** を含める
- GitHub Release と BOOTH には **同一成果物** を載せる

## ソースオブトゥルース

優先順位は次の通り。

1. `requirements-definition-v0.3.md`
2. `specification-v0.1.md`
3. `Skill.md`
4. この `Agents.md`

仕様と実装が衝突した場合は、まず仕様書を見直し、必要なら要件定義書まで遡って判断する。  
実装都合で勝手に仕様を変えない。

## 推奨エージェント構成

本プロジェクトでは、以下の役割に分けると進めやすい。

### 1. Requirements / Docs Agent

**責務**

- 要件定義書、仕様書、マニュアル、利用規約、リリースノートを管理する
- 実装変更が文書へ影響する場合、差分を明文化する
- GitHub Release / BOOTH掲載内容の整合性を確認する

**成果物**

- `requirements-definition-v0.3.md`
- `specification-v0.1.md`
- `Documentation~/Manual.md`
- `Documentation~/TermsOfUse.md`
- `CHANGELOG.md`

**完了条件**

- 実装と文書に矛盾がない
- 利用規約が購入前確認可能な形で提示できる

---

### 2. Editor UX Agent

**責務**

- EditorWindow
- ツールバー
- タイムライン描画
- ノート入力
- インスペクタ
- 再生カーソル表示
- ショートカット
- 通知UI

**重視すること**

- 置く / 動かす / 伸ばす の気持ちよさ
- 和音と複数トラックの扱いやすさ
- Undo / Redoしやすい操作単位

**成果物の例**

- `Editor/Windows/*`
- `Editor/Timeline/*`
- `Editor/Inspector/*`

**完了条件**

- 主要導線がマウス主体で成立する
- 数値入力補助がある
- 再生カーソルが視認しやすい

---

### 3. Audio Engine Agent

**責務**

- 波形生成
- ノイズ生成
- ADSR
- 音量、パン、ピッチ、フェード、ディレイ
- 複数トラックのミックスダウン
- 再現性担保

**成果物の例**

- `Editor/Audio/*`
- `Editor/Playback/*`
- `Editor/Export/*`

**重視すること**

- 再現性
- バッファ処理の明快さ
- テストしやすさ
- 乱数シード固定

**完了条件**

- 同一JSONから同一WAVが得られる
- 複数トラック・和音・Release・Delay tail が正しく反映される

---

### 4. Persistence / Config Agent

**責務**

- JSON保存 / 読込
- フォーマットバージョン管理
- 設定ファイル保存 / 読込
- データ移行戦略の入口を用意する
- エラーハンドリングとバリデーション

**成果物の例**

- `Editor/Persistence/*`
- `Editor/Config/*`

**重視すること**

- JSONの安定性
- バージョン互換性
- 壊れたデータへの安全な対応
- 不明フィールドや将来拡張への余地

**完了条件**

- Save / Open / Save As が安定して動く
- 不正データ読込時に安全に失敗できる

---

### 5. Command / Undo Agent

**責務**

- コマンドパターン実装
- Undo / Redo履歴
- 複数選択編集の一括コマンド化
- UI操作との橋渡し

**成果物の例**

- `Editor/Commands/*`
- `Editor/Application/*`

**重視すること**

- 履歴の一貫性
- 小さすぎず大きすぎない操作粒度
- 読込・保存・再生などUndo対象外の切り分け

**完了条件**

- 主要編集操作がすべてUndo / Redoで戻せる
- 履歴破損や二重適用がない

---

### 6. QA / Release Agent

**責務**

- 自動テスト
- 手動テストシナリオ
- サンプルファイル検証
- UnityPackage化
- GitHub Release梱包
- BOOTH出品物整合性確認

**成果物の例**

- `Tests/Editor/*`
- `Samples~/*`
- リリース用zip
- 配布チェックリスト

**重視すること**

- 実際の導入体験
- ドキュメント整合性
- サンプルの分かりやすさ
- 依存ライセンス確認

**完了条件**

- 主要シナリオが通る
- 配布物に不足がない
- 利用規約の事前確認導線がある

## エージェント間の作業順

推奨順は次の通り。

1. Requirements / Docs Agent が仕様を固定する
2. Persistence / Config Agent がデータモデルを固める
3. Audio Engine Agent がレンダリングコアを作る
4. Command / Undo Agent が編集基盤を作る
5. Editor UX Agent が画面へ接続する
6. QA / Release Agent が通し検証と梱包を行う

UIを先に作りたくなりやすいが、**保存形式・音声レンダリング・Undo粒度が先**のほうが崩れにくい。

## タスク着手前の共通チェック

各エージェントは作業前に必ず確認する。

- この変更は要件定義書のどの章に関係するか
- この変更は仕様書のどの節に関係するか
- JSON形式に影響するか
- Undo対象か
- テスト追加が必要か
- マニュアルや利用規約へ影響するか
- GitHub Release / BOOTH配布物へ影響するか

## 実装ルール

### 1. 小さく分けて進める

1つのタスクで同時にやりすぎない。  
例:

- OK: JSON保存実装 + テスト
- NG: JSON保存、UI、再生、書き出しを一度に全部

### 2. 先にモデルを決める

UIから直接仕様を決めない。  
特に `Track`, `Note`, `Voice`, `Effect`, `Config` は先にモデルを確定する。

### 3. 実装と同時にテストを書く

音声系は見た目で分かりにくいので、**必ず数値・長さ・構造で検証する**。

### 4. 壊れたデータに強くする

読込時は「落ちる」のではなく、**拒否する / 丸める / 警告する** を使い分ける。

### 5. 配布物まで含めてDoneにする

このプロジェクトでは、コードが動くだけでは不十分。  
サンプル、マニュアル、利用規約、Release構成まで見て初めて完了と考える。

## 変更の種類別オーナー

| 変更内容 | 主担当エージェント | 相談先 |
|----------|-------------------|--------|
| 要件変更 | Requirements / Docs | 全員 |
| 画面レイアウト | Editor UX | Command / Undo |
| ノート操作 | Editor UX | Command / Undo |
| 波形追加 | Audio Engine | Requirements / Docs |
| エフェクト追加 | Audio Engine | Editor UX |
| JSONスキーマ変更 | Persistence / Config | Requirements / Docs, QA |
| 書き出し形式変更 | Audio Engine | QA / Release |
| Undo仕様変更 | Command / Undo | Editor UX |
| サンプル更新 | QA / Release | Requirements / Docs |
| BOOTH説明更新 | Requirements / Docs | QA / Release |

## ブロッカーになりやすい論点

以下は早めに合意しておく。

- JSON schema versioning の扱い
- ノート/トラックIDの発行規則
- 乱数シードを保存データへ含めるか
- プロジェクト設定と共通設定の優先順位
- Undoの粒度
- Assets配下以外での出力時挙動
- エラー時の通知UI統一

## 各エージェントのDefinition of Done

### Requirements / Docs Agent
- 文書更新済み
- 実装との差分なし
- 配布文面まで確認済み

### Editor UX Agent
- 基本導線が触れる
- 視認性に問題がない
- Undoと矛盾しない

### Audio Engine Agent
- テストあり
- 再現性あり
- 既定値が仕様と一致

### Persistence / Config Agent
- 往復テストあり
- 破損データの失敗モードが安全
- バージョン情報あり

### Command / Undo Agent
- 主要編集操作を戻せる
- Redoで破綻しない
- 履歴上限での挙動が安定

### QA / Release Agent
- サンプルが動く
- Manual / Terms 同梱済み
- GitHub Release / BOOTH構成が一致

## 迷ったときの優先順位

1. 要件適合
2. 仕様適合
3. 再現性
4. Undo / Redo整合性
5. 配布しやすさ
6. UIの見た目

## してはいけないこと

- 実装都合で勝手に保存形式を変える
- 要件にないオンライン依存を足す
- Undoを後回しにして既成事実化する
- サンプルやマニュアルを後工程に押し込む
- MITと衝突するライセンス依存を無自覚に追加する
- BOOTH向けにGitHub Releaseと別物を配る

## 最後に

このプロジェクトは「音が出る」だけでなく、**配布可能で、再編集できて、他人が使えるツールに仕上げること**が重要である。  
各エージェントは、自分の担当範囲だけで閉じず、**保存・再現性・配布**まで見渡して動くこと。
