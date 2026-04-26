using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using TorusEdison.Editor.Audio;
using TorusEdison.Editor.Config;
using TorusEdison.Editor.Localization;
using TorusEdison.Editor.Persistence;
using TorusEdison.Editor.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TorusEdison.Editor.Windows
{
    internal sealed class GameAudio8BitConversionWindow : EditorWindow
    {
        private const float LabelWidth = 168.0f;

        private readonly GameAudioAudioClipConversionService _conversionService = new GameAudioAudioClipConversionService();
        private readonly GameAudioCommonConfigSerializer _commonConfigSerializer = new GameAudioCommonConfigSerializer();
        private readonly GameAudioProjectConfigSerializer _projectConfigSerializer = new GameAudioProjectConfigSerializer();

        private GameAudioCommonConfig _commonConfig;
        private GameAudioProjectConfig _projectConfig;
        private GameAudioDisplayLanguage _displayLanguage = GameAudioDisplayLanguage.English;
        private AudioClip _sourceClip;
        private string _outputName = string.Empty;
        private string _exportDirectory = string.Empty;
        private int _targetSampleRate = 11025;
        private GameAudioConversionChannelMode _channelMode = GameAudioConversionChannelMode.Mono;
        private bool _autoRefreshAfterExport = true;
        private string _lastWavePath = string.Empty;
        private string _lastProjectPath = string.Empty;

        private ObjectField _sourceClipField;
        private TextField _outputNameField;
        private TextField _exportDirectoryField;
        private PopupField<int> _sampleRateField;
        private PopupField<GameAudioConversionChannelMode> _channelModeField;
        private Toggle _autoRefreshToggle;
        private Label _resolvedFolderValue;
        private Label _lastWaveValue;
        private Label _lastProjectValue;
        private Button _convertButton;

        [MenuItem("Tools/Torus Edison/Utilities/8-bit WAV Converter")]
        public static void OpenWindow()
        {
            var window = GetWindow<GameAudio8BitConversionWindow>();
            window.titleContent = new GUIContent("8-bit WAV Converter");
            window.minSize = new Vector2(680.0f, 460.0f);
            window.Show();
        }

        private void CreateGUI()
        {
            LoadConfig();
            InitializeFromSelectionIfEmpty();
            BuildWindow();
            RefreshView();
        }

        private void OnSelectionChange()
        {
            if (_sourceClip != null)
            {
                return;
            }

            InitializeFromSelectionIfEmpty();
            RefreshView();
        }

        private void LoadConfig()
        {
            _commonConfig = _commonConfigSerializer.LoadOrDefault();
            _projectConfig = _projectConfigSerializer.LoadOrDefault(GameAudioConfigPaths.GetProjectConfigPath(GetProjectRootPath()));
            _displayLanguage = GameAudioLocalization.ResolveLanguage(_commonConfig.DisplayLanguage);
            _autoRefreshAfterExport = _projectConfig?.AutoRefreshAfterExport ?? true;
            GameAudioDiagnosticLogger.Configure(_commonConfig);
        }

        private void InitializeFromSelectionIfEmpty()
        {
            if (_sourceClip == null && Selection.activeObject is AudioClip selectedClip)
            {
                _sourceClip = selectedClip;
                if (string.IsNullOrWhiteSpace(_outputName))
                {
                    _outputName = $"{selectedClip.name}_8bit";
                }
            }
        }

        private void BuildWindow()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 12.0f;
            rootVisualElement.style.paddingRight = 12.0f;
            rootVisualElement.style.paddingTop = 12.0f;
            rootVisualElement.style.paddingBottom = 12.0f;

            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1.0f;
            rootVisualElement.Add(scrollView);

            scrollView.Add(BuildConversionSection());
            scrollView.Add(BuildOutputSection());
            scrollView.Add(BuildResultSection());
        }

        private VisualElement BuildConversionSection()
        {
            VisualElement section = CreateSection(T("export.convert8Bit.title", "8-bit WAV Conversion"));
            section.Add(new HelpBox(
                T("export.convert8Bit.help", "Select an imported AudioClip asset and convert it to 8-bit PCM WAV. Bring your own audio into Unity first and only convert audio you are allowed to use. This tool does not acquire audio from YouTube or other external services."),
                HelpBoxMessageType.Info));

            _sourceClipField = new ObjectField
            {
                objectType = typeof(AudioClip),
                allowSceneObjects = false
            };
            _sourceClipField.RegisterValueChangedCallback(evt =>
            {
                _sourceClip = evt.newValue as AudioClip;
                if (_sourceClip != null && string.IsNullOrWhiteSpace(_outputName))
                {
                    _outputName = $"{_sourceClip.name}_8bit";
                }

                RefreshView();
            });
            section.Add(CreateRow(T("export.convert8Bit.sourceClip", "Source AudioClip"), _sourceClipField));

            _outputNameField = new TextField
            {
                isDelayed = true
            };
            _outputNameField.RegisterValueChangedCallback(evt =>
            {
                _outputName = evt.newValue?.Trim() ?? string.Empty;
                RefreshView();
            });
            section.Add(CreateRow(T("export.convert8Bit.outputName", "Output Name"), _outputNameField));

            List<int> sampleRateOptions = GetSupportedConversionSampleRates().ToList();
            int selectedSampleRateIndex = Math.Max(0, sampleRateOptions.IndexOf(sampleRateOptions.Contains(_targetSampleRate)
                ? _targetSampleRate
                : sampleRateOptions[0]));
            _sampleRateField = new PopupField<int>(
                sampleRateOptions,
                selectedSampleRateIndex,
                FormatSampleRateOption,
                FormatSampleRateOption);
            _sampleRateField.RegisterValueChangedCallback(evt =>
            {
                _targetSampleRate = evt.newValue;
                RefreshView();
            });
            section.Add(CreateRow(T("export.convert8Bit.sampleRate", "Target Sample Rate"), _sampleRateField));

            List<GameAudioConversionChannelMode> channelModeOptions = GetSupportedConversionChannelModes().ToList();
            int selectedChannelModeIndex = Math.Max(0, channelModeOptions.IndexOf(channelModeOptions.Contains(_channelMode)
                ? _channelMode
                : channelModeOptions[0]));
            _channelModeField = new PopupField<GameAudioConversionChannelMode>(
                channelModeOptions,
                selectedChannelModeIndex,
                FormatConversionChannelMode,
                FormatConversionChannelMode);
            _channelModeField.RegisterValueChangedCallback(evt =>
            {
                _channelMode = evt.newValue;
                RefreshView();
            });
            section.Add(CreateRow(T("export.convert8Bit.channelMode", "Channel Mode"), _channelModeField));

            _convertButton = CreateButton(T("export.convert8Bit.export", "Convert To 8-bit WAV"), Export8BitWav);
            section.Add(CreateButtonRow(_convertButton));

            return section;
        }

        private VisualElement BuildOutputSection()
        {
            VisualElement section = CreateSection(T("export.convert8Bit.outputSection", "Output"));

            _exportDirectoryField = new TextField
            {
                isDelayed = true
            };
            _exportDirectoryField.RegisterValueChangedCallback(evt =>
            {
                _exportDirectory = evt.newValue?.Trim() ?? string.Empty;
                RefreshView();
            });
            section.Add(CreateRow(T("export.convert8Bit.exportFolder", "Export Folder"), _exportDirectoryField));

            VisualElement buttonRow = CreateButtonRow(
                CreateButton(T("export.browse", "Browse"), BrowseExportDirectory),
                CreateButton(T("export.useProjectFolder", "Project/Exports"), SetExportDirectoryToProjectExports),
                CreateButton(T("export.useAssetsFolder", "Assets/Exports"), SetExportDirectoryToAssetsExports),
                CreateButton(T("export.clearOverride", "Clear"), ClearExportDirectoryOverride),
                CreateButton(T("export.openFolder", "Open Export Folder"), OpenExportFolder));
            section.Add(buttonRow);

            _autoRefreshToggle = new Toggle();
            _autoRefreshToggle.RegisterValueChangedCallback(evt =>
            {
                _autoRefreshAfterExport = evt.newValue;
                RefreshView();
            });
            section.Add(CreateRow(T("export.autoRefresh", "Auto Refresh Assets"), _autoRefreshToggle));

            _resolvedFolderValue = AddKeyValue(section, T("export.resolvedFolder", "Resolved Folder"));
            return section;
        }

        private VisualElement BuildResultSection()
        {
            VisualElement section = CreateSection(T("export.convert8Bit.resultSection", "Last Result"));
            _lastWaveValue = AddKeyValue(section, T("export.convert8Bit.lastExport", "Last 8-bit Export"));
            _lastProjectValue = AddKeyValue(section, T("export.convert8Bit.lastProject", "Last Conversion Project"));
            return section;
        }

        private void BrowseExportDirectory()
        {
            string selectedPath = EditorUtility.OpenFolderPanel(
                GameAudioToolInfo.DisplayName,
                ResolveExistingDirectory(GetResolvedExportDirectory(), GetProjectRootPath()),
                string.Empty);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            _exportDirectory = GameAudioExportUtility.NormalizeStoredExportDirectory(selectedPath, GetProjectRootPath());
            RefreshView();
        }

        private void SetExportDirectoryToProjectExports()
        {
            _exportDirectory = "Exports/Audio";
            RefreshView();
        }

        private void SetExportDirectoryToAssetsExports()
        {
            _exportDirectory = "Assets/Exports/Audio";
            RefreshView();
        }

        private void ClearExportDirectoryOverride()
        {
            _exportDirectory = string.Empty;
            RefreshView();
        }

        private void OpenExportFolder()
        {
            try
            {
                string exportDirectory = GetResolvedExportDirectory();
                Directory.CreateDirectory(exportDirectory);
                GameAudioDiagnosticLogger.Verbose("Conversion", $"Opening conversion export folder {exportDirectory}.");
                EditorUtility.RevealInFinder(exportDirectory);
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Conversion", "Opening conversion export folder failed");
            }
        }

        private void Export8BitWav()
        {
            if (_sourceClip == null)
            {
                ShowEditorException(new InvalidOperationException(T("export.convert8Bit.selectSourceFirst", "Select a source AudioClip first.")), "Conversion", "8-bit WAV export failed");
                return;
            }

            try
            {
                string exportDirectory = GetResolvedExportDirectory();
                string outputName = GetResolvedOutputName();
                GameAudioAudioClipConversionExportResult exportResult = _conversionService.ExportAsPcm8(
                    _sourceClip,
                    exportDirectory,
                    outputName,
                    _targetSampleRate,
                    _channelMode);
                _lastWavePath = exportResult.WaveFilePath;
                _lastProjectPath = exportResult.ProjectFilePath;

                bool shouldRefresh = _autoRefreshAfterExport
                    && (GameAudioExportUtility.ShouldRefreshAssetDatabase(exportResult.WaveFilePath, GetProjectRootPath())
                        || GameAudioExportUtility.ShouldRefreshAssetDatabase(exportResult.ProjectFilePath, GetProjectRootPath()));
                if (shouldRefresh)
                {
                    AssetDatabase.Refresh();
                }

                GameAudioDiagnosticLogger.Info(
                    "Conversion",
                    $"Exported 8-bit WAV to {exportResult.WaveFilePath} and conversion project to {exportResult.ProjectFilePath}. AutoRefresh={shouldRefresh}. Source={_sourceClip.name}; SampleRate={_targetSampleRate}; ChannelMode={_channelMode}.");
                ShowNotification(new GUIContent(shouldRefresh
                    ? T("status.convert8BitExportedAndRefreshed", "8-bit WAV and conversion project exported, then assets refreshed.")
                    : T("status.convert8BitExported", "8-bit WAV and conversion project exported.")));
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Conversion", "8-bit WAV export failed");
            }
        }

        private string GetResolvedOutputName()
        {
            if (!string.IsNullOrWhiteSpace(_outputName))
            {
                return _outputName;
            }

            if (_sourceClip != null && !string.IsNullOrWhiteSpace(_sourceClip.name))
            {
                return $"{_sourceClip.name}_8bit";
            }

            return "Converted8Bit";
        }

        private string GetResolvedExportDirectory()
        {
            string projectRoot = GetProjectRootPath();
            if (!string.IsNullOrWhiteSpace(_exportDirectory))
            {
                return GameAudioExportUtility.NormalizeExportDirectory(_exportDirectory, projectRoot);
            }

            return GameAudioConfigResolver.ResolveExportDirectory(_commonConfig, _projectConfig, projectRoot);
        }

        private void RefreshView()
        {
            _sourceClipField?.SetValueWithoutNotify(_sourceClip);
            _outputNameField?.SetValueWithoutNotify(_outputName);
            _exportDirectoryField?.SetValueWithoutNotify(_exportDirectory);
            _sampleRateField?.SetValueWithoutNotify(_targetSampleRate);
            _channelModeField?.SetValueWithoutNotify(_channelMode);
            _autoRefreshToggle?.SetValueWithoutNotify(_autoRefreshAfterExport);
            _convertButton?.SetEnabled(_sourceClip != null);

            if (_resolvedFolderValue != null)
            {
                _resolvedFolderValue.text = GetResolvedExportDirectory();
            }

            if (_lastWaveValue != null)
            {
                _lastWaveValue.text = string.IsNullOrWhiteSpace(_lastWavePath)
                    ? T("status.notExported", "(not exported)")
                    : _lastWavePath;
            }

            if (_lastProjectValue != null)
            {
                _lastProjectValue.text = string.IsNullOrWhiteSpace(_lastProjectPath)
                    ? T("status.notExported", "(not exported)")
                    : _lastProjectPath;
            }
        }

        private void ShowEditorException(Exception exception, string area, string context)
        {
            GameAudioDiagnosticLogger.Exception(area, exception, context);
            EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, T("dialog.ok", "OK"));
        }

        private string T(string key, string englishText)
        {
            return GameAudioLocalization.Get(_displayLanguage, key, englishText);
        }

        private string FormatConversionChannelMode(GameAudioConversionChannelMode channelMode)
        {
            return channelMode switch
            {
                GameAudioConversionChannelMode.Mono => T("export.convert8Bit.channelMode.mono", "Mono"),
                GameAudioConversionChannelMode.Stereo => T("export.convert8Bit.channelMode.stereo", "Stereo"),
                _ => T("export.convert8Bit.channelMode.preserve", "Preserve Source")
            };
        }

        private static VisualElement CreateSection(string title)
        {
            var section = new VisualElement();
            section.style.backgroundColor = new Color(0.12f, 0.15f, 0.20f);
            section.style.paddingLeft = 12.0f;
            section.style.paddingRight = 12.0f;
            section.style.paddingTop = 12.0f;
            section.style.paddingBottom = 12.0f;
            section.style.marginBottom = 10.0f;
            section.style.borderTopLeftRadius = 6.0f;
            section.style.borderTopRightRadius = 6.0f;
            section.style.borderBottomLeftRadius = 6.0f;
            section.style.borderBottomRightRadius = 6.0f;

            var titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 14.0f;
            titleLabel.style.marginBottom = 8.0f;
            section.Add(titleLabel);
            return section;
        }

        private static VisualElement CreateRow(string labelText, VisualElement input)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 6.0f;

            var label = new Label(labelText);
            label.style.minWidth = LabelWidth;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(label);

            input.style.flexGrow = 1.0f;
            row.Add(input);
            return row;
        }

        private static VisualElement CreateButtonRow(params Button[] buttons)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;
            row.style.marginTop = 8.0f;

            foreach (Button button in buttons)
            {
                row.Add(button);
            }

            return row;
        }

        private static Button CreateButton(string text, Action action)
        {
            var button = new Button(() => action?.Invoke())
            {
                text = text
            };
            button.style.minWidth = 120.0f;
            button.style.marginRight = 6.0f;
            button.style.marginBottom = 6.0f;
            return button;
        }

        private static Label AddKeyValue(VisualElement parent, string label)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 6.0f;

            var key = new Label(label);
            key.style.minWidth = LabelWidth;
            key.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(key);

            var value = new Label();
            value.style.whiteSpace = WhiteSpace.Normal;
            value.style.flexGrow = 1.0f;
            row.Add(value);

            parent.Add(row);
            return value;
        }

        private static IEnumerable<int> GetSupportedConversionSampleRates()
        {
            yield return 8000;
            yield return 11025;
            yield return 22050;
            yield return 32000;
            yield return 44100;
            yield return 48000;
        }

        private static IEnumerable<GameAudioConversionChannelMode> GetSupportedConversionChannelModes()
        {
            return (GameAudioConversionChannelMode[])Enum.GetValues(typeof(GameAudioConversionChannelMode));
        }

        private static string FormatSampleRateOption(int sampleRate)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} Hz", sampleRate);
        }

        private static string ResolveExistingDirectory(string path, string fallbackDirectory)
        {
            string candidate = string.IsNullOrWhiteSpace(path)
                ? fallbackDirectory
                : path;

            while (!string.IsNullOrWhiteSpace(candidate) && !Directory.Exists(candidate))
            {
                candidate = Path.GetDirectoryName(candidate);
            }

            return string.IsNullOrWhiteSpace(candidate)
                ? fallbackDirectory
                : candidate;
        }

        private static string GetProjectRootPath()
        {
            return Path.GetDirectoryName(UnityEngine.Application.dataPath) ?? Environment.CurrentDirectory;
        }
    }
}
