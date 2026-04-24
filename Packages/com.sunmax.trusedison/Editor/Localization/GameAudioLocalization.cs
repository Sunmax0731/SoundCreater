using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reflection;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace TorusEdison.Editor.Localization
{
    internal static class GameAudioLocalization
    {
        private static readonly IReadOnlyList<GameAudioLanguageMode> SupportedLanguageModes = new[]
        {
            GameAudioLanguageMode.Auto,
            GameAudioLanguageMode.Japanese,
            GameAudioLanguageMode.English,
            GameAudioLanguageMode.Chinese
        };

        private static readonly IReadOnlyDictionary<string, string> English = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["language.auto"] = "Auto",
            ["language.japanese"] = "Japanese",
            ["language.english"] = "English",
            ["language.chinese"] = "Chinese",
            ["logLevel.error"] = "Error",
            ["logLevel.warning"] = "Warning",
            ["logLevel.info"] = "Info",
            ["logLevel.verbose"] = "Verbose",
            ["audio.mono"] = "Mono",
            ["audio.stereo"] = "Stereo",
            ["channel.mono"] = "Mono",
            ["channel.stereo"] = "Stereo",
            ["settings.inheritCommonDefault"] = "Use Common Default",
            ["inspector.projectDefaults"] = "New Project Defaults",
            ["inspector.projectDefault.sampleRate"] = "Sample Rate Override",
            ["inspector.projectDefault.channelMode"] = "Channel Mode Override",
            ["inspector.projectDefaults.help"] = "These project settings override common defaults when New creates a project. Use Common Default keeps the shared setting in control.",
            ["waveform.sine"] = "Sine",
            ["waveform.square"] = "Square",
            ["waveform.triangle"] = "Triangle",
            ["waveform.saw"] = "Saw",
            ["waveform.pulse"] = "Pulse",
            ["noise.white"] = "White"
        };

        private static readonly IReadOnlyDictionary<string, string> Japanese = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["language.auto"] = "自動",
            ["language.japanese"] = "日本語",
            ["language.english"] = "英語",
            ["language.chinese"] = "中国語",
            ["audio.mono"] = "モノラル",
            ["audio.stereo"] = "ステレオ",
            ["channel.mono"] = "モノラル",
            ["channel.stereo"] = "ステレオ",
            ["waveform.sine"] = "サイン波",
            ["waveform.square"] = "矩形波",
            ["waveform.triangle"] = "三角波",
            ["waveform.saw"] = "ノコギリ波",
            ["waveform.pulse"] = "パルス波",
            ["noise.white"] = "ホワイト",
            ["toolbar.new"] = "新規",
            ["toolbar.open"] = "開く",
            ["toolbar.save"] = "保存",
            ["toolbar.saveAs"] = "名前を付けて保存",
            ["workspace.file"] = "ファイル",
            ["workspace.edit"] = "編集",
            ["workspace.preview"] = "プレビュー",
            ["workspace.export"] = "書き出し",
            ["workspace.settings"] = "設定",
            ["page.file.title"] = "ファイル",
            ["page.file.description"] = "プロジェクトファイル、現在の状態、サンプル操作を確認します。",
            ["page.edit.title"] = "編集",
            ["page.edit.description"] = "タイムライン編集、プレビュー再生、選択中のノートやトラックの編集を同じ画面で行います。",
            ["page.preview.title"] = "プレビュー",
            ["page.preview.description"] = "エディタ内で現在のプロジェクトをレンダリングし、試聴します。",
            ["page.export.title"] = "書き出し",
            ["page.export.description"] = "WAV を書き出し、現在の出力先を確認します。",
            ["page.settings.title"] = "設定",
            ["page.settings.description"] = "プロジェクト設定と基盤の診断情報を確認します。",
            ["summary.currentProject"] = "現在のプロジェクト",
            ["summary.name"] = "名前",
            ["summary.bpm"] = "BPM",
            ["summary.bars"] = "小節数",
            ["summary.tracks"] = "トラック数",
            ["summary.file"] = "ファイル",
            ["summary.status"] = "状態",
            ["timeline.title"] = "タイムライン編集",
            ["timeline.undo"] = "元に戻す",
            ["timeline.redo"] = "やり直す",
            ["timeline.grid"] = "グリッド {0}",
            ["timeline.help.title"] = "タイムライン操作ヘルプ",
            ["timeline.help.summary"] = "タイムラインのマウス操作やショートカットを確認したいときに開いてください。",
            ["timeline.help.empty"] = "エディタ UI の準備ができると、ここに操作方法が表示されます。",
            ["timeline.help.close"] = "閉じる",
            ["timeline.barLabel"] = "Bar {0:00}",
            ["timeline.trackInfo"] = "ノート {0} / Pan {1:0.00}",
            ["timeline.noteLabel"] = "MIDI {0}",
            ["selectionInspector.title"] = "選択インスペクター",
            ["projectInspector.title"] = "プロジェクトインスペクター",
            ["preview.title"] = "プレビュー再生",
            ["preview.render"] = "プレビュー生成",
            ["preview.play"] = "再生",
            ["preview.pause"] = "一時停止",
            ["preview.stop"] = "停止",
            ["preview.rewind"] = "巻き戻し",
            ["preview.loop"] = "ループ",
            ["preview.key.preview"] = "プレビュー",
            ["preview.key.buffer"] = "バッファ",
            ["preview.key.cursor"] = "カーソル",
            ["preview.cursorNotStarted"] = "カーソル未開始",
            ["preview.waveform.empty"] = "プレビューを生成すると波形が表示されます。",
            ["preview.waveform.silent"] = "プレビューバッファは無音です。",
            ["export.title"] = "WAV 書き出し",
            ["export.exportWav"] = "WAV 書き出し",
            ["export.openFolder"] = "書き出し先を開く",
            ["export.resolvedFolder"] = "解決後フォルダ",
            ["export.exportFile"] = "出力ファイル",
            ["export.lastExport"] = "前回の書き出し",
            ["export.commonDefaultFolder"] = "共通の既定フォルダ",
            ["export.projectOverrideFolder"] = "プロジェクト別上書きフォルダ",
            ["export.autoRefresh"] = "AssetDatabase を自動更新",
            ["export.browse"] = "参照",
            ["export.useProjectFolder"] = "Project/Exports",
            ["export.useAssetsFolder"] = "Assets/Exports",
            ["export.clearOverride"] = "クリア",
            ["export.folderHelp"] = "この Unity プロジェクト内のフォルダは相対パスとして保存され、外部フォルダは絶対パスのまま保持されます。プロジェクト直下に出したい場合は Project/Exports、Unity に WAV を再読み込みさせたい場合は Assets/Exports を使ってください。",
            ["sample.title"] = "サンプルとワークフロー",
            ["sample.create"] = "サンプル作成",
            ["sample.loadBasic"] = "Basic SE を読み込む",
            ["sample.loadLoop"] = "Simple Loop を読み込む",
            ["sample.openFolder"] = "フォルダを開く",
            ["sample.location"] = "サンプルファイルの保存先: {0}",
            ["sample.editing"] = "タイムライン編集とインスペクター編集に対応しました。編集タブからノートの作成、移動、リサイズ、ノートやトラックの調整を行えます。",
            ["sample.json"] = "JSON は一括編集、レビュー、バージョン管理に引き続き有効ですが、ファイル操作、編集とプレビュー、書き出し、設定は役割ごとに整理されています。",
            ["info.title"] = "基盤ステータス",
            ["info.currentScope"] = "このウィンドウはファイル、編集とプレビュー、書き出し、設定を分けつつ、同じプロジェクト状態、選択状態、再生、Undo / Redo、JSON 保存 / 読み込み、WAV 書き出し基盤を共有します。",
            ["info.nextScope"] = "次の層はリリース検証、ドキュメント同期、配布パッケージ化です。",
            ["inspector.selection"] = "選択内容",
            ["inspector.project"] = "プロジェクト",
            ["inspector.toolSettings"] = "ツール設定",
            ["settings.inheritCommonDefault"] = "共通設定を継承",
            ["inspector.projectDefaults"] = "新規プロジェクト既定値",
            ["inspector.projectDefault.sampleRate"] = "サンプルレート上書き",
            ["inspector.projectDefault.channelMode"] = "チャンネルモード上書き",
            ["inspector.projectDefaults.help"] = "このプロジェクト設定は、新規作成時に共通設定より優先されます。共通設定を継承すると、共通側の既定値を使います。",
            ["inspector.language"] = "表示言語",
            ["inspector.language.help"] = "自動は、取得できる場合は現在の Unity Editor の言語に追従します。上書き設定はサポート時やスクリーンショットの統一に便利です。",
            ["inspector.showStartupGuide"] = "起動ガイドを表示",
            ["inspector.rememberLastProject"] = "前回プロジェクトを記憶",
            ["inspector.lastProjectPath"] = "前回プロジェクト: {0}",
            ["inspector.startup.help"] = "起動ガイドは既定で一度だけ表示され、ここから再度有効にできます。前回プロジェクトを記憶すると、起動時に最後に保存または読み込みした .gats.json を復元します。",
            ["inspector.debugMode"] = "デバッグモード",
            ["inspector.logLevel"] = "ログレベル",
            ["inspector.diagnostics.help"] = "有効にすると、Torus Edison は Unity Console へ診断ログを出力します。エンドユーザ調査時は必要に応じてログレベルを上げてください。",
            ["about.version"] = "バージョン情報",
            ["about.toolVersion"] = "ツールバージョン",
            ["about.packageId"] = "パッケージ ID",
            ["about.sessionFormat"] = "セッション形式",
            ["about.supportedEnv"] = "対応環境",
            ["about.capabilities"] = "現在の範囲",
            ["about.links"] = "クイックリンク",
            ["about.manualJa"] = "日本語マニュアルを開く",
            ["about.manualEn"] = "英語マニュアルを開く",
            ["about.terms"] = "利用規約を開く",
            ["about.releaseNotes"] = "リリースノートを開く",
            ["about.licenseFile"] = "LICENSE を開く",
            ["about.github"] = "GitHub Releases を開く",
            ["about.license"] = "ライセンス",
            ["about.licenseBody"] = "利用条件は同梱の LICENSE.md と TermsOfUse.md に従います。再配布、商用利用、別ツールへの同梱前には該当ファイルを確認してください。",
            ["about.support"] = "サポート",
            ["about.supportBody"] = "問い合わせ時は、Unity バージョン、Torus Edison バージョン、読み込んだ .gats.json 名、再現手順、Console の警告やエラーを共有してください。",
            ["inspector.selectTrackOrNote"] = "編集を始めるには、タイムラインでトラックヘッダーまたはノートを選択してください。",
            ["inspector.note.singleSummary"] = "{1} 上のノート {0} を編集中です。",
            ["inspector.note.multiSummary"] = "{1} トラックにまたがる {0} 個のノートを編集中です。変更はすべての選択ノートに適用されます。",
            ["inspector.note.startBeat"] = "開始拍",
            ["inspector.note.durationBeat"] = "長さ拍",
            ["inspector.note.midi"] = "MIDI ノート",
            ["inspector.note.velocity"] = "ベロシティ",
            ["inspector.note.useVoiceOverride"] = "ボイス上書きを使用",
            ["inspector.note.overridePartial"] = "選択中の一部ノートはまだトラック既定のボイスを使っています。ボイス上書きを有効にすると、選択中のすべてにノート単位の明示設定を適用できます。",
            ["inspector.note.overrideNone"] = "選択中のノートは現在、各トラックの既定ボイスを使っています。ノート単位のボイス設定を編集するにはボイス上書きを有効にしてください。",
            ["inspector.track.summary"] = "{0} を編集中です。ノート数: {1}",
            ["inspector.track.name"] = "トラック名",
            ["inspector.track.mute"] = "ミュート",
            ["inspector.track.solo"] = "ソロ",
            ["inspector.track.volume"] = "音量 (dB)",
            ["inspector.track.pan"] = "パン",
            ["inspector.track.actions"] = "トラック操作",
            ["inspector.track.delete"] = "トラックを削除",
            ["inspector.project.summary"] = "現在のプロジェクトの再生、出力、レンダリング設定です。",
            ["inspector.project.name"] = "プロジェクト名",
            ["inspector.project.timeSignature"] = "拍子",
            ["inspector.project.totalBars"] = "総小節数",
            ["inspector.project.sampleRate"] = "サンプルレート",
            ["inspector.project.channelMode"] = "チャンネルモード",
            ["inspector.project.masterGain"] = "マスターゲイン (dB)",
            ["voice.override"] = "ボイス上書き",
            ["voice.default"] = "既定ボイス",
            ["voice.waveform"] = "波形",
            ["voice.pulseWidth"] = "パルス幅",
            ["voice.noiseEnabled"] = "ノイズ有効",
            ["voice.noiseType"] = "ノイズ種別",
            ["voice.noiseMix"] = "ノイズミックス",
            ["voice.envelope"] = "エンベロープ",
            ["voice.attack"] = "アタック (ms)",
            ["voice.decay"] = "ディケイ (ms)",
            ["voice.sustain"] = "サステイン",
            ["voice.release"] = "リリース (ms)",
            ["voice.effect"] = "エフェクト",
            ["voice.effectVolume"] = "音量 (dB)",
            ["voice.effectPan"] = "パン",
            ["voice.effectPitch"] = "ピッチ (半音)",
            ["voice.fadeIn"] = "フェードイン (ms)",
            ["voice.fadeOut"] = "フェードアウト (ms)",
            ["voice.delay"] = "ディレイ",
            ["voice.delayEnabled"] = "有効",
            ["voice.delayTime"] = "時間 (ms)",
            ["voice.delayFeedback"] = "フィードバック",
            ["voice.delayMix"] = "ミックス",
            ["status.saved"] = "保存済み",
            ["status.unsaved"] = "未保存の変更あり",
            ["status.unsavedFile"] = "(未保存)",
            ["status.notExported"] = "(未書き出し)",
            ["status.notRendered"] = "(未レンダリング)",
            ["status.noProjectLoaded"] = "プロジェクトが読み込まれていません。",
            ["status.preview.notRendered"] = "プレビューは未レンダリングです。",
            ["status.preview.renderFailed"] = "プレビューのレンダリングに失敗しました。",
            ["status.preview.loopPlaying"] = "ループプレビューを再生中です。",
            ["status.preview.playing"] = "プレビューを再生中です。",
            ["status.preview.loopPaused"] = "ループプレビューを一時停止しました。",
            ["status.preview.paused"] = "プレビューを一時停止しました。",
            ["status.preview.loopReady"] = "ループプレビューの準備ができました。",
            ["status.preview.ready"] = "プレビューの準備ができました。",
            ["status.preview.stopped"] = "プレビューを停止しました。",
            ["status.preview.rewound"] = "プレビューを巻き戻しました。",
            ["status.preview.complete"] = "プレビューが完了しました。",
            ["status.preview.apiUnavailable"] = "プレビューは準備できましたが、この Editor では UnityEditor の再生 API が利用できません。",
            ["status.preview.silent"] = "プレビューの準備ができました (無音バッファ)。",
            ["status.preview.startFailed"] = "プレビューの開始に失敗しました。",
            ["status.preview.loopRestartFailed"] = "ループプレビューの再開に失敗しました。",
            ["status.preview.readyButApiMissing"] = "レンダリングは利用できますが、この Editor ビルドでは UnityEditor のプレビュー再生 API が見つかりませんでした。",
            ["status.projectSaved"] = "プロジェクトを保存しました。",
            ["status.projectLoaded"] = "プロジェクトを読み込みました。",
            ["status.samplesCreated"] = "サンプルプロジェクトを作成しました。",
            ["status.wavExported"] = "WAV を書き出しました。",
            ["status.wavExportedAndRefreshed"] = "WAV を書き出し、アセットを更新しました。",
            ["status.previewRendered"] = "プレビューを生成しました。",
            ["status.timelineHint"] = "グリッド {0} | 選択ノート {1} 個 | 空レーンのドラッグで作成 | ノートのドラッグで移動 | 端のドラッグでリサイズ | Ctrl+D で複製 | Delete で選択ノートまたは選択トラックを削除 | Ctrl+Z / Ctrl+Y で Undo / Redo",
            ["status.trackDeleteNeedsOneTrack"] = "少なくとも 1 トラックは残す必要があります。",
            ["status.previewBuffer"] = "{0} Hz / {1} / project {2:0.00}s / output {3:0.00}s / peak {4:0.000}",
            ["status.previewCursor"] = "Bar {0:00} / Beat {1:0.00} ({2:0.00}s)",
            ["status.previewCursorTail"] = "{0} / tail +{1:0.00}s",
            ["status.previewProgress"] = "Bar {0:00} / Beat {1:0.00}",
            ["status.previewTail"] = "再生テール +{0:0.00}s",
            ["dialog.ok"] = "OK",
            ["dialog.openProject"] = "ゲームオーディオプロジェクトを開く",
            ["dialog.saveProject"] = "ゲームオーディオプロジェクトを保存",
            ["dialog.discardMessage"] = "未保存の変更は失われます。続行しますか？",
            ["dialog.discard"] = "変更を破棄",
            ["dialog.cancel"] = "キャンセル",
            ["dialog.saveFirst"] = "先に保存",
            ["dialog.fileNotFound"] = "ファイルが見つかりません:\n{0}",
            ["dialog.deleteTrack.title"] = "トラックを削除",
            ["dialog.deleteTrack.message"] = "\"{0}\" と含まれる {1} 個のノートを削除しますか？この操作は Undo できます。",
            ["dialog.deleteTrack.confirm"] = "トラックを削除",
            ["startup.guide.message"] = ".gats.json プロジェクトを新規作成または読み込み、タイムラインでノートを編集し、プレビューで確認してから Export 画面で WAV を書き出します。この起動ガイドは既定で一度だけ表示され、Settings から再度有効化できます。",
            ["startup.guide.openManual"] = "マニュアルを開く",
            ["startup.guide.startEditing"] = "編集を始める",
            ["startup.guide.showNextTime"] = "次回も表示",
            ["notification.clampedInt"] = "{0} は {1} に補正されました。",
            ["notification.clampedFloat"] = "{0} は {1:0.###} に補正されました。",
            ["notification.requiresFinite"] = "{0} には有限の数値が必要です。"
        };

        private static readonly IReadOnlyDictionary<string, string> Chinese = new Dictionary<string, string>(StringComparer.Ordinal)
        {
            ["language.auto"] = "自动",
            ["language.japanese"] = "日语",
            ["language.english"] = "英语",
            ["language.chinese"] = "中文",
            ["logLevel.error"] = "错误",
            ["logLevel.warning"] = "警告",
            ["logLevel.info"] = "信息",
            ["logLevel.verbose"] = "详细",
            ["audio.mono"] = "单声道",
            ["audio.stereo"] = "立体声",
            ["channel.mono"] = "单声道",
            ["channel.stereo"] = "立体声",
            ["waveform.sine"] = "正弦波",
            ["waveform.square"] = "方波",
            ["waveform.triangle"] = "三角波",
            ["waveform.saw"] = "锯齿波",
            ["waveform.pulse"] = "脉冲波",
            ["noise.white"] = "白噪声",
            ["toolbar.new"] = "新建",
            ["toolbar.open"] = "打开",
            ["toolbar.save"] = "保存",
            ["toolbar.saveAs"] = "另存为",
            ["workspace.file"] = "文件",
            ["workspace.edit"] = "编辑",
            ["workspace.preview"] = "预览",
            ["workspace.export"] = "导出",
            ["workspace.settings"] = "设置",
            ["page.file.title"] = "文件",
            ["page.file.description"] = "查看项目文件、当前状态和示例操作。",
            ["page.edit.title"] = "编辑",
            ["page.edit.description"] = "在同一页面中进行时间线编辑、预览播放，以及对所选音符或轨道进行编辑。",
            ["page.preview.title"] = "预览",
            ["page.preview.description"] = "在编辑器内渲染并试听当前项目。",
            ["page.export.title"] = "导出",
            ["page.export.description"] = "导出 WAV，并确认当前输出目录。",
            ["page.settings.title"] = "设置",
            ["page.settings.description"] = "查看项目设置和基础诊断信息。",
            ["summary.currentProject"] = "当前项目",
            ["summary.name"] = "名称",
            ["summary.bpm"] = "BPM",
            ["summary.bars"] = "小节数",
            ["summary.tracks"] = "轨道数",
            ["summary.file"] = "文件",
            ["summary.status"] = "状态",
            ["timeline.title"] = "时间线编辑",
            ["timeline.undo"] = "撤销",
            ["timeline.redo"] = "重做",
            ["timeline.grid"] = "网格 {0}",
            ["timeline.help.title"] = "时间线操作帮助",
            ["timeline.help.summary"] = "需要快速确认时间线鼠标操作或快捷键时，请打开这里。",
            ["timeline.help.empty"] = "编辑器界面准备完成后，这里会显示操作说明。",
            ["timeline.help.close"] = "关闭",
            ["timeline.barLabel"] = "Bar {0:00}",
            ["timeline.trackInfo"] = "音符 {0} / 声像 {1:0.00}",
            ["timeline.noteLabel"] = "MIDI {0}",
            ["selectionInspector.title"] = "选择检查器",
            ["projectInspector.title"] = "项目检查器",
            ["preview.title"] = "预览播放",
            ["preview.render"] = "生成预览",
            ["preview.play"] = "播放",
            ["preview.pause"] = "暂停",
            ["preview.stop"] = "停止",
            ["preview.rewind"] = "回到开头",
            ["preview.loop"] = "循环",
            ["preview.key.preview"] = "预览",
            ["preview.key.buffer"] = "缓冲区",
            ["preview.key.cursor"] = "光标",
            ["preview.cursorNotStarted"] = "光标尚未开始",
            ["preview.waveform.empty"] = "生成预览后会在这里显示波形。",
            ["preview.waveform.silent"] = "预览缓冲区当前为静音。",
            ["export.title"] = "WAV 导出",
            ["export.exportWav"] = "导出 WAV",
            ["export.openFolder"] = "打开导出目录",
            ["export.resolvedFolder"] = "解析后的目录",
            ["export.exportFile"] = "导出文件",
            ["export.lastExport"] = "上次导出",
            ["export.commonDefaultFolder"] = "公共默认目录",
            ["export.projectOverrideFolder"] = "项目覆盖目录",
            ["export.autoRefresh"] = "自动刷新资源",
            ["export.browse"] = "浏览",
            ["export.useProjectFolder"] = "Project/Exports",
            ["export.useAssetsFolder"] = "Assets/Exports",
            ["export.clearOverride"] = "清除",
            ["export.folderHelp"] = "位于当前 Unity 项目中的文件夹会以相对路径保存，项目外文件夹会保持为绝对路径。希望导出到项目根目录附近时使用 Project/Exports，希望 Unity 自动刷新导出的 WAV 时使用 Assets/Exports。",
            ["sample.title"] = "示例与工作流",
            ["sample.create"] = "创建示例",
            ["sample.loadBasic"] = "载入 Basic SE",
            ["sample.loadLoop"] = "载入 Simple Loop",
            ["sample.openFolder"] = "打开文件夹",
            ["sample.location"] = "示例文件保存在 {0}",
            ["sample.editing"] = "现已支持时间线编辑和检查器编辑。使用编辑页可创建、移动、调整音符长度，并修改音符或轨道参数。",
            ["sample.json"] = "JSON 仍适合批量编辑、审查和版本管理，但文件操作、编辑与预览、导出和设置现在按职责分区整理。",
            ["info.title"] = "基础状态",
            ["info.currentScope"] = "此窗口将文件、编辑与预览、导出和设置分区整理，同时保留同一项目状态、选择状态、播放、Undo / Redo、JSON 保存 / 读取，以及 WAV 导出基础。",
            ["info.nextScope"] = "下一层将接入发布验证、文档同步和分发打包。",
            ["inspector.selection"] = "当前选择",
            ["inspector.project"] = "项目",
            ["inspector.toolSettings"] = "工具设置",
            ["settings.inheritCommonDefault"] = "继承公共设置",
            ["inspector.projectDefaults"] = "新项目默认值",
            ["inspector.projectDefault.sampleRate"] = "采样率覆盖",
            ["inspector.projectDefault.channelMode"] = "声道模式覆盖",
            ["inspector.projectDefaults.help"] = "这些项目设置会在新建项目时优先于公共默认值。继承公共设置会继续使用共享默认值。",
            ["inspector.language"] = "显示语言",
            ["inspector.language.help"] = "自动模式会在可用时跟随当前 Unity Editor 语言。覆盖模式适合客服支持和统一截图语言。",
            ["inspector.showStartupGuide"] = "显示启动指南",
            ["inspector.rememberLastProject"] = "记住上次项目",
            ["inspector.lastProjectPath"] = "上次项目：{0}",
            ["inspector.startup.help"] = "启动指南默认只显示一次，可在此重新启用。启用记住上次项目后，Torus Edison 会在启动时恢复最后保存或打开的 .gats.json 文件。",
            ["inspector.debugMode"] = "调试模式",
            ["inspector.logLevel"] = "日志级别",
            ["inspector.diagnostics.help"] = "启用后，Torus Edison 会向 Unity Console 输出诊断日志。与终端用户一起排查时，可按需提高日志级别。",
            ["about.version"] = "版本信息",
            ["about.toolVersion"] = "工具版本",
            ["about.packageId"] = "包 ID",
            ["about.sessionFormat"] = "会话格式",
            ["about.supportedEnv"] = "支持环境",
            ["about.capabilities"] = "当前范围",
            ["about.links"] = "快捷链接",
            ["about.manualJa"] = "打开日文手册",
            ["about.manualEn"] = "打开英文手册",
            ["about.terms"] = "打开使用条款",
            ["about.releaseNotes"] = "打开发布说明",
            ["about.licenseFile"] = "打开 LICENSE",
            ["about.github"] = "打开 GitHub Releases",
            ["about.license"] = "许可证",
            ["about.licenseBody"] = "使用条件受随附的 LICENSE.md 与 TermsOfUse.md 约束。在重新分发、商业使用或集成到其他工具链前，请先确认这些文件。",
            ["about.support"] = "支持",
            ["about.supportBody"] = "联系支持时，请提供 Unity 版本、Torus Edison 版本、已加载的 .gats.json 文件名、复现步骤，以及 Console 中的警告或错误。",
            ["inspector.selectTrackOrNote"] = "请先在时间线中选择轨道标题或音符，再开始编辑。",
            ["inspector.note.singleSummary"] = "正在编辑 {1} 上的音符 {0}。",
            ["inspector.note.multiSummary"] = "正在编辑跨 {1} 条轨道的 {0} 个音符。修改会应用到所有已选音符。",
            ["inspector.note.startBeat"] = "开始拍",
            ["inspector.note.durationBeat"] = "持续拍",
            ["inspector.note.midi"] = "MIDI 音高",
            ["inspector.note.velocity"] = "力度",
            ["inspector.note.useVoiceOverride"] = "使用音色覆盖",
            ["inspector.note.overridePartial"] = "部分已选音符仍在使用轨道默认音色。启用音色覆盖后，可为整个选择集应用显式的音符级音色设置。",
            ["inspector.note.overrideNone"] = "当前所选音符都在使用各轨道的默认音色。启用音色覆盖后即可编辑音符级音色设置。",
            ["inspector.track.summary"] = "正在编辑 {0}。音符数: {1}",
            ["inspector.track.name"] = "轨道名称",
            ["inspector.track.mute"] = "静音",
            ["inspector.track.solo"] = "独奏",
            ["inspector.track.volume"] = "音量 (dB)",
            ["inspector.track.pan"] = "声像",
            ["inspector.track.actions"] = "轨道操作",
            ["inspector.track.delete"] = "删除轨道",
            ["inspector.project.summary"] = "当前项目的播放、输出和渲染设置。",
            ["inspector.project.name"] = "项目名称",
            ["inspector.project.timeSignature"] = "拍号",
            ["inspector.project.totalBars"] = "总小节数",
            ["inspector.project.sampleRate"] = "采样率",
            ["inspector.project.channelMode"] = "声道模式",
            ["inspector.project.masterGain"] = "主增益 (dB)",
            ["voice.override"] = "音色覆盖",
            ["voice.default"] = "默认音色",
            ["voice.waveform"] = "波形",
            ["voice.pulseWidth"] = "脉冲宽度",
            ["voice.noiseEnabled"] = "启用噪声",
            ["voice.noiseType"] = "噪声类型",
            ["voice.noiseMix"] = "噪声混合",
            ["voice.envelope"] = "包络",
            ["voice.attack"] = "起音 (ms)",
            ["voice.decay"] = "衰减 (ms)",
            ["voice.sustain"] = "持续",
            ["voice.release"] = "释放 (ms)",
            ["voice.effect"] = "效果",
            ["voice.effectVolume"] = "音量 (dB)",
            ["voice.effectPan"] = "声像",
            ["voice.effectPitch"] = "音高 (半音)",
            ["voice.fadeIn"] = "淡入 (ms)",
            ["voice.fadeOut"] = "淡出 (ms)",
            ["voice.delay"] = "延迟",
            ["voice.delayEnabled"] = "启用",
            ["voice.delayTime"] = "时间 (ms)",
            ["voice.delayFeedback"] = "反馈",
            ["voice.delayMix"] = "混合",
            ["status.saved"] = "已保存",
            ["status.unsaved"] = "有未保存的更改",
            ["status.unsavedFile"] = "(未保存)",
            ["status.notExported"] = "(尚未导出)",
            ["status.notRendered"] = "(尚未渲染)",
            ["status.noProjectLoaded"] = "尚未加载项目。",
            ["status.preview.notRendered"] = "预览尚未渲染。",
            ["status.preview.renderFailed"] = "预览渲染失败。",
            ["status.preview.loopPlaying"] = "正在播放循环预览。",
            ["status.preview.playing"] = "正在播放预览。",
            ["status.preview.loopPaused"] = "循环预览已暂停。",
            ["status.preview.paused"] = "预览已暂停。",
            ["status.preview.loopReady"] = "循环预览已准备完成。",
            ["status.preview.ready"] = "预览已准备完成。",
            ["status.preview.stopped"] = "预览已停止。",
            ["status.preview.rewound"] = "预览已回到开头。",
            ["status.preview.complete"] = "预览已播放完成。",
            ["status.preview.apiUnavailable"] = "预览已准备完成，但当前 Editor 构建中无法使用 UnityEditor 的播放 API。",
            ["status.preview.silent"] = "预览已准备完成（静音缓冲区）。",
            ["status.preview.startFailed"] = "预览启动失败。",
            ["status.preview.loopRestartFailed"] = "循环预览重启失败。",
            ["status.preview.readyButApiMissing"] = "可以完成渲染，但当前 Editor 构建中未找到 UnityEditor 预览播放 API。",
            ["status.projectSaved"] = "项目已保存。",
            ["status.projectLoaded"] = "项目已加载。",
            ["status.samplesCreated"] = "示例项目已创建。",
            ["status.wavExported"] = "WAV 已导出。",
            ["status.wavExportedAndRefreshed"] = "WAV 已导出，资源已刷新。",
            ["status.previewRendered"] = "预览已生成。",
            ["status.timelineHint"] = "网格 {0} | 已选音符 {1} 个 | 拖拽空白轨道创建 | 拖拽音符移动 | 拖拽边缘调整长度 | Ctrl+D 复制 | Delete 删除所选音符或所选轨道 | Ctrl+Z / Ctrl+Y 撤销 / 重做",
            ["status.trackDeleteNeedsOneTrack"] = "至少需要保留 1 条轨道。",
            ["status.previewBuffer"] = "{0} Hz / {1} / project {2:0.00}s / output {3:0.00}s / peak {4:0.000}",
            ["status.previewCursor"] = "Bar {0:00} / Beat {1:0.00} ({2:0.00}s)",
            ["status.previewCursorTail"] = "{0} / tail +{1:0.00}s",
            ["status.previewProgress"] = "Bar {0:00} / Beat {1:0.00}",
            ["status.previewTail"] = "播放尾部 +{0:0.00}s",
            ["dialog.ok"] = "确定",
            ["dialog.openProject"] = "打开游戏音频项目",
            ["dialog.saveProject"] = "保存游戏音频项目",
            ["dialog.discardMessage"] = "未保存的更改将会丢失。是否继续？",
            ["dialog.discard"] = "放弃更改",
            ["dialog.cancel"] = "取消",
            ["dialog.saveFirst"] = "先保存",
            ["dialog.fileNotFound"] = "找不到文件:\n{0}",
            ["dialog.deleteTrack.title"] = "删除轨道",
            ["dialog.deleteTrack.message"] = "要删除 \"{0}\" 以及其中的 {1} 个音符吗？此操作可通过撤销恢复。",
            ["dialog.deleteTrack.confirm"] = "删除轨道",
            ["startup.guide.message"] = "新建或打开 .gats.json 项目，在时间线上编辑音符，预览结果，然后在导出页写出 WAV。启动指南默认只显示一次，可在 Settings 中重新启用。",
            ["startup.guide.openManual"] = "打开手册",
            ["startup.guide.startEditing"] = "开始编辑",
            ["startup.guide.showNextTime"] = "下次再显示",
            ["notification.clampedInt"] = "{0} 已被限制为 {1}。",
            ["notification.clampedFloat"] = "{0} 已被限制为 {1:0.###}。",
            ["notification.requiresFinite"] = "{0} 需要有限数值。"
        };

        private static readonly IReadOnlyDictionary<GameAudioDisplayLanguage, IReadOnlyDictionary<string, string>> Tables =
            new Dictionary<GameAudioDisplayLanguage, IReadOnlyDictionary<string, string>>
            {
                [GameAudioDisplayLanguage.English] = English,
                [GameAudioDisplayLanguage.Japanese] = Japanese,
                [GameAudioDisplayLanguage.Chinese] = Chinese
            };

        public static IReadOnlyList<GameAudioLanguageMode> GetSupportedLanguageModes()
        {
            return SupportedLanguageModes;
        }

        public static GameAudioDisplayLanguage ResolveLanguage(GameAudioLanguageMode languageMode)
        {
            return languageMode switch
            {
                GameAudioLanguageMode.Japanese => GameAudioDisplayLanguage.Japanese,
                GameAudioLanguageMode.English => GameAudioDisplayLanguage.English,
                GameAudioLanguageMode.Chinese => GameAudioDisplayLanguage.Chinese,
                _ => MapSystemLanguage(GetPreferredUnityEditorLanguage())
            };
        }

        public static string Get(GameAudioDisplayLanguage language, string key)
        {
            if (Tables.TryGetValue(language, out IReadOnlyDictionary<string, string> table)
                && table.TryGetValue(key, out string localized))
            {
                return localized;
            }

            if (English.TryGetValue(key, out string fallback))
            {
                return fallback;
            }

            return key;
        }

        public static string Get(GameAudioDisplayLanguage language, string key, string englishText)
        {
            if (language == GameAudioDisplayLanguage.English)
            {
                return englishText;
            }

            if (Tables.TryGetValue(language, out IReadOnlyDictionary<string, string> table)
                && table.TryGetValue(key, out string localized))
            {
                return localized;
            }

            return englishText;
        }

        public static string Format(GameAudioDisplayLanguage language, string key, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(language, key), args);
        }

        public static string Format(GameAudioDisplayLanguage language, string key, string englishFormat, params object[] args)
        {
            return string.Format(CultureInfo.InvariantCulture, Get(language, key, englishFormat), args);
        }

        public static string GetLanguageModeLabel(GameAudioDisplayLanguage language, GameAudioLanguageMode languageMode)
        {
            return Get(
                language,
                languageMode switch
                {
                    GameAudioLanguageMode.Japanese => "language.japanese",
                    GameAudioLanguageMode.English => "language.english",
                    GameAudioLanguageMode.Chinese => "language.chinese",
                    _ => "language.auto"
                });
        }

        public static string GetDiagnosticLogLevelLabel(GameAudioDisplayLanguage language, GameAudioDiagnosticLogLevel logLevel)
        {
            return Get(
                language,
                logLevel switch
                {
                    GameAudioDiagnosticLogLevel.Error => "logLevel.error",
                    GameAudioDiagnosticLogLevel.Warning => "logLevel.warning",
                    GameAudioDiagnosticLogLevel.Verbose => "logLevel.verbose",
                    _ => "logLevel.info"
                });
        }

        public static string GetChannelModeLabel(GameAudioDisplayLanguage language, GameAudioChannelMode channelMode)
        {
            return Get(
                language,
                channelMode == GameAudioChannelMode.Mono ? "channel.mono" : "channel.stereo");
        }

        public static string GetWaveformLabel(GameAudioDisplayLanguage language, GameAudioWaveformType waveform)
        {
            return Get(
                language,
                waveform switch
                {
                    GameAudioWaveformType.Sine => "waveform.sine",
                    GameAudioWaveformType.Triangle => "waveform.triangle",
                    GameAudioWaveformType.Saw => "waveform.saw",
                    GameAudioWaveformType.Pulse => "waveform.pulse",
                    _ => "waveform.square"
                });
        }

        public static string GetNoiseTypeLabel(GameAudioDisplayLanguage language, GameAudioNoiseType noiseType)
        {
            return Get(
                language,
                noiseType switch
                {
                    GameAudioNoiseType.White => "noise.white",
                    _ => "noise.white"
                });
        }

        private static SystemLanguage GetPreferredUnityEditorLanguage()
        {
            try
            {
                Type localizationDatabaseType = typeof(EditorWindow).Assembly.GetType("UnityEditor.LocalizationDatabase");
                PropertyInfo enabledProperty = localizationDatabaseType?.GetProperty("enableEditorLocalization", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                bool isLocalizationEnabled = enabledProperty?.GetValue(null) is bool enabled && enabled;
                if (!isLocalizationEnabled)
                {
                    return UnityEngine.Application.systemLanguage;
                }

                PropertyInfo languageProperty = localizationDatabaseType.GetProperty("currentEditorLanguage", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                object currentLanguage = languageProperty?.GetValue(null);
                if (TryConvertEditorLanguage(currentLanguage, out SystemLanguage resolvedLanguage))
                {
                    return resolvedLanguage;
                }
            }
            catch (Exception)
            {
            }

            return UnityEngine.Application.systemLanguage;
        }

        private static bool TryConvertEditorLanguage(object currentLanguage, out SystemLanguage resolvedLanguage)
        {
            if (currentLanguage is SystemLanguage systemLanguage)
            {
                resolvedLanguage = systemLanguage;
                return true;
            }

            if (currentLanguage != null
                && Enum.TryParse(currentLanguage.ToString(), true, out SystemLanguage parsedLanguage))
            {
                resolvedLanguage = parsedLanguage;
                return true;
            }

            resolvedLanguage = UnityEngine.Application.systemLanguage;
            return false;
        }

        internal static GameAudioDisplayLanguage MapSystemLanguage(SystemLanguage systemLanguage)
        {
            return systemLanguage switch
            {
                SystemLanguage.Japanese => GameAudioDisplayLanguage.Japanese,
                SystemLanguage.Chinese => GameAudioDisplayLanguage.Chinese,
                SystemLanguage.ChineseSimplified => GameAudioDisplayLanguage.Chinese,
                SystemLanguage.ChineseTraditional => GameAudioDisplayLanguage.Chinese,
                _ => GameAudioDisplayLanguage.English
            };
        }
    }
}
