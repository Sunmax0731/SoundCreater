# ゲーム内音声作成ツール 仕様書 v0.1

## 1. 文書情報

- 文書名: ゲーム内音声作成ツール 仕様書
- 版数: v0.1
- 対応要件定義書: `requirements-definition-v0.3.md`
- 目的: 要件定義書 v0.3 に基づき、初期リリース(MVP)の具体仕様を定義する
- 対象成果物: Unity Editor 拡張、配布用UnityPackage、GitHub Release成果物、BOOTH掲載用成果物
- ライセンス前提: MIT

## 2. 仕様書の使い方

本書は、実装担当者・レビュー担当者・将来の保守担当者が同じ前提で作業できるように、初期リリースの**画面仕様・データ仕様・音声生成仕様・保存仕様・配布仕様**をまとめたものである。  
未実装や将来拡張の項目は明示し、MVPで実装すべき範囲を曖昧にしないことを主目的とする。

## 3. 前提・適用範囲

### 3.1 対象環境

| 項目 | 内容 |
|------|------|
| 実行環境 | Unity Editor |
| Unityバージョン | 6000以降 |
| 対応OS | Windows 11 |
| ネットワーク | 不要 |
| 動作形態 | オフライン完結 |

### 3.2 対象ユーザ

- Unity上でゲーム用の効果音・短いBGM・ジングルを作りたい開発者
- 外部DAWより軽量な制作フローを求める開発者
- GitHub ReleaseまたはBOOTHから配布物を取得して導入する利用者

### 3.3 対象外

以下は本仕様の対象外とする。

- 人声や歌声の生成
- MP3書き出し
- 立体音響の本格対応
- 範囲再生
- サンプル音源を中心としたリアル楽器再現
- 外部オンラインサービス連携

## 4. 用語

| 用語 | 定義 |
|------|------|
| プロジェクト | 本ツールで扱う1つの編集データ単位 |
| トラック | ノート列を格納する編集レーン |
| ノートブロック | タイムライン上に配置される1音の編集単位 |
| クリップ | 書き出しまたは再生用にレンダリングされた音声バッファ |
| ループ再生 | 指定範囲を繰り返し再生する動作。MVPではプロジェクト全体のみ |
| ADSR | Attack / Decay / Sustain / Release による音量エンベロープ |
| 共通設定 | ツール全体に共通する設定 |
| プロジェクト設定 | 開いているUnityプロジェクト単位で保持する設定 |

## 5. システム全体構成

### 5.1 構成レイヤ

本ツールは以下のレイヤ構成を前提とする。

1. **Editor UI Layer**  
   EditorWindow、Timeline描画、Inspector、ダイアログ、通知表示を担当する。

2. **Application Layer**  
   ユーザ操作をコマンドとして受け付け、Undo / Redo、保存、読込、レンダリング要求を制御する。

3. **Domain Layer**  
   Project / Track / Note / Envelope / Effect / ExportSetting 等のモデルを保持する。

4. **Audio Render Layer**  
   波形生成、ノイズ生成、ADSR適用、ディレイ適用、ミックスダウン、WAV出力用PCM生成を担当する。

5. **Persistence Layer**  
   JSON保存、JSON読込、設定ファイル保存、設定ファイル読込を担当する。

6. **Packaging Layer**  
   サンプルファイル、マニュアル、利用規約を含む配布物構成を管理する。

### 5.2 パッケージ構成(推奨)

```text
Packages/com.example.gameaudiotool/
  package.json
  Runtime/
    (MVPでは最小限。共通モデルのみ許容)
  Editor/
    Windows/
    Timeline/
    Inspector/
    Playback/
    Export/
    Persistence/
    Commands/
    Config/
  Samples~
    BasicSE/
    SimpleLoop/
  Documentation~
    Manual.md
    TermsOfUse.md
    ReleaseNotes.md
  Tests/
    Editor/
```

> MVPはEditor拡張が主体であるため、音声生成の実処理も原則 `Editor/` 配下で成立してよい。将来Runtime再生へ拡張する場合は `Runtime/` へ分離する。

## 6. メニューおよび起動仕様

### 6.1 起動経路

ツールはUnityメニューの以下から起動可能とする。

- `Tools/Game Audio Tool/Open Editor`

### 6.2 起動時の挙動

- 既存の未保存変更がない場合、新規プロジェクトまたは直前の編集中プロジェクトを開く
- 未保存変更がある状態で別プロジェクトを開こうとした場合、保存確認ダイアログを表示する
- 初回起動時はサンプルデータとマニュアルの場所を案内する

### 6.3 ウィンドウタイトル

- 表示名: `Game Audio Tool`
- 未保存変更がある場合はタイトル末尾に `*` を付与する

## 7. 画面仕様

### 7.1 画面全体レイアウト

メイン画面は以下の5領域で構成する。

1. **トップツールバー**
2. **トラック一覧・トラックヘッダ領域**
3. **タイムライン領域**
4. **インスペクタ領域**
5. **ステータスバー**

### 7.2 トップツールバー項目

| 項目 | 内容 |
|------|------|
| New | 新規プロジェクト作成 |
| Open | JSONプロジェクト読込 |
| Save | 現在プロジェクト保存 |
| Save As | 別名保存 |
| Export WAV | WAV書き出し |
| BPM | テンポ設定 |
| Time Signature | 拍子設定 |
| Grid | グリッド分解能設定 |
| Play | 再生 |
| Stop | 停止 |
| Loop | ループ再生ON/OFF |
| Cursor Jump | 再生カーソルを先頭へ移動 |

### 7.3 トラック領域

各トラックは以下を持つ。

| 項目 | 内容 |
|------|------|
| Track Name | トラック名 |
| Mute | ミュート切替 |
| Solo | ソロ切替 |
| Volume | トラック音量(dB) |
| Pan | トラックパン |
| Arm Edit | 編集対象としての強調表示 |
| Color | 視認性用色設定(任意) |

MVPでは**最大32トラック**まで生成可能とする。  
32を超えるデータを読み込んだ場合は警告を出し、読み込みを拒否する。

### 7.4 タイムライン領域

- 横軸は時間(Beat/Bar)
- 縦軸はトラック
- ノートブロックは矩形で表示する
- 選択中ノートはハイライトする
- 再生カーソルは縦線で表示する
- グリッドは拍・小節単位で表示する
- 同一トラック内に時間的に重なるノートの配置を許可する
- 重なったノートは描画上少し上下にずらさず、同一レーンに重ね表示してよい
- 必要に応じてズーム(横方向)を提供する

### 7.5 インスペクタ領域

選択対象に応じて以下を表示する。

#### 7.5.1 ノート選択時

- 基本情報  
  - Start Beat
  - Duration Beat
  - MIDI Note
  - Frequency(参考表示)
  - Velocity
- 波形  
  - Waveform Type
  - Pulse Width(該当時のみ)
  - Noise Enabled
  - Noise Type
  - Noise Mix
- ADSR  
  - Attack ms
  - Decay ms
  - Sustain level
  - Release ms
- エフェクト  
  - Volume dB
  - Pan
  - Pitch Semitone
  - Fade In ms
  - Fade Out ms
  - Delay Enabled
  - Delay Time ms
  - Delay Feedback
  - Delay Mix

#### 7.5.2 トラック選択時

- Track Name
- Mute
- Solo
- Volume dB
- Pan
- Default Waveform
- Default ADSR
- Default Effect Preset(任意)

#### 7.5.3 プロジェクト選択時

- Project Name
- BPM
- Time Signature
- Total Bars
- Master Gain dB
- Export Sample Rate
- Channel Mode
- Loop Enabled

### 7.6 ステータスバー

以下の情報を表示する。

- 現在の選択状態
- プロジェクト保存先
- 出力先フォルダ
- 最終再生・最終書き出し結果
- エラー/警告メッセージの簡易表示

## 8. 編集仕様

### 8.1 ノート作成

- タイムライン上でドラッグしてノートを作成する
- ノートはグリッドにスナップして配置する
- 作成直後に既定の長さを持つ
- 既定値は選択中トラックのデフォルト設定を継承する

### 8.2 ノート選択

- 単一クリックで単一選択
- ShiftまたはCtrl(Cmd相当)で複数選択
- 空白クリックで選択解除
- 複数選択時は共通編集可能な項目のみ一括変更を許可する

### 8.3 ノート移動

- ドラッグで開始位置を変更できる
- Shiftドラッグ時はグリッドスナップを一時解除してよい
- 他トラックへドラッグ移動できる
- 同一時刻への複数ノート配置を許可する

### 8.4 ノート長さ変更

- ノート左右端のドラッグで長さ変更
- 最小長は `1/64 note` 相当とする
- 長さ変更後に 0 以下となる操作は禁止する

### 8.5 数値入力補助

ノートはマウス操作に加え、インスペクタから以下を数値入力できる。

- Start Beat
- Duration Beat
- MIDI Note
- Frequency換算値(参考表示、編集時はMIDI値へ丸める)
- Velocity
- ADSR
- 各エフェクト値

### 8.6 和音表現

MVPでは和音専用オブジェクトは持たない。  
**同一または近接した開始位置に複数ノートを配置することで和音を表現する。**

### 8.7 複製と削除

- Ctrl+D で複製
- Delete / Backspace で削除
- 複製時は元ノートの右隣へグリッド単位で配置してよい
- 複数ノート複製に対応する

### 8.8 クオンタイズ

MVPでは常時グリッドスナップを標準動作とし、独立したクオンタイズ機能は持たない。  
将来拡張として、既存ノートの一括クオンタイズを追加できる構造にする。

## 9. 音源仕様

### 9.1 波形種別(MVP必須)

| 種別 | 説明 |
|------|------|
| Sine | 正弦波 |
| Square | 矩形波 |
| Triangle | 三角波 |
| Saw | ノコギリ波 |
| Pulse | 可変デューティ矩形波 |

### 9.2 波形パラメータ

| 項目 | 範囲 | 既定値 |
|------|------|--------|
| Pulse Width | 0.10 - 0.90 | 0.50 |
| Velocity | 0.0 - 1.0 | 0.80 |
| Master Gain dB | -24.0 - +6.0 | 0.0 |

### 9.3 ノイズ仕様

MVPで必須とするノイズ種別は**White Noise**とする。

| 項目 | 範囲 | 既定値 |
|------|------|--------|
| Noise Enabled | true / false | false |
| Noise Mix | 0.0 - 1.0 | 0.0 |

レンダリング時の音源合成は以下とする。

```text
source = waveform * (1 - noiseMix) + whiteNoise * noiseMix
```

## 10. ピッチ仕様

### 10.1 ピッチ表現

ノートの基準音程は `MIDI Note Number` で管理する。  
周波数は以下で算出する。

```text
frequency = 440.0 * 2 ^ ((midiNote - 69) / 12)
```

### 10.2 ピッチエフェクト

Pitch Semitone はノート単位で相対値を持ち、レンダリング時に基準MIDI Noteへ加算する。

- 範囲: `-24.0` から `+24.0`
- 既定値: `0.0`

## 11. ADSR仕様

### 11.1 パラメータ範囲

| 項目 | 範囲 | 既定値 |
|------|------|--------|
| Attack | 0 - 5000 ms | 5 ms |
| Decay | 0 - 5000 ms | 80 ms |
| Sustain | 0.0 - 1.0 | 0.70 |
| Release | 0 - 5000 ms | 120 ms |

### 11.2 適用ルール

- Attack は 0 から 1.0 へ線形増加
- Decay は 1.0 から Sustain 値へ線形減衰
- Sustain はノート本体長の終了まで維持
- Release はノート終了後に 0 へ線形減衰

### 11.3 例外処理

- Attack + Decay がノート本体長を超える場合、ノート本体長に収まるよう比率で圧縮する
- Release はノート本体長の外側に追加レンダリングする
- Sustain = 0 でも有効値として扱う

## 12. 簡易エフェクト仕様

### 12.1 適用順序

MVPでは以下の順で処理する。

1. 基本ピッチ決定
2. 波形 + ノイズ生成
3. ADSR適用
4. Fade In / Fade Out 適用
5. Note Volume 適用
6. Note Pan 適用
7. Delay 適用
8. Track Volume / Track Pan 適用
9. Master Gain 適用
10. Channel Mix / WAV書き出し

### 12.2 Volume

- 単位: dB
- 範囲: `-48.0` から `+6.0`
- 既定値: `0.0`

### 12.3 Pan

- 範囲: `-1.0` (L) から `+1.0` (R)
- 既定値: `0.0`
- モノラル出力時はPanを無視する

### 12.4 Fade In / Fade Out

| 項目 | 範囲 | 既定値 |
|------|------|--------|
| Fade In | 0 - 3000 ms | 0 |
| Fade Out | 0 - 3000 ms | 0 |

Fade In / Fade Out はADSRと独立して乗算する。

### 12.5 Delay

| 項目 | 範囲 | 既定値 |
|------|------|--------|
| Enabled | true / false | false |
| Time | 20 - 1000 ms | 180 |
| Feedback | 0.0 - 0.70 | 0.25 |
| Mix | 0.0 - 1.0 | 0.20 |

Delayはシンプルなフィードバックディレイとし、**ノート単位**で適用する。  
MVPではローパス付きディレイやステレオクロスディレイは対応しない。

## 13. トラック仕様

### 13.1 トラック既定値

新規作成トラックは以下を持つ。

| 項目 | 既定値 |
|------|--------|
| Name | Track 01, 02... |
| Mute | false |
| Solo | false |
| Volume dB | 0.0 |
| Pan | 0.0 |
| Default Waveform | Square |
| Default ADSR | A=5, D=80, S=0.7, R=120 |
| Color | 自動割当 |

### 13.2 ミュート・ソロ

- いずれかのトラックに Solo がある場合、Solo対象のみ再生対象とする
- Muteは常に再生対象外とする
- MuteとSoloが両方有効な場合、Muteを優先する

### 13.3 トラック数上限

- MVP上限: 32
- 32超の読込はエラー
- 32超の作成操作は不可

## 14. プロジェクト仕様

### 14.1 プロジェクト既定値

| 項目 | 既定値 |
|------|--------|
| Name | New Audio Project |
| BPM | 120 |
| Time Signature | 4/4 |
| Total Bars | 8 |
| Sample Rate | 48000 Hz |
| Channel Mode | Stereo |
| Master Gain dB | 0.0 |
| Loop | false |

### 14.2 長さ管理

- ノート配置範囲は `Total Bars` に基づく
- `Total Bars` を縮小して既存ノートがはみ出す場合は警告を出す
- 書き出し長は**最後の発音 + Release + Delay tail**まで自動延長する
- ループ再生時のみ再生長を `Total Bars` で固定する

### 14.3 拍子

MVPの拍子は以下に限定する。

- 4/4
- 3/4
- 6/8

将来拡張で任意拍子へ対応できるよう、内部表現は分子・分母を別フィールドで保持する。

## 15. 再生仕様

### 15.1 再生方式

- 再生開始時に現在プロジェクトからプレビュー用PCMバッファを生成する
- バッファを `AudioClip` 相当へ変換し、Editor内でプレビュー再生する
- 再生中は再生カーソルを更新する

### 15.2 ループ再生

- MVPではプロジェクト全体をループ対象とする
- 個別範囲ループは対応しない

### 15.3 再生カーソル更新

- 再生中はエディタ更新タイミングに合わせてカーソル位置を更新する
- 停止時は現在位置を維持する
- 先頭戻し操作時は 0 beat に移動する

### 15.4 プレビューの再生成

以下の場合はプレビュー再生成を行う。

- ノートの追加・削除・移動・長さ変更
- 波形・ADSR・エフェクト変更
- BPM・拍子・総小節数変更
- トラックミュート/ソロ変更

短い変更のたびに完全再生成してもよいが、将来は差分更新へ拡張可能な構造とする。

## 16. 保存・読込仕様

### 16.1 保存形式

- 保存形式: JSON
- 文字コード: UTF-8
- 改行: LF
- 拡張子: `.gats.json`  
  (`Game Audio Tool Session` の略称。内部識別しやすくするため)

### 16.2 フォーマットバージョン

JSONのルートには必ず以下を含める。

```json
{
  "formatVersion": "1.0.0",
  "toolVersion": "0.1.0",
  "project": {}
}
```

### 16.3 保存タイミング

- 明示的な Save / Save As 操作で保存する
- 自動保存はMVP対象外
- 保存成功時は通知を表示する
- 保存失敗時はエラー通知を表示する

### 16.4 読込ルール

- `formatVersion` が `1.x.x` の場合は読込許可
- メジャーバージョンが異なる場合は警告して読込拒否
- 不明フィールドは原則無視する
- 必須フィールド欠落時は読込拒否する

## 17. JSONデータ仕様

### 17.1 ルート構造

```json
{
  "formatVersion": "1.0.0",
  "toolVersion": "0.1.0",
  "project": {
    "id": "proj-001",
    "name": "Sample Loop",
    "bpm": 120,
    "timeSignature": { "numerator": 4, "denominator": 4 },
    "totalBars": 8,
    "sampleRate": 48000,
    "channelMode": "Stereo",
    "masterGainDb": 0.0,
    "loopPlayback": false,
    "tracks": []
  }
}
```

### 17.2 Track構造

```json
{
  "id": "track-001",
  "name": "Lead",
  "mute": false,
  "solo": false,
  "volumeDb": 0.0,
  "pan": 0.0,
  "defaultVoice": {
    "waveform": "Square",
    "pulseWidth": 0.5,
    "noiseEnabled": false,
    "noiseType": "White",
    "noiseMix": 0.0,
    "adsr": { "attackMs": 5, "decayMs": 80, "sustain": 0.7, "releaseMs": 120 },
    "effect": {
      "volumeDb": 0.0,
      "pan": 0.0,
      "pitchSemitone": 0.0,
      "fadeInMs": 0,
      "fadeOutMs": 0,
      "delay": { "enabled": false, "timeMs": 180, "feedback": 0.25, "mix": 0.2 }
    }
  },
  "notes": []
}
```

### 17.3 Note構造

```json
{
  "id": "note-001",
  "startBeat": 0.0,
  "durationBeat": 1.0,
  "midiNote": 60,
  "velocity": 0.8,
  "voiceOverride": {
    "waveform": "Square",
    "pulseWidth": 0.5,
    "noiseEnabled": false,
    "noiseType": "White",
    "noiseMix": 0.0,
    "adsr": { "attackMs": 5, "decayMs": 80, "sustain": 0.7, "releaseMs": 120 },
    "effect": {
      "volumeDb": 0.0,
      "pan": 0.0,
      "pitchSemitone": 0.0,
      "fadeInMs": 0,
      "fadeOutMs": 0,
      "delay": { "enabled": false, "timeMs": 180, "feedback": 0.25, "mix": 0.2 }
    }
  }
}
```

### 17.4 実装ルール

- `voiceOverride` は存在しない場合、トラックの `defaultVoice` を使用する
- `notes` は `startBeat` 昇順に保存する
- IDはUUID形式または一意文字列でよい
- 数値はJSON numberで保持する

## 18. WAV書き出し仕様

### 18.1 対応形式

| 項目 | 内容 |
|------|------|
| Container | RIFF/WAVE |
| PCM | 16-bit PCM |
| Sample Rate | 44100 または 48000 |
| Channel | Mono / Stereo |
| Endian | Little Endian |

既定値は `48000 Hz / Stereo / 16-bit PCM` とする。

### 18.2 書き出しファイル名

既定の書き出しファイル名は以下とする。

```text
{ProjectName}_{yyyyMMdd_HHmmss}.wav
```

禁止文字は `_` に置換する。

### 18.3 書き出し先

- 書き出し先はプロジェクト設定に保持する
- 共通設定側で既定出力先を持てる
- プロジェクト設定が存在する場合はプロジェクト設定を優先する
- 書き出し先が存在しない場合は作成を試みる
- 作成に失敗した場合はエラー表示する

### 18.4 Unityアセット自動リフレッシュ

- 出力先が `Assets/` 配下であり、かつ `autoRefreshAfterExport = true` の場合のみ `AssetDatabase.Refresh()` を呼ぶ
- `Assets/` 外へ出力した場合は自動リフレッシュを行わない

## 19. コンフィグ仕様

### 19.1 設定の優先順位

1. プロジェクト設定
2. 共通設定
3. ハードコード既定値

### 19.2 共通設定項目

| キー | 内容 | 既定値 |
|------|------|--------|
| defaultSampleRate | 既定サンプルレート | 48000 |
| defaultChannelMode | Mono / Stereo | Stereo |
| defaultExportDirectory | 既定出力先 | `ProjectRoot/Exports/Audio` |
| showStartupGuide | 初回ガイド表示 | true |
| rememberLastProject | 前回プロジェクト復元 | true |
| defaultGridDivision | グリッド分解能 | 1/16 |
| undoHistoryLimit | Undo保持上限 | 100 |

### 19.3 プロジェクト設定項目

| キー | 内容 | 既定値 |
|------|------|--------|
| exportDirectory | 当該プロジェクトの出力先 | 未設定 |
| autoRefreshAfterExport | Assets配下出力後の自動リフレッシュ | true |
| preferredSampleRate | 優先サンプルレート | 共通設定継承 |
| preferredChannelMode | 優先チャンネル | 共通設定継承 |

### 19.4 保存先

- 共通設定: `%AppData%/Local/GameAudioTool/config.json`
- プロジェクト設定: `ProjectSettings/GameAudioToolSettings.json`

> 将来、機密性やチーム共有方針に応じて `UserSettings/` へ逃がす拡張は可能。ただしMVPでは設定ファイルの発見しやすさを優先し `ProjectSettings/` を採用する。

## 20. Undo / Redo仕様

### 20.1 基本方針

- コマンドパターンで操作履歴を保持する
- Undo / Redo は少なくとも直近100操作まで保持する
- 保存操作自体はUndo対象外とする

### 20.2 Undo対象操作

- ノート追加
- ノート削除
- ノート移動
- ノート長変更
- ノート複製
- ノートプロパティ変更
- トラック追加/削除
- トラックプロパティ変更
- プロジェクト設定変更

### 20.3 Undo対象外

- 再生
- 停止
- エクスポート
- 読込
- マニュアル表示

## 21. エラー処理・バリデーション仕様

### 21.1 保存系

- 保存先が書込不可: エラー通知
- JSONシリアライズ失敗: エラー通知
- ファイル名不正: 自動補正 + 通知

### 21.2 読込系

- JSON構文不正: 読込拒否
- `formatVersion` 不整合: 警告 + 読込拒否
- トラック上限超過: 読込拒否
- 不明波形: 既定値へフォールバック + 警告
- 不明ノイズ種別: Whiteへフォールバック + 警告
- 範囲外数値: 有効範囲へ丸め + 警告

### 21.3 編集系

- 負の長さ入力は禁止
- MIDI Note は 0 - 127 に丸める
- Sustain は 0.0 - 1.0 に丸める
- Delay Feedback は 0.0 - 0.70 に丸める

## 22. ショートカット仕様

| 操作 | ショートカット |
|------|----------------|
| 新規 | Ctrl+N |
| 開く | Ctrl+O |
| 保存 | Ctrl+S |
| 別名保存 | Ctrl+Shift+S |
| 再生/停止切替 | Space |
| Undo | Ctrl+Z |
| Redo | Ctrl+Y |
| 複製 | Ctrl+D |
| 削除 | Delete |
| 先頭へ移動 | Home |

## 23. 性能仕様

MVPで達成目標とする性能基準を以下とする。

| 項目 | 目標 |
|------|------|
| 8小節・8トラック・128ノート程度のプレビュー再生成 | 1秒以内目標 |
| 1分程度のWAV書き出し | 実用上支障ない時間(目安3秒以内) |
| ノート移動・長さ変更時のUI応答 | 100ms以内目標 |
| 保存・読込 | 1秒以内目標 |

> いずれも厳密なリアルタイム保証ではなく、一般的なWindows 11 開発PCでの実用水準を目標とする。

## 24. 再現性仕様

- 同一JSON、同一サンプルレート、同一チャンネル設定からのWAV書き出し結果は同一であること
- 乱数を使用するWhite Noiseはノート単位のシード値を内部保持し、再生成時に同じ波形を得る
- ノートの乱数シードは保存データへ含めてもよい

## 25. テスト仕様

### 25.1 自動テスト

- JSON保存/読込の往復テスト
- WAVヘッダ検証テスト
- ADSR適用結果テスト
- Delay出力長テスト
- Undo / Redo整合性テスト
- フォーマットバージョン互換テスト

### 25.2 手動テスト

- 新規作成から書き出しまでの基本導線
- 和音入力
- 複数トラックの同時再生
- Mute / Solo挙動
- Assets配下出力時の自動リフレッシュ
- JSON読込失敗時のエラー表示
- サンプルファイルの動作確認

## 26. 配布仕様

### 26.1 GitHub Release成果物

GitHub Releaseには少なくとも以下を含める。

```text
GameAudioTool_vX.Y.Z.zip
  /UnityPackage/GameAudioTool.unitypackage
  /Samples/basic-se.gats.json
  /Samples/simple-loop.gats.json
  /Manual/Manual.md
  /Terms/TermsOfUse.md
  /LICENSE
  /CHANGELOG.md
```

### 26.2 BOOTH出品物

- BOOTHにはGitHub Release成果物と同一内容の成果物を出品する
- 商品説明欄または外部公開URLで、利用規約を購入前に確認できる状態にする
- BOOTH用説明文には、最低でも対応Unityバージョン、対応OS、対象外機能、ライセンスを記載する

### 26.3 サンプルファイル要件

最低2種を含める。

1. **Basic SE Sample**  
   単音 + ノイズ + ADSR + Delay を確認できるサンプル

2. **Simple Loop Sample**  
   複数トラック + 和音 + ループ再生を確認できるサンプル

### 26.4 マニュアル要件

Manual.md は以下の章を持つ。

- 概要
- 導入手順
- 起動方法
- 画面説明
- 基本操作
- 保存と読込
- WAV書き出し
- コンフィグ
- FAQ
- 制約事項
- ライセンス・サポート

### 26.5 利用規約要件

TermsOfUse.md は以下を最低含む。

- 利用許諾
- 禁止事項
- 免責
- 再配布条件
- サポート方針
- 規約変更
- 問い合わせ先記載方針

## 27. 将来拡張のための設計指針

MVP実装時点で以下の拡張余地を残す。

- JSON schema versioning と migration
- XML import/export アダプタ追加余地
- 追加波形・追加ノイズ種別
- サンプラー方式や外部音源対応
- 範囲再生
- 立体音響
- より高度なエフェクトチェーン
- ノート以外にクリップ単位オブジェクトを置く拡張
- 他OS対応

## 28. 受け入れ基準

以下を満たしたとき、MVP仕様を満たしたものとみなす。

1. EditorWindow からツールを起動できる  
2. 新規プロジェクト作成・保存・読込ができる  
3. 複数トラックにノートブロックを配置できる  
4. 同時発音と和音を表現できる  
5. 波形 + ノイズ + ADSR + 簡易エフェクトが反映される  
6. 単発再生とループ再生ができる  
7. 再生カーソルが表示される  
8. WAVを書き出せる  
9. モノラル / ステレオを切り替えられる  
10. 出力先と自動リフレッシュをコンフィグで管理できる  
11. Undo / Redo が機能する  
12. サンプルファイル、マニュアル、利用規約を配布物へ含められる  
13. GitHub ReleaseとBOOTHへ同一成果物を展開できる構成になっている  

## 付録A. JSONサンプル

```json
{
  "formatVersion": "1.0.0",
  "toolVersion": "0.1.0",
  "project": {
    "id": "proj-sample-001",
    "name": "SimpleLoop",
    "bpm": 120,
    "timeSignature": { "numerator": 4, "denominator": 4 },
    "totalBars": 4,
    "sampleRate": 48000,
    "channelMode": "Stereo",
    "masterGainDb": 0.0,
    "loopPlayback": true,
    "tracks": [
      {
        "id": "track-lead",
        "name": "Lead",
        "mute": false,
        "solo": false,
        "volumeDb": 0.0,
        "pan": 0.0,
        "defaultVoice": {
          "waveform": "Square",
          "pulseWidth": 0.5,
          "noiseEnabled": false,
          "noiseType": "White",
          "noiseMix": 0.0,
          "adsr": { "attackMs": 5, "decayMs": 80, "sustain": 0.7, "releaseMs": 120 },
          "effect": {
            "volumeDb": 0.0,
            "pan": 0.0,
            "pitchSemitone": 0.0,
            "fadeInMs": 0,
            "fadeOutMs": 0,
            "delay": { "enabled": false, "timeMs": 180, "feedback": 0.25, "mix": 0.2 }
          }
        },
        "notes": [
          {
            "id": "lead-001",
            "startBeat": 0.0,
            "durationBeat": 1.0,
            "midiNote": 60,
            "velocity": 0.8
          },
          {
            "id": "lead-002",
            "startBeat": 1.0,
            "durationBeat": 1.0,
            "midiNote": 64,
            "velocity": 0.8
          }
        ]
      },
      {
        "id": "track-chord",
        "name": "Chord",
        "mute": false,
        "solo": false,
        "volumeDb": -3.0,
        "pan": -0.1,
        "defaultVoice": {
          "waveform": "Triangle",
          "pulseWidth": 0.5,
          "noiseEnabled": false,
          "noiseType": "White",
          "noiseMix": 0.0,
          "adsr": { "attackMs": 10, "decayMs": 120, "sustain": 0.6, "releaseMs": 180 },
          "effect": {
            "volumeDb": 0.0,
            "pan": 0.0,
            "pitchSemitone": 0.0,
            "fadeInMs": 0,
            "fadeOutMs": 0,
            "delay": { "enabled": true, "timeMs": 220, "feedback": 0.2, "mix": 0.15 }
          }
        },
        "notes": [
          { "id": "chord-001", "startBeat": 0.0, "durationBeat": 2.0, "midiNote": 48, "velocity": 0.7 },
          { "id": "chord-002", "startBeat": 0.0, "durationBeat": 2.0, "midiNote": 52, "velocity": 0.7 },
          { "id": "chord-003", "startBeat": 0.0, "durationBeat": 2.0, "midiNote": 55, "velocity": 0.7 }
        ]
      }
    ]
  }
}
```
