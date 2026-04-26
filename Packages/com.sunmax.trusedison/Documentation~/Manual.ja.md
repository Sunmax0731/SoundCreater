# Torus Edison マニュアル

英語版: [Manual.md](Manual.md)

関連ドキュメント:

- [TermsOfUse.md](TermsOfUse.md)
- [ReleaseNotes.md](ReleaseNotes.md)
- [ValidationChecklist.md](ValidationChecklist.md)

## 概要

`Torus Edison` は、Unity Editor 上でゲーム向けの短い効果音やループ素材を試作するためのエディタ拡張です。
プロジェクトデータを `.gats.json` で保存し、編集、試聴、書き出しまでを Unity 内で完結できます。

現在の実装範囲:

- `New / Open / Save / Save As`
- `.gats.json` の保存と読み込み
- タイムライン上でのノート編集
- note / track / project の Inspector 編集
- プレビュー生成と再生
- 16-bit WAV 書き出し
- 8-bit WAV 変換
- Undo / Redo
- 検証用サンプル同梱

## 対応環境

- Unity `6000.0` 以降
- Windows 上の Unity Editor 利用
- オフライン利用

## 起動方法

次のメニューから起動します。

- `Tools/Torus Edison/メイン画面`
- `Tools/Torus Edison/ライセンス`
- `Tools/Torus Edison/バージョン情報`
- `Tools/Torus Edison/Utilities/8-bit WAV Converter`

## 画面構成

現在のエディタウィンドウは、上部タブで 4 つの画面に分かれています。

### File

この画面では次の操作を行います。

- 現在のプロジェクト状態と保存先の確認
- 新規作成
- built-in template からの新規 project 作成
- 読み込み
- 保存
- ローカルサンプルの作成
- `Basic SE` と `Simple Loop` のクイック読み込み
- `Create Samples` による 10 本の同梱サンプル展開

### Edit

この画面では、編集とプレビュー確認をまとめて行います。

- プレビュー生成と再生操作
- 生成済みプレビューの波形表示
- `128 bars` までのプロジェクト長設定
- タイムライン上でのノート作成
- ノート移動
- ノートの長さ変更
- ノート複製と削除
- タイムライン下部の `+ Add Track` ボタンからのトラック追加
- note / track / project の Inspector 編集
- グリッド分解能をプルダウンで直接選択、または `Grid` ボタンで段階切り替え
- `?` ボタンから開く操作ヘルプウィンドウ

主なショートカット:

- `Ctrl+N` 新規プロジェクト
- `Ctrl+O` プロジェクトを開く
- `Ctrl+S` 保存
- `Ctrl+Shift+S` 名前を付けて保存
- `Space` プレビュー再生 / 一時停止
- `Ctrl+D` 選択ノート複製
- `Delete` 選択ノート削除
- `Ctrl+Z` Undo
- `Ctrl+Y` Redo

### Export

この画面では、現在の `.gats.json` プロジェクトからの WAV 書き出しを行います。

利用できる操作:

- `Export WAV`
- `Open Export Folder`
- 書き出し長モード: `Project Bars` / `Seconds` / `Auto Trim`
- 書き出し目標長を超える release / delay tail の含める・切る設定
- 共通既定フォルダ設定
- プロジェクト上書きフォルダ設定
- `Assets/` 配下書き出し時の自動リフレッシュ設定

### Settings

この画面では、プロジェクト設定とツール設定を扱います。

- BPM
- Total Bars
- 現在のプロジェクトの Sample Rate
- 現在のプロジェクトの Channel Mode
- 新規プロジェクト用 Sample Rate 上書き
- 新規プロジェクト用 Channel Mode 上書き
- 表示言語
- 起動ガイド表示の切り替え
- 前回プロジェクト記憶の切り替えと記憶中パスの確認
- Debug Mode / Log Level
- Foundation Status
- 読み込み時の警告確認

## 保存と読み込み

- `New`
  新しいプロジェクトを作成します。
- `New From Template`
  選択中の built-in template から新しいプロジェクトを作成します。既存の `New` は空のプロジェクト作成として維持します。
- `Open`
  `.gats.json` プロジェクトファイルを読み込みます。形式が不正な場合は読み込みを拒否します。
- `Save`
  現在のプロジェクトを保存します。
- `Save As`
  別名保存します。保存時は `.gats.json` 拡張子に正規化されます。

標準のセッション形式は `.gats.json` です。
前回プロジェクト記憶が有効な場合、次回エディタウィンドウ起動時に最後に保存または読み込みした `.gats.json` を復元します。記憶中のファイルが存在しない場合はパスをクリアし、新規プロジェクトを作成します。

## Project Templates

toolbar には `Template` selector と `New From Template` があります。built-in template は project name、BPM、bars、loop mode、track name、default voice、starter notes、export settings の初期値を持ちます。

現在の built-in template:

- `UI / UI Click`
- `Pickup / Coin Pickup`
- `Impact / Explosion`
- `Action / Laser Shot`
- `Loop / Simple Loop`

通常の `New` は従来通り空の project を作成します。この release では template 定義は code-defined built-in として提供します。将来の custom template 用に `%LocalAppData%/GameAudioTool/project-templates` と `.gats-template.json`、`kind: "torusEdison.projectTemplate"`、`templateFormatVersion: "1.0.0"` を予約しています。

## タイムライン編集

現在対応している主な編集操作:

- 空レーン上のドラッグによるノート作成
- ノートドラッグによる移動
- ノート端ドラッグによる長さ変更
- 複数選択を前提にした編集
- Inspector からの pitch や velocity の変更
- タイムライン下部ボタンからのトラック追加
- Selection Inspector、またはノート未選択時の `Delete` / `Backspace` による選択トラック削除
- Edit 画面の `Bars` 入力欄、`-` / `+` ボタン、Settings の `Total Bars` スライダー、またはタイムライン右端ドラッグによるプロジェクト長変更
- Edit 画面のグリッド分解能プルダウンまたは `Grid` ボタンによる分解能変更。ノートのスナップ、複製位置、グリッド描画は選択中の分解能に従います。
- stereo voice effect: `Stereo Detune` は左右の pitch を分け、`Stereo Delay` は右チャンネルを遅らせます。
- 主要編集操作に対する Undo / Redo

プロジェクトには少なくとも 1 トラックを残します。最後の 1 トラックは削除できません。選択トラックにノートが含まれる場合は、トラックとノートを削除する前に確認ダイアログが表示されます。トラック削除は Undo / Redo の対象です。

Bars を短くしても既存ノートは削除または移動されません。現在の Bars 範囲外になったノートはプロジェクトデータに残り、Bars を再度伸ばすまで preview / export の対象外として扱われます。

## プレビュー再生

プレビュー再生では、現在のプロジェクト内容からオフライン音声バッファを生成し、Unity Editor の preview 経路で再生します。

現在の実装内容:

- 波形生成
- White Noise
- ADSR
- Delay
- Project Channel Mode が `Stereo` のときの stereo detune と右チャンネル stereo delay
- トラック / プロジェクトの mixdown
- プレビュー波形表示
- `Render Preview / Play / Pause / Stop / Rewind / Loop`

## WAV 書き出し

現在の書き出し仕様:

- 16-bit PCM `.wav`
- `48000` / `44100` Hz
- Mono / Stereo
- ファイル名禁止文字の補正
- 出力先フォルダ自動作成
- `Assets/` 配下書き出し時の `AssetDatabase.Refresh()`
- プロジェクト長と書き出し長を分離します。`Project Bars` はタイムライン長、`Seconds` は指定秒数、`Auto Trim` は最後のノート本体末尾で書き出します。
- release / delay tail は、目標長を超えて含めるか、目標長で切るかを選べます。
- Export Quality は、peak、source peak、目標長、実際の出力長、project length、tail length、normalize 状態、前回書き出しとの差分を表示します。

Stereo の扱い:

- `Track Pan` と voice `Pan` は従来通り左右の音量差を作る pan 設定です。
- voice `Stereo Detune` は左右チャンネルで異なる pitch の音声内容を作ります。
- voice `Stereo Delay` は左に対して右チャンネルを遅らせます。
- Project Channel Mode が `Mono` の場合、stereo detune と stereo delay は無視され、単一の mono 信号としてレンダリングされます。

## 8-bit WAV 変換

Unity に取り込んだ `AudioClip` アセットを 8-bit PCM `.wav` に変換する場合は、`Tools/Torus Edison/Utilities/8-bit WAV Converter` を開きます。

- 変換元 `AudioClip` を選択
- 出力名を指定
- 出力先フォルダを指定
- 変換後サンプルレートを指定
- `Preserve Source` / `Mono` / `Stereo` を選択
- 8-bit PCM `.wav` として書き出し
- 同じフォルダへ変換用 `.gats.json` を自動生成

この機能は、すでに Unity に取り込んだ音声を変換するためのものです。YouTube など外部サービスから音源を取得する機能は含みません。

## Voice Presets

Edit 画面の Selection Inspector では、voice preset browser と preset file を扱えます。

- トラックヘッダーを選択すると、preset を track default voice に適用できます。
- 1 つ以上のノートを選択すると、preset を note voice override として適用できます。
- preset は name / id / category / tag / description / waveform / noise / delay metadata で検索できます。
- category または tag で preset を絞り込めます。
- 最近使用した preset は Recent Presets から選択できます。
- 選択ノートに voice override がない場合でも、preset import / apply 時に override を作成します。
- `%LocalAppData%/GameAudioTool/voice-presets` 配下の `.gats-preset.json` は user preset として browser に表示されます。
- 破損または未対応の user preset file は warning として表示され、built-in preset の利用は継続できます。
- `Import Preset` は `.gats-preset.json` を読み込み、現在の note override または track default voice へ直接適用します。
- `Export Current Voice` は現在編集中の voice を共有用 `.gats-preset.json` として保存します。
- browser の search / category・tag filter / recent preset keys は common config に保存されます。
- export 先に既存 file がある場合は上書き確認を行います。
- preset の適用は Undo / Redo の対象です。

preset file は次の schema を使います。

```json
{
  "kind": "torusEdison.voicePreset",
  "presetFormatVersion": "1.0.0",
  "toolVersion": "0.3.0",
  "preset": {
    "id": "team.ui-click",
    "category": "UI",
    "tags": ["button", "menu", "short"],
    "displayName": "Team UI Click",
    "description": "Short reusable menu click.",
    "voice": {
      "waveform": "Square",
      "pulseWidth": 0.35,
      "noiseEnabled": false,
      "noiseType": "White",
      "noiseMix": 0.0,
      "adsr": { "attackMs": 0, "decayMs": 35, "sustain": 0.15, "releaseMs": 45 },
      "effect": {
        "volumeDb": -4.0,
        "pan": 0.0,
        "pitchSemitone": 12.0,
        "stereoDetuneSemitone": 0.0,
        "stereoDelayMs": 0,
        "fadeInMs": 0,
        "fadeOutMs": 25,
        "delay": { "enabled": false, "timeMs": 180, "feedback": 0.25, "mix": 0.2 }
      }
    }
  }
}
```

共有用 preset file には `.gats-preset.json` 拡張子を使います。user preset の既定場所は `%LocalAppData%/GameAudioTool/voice-presets` です。
`presetFormatVersion` は `1.x.x` を読み込み対象とし、異なる major version は拒否します。`toolVersion` は互換性 warning の対象ですが、単独では import を止めません。

## バージョン / ライセンス画面

`Tools/Torus Edison/バージョン情報` から現在のバージョンと配布元情報を確認できます。`Tools/Torus Edison/ライセンス` から MIT License の概要と同梱 `LICENSE.md` の場所を確認できます。

## 設定ファイル

現在の設定ファイル:

- 共通設定: `%LocalAppData%/GameAudioTool/config.json`
- プロジェクト設定: `ProjectSettings/GameAudioToolSettings.json`

共通設定には、起動ガイド表示、前回プロジェクト記憶、前回プロジェクトパス、既定の書き出し先、表示言語、診断ログ、基準となる音声既定値が保存されます。
プロジェクト設定は、サンプルレート、チャンネルモード、出力先、`Assets/` 自動リフレッシュ設定などで共通設定より優先されます。
サンプルレートとチャンネルモードの上書きは、`New` で新規プロジェクトを作るときに使われます。既存の `.gats.json` を開いた場合は、ファイル内に保存されたサンプルレートとチャンネルモードが使われます。

## サンプル

同梱サンプル:

- `BasicSE/basic-se.gats.json`
- `SimpleLoop/simple-loop.gats.json`
- `UIClick/ui-click.gats.json`
- `UIConfirm/ui-confirm.gats.json`
- `UICancel/ui-cancel.gats.json`
- `CoinPickup/coin-pickup.gats.json`
- `PowerUpRise/power-up-rise.gats.json`
- `LaserShot/laser-shot.gats.json`
- `ExplosionBurst/explosion-burst.gats.json`
- `AlarmLoop/alarm-loop.gats.json`

サンプル内容と確認手順:

- [Samples~/README.md](../Samples~/README.md)
- [ValidationChecklist.md](ValidationChecklist.md)

## 既知の制約

- マウス操作の感触は、実際の Unity Editor 上での確認を推奨します。
- `Assets/` 配下書き出し時の挙動は、実際の Unity Editor 上での確認を推奨します。
- UI 表示言語は日本語 / 英語 / 中国語に対応しています。
- 診断ログは Unity Console を使ったローカルサポート用途を前提にしています。

## 補足

このマニュアルは、現在の実装状態に合わせて更新しています。今後の機能追加に応じて内容も追従します。
