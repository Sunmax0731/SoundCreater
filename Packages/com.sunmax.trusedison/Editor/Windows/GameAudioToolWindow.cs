using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Audio;
using TorusEdison.Editor.Commands;
using TorusEdison.Editor.Config;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Localization;
using TorusEdison.Editor.Persistence;
using TorusEdison.Editor.Presets;
using TorusEdison.Editor.Utilities;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace TorusEdison.Editor.Windows
{
    public sealed class GameAudioToolWindow : EditorWindow
    {
        private const float InspectorLabelWidth = 164.0f;
        private const float TimelineHeaderWidth = 168.0f;
        private const float TimelineRulerHeight = 28.0f;
        private const float TimelineRowHeight = 42.0f;
        private const float TimelineNoteHeight = 22.0f;
        private const float TimelinePixelsPerBeat = 24.0f;
        private const float TimelineViewportHeight = 340.0f;
        private const float TimelineResizeHandleWidth = 6.0f;
        private const float TimelineFooterHeight = 34.0f;
        private const float InspectorValueWidth = 92.0f;
        private const float PreviewWaveformHeight = 108.0f;

        private readonly GameAudioCommonConfigSerializer _commonConfigSerializer = new GameAudioCommonConfigSerializer();
        private readonly GameAudioPreviewPlaybackService _previewPlaybackService = new GameAudioPreviewPlaybackService();
        private readonly GameAudioProjectSerializer _projectSerializer = new GameAudioProjectSerializer();
        private readonly GameAudioProjectConfigSerializer _projectConfigSerializer = new GameAudioProjectConfigSerializer();
        private readonly GameAudioProjectDirtyState _dirtyState = new GameAudioProjectDirtyState();
        private readonly GameAudioAudioClipConversionService _audioClipConversionService = new GameAudioAudioClipConversionService();
        private readonly GameAudioWavExportService _wavExportService = new GameAudioWavExportService();
        private readonly HashSet<string> _selectedNoteIds = new HashSet<string>(StringComparer.Ordinal);
        private readonly Dictionary<WorkspacePage, Button> _workspaceTabButtons = new Dictionary<WorkspacePage, Button>();
        private readonly Dictionary<WorkspacePage, ScrollView> _workspacePages = new Dictionary<WorkspacePage, ScrollView>();

        private GameAudioCommonConfig _commonConfig;
        private GameAudioProjectConfig _projectConfig;
        private GameAudioEditorSession _editorSession;
        private GameAudioProject _project;
        private string _projectPath = string.Empty;
        private bool _isDirty;
        private List<string> _loadWarnings = new List<string>();
        private string _lastExportedPath = string.Empty;
        private GameAudioExportQualityReport _lastExportQualityReport;
        private string _lastExportComparisonText = string.Empty;
        private bool _normalizeExport;
        private float _exportNormalizeHeadroomDb = -1.0f;
        private string _lastConverted8BitPath = string.Empty;
        private string _lastConverted8BitProjectPath = string.Empty;
        private IVisualElementScheduledItem _previewTicker;
        private TimelineDragState _timelineDragState;
        private Vector2 _timelineScrollPosition;
        private string _selectedTrackId = string.Empty;
        private string _currentGridDivision = "1/16";
        private WorkspacePage _currentWorkspacePage = WorkspacePage.File;
        private GameAudioDisplayLanguage _displayLanguage = GameAudioDisplayLanguage.English;
        private bool _pendingUiRebuild;
        private bool _startupGuideScheduled;
        private AudioClip _conversionSourceClip;
        private string _conversionOutputName = string.Empty;
        private int _conversionTargetSampleRate = 11025;
        private GameAudioConversionChannelMode _conversionChannelMode = GameAudioConversionChannelMode.Mono;
        private string _selectedVoicePresetId = string.Empty;

        private Label _nameValue;
        private Label _bpmValue;
        private Label _barsValue;
        private Label _tracksValue;
        private Label _pathValue;
        private Label _statusValue;
        private HelpBox _warningBox;
        private Label _previewStateValue;
        private Label _previewBufferValue;
        private Label _previewCursorValue;
        private ProgressBar _previewProgressBar;
        private TextField _commonExportDirectoryField;
        private TextField _projectExportDirectoryField;
        private Toggle _autoRefreshAfterExportToggle;
        private Label _exportResolvedPathValue;
        private Label _exportFileNameValue;
        private Label _exportLastResultValue;
        private Label _exportQualityValue;
        private HelpBox _exportQualityHelpBox;
        private Toggle _normalizeExportToggle;
        private FloatField _exportNormalizeHeadroomField;
        private ObjectField _conversionSourceClipField;
        private TextField _conversionOutputNameField;
        private PopupField<int> _conversionSampleRateField;
        private PopupField<GameAudioConversionChannelMode> _conversionChannelModeField;
        private Label _conversionLastResultValue;
        private Label _conversionLastProjectValue;
        private IntegerField _timelineBarsField;
        private IntegerField _toolbarBpmField;
        private PopupField<string> _toolbarGridField;
        private Toggle _toolbarLoopToggle;
        private Toggle _loopToggle;
        private HelpBox _previewHelpBox;
        private Button _undoButton;
        private Button _redoButton;
        private Button _gridButton;
        private VisualElement _selectionInspectorContainer;
        private VisualElement _projectInspectorContainer;
        private IMGUIContainer _timelineSurface;
        private IMGUIContainer _previewWaveformView;
        private string _inspectorStateKey = string.Empty;

        [MenuItem("Tools/Torus Edison/Open Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<GameAudioToolWindow>();
            window.titleContent = new GUIContent(GameAudioToolInfo.DisplayName);
            window.minSize = new Vector2(920.0f, 760.0f);
            window.Show();
        }

        private GameAudioProject CurrentProject => _editorSession?.CurrentProject ?? _project;

        private void CreateGUI()
        {
            _commonConfig = _commonConfigSerializer.LoadOrDefault();
            _projectConfig = _projectConfigSerializer.LoadOrDefault(GameAudioConfigPaths.GetProjectConfigPath(GetProjectRootPath()));
            GameAudioDiagnosticLogger.Configure(_commonConfig);
            _currentGridDivision = GameAudioTimelineGridUtility.NormalizeDivision(_commonConfig.DefaultGridDivision);
            _displayLanguage = GameAudioLocalization.ResolveLanguage(_commonConfig.DisplayLanguage);

            if (_project == null)
            {
                if (!TryRestoreRememberedProject())
                {
                    BindProject(CreateConfiguredProject(), false, string.Empty, Array.Empty<string>());
                }
            }

            rootVisualElement.Clear();
            rootVisualElement.UnregisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);
            rootVisualElement.focusable = true;
            rootVisualElement.tabIndex = 0;
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.paddingLeft = 12;
            rootVisualElement.style.paddingRight = 12;
            rootVisualElement.style.paddingTop = 12;
            rootVisualElement.style.paddingBottom = 12;
            _workspaceTabButtons.Clear();
            _workspacePages.Clear();
            _inspectorStateKey = string.Empty;

            rootVisualElement.Add(BuildToolbar());
            rootVisualElement.Add(BuildNavigationBar());
            rootVisualElement.Add(BuildWorkspacePages());

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);

            _previewTicker?.Pause();
            _previewTicker = rootVisualElement.schedule.Execute(HandlePreviewTick).Every(50);

            SetWorkspacePage(_currentWorkspacePage);
            RefreshView();
            ScheduleStartupGuideIfNeeded();
        }

        private string T(string key, string englishText)
        {
            return GameAudioLocalization.Get(_displayLanguage, key, englishText);
        }

        private string TF(string key, string englishFormat, params object[] args)
        {
            return GameAudioLocalization.Format(_displayLanguage, key, englishFormat, args);
        }

        private VisualElement BuildToolbar()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.flexWrap = Wrap.Wrap;
            container.style.marginBottom = 12;

            container.Add(CreateToolbarButton(T("toolbar.new", "New"), CreateNewProject));
            container.Add(CreateToolbarButton(T("toolbar.open", "Open"), OpenProject));
            container.Add(CreateToolbarButton(T("toolbar.save", "Save"), SaveProject));
            container.Add(CreateToolbarButton(T("toolbar.saveAs", "Save As"), SaveProjectAs));

            return container;
        }

        private VisualElement BuildNavigationBar()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexWrap = Wrap.Wrap;
            container.style.marginBottom = 12.0f;

            AddWorkspaceTabButton(container, WorkspacePage.File, T("workspace.file", "File"));
            AddWorkspaceTabButton(container, WorkspacePage.Edit, T("workspace.edit", "Edit"));
            AddWorkspaceTabButton(container, WorkspacePage.Export, T("workspace.export", "Export"));
            AddWorkspaceTabButton(container, WorkspacePage.Settings, T("workspace.settings", "Settings"));

            return container;
        }

        private VisualElement BuildWorkspacePages()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1.0f;

            container.Add(CreateWorkspacePage(WorkspacePage.File, page =>
            {
                page.Add(BuildPageHeader(
                    T("page.file.title", "File"),
                    T("page.file.description", "Project files, current status, and sample workflows.")));
                page.Add(BuildSummaryPanel());
                page.Add(BuildSamplePanel());
            }));

            container.Add(CreateWorkspacePage(WorkspacePage.Edit, page =>
            {
                page.Add(BuildPageHeader(
                    T("page.edit.title", "Edit"),
                    T("page.edit.description", "Timeline editing, preview playback, and selection-scoped note or track changes in one workspace.")));
                page.Add(BuildPreviewPanel());
                page.Add(BuildTimelinePanel());
                page.Add(BuildSelectionInspectorPanel());
            }));

            container.Add(CreateWorkspacePage(WorkspacePage.Export, page =>
            {
                page.Add(BuildPageHeader(
                    T("page.export.title", "Export"),
                    T("page.export.description", "Write WAV files and confirm the current output destination.")));
                page.Add(BuildExportPanel());
            }));

            container.Add(CreateWorkspacePage(WorkspacePage.Settings, page =>
            {
                page.Add(BuildPageHeader(
                    T("page.settings.title", "Settings"),
                    T("page.settings.description", "Project-level settings and foundation diagnostics.")));
                page.Add(BuildProjectInspectorPanel());
                page.Add(BuildInfoPanel());
            }));

            return container;
        }

        private void AddWorkspaceTabButton(VisualElement parent, WorkspacePage page, string label)
        {
            var button = new Button(() => SetWorkspacePage(page))
            {
                text = label
            };
            button.style.minWidth = 104.0f;
            button.style.marginRight = 8.0f;
            button.style.marginBottom = 4.0f;
            button.style.paddingLeft = 12.0f;
            button.style.paddingRight = 12.0f;
            _workspaceTabButtons[page] = button;
            parent.Add(button);
        }

        private ScrollView CreateWorkspacePage(WorkspacePage page, Action<ScrollView> populate)
        {
            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1.0f;
            scrollView.style.display = DisplayStyle.None;
            _workspacePages[page] = scrollView;
            populate?.Invoke(scrollView);
            return scrollView;
        }

        private static VisualElement BuildPageHeader(string title, string description)
        {
            var container = new VisualElement();
            container.style.marginBottom = 12.0f;

            var titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 15.0f;
            titleLabel.style.marginBottom = 4.0f;
            container.Add(titleLabel);

            var descriptionLabel = new Label(description);
            descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            container.Add(descriptionLabel);

            return container;
        }

        private void SetWorkspacePage(WorkspacePage page)
        {
            if (!_workspacePages.ContainsKey(page))
            {
                page = WorkspacePage.Edit;
            }

            _currentWorkspacePage = page;

            foreach (KeyValuePair<WorkspacePage, ScrollView> entry in _workspacePages)
            {
                entry.Value.style.display = entry.Key == page ? DisplayStyle.Flex : DisplayStyle.None;
            }

            UpdateWorkspaceTabStyles();
            _timelineSurface?.MarkDirtyRepaint();
        }

        private void UpdateWorkspaceTabStyles()
        {
            foreach (KeyValuePair<WorkspacePage, Button> entry in _workspaceTabButtons)
            {
                bool isActive = entry.Key == _currentWorkspacePage;
                entry.Value.style.backgroundColor = isActive ? new Color(0.95f, 0.62f, 0.18f) : new Color(0.20f, 0.20f, 0.20f);
                entry.Value.style.color = isActive ? new Color(0.07f, 0.07f, 0.07f) : new Color(0.92f, 0.92f, 0.92f);
                entry.Value.style.unityFontStyleAndWeight = isActive ? FontStyle.Bold : FontStyle.Normal;
            }
        }

        private VisualElement BuildSummaryPanel()
        {
            var panel = CreateSectionPanel(new Color(0.16f, 0.16f, 0.16f));

            panel.Add(CreateSectionTitle(T("summary.currentProject", "Current Project")));
            _nameValue = AddKeyValue(panel, T("summary.name", "Name"));
            _bpmValue = AddKeyValue(panel, T("summary.bpm", "BPM"));
            _barsValue = AddKeyValue(panel, T("summary.bars", "Bars"));
            _tracksValue = AddKeyValue(panel, T("summary.tracks", "Tracks"));
            _pathValue = AddKeyValue(panel, T("summary.file", "File"));
            _statusValue = AddKeyValue(panel, T("summary.status", "Status"));

            return panel;
        }

        private VisualElement BuildTimelinePanel()
        {
            var panel = CreateSectionPanel(new Color(0.12f, 0.12f, 0.12f));

            panel.Add(CreateSectionTitle(T("timeline.title", "Timeline Editing")));

            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.alignItems = Align.Center;
            actionRow.style.flexWrap = Wrap.Wrap;
            actionRow.style.marginBottom = 8;

            _undoButton = CreateToolbarButton(T("timeline.undo", "Undo"), UndoLastEdit);
            _redoButton = CreateToolbarButton(T("timeline.redo", "Redo"), RedoLastEdit);
            _gridButton = CreateToolbarButton(TF("timeline.grid", "Grid {0}", "1/16"), CycleGridDivision);
            Button helpButton = CreateToolbarButton(T("timeline.help", "?"), ShowTimelineHelp);
            helpButton.style.minWidth = 36.0f;

            actionRow.Add(_undoButton);
            actionRow.Add(_redoButton);
            actionRow.Add(_gridButton);
            actionRow.Add(helpButton);

            _timelineBarsField = new IntegerField
            {
                isDelayed = true
            };
            _timelineBarsField.style.width = 72.0f;
            _timelineBarsField.RegisterValueChangedCallback(OnTimelineBarsChanged);
            actionRow.Add(CreateToolbarValueGroup(T("timeline.bars", "Bars"), _timelineBarsField));

            panel.Add(actionRow);
            panel.Add(CreateInspectorHelpBox(
                T("timeline.lengthHelp", "Use the Bars field here or the Total Bars field in Settings to change project length."),
                HelpBoxMessageType.Info));

            _timelineSurface = new IMGUIContainer(DrawTimelineGui)
            {
                focusable = true
            };
            _timelineSurface.tabIndex = 0;
            _timelineSurface.style.height = TimelineViewportHeight;
            _timelineSurface.style.marginBottom = 4;
            _timelineSurface.RegisterCallback<MouseDownEvent>(_ => FocusTimelineShortcutTarget());
            _timelineSurface.RegisterCallback<KeyDownEvent>(OnRootKeyDown, TrickleDown.TrickleDown);
            panel.Add(_timelineSurface);

            return panel;
        }

        private VisualElement BuildSelectionInspectorPanel()
        {
            var panel = CreateSectionPanel(new Color(0.14f, 0.14f, 0.14f));

            panel.Add(CreateSectionTitle(T("selectionInspector.title", "Selection Inspector")));

            _selectionInspectorContainer = new VisualElement();
            _selectionInspectorContainer.style.marginBottom = 12.0f;
            panel.Add(_selectionInspectorContainer);

            return panel;
        }

        private VisualElement BuildProjectInspectorPanel()
        {
            var panel = CreateSectionPanel(new Color(0.14f, 0.14f, 0.14f));

            panel.Add(CreateSectionTitle(T("projectInspector.title", "Project Inspector")));

            _projectInspectorContainer = new VisualElement();
            panel.Add(_projectInspectorContainer);

            return panel;
        }

        private VisualElement BuildPreviewPanel()
        {
            var panel = CreateSectionPanel(new Color(0.13f, 0.13f, 0.13f));

            panel.Add(CreateSectionTitle(T("preview.title", "Preview Playback")));

            var transportRow = new VisualElement();
            transportRow.style.flexDirection = FlexDirection.Row;
            transportRow.style.alignItems = Align.Center;
            transportRow.style.flexWrap = Wrap.Wrap;
            transportRow.style.marginBottom = 8;

            transportRow.Add(CreateToolbarButton(T("preview.render", "Render Preview"), RenderPreview));
            transportRow.Add(CreateToolbarButton(T("preview.play", "Play"), PlayPreview));
            transportRow.Add(CreateToolbarButton(T("preview.pause", "Pause"), PausePreview));
            transportRow.Add(CreateToolbarButton(T("preview.stop", "Stop"), StopPreview));
            transportRow.Add(CreateToolbarButton(T("preview.rewind", "Rewind"), RewindPreview));

            _loopToggle = new Toggle(T("preview.loop", "Loop"));
            _loopToggle.style.marginLeft = 4;
            _loopToggle.RegisterValueChangedCallback(OnLoopPlaybackChanged);
            transportRow.Add(_loopToggle);

            panel.Add(transportRow);

            _previewStateValue = AddKeyValue(panel, T("preview.key.preview", "Preview"));
            _previewBufferValue = AddKeyValue(panel, T("preview.key.buffer", "Buffer"));
            _previewCursorValue = AddKeyValue(panel, T("preview.key.cursor", "Cursor"));

            _previewProgressBar = new ProgressBar
            {
                title = T("preview.cursorNotStarted", "Cursor not started")
            };
            _previewProgressBar.lowValue = 0.0f;
            _previewProgressBar.highValue = 100.0f;
            _previewProgressBar.style.marginTop = 4;
            panel.Add(_previewProgressBar);

            _previewWaveformView = new IMGUIContainer(DrawPreviewWaveformGui);
            _previewWaveformView.style.height = PreviewWaveformHeight;
            _previewWaveformView.style.marginTop = 8.0f;
            _previewWaveformView.style.marginBottom = 4.0f;
            panel.Add(_previewWaveformView);

            _previewHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning)
            {
                style =
                {
                    display = DisplayStyle.None
                }
            };
            panel.Add(_previewHelpBox);

            return panel;
        }

        private VisualElement BuildExportPanel()
        {
            var panel = CreateSectionPanel(new Color(0.15f, 0.14f, 0.13f));

            panel.Add(CreateSectionTitle(T("export.title", "WAV Export")));

            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.alignItems = Align.Center;
            actionRow.style.flexWrap = Wrap.Wrap;
            actionRow.style.marginBottom = 8.0f;
            actionRow.Add(CreateToolbarButton(T("export.exportWav", "Export WAV"), ExportWav));
            actionRow.Add(CreateToolbarButton(T("export.openFolder", "Open Export Folder"), OpenExportFolder));
            panel.Add(actionRow);

            _exportResolvedPathValue = AddKeyValue(panel, T("export.resolvedFolder", "Resolved Folder"));
            _exportFileNameValue = AddKeyValue(panel, T("export.exportFile", "Export File"));
            _exportLastResultValue = AddKeyValue(panel, T("export.lastExport", "Last Export"));
            _exportQualityValue = AddKeyValue(panel, T("export.quality", "Export Quality"));
            _exportQualityHelpBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning)
            {
                style =
                {
                    display = DisplayStyle.None
                }
            };
            panel.Add(_exportQualityHelpBox);

            _normalizeExportToggle = new Toggle();
            _normalizeExportToggle.RegisterValueChangedCallback(OnNormalizeExportChanged);
            panel.Add(CreateInspectorRow(T("export.normalize", "Normalize Export"), _normalizeExportToggle));

            _exportNormalizeHeadroomField = new FloatField
            {
                isDelayed = true
            };
            _exportNormalizeHeadroomField.RegisterValueChangedCallback(OnExportNormalizeHeadroomChanged);
            panel.Add(CreateInspectorRow(T("export.normalizeHeadroom", "Normalize Headroom dB"), _exportNormalizeHeadroomField));

            _commonExportDirectoryField = new TextField
            {
                isDelayed = true
            };
            _commonExportDirectoryField.RegisterValueChangedCallback(OnCommonExportDirectoryChanged);
            panel.Add(CreateInspectorRow(
                T("export.commonDefaultFolder", "Common Default Folder"),
                CreateExportDirectoryEditor(
                    _commonExportDirectoryField,
                    BrowseCommonExportDirectory,
                    SetCommonExportDirectoryToProjectExports,
                    SetCommonExportDirectoryToAssetsExports,
                    null,
                    null)));

            _projectExportDirectoryField = new TextField
            {
                isDelayed = true
            };
            _projectExportDirectoryField.RegisterValueChangedCallback(OnProjectExportDirectoryChanged);
            panel.Add(CreateInspectorRow(
                T("export.projectOverrideFolder", "Project Override Folder"),
                CreateExportDirectoryEditor(
                    _projectExportDirectoryField,
                    BrowseProjectExportDirectory,
                    SetProjectExportDirectoryToProjectExports,
                    SetProjectExportDirectoryToAssetsExports,
                    ClearProjectExportDirectoryOverride,
                    T("export.clearOverride", "Clear"))));

            panel.Add(CreateInspectorHelpBox(
                T("export.folderHelp", "Folders inside this Unity project are stored as relative paths. External folders stay absolute. Use Project/Exports for project-side files or Assets/Exports when you want Unity to refresh imported WAV files."),
                HelpBoxMessageType.Info));

            _autoRefreshAfterExportToggle = new Toggle();
            _autoRefreshAfterExportToggle.RegisterValueChangedCallback(OnAutoRefreshAfterExportChanged);
            panel.Add(CreateInspectorRow(T("export.autoRefresh", "Auto Refresh Assets"), _autoRefreshAfterExportToggle));

            panel.Add(CreateSectionTitle(T("export.convert8Bit.title", "8-bit WAV Conversion")));
            panel.Add(CreateInspectorHelpBox(
                T("export.convert8Bit.help", "Select an imported AudioClip asset and convert it to 8-bit PCM WAV. Bring your own audio into Unity first and only convert audio you are allowed to use. This tool does not acquire audio from YouTube or other external services."),
                HelpBoxMessageType.Info));

            _conversionSourceClipField = new ObjectField
            {
                objectType = typeof(AudioClip),
                allowSceneObjects = false
            };
            _conversionSourceClipField.RegisterValueChangedCallback(OnConversionSourceClipChanged);
            panel.Add(CreateInspectorRow(T("export.convert8Bit.sourceClip", "Source AudioClip"), _conversionSourceClipField));

            _conversionOutputNameField = new TextField
            {
                isDelayed = true
            };
            _conversionOutputNameField.RegisterValueChangedCallback(OnConversionOutputNameChanged);
            panel.Add(CreateInspectorRow(T("export.convert8Bit.outputName", "Output Name"), _conversionOutputNameField));

            List<int> sampleRateOptions = GetSupportedConversionSampleRates().ToList();
            int selectedSampleRateIndex = Math.Max(0, sampleRateOptions.IndexOf(sampleRateOptions.Contains(_conversionTargetSampleRate)
                ? _conversionTargetSampleRate
                : sampleRateOptions[0]));
            _conversionSampleRateField = new PopupField<int>(
                sampleRateOptions,
                selectedSampleRateIndex,
                FormatSampleRateOption,
                FormatSampleRateOption);
            _conversionSampleRateField.RegisterValueChangedCallback(OnConversionSampleRateChanged);
            panel.Add(CreateInspectorRow(T("export.convert8Bit.sampleRate", "Target Sample Rate"), _conversionSampleRateField));

            List<GameAudioConversionChannelMode> channelModeOptions = GetSupportedConversionChannelModes().ToList();
            int selectedChannelModeIndex = Math.Max(0, channelModeOptions.IndexOf(channelModeOptions.Contains(_conversionChannelMode)
                ? _conversionChannelMode
                : channelModeOptions[0]));
            _conversionChannelModeField = new PopupField<GameAudioConversionChannelMode>(
                channelModeOptions,
                selectedChannelModeIndex,
                FormatConversionChannelMode,
                FormatConversionChannelMode);
            _conversionChannelModeField.RegisterValueChangedCallback(OnConversionChannelModeChanged);
            panel.Add(CreateInspectorRow(T("export.convert8Bit.channelMode", "Channel Mode"), _conversionChannelModeField));

            var conversionActionRow = new VisualElement();
            conversionActionRow.style.flexDirection = FlexDirection.Row;
            conversionActionRow.style.alignItems = Align.Center;
            conversionActionRow.style.flexWrap = Wrap.Wrap;
            conversionActionRow.style.marginBottom = 8.0f;
            conversionActionRow.Add(CreateToolbarButton(T("export.convert8Bit.export", "Convert To 8-bit WAV"), Export8BitWav));
            panel.Add(conversionActionRow);

            _conversionLastResultValue = AddKeyValue(panel, T("export.convert8Bit.lastExport", "Last 8-bit Export"));
            _conversionLastProjectValue = AddKeyValue(panel, T("export.convert8Bit.lastProject", "Last Conversion Project"));

            return panel;
        }

        private VisualElement BuildSamplePanel()
        {
            var panel = CreateSectionPanel(new Color(0.15f, 0.15f, 0.15f));

            panel.Add(CreateSectionTitle(T("sample.title", "Samples And Workflow")));

            var sampleRow = new VisualElement();
            sampleRow.style.flexDirection = FlexDirection.Row;
            sampleRow.style.flexWrap = Wrap.Wrap;
            sampleRow.style.marginBottom = 8;

            sampleRow.Add(CreateToolbarButton(T("sample.create", "Create Samples"), CreateSampleProjects));
            sampleRow.Add(CreateToolbarButton(T("sample.loadBasic", "Load Basic SE"), LoadBasicSampleProject));
            sampleRow.Add(CreateToolbarButton(T("sample.loadLoop", "Load Simple Loop"), LoadSimpleLoopSampleProject));
            sampleRow.Add(CreateToolbarButton(T("sample.openFolder", "Open Folder"), OpenSampleProjectsFolder));

            panel.Add(sampleRow);

            var sampleLocationLabel = new Label(TF("sample.location", "Sample files are stored under {0}", GetUserProjectFolderPath()));
            sampleLocationLabel.style.marginBottom = 4;
            panel.Add(sampleLocationLabel);

            var editingLabel = new Label(T("sample.editing", "Timeline editing and inspector editing are now available. Use the Edit tab to create notes, move them, resize them, and adjust note or track parameters without leaving the editor."));
            editingLabel.style.whiteSpace = WhiteSpace.Normal;
            editingLabel.style.marginBottom = 4;
            panel.Add(editingLabel);

            var fieldsLabel = new Label(T("sample.json", "JSON is still useful for bulk edits, review, and version control, but file actions, preview, export, and settings are now separated into dedicated tabs."));
            fieldsLabel.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(fieldsLabel);

            return panel;
        }

        private VisualElement BuildInfoPanel()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Column;

            panel.Add(CreateSectionTitle(T("info.title", "Foundation Status")));
            var currentScopeLabel = new Label(T("info.currentScope", "This window now separates file, edit, preview, export, and settings workflows while keeping the same project state, selection, playback, Undo / Redo, JSON save/load, and WAV export foundations."));
            currentScopeLabel.style.marginBottom = 4;
            panel.Add(currentScopeLabel);

            var nextScopeLabel = new Label(T("info.nextScope", "Release validation, documentation sync, and distribution packaging are the next layers to connect."));
            nextScopeLabel.style.marginBottom = 8;
            panel.Add(nextScopeLabel);

            _warningBox = new HelpBox(string.Empty, HelpBoxMessageType.Warning)
            {
                style =
                {
                    display = DisplayStyle.None
                }
            };

            panel.Add(_warningBox);
            return panel;
        }

        private Button CreateToolbarButton(string label, Action onClick)
        {
            var button = new Button(onClick)
            {
                text = label
            };

            button.style.minWidth = 84;
            button.style.marginRight = 8;
            button.style.marginBottom = 4;
            return button;
        }

        private VisualElement CreateExportDirectoryEditor(
            TextField field,
            Action browseAction,
            Action useProjectExportsAction,
            Action useAssetsExportsAction,
            Action trailingAction,
            string trailingLabel)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Column;
            container.style.flexGrow = 1.0f;

            var inputRow = new VisualElement();
            inputRow.style.flexDirection = FlexDirection.Row;
            inputRow.style.alignItems = Align.Center;
            inputRow.style.flexGrow = 1.0f;

            field.style.flexGrow = 1.0f;
            inputRow.Add(field);
            inputRow.Add(CreateCompactActionButton(T("export.browse", "Browse"), browseAction));
            container.Add(inputRow);

            var shortcutRow = new VisualElement();
            shortcutRow.style.flexDirection = FlexDirection.Row;
            shortcutRow.style.flexWrap = Wrap.Wrap;
            shortcutRow.style.marginTop = 4.0f;
            shortcutRow.Add(CreateCompactActionButton(T("export.useProjectFolder", "Project/Exports"), useProjectExportsAction));
            shortcutRow.Add(CreateCompactActionButton(T("export.useAssetsFolder", "Assets/Exports"), useAssetsExportsAction));
            if (trailingAction != null && !string.IsNullOrWhiteSpace(trailingLabel))
            {
                shortcutRow.Add(CreateCompactActionButton(trailingLabel, trailingAction));
            }

            container.Add(shortcutRow);
            return container;
        }

        private static Button CreateCompactActionButton(string label, Action onClick)
        {
            var button = new Button(onClick)
            {
                text = label
            };

            button.style.minWidth = 88.0f;
            button.style.height = 22.0f;
            button.style.marginLeft = 6.0f;
            button.style.marginBottom = 4.0f;
            button.style.paddingLeft = 8.0f;
            button.style.paddingRight = 8.0f;
            return button;
        }

        private static VisualElement CreateSectionPanel(Color backgroundColor)
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.marginBottom = 12;
            panel.style.paddingTop = 12;
            panel.style.paddingBottom = 12;
            panel.style.paddingLeft = 12;
            panel.style.paddingRight = 12;
            panel.style.borderTopWidth = 1;
            panel.style.borderRightWidth = 1;
            panel.style.borderBottomWidth = 1;
            panel.style.borderLeftWidth = 1;
            panel.style.borderTopColor = new Color(0.23f, 0.23f, 0.23f);
            panel.style.borderRightColor = new Color(0.23f, 0.23f, 0.23f);
            panel.style.borderBottomColor = new Color(0.23f, 0.23f, 0.23f);
            panel.style.borderLeftColor = new Color(0.23f, 0.23f, 0.23f);
            panel.style.backgroundColor = backgroundColor;
            return panel;
        }

        private static Label AddKeyValue(VisualElement parent, string key)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.marginTop = 3;

            var keyLabel = new Label($"{key}:");
            keyLabel.style.minWidth = 80;
            keyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            var valueLabel = new Label();
            valueLabel.style.flexGrow = 1;
            valueLabel.style.whiteSpace = WhiteSpace.Normal;

            row.Add(keyLabel);
            row.Add(valueLabel);
            parent.Add(row);
            return valueLabel;
        }

        private static Label CreateSectionTitle(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginBottom = 6;
            return label;
        }

        private static VisualElement CreateToolbarValueGroup(string label, VisualElement field)
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.marginRight = 8.0f;
            container.style.marginBottom = 4.0f;

            var labelElement = new Label($"{label}:");
            labelElement.style.minWidth = 40.0f;
            labelElement.style.unityFontStyleAndWeight = FontStyle.Bold;
            labelElement.style.marginRight = 4.0f;
            container.Add(labelElement);
            container.Add(field);
            return container;
        }

        private static Label CreateInspectorGroupTitle(string text)
        {
            var label = new Label(text);
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginTop = 2.0f;
            label.style.marginBottom = 6.0f;
            return label;
        }

        private static VisualElement CreateInspectorRow(string labelText, VisualElement field)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginBottom = 4.0f;

            var label = new Label(labelText);
            label.style.minWidth = InspectorLabelWidth;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            label.style.marginRight = 8.0f;
            row.Add(label);

            field.style.flexGrow = 1.0f;
            row.Add(field);
            return row;
        }

        private static Label CreateInspectorSummaryLabel(string text)
        {
            var label = new Label(text);
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginBottom = 6.0f;
            return label;
        }

        private static HelpBox CreateInspectorHelpBox(string text, HelpBoxMessageType messageType)
        {
            var helpBox = new HelpBox(text, messageType);
            helpBox.style.marginBottom = 6.0f;
            return helpBox;
        }

        private void AddInspectorTextField(VisualElement parent, string label, string value, Action<string> onChanged)
        {
            var field = new TextField
            {
                isDelayed = true
            };
            field.SetValueWithoutNotify(value ?? string.Empty);
            field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            parent.Add(CreateInspectorRow(label, field));
        }

        private void AddInspectorIntegerField(VisualElement parent, string label, int value, Action<int> onChanged)
        {
            var field = new IntegerField
            {
                isDelayed = true
            };
            field.SetValueWithoutNotify(value);
            field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            parent.Add(CreateInspectorRow(label, field));
        }

        private void AddInspectorFloatField(VisualElement parent, string label, float value, Action<float> onChanged)
        {
            var field = new FloatField
            {
                isDelayed = true
            };
            field.SetValueWithoutNotify(value);
            field.RegisterValueChangedCallback(evt =>
            {
                if (!IsFinite(evt.newValue))
                {
                    ShowInvalidNumberMessage(label);
                    _inspectorStateKey = string.Empty;
                    RefreshView();
                    return;
                }

                onChanged?.Invoke(evt.newValue);
            });
            parent.Add(CreateInspectorRow(label, field));
        }

        private void AddInspectorFloatSliderField(
            VisualElement parent,
            string label,
            float value,
            float lowValue,
            float highValue,
            Action<float> onChanged,
            Func<float, string> formatValue = null)
        {
            float actualLow = Math.Min(lowValue, value);
            float actualHigh = Math.Max(highValue, value);

            var fieldContainer = new VisualElement();
            fieldContainer.style.flexDirection = FlexDirection.Row;
            fieldContainer.style.alignItems = Align.Center;
            fieldContainer.style.flexGrow = 1.0f;

            var slider = new Slider(actualLow, actualHigh);
            slider.value = Mathf.Clamp(value, actualLow, actualHigh);
            slider.style.flexGrow = 1.0f;
            slider.style.marginRight = 8.0f;

            float committedValue = slider.value;
            float pendingValue = slider.value;
            var valueLabel = CreateInspectorValueLabel(FormatInspectorValue(slider.value, formatValue));
            slider.RegisterValueChangedCallback(evt =>
            {
                pendingValue = evt.newValue;
                valueLabel.text = FormatInspectorValue(evt.newValue, formatValue);
            });

            void CommitPendingValue()
            {
                if (Mathf.Approximately(pendingValue, committedValue))
                {
                    return;
                }

                committedValue = pendingValue;
                onChanged?.Invoke(pendingValue);
            }

            slider.RegisterCallback<PointerCaptureOutEvent>(_ => CommitPendingValue());
            slider.RegisterCallback<FocusOutEvent>(_ => CommitPendingValue());

            fieldContainer.Add(slider);
            fieldContainer.Add(valueLabel);
            parent.Add(CreateInspectorRow(label, fieldContainer));
        }

        private void AddInspectorIntegerSliderField(
            VisualElement parent,
            string label,
            int value,
            int lowValue,
            int highValue,
            Action<int> onChanged,
            Func<int, string> formatValue = null)
        {
            int actualLow = Math.Min(lowValue, value);
            int actualHigh = Math.Max(highValue, value);

            var fieldContainer = new VisualElement();
            fieldContainer.style.flexDirection = FlexDirection.Row;
            fieldContainer.style.alignItems = Align.Center;
            fieldContainer.style.flexGrow = 1.0f;

            var slider = new SliderInt(actualLow, actualHigh);
            slider.value = Mathf.Clamp(value, actualLow, actualHigh);
            slider.style.flexGrow = 1.0f;
            slider.style.marginRight = 8.0f;

            int committedValue = slider.value;
            int pendingValue = slider.value;
            var valueLabel = CreateInspectorValueLabel(FormatInspectorValue(slider.value, formatValue));
            slider.RegisterValueChangedCallback(evt =>
            {
                pendingValue = evt.newValue;
                valueLabel.text = FormatInspectorValue(evt.newValue, formatValue);
            });

            void CommitPendingValue()
            {
                if (pendingValue == committedValue)
                {
                    return;
                }

                committedValue = pendingValue;
                onChanged?.Invoke(pendingValue);
            }

            slider.RegisterCallback<PointerCaptureOutEvent>(_ => CommitPendingValue());
            slider.RegisterCallback<FocusOutEvent>(_ => CommitPendingValue());

            fieldContainer.Add(slider);
            fieldContainer.Add(valueLabel);
            parent.Add(CreateInspectorRow(label, fieldContainer));
        }

        private void AddInspectorToggleField(VisualElement parent, string label, bool value, Action<bool> onChanged)
        {
            var field = new Toggle();
            field.SetValueWithoutNotify(value);
            field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            parent.Add(CreateInspectorRow(label, field));
        }

        private void AddInspectorPopupField(VisualElement parent, string label, IEnumerable<string> options, string value, Action<string> onChanged)
        {
            List<string> choices = options.ToList();
            int selectedIndex = Math.Max(0, choices.IndexOf(choices.Contains(value) ? value : choices[0]));
            var field = new PopupField<string>(choices, selectedIndex);
            field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            parent.Add(CreateInspectorRow(label, field));
        }

        private void AddInspectorPopupField<TValue>(
            VisualElement parent,
            string label,
            IEnumerable<TValue> options,
            TValue value,
            Func<TValue, string> formatSelectedValue,
            Action<TValue> onChanged)
        {
            List<TValue> choices = options.ToList();
            int selectedIndex = Math.Max(0, choices.IndexOf(choices.Contains(value) ? value : choices[0]));
            var field = new PopupField<TValue>(choices, selectedIndex, formatSelectedValue, formatSelectedValue);
            field.RegisterValueChangedCallback(evt => onChanged?.Invoke(evt.newValue));
            parent.Add(CreateInspectorRow(label, field));
        }

        private void RefreshInspectorPanel(GameAudioProject project)
        {
            if (project == null || _selectionInspectorContainer == null || _projectInspectorContainer == null)
            {
                return;
            }

            string selectionKey = _selectedNoteIds.Count == 0
                ? string.Empty
                : string.Join(",", _selectedNoteIds.OrderBy(noteId => noteId, StringComparer.Ordinal));
            string nextStateKey = string.Format(
                CultureInfo.InvariantCulture,
                "{0}|{1}|{2}|{3}|{4}|{5}|{6}|{7}|{8}|{9}|{10}|{11}",
                RuntimeHelpers.GetHashCode(project),
                _displayLanguage,
                _selectedTrackId,
                selectionKey,
                _commonConfig?.ShowStartupGuide ?? true,
                _commonConfig?.RememberLastProject ?? true,
                _commonConfig?.LastProjectPath ?? string.Empty,
                _commonConfig?.EnableDiagnosticLogging ?? false,
                _commonConfig?.DiagnosticLogLevel ?? GameAudioDiagnosticLogLevel.Info,
                _projectConfig?.PreferredSampleRate?.ToString(CultureInfo.InvariantCulture) ?? string.Empty,
                _projectConfig?.PreferredChannelMode?.ToString() ?? string.Empty,
                _projectConfig?.AutoRefreshAfterExport ?? true);

            if (string.Equals(_inspectorStateKey, nextStateKey, StringComparison.Ordinal))
            {
                return;
            }

            _inspectorStateKey = nextStateKey;
            RebuildInspectorPanel(project);
        }

        private void RebuildInspectorPanel(GameAudioProject project)
        {
            _selectionInspectorContainer.Clear();
            _projectInspectorContainer.Clear();

            _selectionInspectorContainer.Add(CreateInspectorGroupTitle(T("inspector.selection", "Selection")));

            IReadOnlyList<SelectedNoteContext> selectedNotes = GetSelectedNoteContexts(project);
            if (selectedNotes.Count > 0)
            {
                BuildNoteInspector(_selectionInspectorContainer, selectedNotes);
            }
            else
            {
                GameAudioTrack selectedTrack = FindTrackById(project, _selectedTrackId) ?? project.Tracks.FirstOrDefault();
                if (selectedTrack == null)
                {
                    _selectionInspectorContainer.Add(CreateInspectorHelpBox(T("inspector.selectTrackOrNote", "Select a track header or note in the timeline to start editing."), HelpBoxMessageType.Info));
                }
                else
                {
                    BuildTrackInspector(_selectionInspectorContainer, selectedTrack);
                }
            }

            _projectInspectorContainer.Add(CreateInspectorGroupTitle(T("inspector.project", "Project")));
            BuildProjectInspector(_projectInspectorContainer, project);
        }

        private void BuildNoteInspector(VisualElement parent, IReadOnlyList<SelectedNoteContext> selectedNotes)
        {
            SelectedNoteContext primarySelection = selectedNotes[0];
            int trackCount = selectedNotes
                .Select(selection => selection.Track.Id)
                .Distinct(StringComparer.Ordinal)
                .Count();

            string summaryText = selectedNotes.Count == 1
                ? TF("inspector.note.singleSummary", "Editing note {0} on {1}.", primarySelection.Note.Id, primarySelection.Track.Name)
                : TF("inspector.note.multiSummary", "Editing {0} notes across {1} tracks. Changes apply to every selected note.", selectedNotes.Count, trackCount);
            parent.Add(CreateInspectorSummaryLabel(summaryText));

            AddInspectorFloatField(parent, T("inspector.note.startBeat", "Start Beat"), primarySelection.Note.StartBeat, requested =>
            {
                TryApplySelectedNotesChange(
                    "Set Note Start",
                    note => note.StartBeat = requested,
                    actualNotes => NotifyClamp("Start Beat", requested, actualNotes[0].Note.StartBeat));
            });

            AddInspectorFloatField(parent, T("inspector.note.durationBeat", "Duration Beat"), primarySelection.Note.DurationBeat, requested =>
            {
                TryApplySelectedNotesChange(
                    "Set Note Duration",
                    note => note.DurationBeat = requested,
                    actualNotes => NotifyClamp("Duration Beat", requested, actualNotes[0].Note.DurationBeat));
            });

            AddInspectorIntegerSliderField(parent, T("inspector.note.midi", "MIDI Note"), primarySelection.Note.MidiNote, 24, 96, requested =>
            {
                TryApplySelectedNotesChange(
                    "Set Note Pitch",
                    note => note.MidiNote = requested,
                    actualNotes => NotifyClamp("MIDI Note", requested, actualNotes[0].Note.MidiNote));
            }, FormatMidiNoteDisplay);

            AddInspectorFloatSliderField(parent, T("inspector.note.velocity", "Velocity"), primarySelection.Note.Velocity, 0.0f, 1.0f, requested =>
            {
                TryApplySelectedNotesChange(
                    "Set Note Velocity",
                    note => note.Velocity = requested,
                    actualNotes => NotifyClamp("Velocity", requested, actualNotes[0].Note.Velocity));
            }, value => FormatPercentDisplay(value, 0));

            bool allHaveOverride = selectedNotes.All(selection => selection.Note.VoiceOverride != null);
            bool anyHaveOverride = selectedNotes.Any(selection => selection.Note.VoiceOverride != null);
            AddInspectorToggleField(parent, T("inspector.note.useVoiceOverride", "Use Voice Override"), allHaveOverride, enabled =>
            {
                TryApplySelectedNotesChange(
                    enabled ? "Enable Voice Override" : "Disable Voice Override",
                    note => note.VoiceOverride = enabled ? note.VoiceOverride ?? GameAudioProjectFactory.CreateDefaultVoice() : null);
            });

            Action<string, Action<GameAudioVoiceSettings>, Action<GameAudioVoiceSettings>> applySelectedNoteVoiceChange =
                (displayName, applyVoiceChange, afterApply) =>
                {
                    TryApplySelectedNotesChange(
                        displayName,
                        note =>
                        {
                            note.VoiceOverride = note.VoiceOverride ?? GameAudioProjectFactory.CreateDefaultVoice();
                            applyVoiceChange(note.VoiceOverride);
                        },
                        actualNotes => afterApply?.Invoke(actualNotes[0].Note.VoiceOverride ?? GameAudioProjectFactory.CreateDefaultVoice()));
                };

            if (!allHaveOverride)
            {
                AddVoicePresetControls(parent, applySelectedNoteVoiceChange);
                string message = anyHaveOverride
                    ? T("inspector.note.overridePartial", "Some selected notes still use the track default voice. Enable voice override to apply explicit note-level voice settings to the full selection.")
                    : T("inspector.note.overrideNone", "Selected notes currently use each track's default voice. Enable voice override to edit per-note voice settings.");
                parent.Add(CreateInspectorHelpBox(message, HelpBoxMessageType.Info));
                return;
            }

            GameAudioVoiceSettings voice = primarySelection.Note.VoiceOverride ?? GameAudioProjectFactory.CreateDefaultVoice();
            AddVoiceInspector(
                parent,
                T("voice.override", "Voice Override"),
                voice,
                applySelectedNoteVoiceChange);
        }

        private void BuildTrackInspector(VisualElement parent, GameAudioTrack track)
        {
            parent.Add(CreateInspectorSummaryLabel(TF("inspector.track.summary", "Editing {0}. Notes: {1}.", track.Name, track.Notes.Count)));

            AddInspectorTextField(parent, T("inspector.track.name", "Track Name"), track.Name, requested =>
            {
                if (string.Equals(track.Name, requested, StringComparison.Ordinal))
                {
                    return;
                }

                TryApplyTrackChange("Rename Track", track.Id, current => current.Name = requested);
            });

            AddInspectorToggleField(parent, T("inspector.track.mute", "Mute"), track.Mute, requested =>
            {
                if (track.Mute == requested)
                {
                    return;
                }

                TryApplyTrackChange("Toggle Mute", track.Id, current => current.Mute = requested);
            });

            AddInspectorToggleField(parent, T("inspector.track.solo", "Solo"), track.Solo, requested =>
            {
                if (track.Solo == requested)
                {
                    return;
                }

                TryApplyTrackChange("Toggle Solo", track.Id, current => current.Solo = requested);
            });

            AddInspectorFloatSliderField(parent, T("inspector.track.volume", "Volume (dB)"), track.VolumeDb, -48.0f, 6.0f, requested =>
            {
                TryApplyTrackChange(
                    "Set Track Volume",
                    track.Id,
                    current => current.VolumeDb = requested,
                    actualTrack => NotifyClamp("Track Volume", requested, actualTrack.VolumeDb));
            }, FormatDecibelDisplay);

            AddInspectorFloatSliderField(parent, T("inspector.track.pan", "Pan"), track.Pan, -1.0f, 1.0f, requested =>
            {
                TryApplyTrackChange(
                    "Set Track Pan",
                    track.Id,
                    current => current.Pan = requested,
                    actualTrack => NotifyClamp("Track Pan", requested, actualTrack.Pan));
            }, value => value.ToString("0.00", CultureInfo.InvariantCulture));

            AddVoiceInspector(
                parent,
                T("voice.default", "Default Voice"),
                track.DefaultVoice ?? GameAudioProjectFactory.CreateDefaultVoice(),
                (displayName, applyVoiceChange, afterApply) =>
                {
                    TryApplyTrackChange(
                        displayName,
                        track.Id,
                        current =>
                        {
                            current.DefaultVoice ??= GameAudioProjectFactory.CreateDefaultVoice();
                            applyVoiceChange(current.DefaultVoice);
                        },
                        actualTrack => afterApply?.Invoke(actualTrack.DefaultVoice ?? GameAudioProjectFactory.CreateDefaultVoice()));
                });
        }

        private void BuildProjectInspector(VisualElement parent, GameAudioProject project)
        {
            parent.Add(CreateInspectorSummaryLabel(T("inspector.project.summary", "Transport, output, and render settings for the current project.")));

            AddInspectorTextField(parent, T("inspector.project.name", "Project Name"), project.Name, requested =>
            {
                if (string.Equals(project.Name, requested, StringComparison.Ordinal))
                {
                    return;
                }

                TryApplyProjectChange("Rename Project", current => current.Name = requested);
            });

            AddInspectorIntegerSliderField(parent, T("summary.bpm", "BPM"), project.Bpm, 40, 240, requested =>
            {
                TryApplyProjectChange(
                    "Set BPM",
                    current => current.Bpm = requested,
                    actualProject => NotifyClamp("BPM", requested, actualProject.Bpm));
            }, value => string.Format(CultureInfo.InvariantCulture, "{0} BPM", value));

            AddInspectorPopupField(parent, T("inspector.project.timeSignature", "Time Signature"), GetSupportedTimeSignatureOptions(), FormatTimeSignature(project.TimeSignature), requested =>
            {
                TryApplyProjectChange(
                    "Set Time Signature",
                    current => current.TimeSignature = ParseTimeSignature(requested));
            });

            AddInspectorIntegerSliderField(parent, T("inspector.project.totalBars", "Total Bars"), project.TotalBars, 1, GameAudioToolInfo.MaxTotalBars, requested =>
            {
                TryApplyProjectChange(
                    "Set Total Bars",
                    current => current.TotalBars = requested,
                    actualProject => NotifyClamp("Total Bars", requested, actualProject.TotalBars));
            }, value => string.Format(CultureInfo.InvariantCulture, "{0} bars", value));

            AddInspectorPopupField(parent, T("inspector.project.sampleRate", "Sample Rate"), GetSupportedSampleRateOptions(), FormatSampleRateOption(project.SampleRate), requested =>
            {
                TryApplyProjectChange(
                    "Set Sample Rate",
                    current => current.SampleRate = ParseSampleRateOption(requested));
            });

            AddInspectorPopupField(
                parent,
                T("inspector.project.channelMode", "Channel Mode"),
                GetSupportedChannelModeOptions(),
                project.ChannelMode,
                option => GameAudioLocalization.GetChannelModeLabel(_displayLanguage, option),
                requested =>
                {
                    TryApplyProjectChange(
                        "Set Channel Mode",
                        current => current.ChannelMode = requested);
                });

            AddInspectorFloatSliderField(parent, T("inspector.project.masterGain", "Master Gain (dB)"), project.MasterGainDb, -24.0f, 6.0f, requested =>
            {
                TryApplyProjectChange(
                    "Set Master Gain",
                    current => current.MasterGainDb = requested,
                    actualProject => NotifyClamp("Master Gain", requested, actualProject.MasterGainDb));
            }, FormatDecibelDisplay);

            parent.Add(CreateInspectorGroupTitle(T("inspector.projectDefaults", "New Project Defaults")));
            AddInspectorPopupField(
                parent,
                T("inspector.projectDefault.sampleRate", "Sample Rate Override"),
                GetProjectSampleRateOverrideOptions(),
                FormatProjectSampleRateOverride(_projectConfig?.PreferredSampleRate),
                OnProjectPreferredSampleRateChanged);
            AddInspectorPopupField(
                parent,
                T("inspector.projectDefault.channelMode", "Channel Mode Override"),
                GetProjectChannelModeOverrideOptions(),
                FormatProjectChannelModeOverride(_projectConfig?.PreferredChannelMode),
                OnProjectPreferredChannelModeChanged);
            parent.Add(CreateInspectorHelpBox(
                T("inspector.projectDefaults.help", "These project settings override common defaults when New creates a project. Use Common Default keeps the shared setting in control."),
                HelpBoxMessageType.Info));

            parent.Add(CreateInspectorGroupTitle(T("inspector.toolSettings", "Tool Settings")));
            AddInspectorPopupField(
                parent,
                T("inspector.language", "Display Language"),
                GameAudioLocalization.GetSupportedLanguageModes(),
                _commonConfig?.DisplayLanguage ?? GameAudioLanguageMode.Auto,
                option => GameAudioLocalization.GetLanguageModeLabel(_displayLanguage, option),
                OnDisplayLanguageChanged);
            parent.Add(CreateInspectorHelpBox(
                T("inspector.language.help", "Auto follows the current Unity Editor language when available. Override is useful for support and screenshot consistency."),
                HelpBoxMessageType.Info));
            AddInspectorToggleField(
                parent,
                T("inspector.showStartupGuide", "Show Startup Guide"),
                _commonConfig?.ShowStartupGuide ?? true,
                OnShowStartupGuideChanged);
            AddInspectorToggleField(
                parent,
                T("inspector.rememberLastProject", "Remember Last Project"),
                _commonConfig?.RememberLastProject ?? true,
                OnRememberLastProjectChanged);
            if (_commonConfig?.RememberLastProject == true && !string.IsNullOrWhiteSpace(_commonConfig.LastProjectPath))
            {
                parent.Add(CreateInspectorSummaryLabel(TF("inspector.lastProjectPath", "Last project: {0}", _commonConfig.LastProjectPath)));
            }

            parent.Add(CreateInspectorHelpBox(
                T("inspector.startup.help", "The startup guide appears once by default and can be re-enabled here. When Remember Last Project is enabled, Torus Edison restores the last saved or opened .gats.json file on startup."),
                HelpBoxMessageType.Info));
            AddInspectorToggleField(
                parent,
                T("inspector.debugMode", "Debug Mode"),
                _commonConfig?.EnableDiagnosticLogging ?? false,
                OnDiagnosticLoggingChanged);
            AddInspectorPopupField(
                parent,
                T("inspector.logLevel", "Log Level"),
                GetSupportedDiagnosticLogLevels(),
                _commonConfig?.DiagnosticLogLevel ?? GameAudioDiagnosticLogLevel.Info,
                option => GameAudioLocalization.GetDiagnosticLogLevelLabel(_displayLanguage, option),
                OnDiagnosticLogLevelChanged);
            parent.Add(CreateInspectorHelpBox(
                T("inspector.diagnostics.help", "When enabled, Torus Edison writes diagnostic flow and failure logs to the Unity Console. Increase the level when troubleshooting with end users."),
                HelpBoxMessageType.Info));
        }

        private void AddVoicePresetControls(
            VisualElement parent,
            Action<string, Action<GameAudioVoiceSettings>, Action<GameAudioVoiceSettings>> applyVoiceChange)
        {
            IReadOnlyList<GameAudioVoicePreset> presets = GameAudioVoicePresetLibrary.BuiltInPresets;
            if (presets.Count == 0)
            {
                return;
            }

            string selectedPresetId = ResolveSelectedVoicePresetId();
            AddInspectorPopupField(
                parent,
                T("voice.preset", "Voice Preset"),
                presets.Select(preset => preset.Id),
                selectedPresetId,
                GameAudioVoicePresetLibrary.FormatLabel,
                requested => _selectedVoicePresetId = requested);

            var applyButton = CreateCompactActionButton(T("voice.applyPreset", "Apply Preset"), () =>
            {
                string presetId = ResolveSelectedVoicePresetId();
                if (!GameAudioVoicePresetLibrary.TryGetPreset(presetId, out GameAudioVoicePreset preset))
                {
                    return;
                }

                applyVoiceChange(
                    $"Apply Voice Preset: {preset.DisplayName}",
                    current => GameAudioVoicePresetLibrary.CopyTo(preset.Voice, current),
                    null);
            });
            parent.Add(CreateInspectorRow(T("voice.presetAction", "Preset Action"), applyButton));
        }

        private string ResolveSelectedVoicePresetId()
        {
            if (GameAudioVoicePresetLibrary.TryGetPreset(_selectedVoicePresetId, out _))
            {
                return _selectedVoicePresetId;
            }

            _selectedVoicePresetId = GameAudioVoicePresetLibrary.BuiltInPresets[0].Id;
            return _selectedVoicePresetId;
        }

        private void AddVoiceInspector(
            VisualElement parent,
            string header,
            GameAudioVoiceSettings voice,
            Action<string, Action<GameAudioVoiceSettings>, Action<GameAudioVoiceSettings>> applyVoiceChange)
        {
            var voiceFoldout = new Foldout
            {
                text = header,
                value = true
            };
            voiceFoldout.style.marginTop = 6.0f;
            voiceFoldout.style.marginBottom = 6.0f;
            parent.Add(voiceFoldout);

            AddVoicePresetControls(voiceFoldout, applyVoiceChange);

            AddInspectorPopupField(
                voiceFoldout,
                T("voice.waveform", "Waveform"),
                GetSupportedWaveformOptions(),
                voice.Waveform,
                option => GameAudioLocalization.GetWaveformLabel(_displayLanguage, option),
                requested =>
                {
                    applyVoiceChange("Set Waveform", current => current.Waveform = requested, null);
                });

            AddInspectorFloatSliderField(voiceFoldout, T("voice.pulseWidth", "Pulse Width"), voice.PulseWidth, 0.10f, 0.90f, requested =>
            {
                applyVoiceChange(
                    "Set Pulse Width",
                    current => current.PulseWidth = requested,
                    actualVoice => NotifyClamp("Pulse Width", requested, actualVoice.PulseWidth));
            }, value => FormatPercentDisplay(value, 0));

            AddInspectorToggleField(voiceFoldout, T("voice.noiseEnabled", "Noise Enabled"), voice.NoiseEnabled, requested =>
            {
                applyVoiceChange("Toggle Noise", current => current.NoiseEnabled = requested, null);
            });

            AddInspectorPopupField(
                voiceFoldout,
                T("voice.noiseType", "Noise Type"),
                GetSupportedNoiseTypeOptions(),
                voice.NoiseType,
                option => GameAudioLocalization.GetNoiseTypeLabel(_displayLanguage, option),
                requested =>
                {
                    applyVoiceChange("Set Noise Type", current => current.NoiseType = requested, null);
                });

            AddInspectorFloatSliderField(voiceFoldout, T("voice.noiseMix", "Noise Mix"), voice.NoiseMix, 0.0f, 1.0f, requested =>
            {
                applyVoiceChange(
                    "Set Noise Mix",
                    current => current.NoiseMix = requested,
                    actualVoice => NotifyClamp("Noise Mix", requested, actualVoice.NoiseMix));
            }, value => FormatPercentDisplay(value, 0));

            var envelopeFoldout = new Foldout
            {
                text = T("voice.envelope", "Envelope"),
                value = false
            };
            voiceFoldout.Add(envelopeFoldout);

            AddInspectorIntegerSliderField(envelopeFoldout, T("voice.attack", "Attack (ms)"), voice.Adsr.AttackMs, 0, 5000, requested =>
            {
                applyVoiceChange(
                    "Set Attack",
                    current => current.Adsr.AttackMs = requested,
                    actualVoice => NotifyClamp("Attack", requested, actualVoice.Adsr.AttackMs));
            }, FormatMillisecondsDisplay);

            AddInspectorIntegerSliderField(envelopeFoldout, T("voice.decay", "Decay (ms)"), voice.Adsr.DecayMs, 0, 5000, requested =>
            {
                applyVoiceChange(
                    "Set Decay",
                    current => current.Adsr.DecayMs = requested,
                    actualVoice => NotifyClamp("Decay", requested, actualVoice.Adsr.DecayMs));
            }, FormatMillisecondsDisplay);

            AddInspectorFloatSliderField(envelopeFoldout, T("voice.sustain", "Sustain"), voice.Adsr.Sustain, 0.0f, 1.0f, requested =>
            {
                applyVoiceChange(
                    "Set Sustain",
                    current => current.Adsr.Sustain = requested,
                    actualVoice => NotifyClamp("Sustain", requested, actualVoice.Adsr.Sustain));
            }, value => FormatPercentDisplay(value, 0));

            AddInspectorIntegerSliderField(envelopeFoldout, T("voice.release", "Release (ms)"), voice.Adsr.ReleaseMs, 0, 5000, requested =>
            {
                applyVoiceChange(
                    "Set Release",
                    current => current.Adsr.ReleaseMs = requested,
                    actualVoice => NotifyClamp("Release", requested, actualVoice.Adsr.ReleaseMs));
            }, FormatMillisecondsDisplay);

            var effectFoldout = new Foldout
            {
                text = T("voice.effect", "Effect"),
                value = false
            };
            voiceFoldout.Add(effectFoldout);

            AddInspectorFloatSliderField(effectFoldout, T("voice.effectVolume", "Volume (dB)"), voice.Effect.VolumeDb, -48.0f, 6.0f, requested =>
            {
                applyVoiceChange(
                    "Set Voice Volume",
                    current => current.Effect.VolumeDb = requested,
                    actualVoice => NotifyClamp("Voice Volume", requested, actualVoice.Effect.VolumeDb));
            }, FormatDecibelDisplay);

            AddInspectorFloatSliderField(effectFoldout, T("voice.effectPan", "Pan"), voice.Effect.Pan, -1.0f, 1.0f, requested =>
            {
                applyVoiceChange(
                    "Set Voice Pan",
                    current => current.Effect.Pan = requested,
                    actualVoice => NotifyClamp("Voice Pan", requested, actualVoice.Effect.Pan));
            }, value => value.ToString("0.00", CultureInfo.InvariantCulture));

            AddInspectorFloatSliderField(effectFoldout, T("voice.effectPitch", "Pitch (semitone)"), voice.Effect.PitchSemitone, -24.0f, 24.0f, requested =>
            {
                applyVoiceChange(
                    "Set Voice Pitch",
                    current => current.Effect.PitchSemitone = requested,
                    actualVoice => NotifyClamp("Voice Pitch", requested, actualVoice.Effect.PitchSemitone));
            }, value => string.Format(CultureInfo.InvariantCulture, "{0:+0.0;-0.0;0.0} st", value));

            AddInspectorIntegerSliderField(effectFoldout, T("voice.fadeIn", "Fade In (ms)"), voice.Effect.FadeInMs, 0, 3000, requested =>
            {
                applyVoiceChange(
                    "Set Fade In",
                    current => current.Effect.FadeInMs = requested,
                    actualVoice => NotifyClamp("Fade In", requested, actualVoice.Effect.FadeInMs));
            }, FormatMillisecondsDisplay);

            AddInspectorIntegerSliderField(effectFoldout, T("voice.fadeOut", "Fade Out (ms)"), voice.Effect.FadeOutMs, 0, 3000, requested =>
            {
                applyVoiceChange(
                    "Set Fade Out",
                    current => current.Effect.FadeOutMs = requested,
                    actualVoice => NotifyClamp("Fade Out", requested, actualVoice.Effect.FadeOutMs));
            }, FormatMillisecondsDisplay);

            var delayFoldout = new Foldout
            {
                text = T("voice.delay", "Delay"),
                value = false
            };
            effectFoldout.Add(delayFoldout);

            AddInspectorToggleField(delayFoldout, T("voice.delayEnabled", "Enabled"), voice.Effect.Delay.Enabled, requested =>
            {
                applyVoiceChange("Toggle Delay", current => current.Effect.Delay.Enabled = requested, null);
            });

            AddInspectorIntegerSliderField(delayFoldout, T("voice.delayTime", "Time (ms)"), voice.Effect.Delay.TimeMs, 20, 1000, requested =>
            {
                applyVoiceChange(
                    "Set Delay Time",
                    current => current.Effect.Delay.TimeMs = requested,
                    actualVoice => NotifyClamp("Delay Time", requested, actualVoice.Effect.Delay.TimeMs));
            }, FormatMillisecondsDisplay);

            AddInspectorFloatSliderField(delayFoldout, T("voice.delayFeedback", "Feedback"), voice.Effect.Delay.Feedback, 0.0f, 0.70f, requested =>
            {
                applyVoiceChange(
                    "Set Delay Feedback",
                    current => current.Effect.Delay.Feedback = requested,
                    actualVoice => NotifyClamp("Delay Feedback", requested, actualVoice.Effect.Delay.Feedback));
            }, value => FormatPercentDisplay(value, 0));

            AddInspectorFloatSliderField(delayFoldout, T("voice.delayMix", "Mix"), voice.Effect.Delay.Mix, 0.0f, 1.0f, requested =>
            {
                applyVoiceChange(
                    "Set Delay Mix",
                    current => current.Effect.Delay.Mix = requested,
                    actualVoice => NotifyClamp("Delay Mix", requested, actualVoice.Effect.Delay.Mix));
            }, value => FormatPercentDisplay(value, 0));
        }

        private void CreateNewProject()
        {
            if (!ConfirmDiscardIfDirty())
            {
                return;
            }

            BindProject(CreateConfiguredProject(), false, string.Empty, Array.Empty<string>());
            RememberProjectPath(string.Empty);
            RefreshView();
        }

        private IReadOnlyList<SelectedNoteContext> GetSelectedNoteContexts(GameAudioProject project)
        {
            var selections = new List<SelectedNoteContext>();
            if (project?.Tracks == null || _selectedNoteIds.Count == 0)
            {
                return selections;
            }

            foreach (GameAudioTrack track in project.Tracks)
            {
                foreach (GameAudioNote note in track.Notes)
                {
                    if (_selectedNoteIds.Contains(note.Id))
                    {
                        selections.Add(new SelectedNoteContext(track, note));
                    }
                }
            }

            return selections;
        }

        private static GameAudioTrack FindTrackById(GameAudioProject project, string trackId)
        {
            return project?.Tracks?.FirstOrDefault(track => string.Equals(track.Id, trackId, StringComparison.Ordinal));
        }

        private static IEnumerable<string> GetSupportedTimeSignatureOptions()
        {
            yield return "4/4";
            yield return "3/4";
            yield return "6/8";
        }

        private static IEnumerable<string> GetSupportedSampleRateOptions()
        {
            yield return FormatSampleRateOption(GameAudioToolInfo.DefaultSampleRate);
            yield return FormatSampleRateOption(GameAudioToolInfo.AlternateSampleRate);
        }

        private IEnumerable<string> GetProjectSampleRateOverrideOptions()
        {
            yield return T("settings.inheritCommonDefault", "Use Common Default");
            foreach (string option in GetSupportedSampleRateOptions())
            {
                yield return option;
            }
        }

        private static IEnumerable<GameAudioDiagnosticLogLevel> GetSupportedDiagnosticLogLevels()
        {
            return (GameAudioDiagnosticLogLevel[])Enum.GetValues(typeof(GameAudioDiagnosticLogLevel));
        }

        private static IEnumerable<GameAudioChannelMode> GetSupportedChannelModeOptions()
        {
            return (GameAudioChannelMode[])Enum.GetValues(typeof(GameAudioChannelMode));
        }

        private IEnumerable<string> GetProjectChannelModeOverrideOptions()
        {
            yield return T("settings.inheritCommonDefault", "Use Common Default");
            foreach (GameAudioChannelMode option in GetSupportedChannelModeOptions())
            {
                yield return GameAudioLocalization.GetChannelModeLabel(_displayLanguage, option);
            }
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

        private static Label CreateInspectorValueLabel(string text)
        {
            var label = new Label(text ?? string.Empty);
            label.style.minWidth = InspectorValueWidth;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            label.style.color = new Color(0.78f, 0.82f, 0.88f);
            return label;
        }

        private static string FormatInspectorValue(float value, Func<float, string> formatValue)
        {
            return formatValue?.Invoke(value)
                ?? value.ToString("0.00", CultureInfo.InvariantCulture);
        }

        private static string FormatInspectorValue(int value, Func<int, string> formatValue)
        {
            return formatValue?.Invoke(value)
                ?? value.ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatMidiNoteDisplay(int midiNote)
        {
            string[] noteNames = { "C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B" };
            int normalized = Mathf.Clamp(midiNote, 0, 127);
            string noteName = noteNames[normalized % noteNames.Length];
            int octave = (normalized / 12) - 1;
            return string.Format(CultureInfo.InvariantCulture, "{0}{1} ({2})", noteName, octave, normalized);
        }

        private static string FormatPercentDisplay(float value, int decimals)
        {
            float percentage = value * 100.0f;
            string format = decimals <= 0 ? "0" : "0." + new string('0', decimals);
            return string.Format(CultureInfo.InvariantCulture, "{0}%", percentage.ToString(format, CultureInfo.InvariantCulture));
        }

        private static string FormatMillisecondsDisplay(int value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} ms", value);
        }

        private static string FormatDecibelDisplay(float value)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0:+0.0;-0.0;0.0} dB", value);
        }

        private static IEnumerable<GameAudioWaveformType> GetSupportedWaveformOptions()
        {
            return (GameAudioWaveformType[])Enum.GetValues(typeof(GameAudioWaveformType));
        }

        private static IEnumerable<GameAudioNoiseType> GetSupportedNoiseTypeOptions()
        {
            return (GameAudioNoiseType[])Enum.GetValues(typeof(GameAudioNoiseType));
        }

        private static string FormatTimeSignature(GameAudioTimeSignature timeSignature)
        {
            return $"{timeSignature?.Numerator ?? 4}/{timeSignature?.Denominator ?? 4}";
        }

        private static GameAudioTimeSignature ParseTimeSignature(string option)
        {
            return option switch
            {
                "3/4" => new GameAudioTimeSignature { Numerator = 3, Denominator = 4 },
                "6/8" => new GameAudioTimeSignature { Numerator = 6, Denominator = 8 },
                _ => new GameAudioTimeSignature { Numerator = 4, Denominator = 4 }
            };
        }

        private static string FormatSampleRateOption(int sampleRate)
        {
            return string.Format(CultureInfo.InvariantCulture, "{0} Hz", sampleRate);
        }

        private string FormatProjectSampleRateOverride(int? sampleRate)
        {
            return sampleRate.HasValue
                ? FormatSampleRateOption(sampleRate.Value)
                : T("settings.inheritCommonDefault", "Use Common Default");
        }

        private string FormatProjectChannelModeOverride(GameAudioChannelMode? channelMode)
        {
            return channelMode.HasValue
                ? GameAudioLocalization.GetChannelModeLabel(_displayLanguage, channelMode.Value)
                : T("settings.inheritCommonDefault", "Use Common Default");
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

        private static int ParseSampleRateOption(string option)
        {
            string digits = new string((option ?? string.Empty).Where(char.IsDigit).ToArray());
            return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : GameAudioToolInfo.DefaultSampleRate;
        }

        private static int? ParseProjectSampleRateOverride(string option)
        {
            string digits = new string((option ?? string.Empty).Where(char.IsDigit).ToArray());
            if (int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                && GameAudioValidationUtility.IsSupportedSampleRate(parsed))
            {
                return parsed;
            }

            return null;
        }

        private GameAudioChannelMode? ParseProjectChannelModeOverride(string option)
        {
            foreach (GameAudioChannelMode channelMode in GetSupportedChannelModeOptions())
            {
                if (string.Equals(option, channelMode.ToString(), StringComparison.OrdinalIgnoreCase)
                    || string.Equals(option, GameAudioLocalization.GetChannelModeLabel(_displayLanguage, channelMode), StringComparison.Ordinal))
                {
                    return channelMode;
                }
            }

            return null;
        }

        private string GetResolvedConversionOutputName()
        {
            if (!string.IsNullOrWhiteSpace(_conversionOutputName))
            {
                return _conversionOutputName;
            }

            if (_conversionSourceClip != null && !string.IsNullOrWhiteSpace(_conversionSourceClip.name))
            {
                return $"{_conversionSourceClip.name}_8bit";
            }

            return "Converted8Bit";
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void NotifyClamp(string fieldName, int requestedValue, int actualValue)
        {
            if (requestedValue != actualValue)
            {
                ShowNotification(new GUIContent(TF("notification.clampedInt", "{0} clamped to {1}.", fieldName, actualValue)));
            }
        }

        private void NotifyClamp(string fieldName, float requestedValue, float actualValue)
        {
            if (Math.Abs(requestedValue - actualValue) > 0.0001f)
            {
                ShowNotification(new GUIContent(TF("notification.clampedFloat", "{0} clamped to {1:0.###}.", fieldName, actualValue)));
            }
        }

        private void ShowInvalidNumberMessage(string fieldName)
        {
            ShowNotification(new GUIContent(TF("notification.requiresFinite", "{0} requires a finite number.", fieldName)));
        }

        private void ShowEditorException(Exception exception, string area = "Window", string context = null)
        {
            GameAudioDiagnosticLogger.Exception(area, exception, context);
            EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, T("dialog.ok", "OK"));
        }

        private void TryApplyProjectChange(string displayName, Action<GameAudioProject> applyChange, Action<GameAudioProject> afterApply = null)
        {
            GameAudioProject project = CurrentProject;
            if (project == null)
            {
                return;
            }

            try
            {
                ApplyEditorCommand(
                    GameAudioProjectCommandFactory.ChangeProject(project, displayName, applyChange),
                    false,
                    () => afterApply?.Invoke(CurrentProject));
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Edit", $"Project change failed: {displayName}");
            }
        }

        private void TryApplyTrackChange(string displayName, string trackId, Action<GameAudioTrack> applyChange, Action<GameAudioTrack> afterApply = null)
        {
            GameAudioProject project = CurrentProject;
            if (project == null || string.IsNullOrWhiteSpace(trackId))
            {
                return;
            }

            try
            {
                ApplyEditorCommand(
                    GameAudioProjectCommandFactory.ChangeTracks(project, new[] { trackId }, displayName, applyChange),
                    false,
                    () =>
                    {
                        GameAudioTrack updatedTrack = FindTrackById(CurrentProject, trackId);
                        if (updatedTrack != null)
                        {
                            afterApply?.Invoke(updatedTrack);
                        }
                    });
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Edit", $"Track change failed: {displayName}");
            }
        }

        private void TryApplySelectedNotesChange(string displayName, Action<GameAudioNote> applyChange, Action<IReadOnlyList<SelectedNoteContext>> afterApply = null)
        {
            GameAudioProject project = CurrentProject;
            if (project == null || _selectedNoteIds.Count == 0)
            {
                return;
            }

            string[] noteIds = _selectedNoteIds.ToArray();
            try
            {
                ApplyEditorCommand(
                    GameAudioProjectCommandFactory.ChangeNotes(project, noteIds, displayName, applyChange),
                    false,
                    () =>
                    {
                        IReadOnlyList<SelectedNoteContext> updatedNotes = GetSelectedNoteContexts(CurrentProject);
                        if (updatedNotes.Count > 0)
                        {
                            afterApply?.Invoke(updatedNotes);
                        }
                    });
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Edit", $"Note change failed: {displayName}");
            }
        }

        private void OpenProject()
        {
            if (!ConfirmDiscardIfDirty())
            {
                return;
            }

            string selectedPath = EditorUtility.OpenFilePanel(
                T("dialog.openProject", "Open Game Audio Project"),
                string.IsNullOrWhiteSpace(_projectPath) ? UnityEngine.Application.dataPath : _projectPath,
                "json");
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            LoadProjectFromPath(selectedPath);
        }

        private void SaveProject()
        {
            if (string.IsNullOrWhiteSpace(_projectPath))
            {
                SaveProjectAs();
                return;
            }

            TrySaveToPath(_projectPath);
        }

        private void SaveProjectAs()
        {
            GameAudioProject project = CurrentProject;
            string projectName = project == null ? "New Audio Project" : project.Name;
            string defaultFileName = $"{GameAudioValidationUtility.SanitizeExportFileName(projectName)}{GameAudioToolInfo.SessionFileExtension}";
            string selectedPath = EditorUtility.SaveFilePanel(
                T("dialog.saveProject", "Save Game Audio Project"),
                string.IsNullOrWhiteSpace(_projectPath) ? UnityEngine.Application.dataPath : Path.GetDirectoryName(_projectPath),
                defaultFileName,
                "json");

            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            TrySaveToPath(selectedPath);
        }

        private void TrySaveToPath(string targetPath)
        {
            try
            {
                string resolvedPath = GameAudioProjectFileUtility.NormalizeSavePath(targetPath);
                _projectSerializer.SaveToFile(resolvedPath, CurrentProject);
                _projectPath = resolvedPath;
                MarkProjectClean();
                RememberProjectPath(resolvedPath);
                GameAudioDiagnosticLogger.Info("Project", $"Saved project to {resolvedPath}.");
                ShowNotification(new GUIContent(T("status.projectSaved", "Project saved.")));
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Project", $"Saving project failed: {targetPath}");
            }
        }

        private bool ConfirmDiscardIfDirty()
        {
            if (!_isDirty)
            {
                return true;
            }

            int choice = EditorUtility.DisplayDialogComplex(
                GameAudioToolInfo.DisplayName,
                T("dialog.discardMessage", "Unsaved changes will be lost. Continue?"),
                T("dialog.discard", "Discard Changes"),
                T("dialog.cancel", "Cancel"),
                T("dialog.saveFirst", "Save First"));

            if (choice == 0)
            {
                return true;
            }

            if (choice == 2)
            {
                SaveProject();
                return !_isDirty;
            }

            return false;
        }

        private GameAudioProject CreateConfiguredProject()
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            _projectConfig ??= _projectConfigSerializer.LoadOrDefault(GameAudioConfigPaths.GetProjectConfigPath(GetProjectRootPath()));
            int sampleRate = GameAudioConfigResolver.ResolveSampleRate(_commonConfig, _projectConfig);
            GameAudioChannelMode channelMode = GameAudioConfigResolver.ResolveChannelMode(_commonConfig, _projectConfig);
            _currentGridDivision = GameAudioTimelineGridUtility.NormalizeDivision(_commonConfig.DefaultGridDivision);
            return GameAudioProjectFactory.CreateDefaultProject(sampleRate, channelMode);
        }

        private string GetResolvedExportDirectory()
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            _projectConfig ??= _projectConfigSerializer.LoadOrDefault(GameAudioConfigPaths.GetProjectConfigPath(GetProjectRootPath()));
            return GameAudioConfigResolver.ResolveExportDirectory(_commonConfig, _projectConfig, GetProjectRootPath());
        }

        private void SaveCommonConfig()
        {
            _commonConfigSerializer.Save(_commonConfig ?? new GameAudioCommonConfig());
        }

        private void SaveProjectConfig()
        {
            _projectConfigSerializer.Save(
                _projectConfig ?? new GameAudioProjectConfig(),
                GameAudioConfigPaths.GetProjectConfigPath(GetProjectRootPath()));
        }

        private void MarkProjectClean()
        {
            _dirtyState.MarkClean(CurrentProject);
            _isDirty = false;
        }

        private void RefreshDirtyState()
        {
            _isDirty = _dirtyState.IsDirty(CurrentProject);
        }

        private bool TryRestoreRememberedProject()
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            if (!_commonConfig.RememberLastProject || string.IsNullOrWhiteSpace(_commonConfig.LastProjectPath))
            {
                return false;
            }

            string configuredPath = _commonConfig.LastProjectPath;
            string rememberedPath = NormalizeRememberedProjectPath(configuredPath);
            if (string.IsNullOrWhiteSpace(rememberedPath) || !File.Exists(rememberedPath))
            {
                ClearRememberedProjectPath();
                GameAudioDiagnosticLogger.Warning("Project", $"Remembered project path was cleared because the file was not found: {configuredPath}");
                return false;
            }

            try
            {
                GameAudioProjectLoadResult loadResult = _projectSerializer.LoadFromFile(rememberedPath);
                BindProject(loadResult.Project, false, rememberedPath, loadResult.Warnings);
                RememberProjectPath(rememberedPath);
                if (loadResult.Warnings.Count > 0)
                {
                    GameAudioDiagnosticLogger.Warning("Project", $"Restored remembered project with {loadResult.Warnings.Count} warning(s): {rememberedPath}");
                }

                GameAudioDiagnosticLogger.Info("Project", $"Restored remembered project from {rememberedPath}.");
                return true;
            }
            catch (Exception exception)
            {
                GameAudioDiagnosticLogger.Warning("Project", $"Remembered project restore failed for {rememberedPath}. {exception.Message}");
                ClearRememberedProjectPath();
                return false;
            }
        }

        private void ScheduleStartupGuideIfNeeded()
        {
            if (_startupGuideScheduled)
            {
                return;
            }

            _startupGuideScheduled = true;
            if (_commonConfig?.ShowStartupGuide != true)
            {
                return;
            }

            EditorApplication.delayCall += ShowStartupGuideIfNeeded;
        }

        private void ShowStartupGuideIfNeeded()
        {
            if (this == null)
            {
                return;
            }

            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            if (!_commonConfig.ShowStartupGuide)
            {
                return;
            }

            int choice = EditorUtility.DisplayDialogComplex(
                GameAudioToolInfo.DisplayName,
                T("startup.guide.message", "Create or open a .gats.json project, edit notes on the timeline, preview the result, then export WAV from the Export page. This startup guide appears once by default and can be re-enabled in Settings."),
                T("startup.guide.openManual", "Open Manual"),
                T("startup.guide.startEditing", "Start Editing"),
                T("startup.guide.showNextTime", "Show Next Time"));

            if (choice != 2)
            {
                _commonConfig.ShowStartupGuide = false;
                SaveCommonConfig();
                _inspectorStateKey = string.Empty;
            }

            if (choice == 0)
            {
                OpenStartupManual();
            }

            RefreshView();
        }

        private void OpenStartupManual()
        {
            string manualFileName = _displayLanguage == GameAudioDisplayLanguage.Japanese
                ? "Manual.ja.md"
                : "Manual.md";
            string manualPath = Path.Combine(GetEmbeddedPackageRootPath(), "Documentation~", manualFileName);
            if (!File.Exists(manualPath))
            {
                EditorUtility.DisplayDialog(
                    GameAudioToolInfo.DisplayName,
                    TF("dialog.fileNotFound", "File not found:\n{0}", manualPath),
                    T("dialog.ok", "OK"));
                return;
            }

            EditorUtility.OpenWithDefaultApp(manualPath);
        }

        private void RememberProjectPath(string path)
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            if (!_commonConfig.RememberLastProject)
            {
                return;
            }

            string normalizedPath = NormalizeRememberedProjectPath(path);
            if (string.Equals(_commonConfig.LastProjectPath ?? string.Empty, normalizedPath, StringComparison.Ordinal))
            {
                return;
            }

            _commonConfig.LastProjectPath = normalizedPath;
            SaveCommonConfig();
            _inspectorStateKey = string.Empty;
        }

        private void ClearRememberedProjectPath()
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            if (string.IsNullOrWhiteSpace(_commonConfig.LastProjectPath))
            {
                return;
            }

            _commonConfig.LastProjectPath = string.Empty;
            SaveCommonConfig();
            _inspectorStateKey = string.Empty;
        }

        private static string NormalizeRememberedProjectPath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            try
            {
                return Path.GetFullPath(path.Trim());
            }
            catch (Exception)
            {
                return path.Trim();
            }
        }

        private void OnShowStartupGuideChanged(bool enabled)
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            if (_commonConfig.ShowStartupGuide == enabled)
            {
                return;
            }

            try
            {
                _commonConfig.ShowStartupGuide = enabled;
                SaveCommonConfig();
                GameAudioDiagnosticLogger.Verbose("Settings", $"Startup guide setting changed to {enabled}.");
                _inspectorStateKey = string.Empty;
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Settings", "Saving startup guide setting failed");
            }
        }

        private void OnRememberLastProjectChanged(bool enabled)
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            if (_commonConfig.RememberLastProject == enabled)
            {
                return;
            }

            try
            {
                _commonConfig.RememberLastProject = enabled;
                _commonConfig.LastProjectPath = enabled
                    ? NormalizeRememberedProjectPath(_projectPath)
                    : string.Empty;
                SaveCommonConfig();
                GameAudioDiagnosticLogger.Verbose("Settings", $"Remember last project setting changed to {enabled}.");
                _inspectorStateKey = string.Empty;
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Settings", "Saving remember last project setting failed");
            }
        }

        private void OnDisplayLanguageChanged(GameAudioLanguageMode nextLanguage)
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            if (_commonConfig.DisplayLanguage == nextLanguage)
            {
                return;
            }

            _commonConfig.DisplayLanguage = nextLanguage;
            SaveCommonConfig();
            GameAudioDiagnosticLogger.Configure(_commonConfig);
            _displayLanguage = GameAudioLocalization.ResolveLanguage(nextLanguage);
            GameAudioDiagnosticLogger.Info("Settings", $"Display language changed to {nextLanguage}.");
            RequestUiRebuild();
        }

        private void OnDiagnosticLoggingChanged(bool enabled)
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            if (_commonConfig.EnableDiagnosticLogging == enabled)
            {
                return;
            }

            _commonConfig.EnableDiagnosticLogging = enabled;
            SaveCommonConfig();
            GameAudioDiagnosticLogger.Configure(_commonConfig);
            if (enabled)
            {
                GameAudioDiagnosticLogger.Info("Settings", $"Diagnostic logging enabled at {_commonConfig.DiagnosticLogLevel}.");
            }

            RefreshView();
        }

        private void OnDiagnosticLogLevelChanged(GameAudioDiagnosticLogLevel nextLogLevel)
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            if (_commonConfig.DiagnosticLogLevel == nextLogLevel)
            {
                return;
            }

            _commonConfig.DiagnosticLogLevel = nextLogLevel;
            SaveCommonConfig();
            GameAudioDiagnosticLogger.Configure(_commonConfig);
            GameAudioDiagnosticLogger.Info("Settings", $"Diagnostic log level changed to {nextLogLevel}.");
            RefreshView();
        }

        private void BrowseCommonExportDirectory()
        {
            string selectedPath = OpenExportDirectoryPanel(_commonConfig?.DefaultExportDirectory);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            ApplyCommonExportDirectory(GameAudioExportUtility.NormalizeStoredExportDirectory(selectedPath, GetProjectRootPath()));
        }

        private void BrowseProjectExportDirectory()
        {
            _projectConfig ??= _projectConfigSerializer.LoadOrDefault(GameAudioConfigPaths.GetProjectConfigPath(GetProjectRootPath()));
            string seed = string.IsNullOrWhiteSpace(_projectConfig.ExportDirectory)
                ? _commonConfig?.DefaultExportDirectory
                : _projectConfig.ExportDirectory;
            string selectedPath = OpenExportDirectoryPanel(seed);
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            ApplyProjectExportDirectory(GameAudioExportUtility.NormalizeStoredExportDirectory(selectedPath, GetProjectRootPath()));
        }

        private void SetCommonExportDirectoryToProjectExports()
        {
            ApplyCommonExportDirectory("Exports/Audio");
        }

        private void SetCommonExportDirectoryToAssetsExports()
        {
            ApplyCommonExportDirectory("Assets/Exports/Audio");
        }

        private void SetProjectExportDirectoryToProjectExports()
        {
            ApplyProjectExportDirectory("Exports/Audio");
        }

        private void SetProjectExportDirectoryToAssetsExports()
        {
            ApplyProjectExportDirectory("Assets/Exports/Audio");
        }

        private void ClearProjectExportDirectoryOverride()
        {
            ApplyProjectExportDirectory(string.Empty);
        }

        private void OnProjectPreferredSampleRateChanged(string selectedOption)
        {
            _projectConfig ??= _projectConfigSerializer.LoadOrDefault(GameAudioConfigPaths.GetProjectConfigPath(GetProjectRootPath()));
            int? preferredSampleRate = ParseProjectSampleRateOverride(selectedOption);
            if (_projectConfig.PreferredSampleRate == preferredSampleRate)
            {
                return;
            }

            try
            {
                _projectConfig.PreferredSampleRate = preferredSampleRate;
                SaveProjectConfig();
                GameAudioDiagnosticLogger.Verbose("Settings", $"Project sample rate override set to {FormatNullableConfigValue(preferredSampleRate)}.");
                _inspectorStateKey = string.Empty;
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Settings", "Saving project sample rate override failed");
            }
        }

        private void OnProjectPreferredChannelModeChanged(string selectedOption)
        {
            _projectConfig ??= _projectConfigSerializer.LoadOrDefault(GameAudioConfigPaths.GetProjectConfigPath(GetProjectRootPath()));
            GameAudioChannelMode? preferredChannelMode = ParseProjectChannelModeOverride(selectedOption);
            if (_projectConfig.PreferredChannelMode == preferredChannelMode)
            {
                return;
            }

            try
            {
                _projectConfig.PreferredChannelMode = preferredChannelMode;
                SaveProjectConfig();
                GameAudioDiagnosticLogger.Verbose("Settings", $"Project channel mode override set to {FormatNullableConfigValue(preferredChannelMode)}.");
                _inspectorStateKey = string.Empty;
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Settings", "Saving project channel mode override failed");
            }
        }

        private string OpenExportDirectoryPanel(string configuredDirectory)
        {
            string projectRoot = GetProjectRootPath();
            string initialDirectory = ResolveExistingDirectory(
                string.IsNullOrWhiteSpace(configuredDirectory)
                    ? GetResolvedExportDirectory()
                    : GameAudioExportUtility.NormalizeExportDirectory(configuredDirectory, projectRoot),
                projectRoot);

            return EditorUtility.OpenFolderPanel(
                GameAudioToolInfo.DisplayName,
                initialDirectory,
                string.Empty);
        }

        private void ApplyCommonExportDirectory(string storedValue)
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            string normalizedValue = string.IsNullOrWhiteSpace(storedValue)
                ? "Exports/Audio"
                : GameAudioExportUtility.NormalizeStoredExportDirectory(storedValue, GetProjectRootPath());
            if (string.Equals(_commonConfig.DefaultExportDirectory, normalizedValue, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                _commonConfig.DefaultExportDirectory = normalizedValue;
                SaveCommonConfig();
                GameAudioDiagnosticLogger.Configure(_commonConfig);
                GameAudioDiagnosticLogger.Verbose("Settings", $"Common export directory set to {normalizedValue}.");
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Settings", "Saving common export directory failed");
            }
        }

        private void ApplyProjectExportDirectory(string storedValue)
        {
            _projectConfig ??= _projectConfigSerializer.LoadOrDefault(GameAudioConfigPaths.GetProjectConfigPath(GetProjectRootPath()));
            string normalizedValue = string.IsNullOrWhiteSpace(storedValue)
                ? string.Empty
                : GameAudioExportUtility.NormalizeStoredExportDirectory(storedValue, GetProjectRootPath());
            if (string.Equals(_projectConfig.ExportDirectory, normalizedValue, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                _projectConfig.ExportDirectory = normalizedValue;
                SaveProjectConfig();
                GameAudioDiagnosticLogger.Verbose("Settings", $"Project export directory set to {normalizedValue}.");
                _inspectorStateKey = string.Empty;
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Settings", "Saving project export directory failed");
            }
        }

        private static string ResolveExistingDirectory(string preferredDirectory, string fallbackDirectory)
        {
            string current = preferredDirectory;
            while (!string.IsNullOrWhiteSpace(current))
            {
                if (Directory.Exists(current))
                {
                    return current;
                }

                string parent = Path.GetDirectoryName(current);
                if (string.Equals(parent, current, StringComparison.OrdinalIgnoreCase))
                {
                    break;
                }

                current = parent;
            }

            return fallbackDirectory;
        }

        private static string FormatNullableConfigValue<TValue>(TValue? value)
            where TValue : struct
        {
            return value.HasValue ? value.Value.ToString() : "(inherit)";
        }

        private void OnCommonExportDirectoryChanged(ChangeEvent<string> evt)
        {
            ApplyCommonExportDirectory(evt.newValue?.Trim() ?? string.Empty);
        }

        private void OnProjectExportDirectoryChanged(ChangeEvent<string> evt)
        {
            ApplyProjectExportDirectory(evt.newValue?.Trim() ?? string.Empty);
        }

        private void OnAutoRefreshAfterExportChanged(ChangeEvent<bool> evt)
        {
            _projectConfig ??= _projectConfigSerializer.LoadOrDefault(GameAudioConfigPaths.GetProjectConfigPath(GetProjectRootPath()));
            if (_projectConfig.AutoRefreshAfterExport == evt.newValue)
            {
                return;
            }

            try
            {
                _projectConfig.AutoRefreshAfterExport = evt.newValue;
                SaveProjectConfig();
                GameAudioDiagnosticLogger.Verbose("Settings", $"Auto refresh after export set to {evt.newValue}.");
                _inspectorStateKey = string.Empty;
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Settings", "Saving auto refresh setting failed");
            }
        }

        private void OnNormalizeExportChanged(ChangeEvent<bool> evt)
        {
            _normalizeExport = evt.newValue;
            RefreshView();
        }

        private void OnExportNormalizeHeadroomChanged(ChangeEvent<float> evt)
        {
            if (!IsFinite(evt.newValue))
            {
                ShowInvalidNumberMessage(T("export.normalizeHeadroom", "Normalize Headroom dB"));
                RefreshView();
                return;
            }

            _exportNormalizeHeadroomDb = GameAudioValidationUtility.ClampFloat(evt.newValue, -12.0f, 0.0f);
            RefreshView();
        }

        private void OnConversionSourceClipChanged(ChangeEvent<UnityEngine.Object> evt)
        {
            _conversionSourceClip = evt.newValue as AudioClip;
            if (_conversionSourceClip != null && string.IsNullOrWhiteSpace(_conversionOutputName))
            {
                _conversionOutputName = $"{_conversionSourceClip.name}_8bit";
            }

            RefreshView();
        }

        private void OnConversionOutputNameChanged(ChangeEvent<string> evt)
        {
            _conversionOutputName = evt.newValue?.Trim() ?? string.Empty;
            RefreshView();
        }

        private void OnConversionSampleRateChanged(ChangeEvent<int> evt)
        {
            _conversionTargetSampleRate = evt.newValue;
            RefreshView();
        }

        private void OnConversionChannelModeChanged(ChangeEvent<GameAudioConversionChannelMode> evt)
        {
            _conversionChannelMode = evt.newValue;
            RefreshView();
        }

        private void OnTimelineBarsChanged(ChangeEvent<int> evt)
        {
            GameAudioProject project = CurrentProject;
            if (project == null || project.TotalBars == evt.newValue)
            {
                return;
            }

            TryApplyProjectChange(
                "Set Total Bars",
                current => current.TotalBars = evt.newValue,
                actualProject => NotifyClamp("Total Bars", evt.newValue, actualProject.TotalBars));
        }

        private void ExportWav()
        {
            GameAudioProject project = CurrentProject;
            if (project == null)
            {
                return;
            }

            try
            {
                string exportDirectory = GetResolvedExportDirectory();
                GameAudioExportQualityReport previousQualityReport = _lastExportQualityReport;
                GameAudioWavExportResult exportResult = _wavExportService.ExportWithResult(
                    project,
                    exportDirectory,
                    project.Name,
                    new GameAudioWavExportOptions
                    {
                        Normalize = _normalizeExport,
                        HeadroomDb = _exportNormalizeHeadroomDb
                    });
                string exportPath = exportResult.WaveFilePath;
                _lastExportedPath = exportPath;
                _lastExportQualityReport = exportResult.QualityReport;
                _lastExportComparisonText = BuildExportComparisonText(previousQualityReport, exportResult.QualityReport);

                bool shouldRefresh = (_projectConfig?.AutoRefreshAfterExport ?? true)
                    && GameAudioExportUtility.ShouldRefreshAssetDatabase(exportPath, GetProjectRootPath());
                if (shouldRefresh)
                {
                    AssetDatabase.Refresh();
                }

                GameAudioDiagnosticLogger.Info(
                    "Export",
                    $"Exported WAV to {exportPath}. AutoRefresh={shouldRefresh}. Peak={exportResult.QualityReport.OutputPeakAmplitude:0.000}; Duration={exportResult.QualityReport.OutputDurationSeconds:0.00}s; Tail={exportResult.QualityReport.TailDurationSeconds:0.00}s; Normalize={exportResult.QualityReport.NormalizeApplied}.");
                ShowNotification(new GUIContent(shouldRefresh
                    ? T("status.wavExportedAndRefreshed", "WAV exported and assets refreshed.")
                    : T("status.wavExported", "WAV exported.")));
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Export", "WAV export failed");
            }
        }

        private void Export8BitWav()
        {
            if (_conversionSourceClip == null)
            {
                ShowEditorException(new InvalidOperationException(T("export.convert8Bit.selectSourceFirst", "Select a source AudioClip first.")), "Conversion", "8-bit WAV export failed");
                return;
            }

            try
            {
                string exportDirectory = GetResolvedExportDirectory();
                string outputName = GetResolvedConversionOutputName();
                GameAudioAudioClipConversionExportResult exportResult = _audioClipConversionService.ExportAsPcm8(
                    _conversionSourceClip,
                    exportDirectory,
                    outputName,
                    _conversionTargetSampleRate,
                    _conversionChannelMode);
                _lastConverted8BitPath = exportResult.WaveFilePath;
                _lastConverted8BitProjectPath = exportResult.ProjectFilePath;

                bool shouldRefresh = (_projectConfig?.AutoRefreshAfterExport ?? true)
                    && (GameAudioExportUtility.ShouldRefreshAssetDatabase(exportResult.WaveFilePath, GetProjectRootPath())
                        || GameAudioExportUtility.ShouldRefreshAssetDatabase(exportResult.ProjectFilePath, GetProjectRootPath()));
                if (shouldRefresh)
                {
                    AssetDatabase.Refresh();
                }

                GameAudioDiagnosticLogger.Info(
                    "Conversion",
                    $"Exported 8-bit WAV to {exportResult.WaveFilePath} and conversion project to {exportResult.ProjectFilePath}. AutoRefresh={shouldRefresh}. Source={_conversionSourceClip.name}; SampleRate={_conversionTargetSampleRate}; ChannelMode={_conversionChannelMode}.");
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

        private void OpenExportFolder()
        {
            try
            {
                string exportDirectory = GetResolvedExportDirectory();
                Directory.CreateDirectory(exportDirectory);
                GameAudioDiagnosticLogger.Verbose("Export", $"Opening export folder {exportDirectory}.");
                EditorUtility.RevealInFinder(exportDirectory);
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Export", "Opening export folder failed");
            }
        }

        private void RenderPreview()
        {
            try
            {
                _previewPlaybackService.Prepare(CurrentProject);
                RefreshView();
                ShowNotification(new GUIContent(T("status.previewRendered", "Preview rendered.")));
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Preview", "Preview render failed");
            }
        }

        private void PlayPreview()
        {
            try
            {
                GameAudioProject project = CurrentProject;
                _previewPlaybackService.Play(project, project != null && project.LoopPlayback);
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Preview", "Preview playback failed");
            }
        }

        private void StopPreview()
        {
            _previewPlaybackService.Stop();
            GameAudioDiagnosticLogger.Verbose("Preview", "Preview stop requested.");
            RefreshView();
        }

        private void PausePreview()
        {
            _previewPlaybackService.Pause();
            GameAudioDiagnosticLogger.Verbose("Preview", "Preview pause requested.");
            RefreshView();
        }

        private void RewindPreview()
        {
            _previewPlaybackService.Rewind();
            GameAudioDiagnosticLogger.Verbose("Preview", "Preview rewind requested.");
            RefreshView();
        }

        private void OnLoopPlaybackChanged(ChangeEvent<bool> evt)
        {
            GameAudioProject project = CurrentProject;
            if (project == null || project.LoopPlayback == evt.newValue)
            {
                return;
            }

            try
            {
                ApplyEditorCommand(
                    GameAudioProjectCommandFactory.ChangeProject(project, "Toggle Loop", current => current.LoopPlayback = evt.newValue),
                    false);
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Preview", "Loop playback toggle failed");
            }
        }

        private void OnToolbarBpmChanged(ChangeEvent<int> evt)
        {
            GameAudioProject project = CurrentProject;
            if (project == null || project.Bpm == evt.newValue)
            {
                return;
            }

            TryApplyProjectChange(
                "Set BPM",
                current => current.Bpm = evt.newValue,
                actualProject => NotifyClamp("BPM", evt.newValue, actualProject.Bpm));
        }

        private void OnToolbarGridChanged(ChangeEvent<string> evt)
        {
            SetGridDivision(evt.newValue);
        }

        private void HandlePreviewTick()
        {
            if (_previewPlaybackService.Update())
            {
                RefreshView();
            }
        }

        private void UndoLastEdit()
        {
            if (_editorSession == null || !_editorSession.Undo())
            {
                return;
            }

            _project = _editorSession.CurrentProject;
            RefreshDirtyState();
            CancelTimelineInteraction();
            PruneTimelineSelection();
            ResetPreviewState();
            RefreshView();
        }

        private void RedoLastEdit()
        {
            if (_editorSession == null || !_editorSession.Redo())
            {
                return;
            }

            _project = _editorSession.CurrentProject;
            RefreshDirtyState();
            CancelTimelineInteraction();
            PruneTimelineSelection();
            ResetPreviewState();
            RefreshView();
        }

        private void CycleGridDivision()
        {
            IReadOnlyList<string> divisions = GameAudioTimelineGridUtility.SupportedDivisions;
            int currentIndex = 0;
            string currentDivision = GameAudioTimelineGridUtility.NormalizeDivision(_currentGridDivision);
            for (int index = 0; index < divisions.Count; index++)
            {
                if (!string.Equals(divisions[index], currentDivision, StringComparison.Ordinal))
                {
                    continue;
                }

                currentIndex = index;
                break;
            }

            int nextIndex = (currentIndex + 1) % divisions.Count;
            SetGridDivision(divisions[nextIndex]);
        }

        private void SetGridDivision(string division)
        {
            string normalized = GameAudioTimelineGridUtility.NormalizeDivision(division);
            if (string.Equals(_currentGridDivision, normalized, StringComparison.Ordinal))
            {
                return;
            }

            _currentGridDivision = normalized;
            RefreshView();
        }

        private void ShowTimelineHelp()
        {
            GameAudioTimelineHelpWindow.Open(
                _displayLanguage,
                TF("status.timelineHint", "Grid {0} | Selected {1} note(s) | Drag empty lane to create | Drag note to move | Drag edge to resize | + Add Track in the footer | Ctrl+D duplicate | Delete remove | Ctrl+Z / Ctrl+Y undo redo", _currentGridDivision, _selectedNoteIds.Count));
        }

        private void DuplicateSelectedNotes()
        {
            if (_selectedNoteIds.Count == 0 || CurrentProject == null)
            {
                return;
            }

            try
            {
                ApplyEditorCommand(
                    GameAudioTimelineCommandFactory.DuplicateNotes(CurrentProject, _selectedNoteIds.ToArray(), GameAudioTimelineGridUtility.GetBeatStep(_currentGridDivision)),
                    false);
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Edit", "Duplicating notes failed");
            }
        }

        private void DeleteSelectedNotes()
        {
            if (_selectedNoteIds.Count == 0 || CurrentProject == null)
            {
                return;
            }

            try
            {
                string[] deletedIds = _selectedNoteIds.ToArray();
                ApplyEditorCommand(
                    GameAudioTimelineCommandFactory.DeleteNotes(CurrentProject, deletedIds),
                    false,
                    () => _selectedNoteIds.Clear());
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Edit", "Deleting notes failed");
            }
        }

        private void AddTrackFromTimeline()
        {
            if (CurrentProject == null)
            {
                return;
            }

            try
            {
                int nextTrackIndex = (CurrentProject.Tracks?.Count ?? 0) + 1;
                GameAudioTrack newTrack = GameAudioProjectFactory.CreateDefaultTrack(nextTrackIndex);
                ApplyEditorCommand(
                    GameAudioProjectCommandFactory.AddTrack(CurrentProject, newTrack),
                    true,
                    () =>
                    {
                        GameAudioTrack addedTrack = CurrentProject?.Tracks?.LastOrDefault();
                        if (addedTrack != null)
                        {
                            _selectedTrackId = addedTrack.Id;
                        }

                        _selectedNoteIds.Clear();
                    });
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Edit", "Adding track failed");
            }
        }

        private void CreateSampleProjects()
        {
            try
            {
                EnsureSampleProjects();
                GameAudioDiagnosticLogger.Info("Samples", $"Sample projects ensured at {GetUserProjectFolderPath()}.");
                ShowNotification(new GUIContent(T("status.samplesCreated", "Sample projects created.")));
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Samples", "Creating sample projects failed");
            }
        }

        private void LoadBasicSampleProject()
        {
            LoadSampleProject("BasicSE.gats.json");
        }

        private void LoadSimpleLoopSampleProject()
        {
            LoadSampleProject("SimpleLoop.gats.json");
        }

        private void OpenSampleProjectsFolder()
        {
            try
            {
                EnsureSampleProjects();
                GameAudioDiagnosticLogger.Verbose("Samples", $"Opening sample folder {GetUserProjectFolderPath()}.");
                EditorUtility.RevealInFinder(GetUserProjectFolderPath());
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Samples", "Opening sample folder failed");
            }
        }

        private void LoadSampleProject(string fileName)
        {
            if (!ConfirmDiscardIfDirty())
            {
                return;
            }

            try
            {
                EnsureSampleProjects();
                GameAudioDiagnosticLogger.Verbose("Samples", $"Loading sample project {fileName}.");
                LoadProjectFromPath(Path.Combine(GetUserProjectFolderPath(), fileName));
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Samples", $"Loading sample project failed: {fileName}");
            }
        }

        private void LoadProjectFromPath(string path)
        {
            try
            {
                GameAudioProjectLoadResult loadResult = _projectSerializer.LoadFromFile(path);
                string resolvedPath = NormalizeRememberedProjectPath(path);
                BindProject(loadResult.Project, false, resolvedPath, loadResult.Warnings);
                RememberProjectPath(resolvedPath);
                if (loadResult.Warnings.Count > 0)
                {
                    GameAudioDiagnosticLogger.Warning("Project", $"Loaded project with {loadResult.Warnings.Count} warning(s): {resolvedPath}");
                }

                GameAudioDiagnosticLogger.Info("Project", $"Loaded project from {resolvedPath}.");
                ShowNotification(new GUIContent(T("status.projectLoaded", "Project loaded.")));
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Project", $"Loading project failed: {path}");
            }
        }

        private void BindProject(GameAudioProject project, bool isDirty, string path, IEnumerable<string> warnings)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            _project = project;
            _projectPath = path ?? string.Empty;
            if (isDirty)
            {
                _dirtyState.Clear();
                _isDirty = true;
            }
            else
            {
                MarkProjectClean();
            }

            _loadWarnings = warnings == null ? new List<string>() : new List<string>(warnings);
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            _editorSession = new GameAudioEditorSession(_project, _commonConfig.UndoHistoryLimit);
            _selectedNoteIds.Clear();
            _selectedTrackId = _project.Tracks.FirstOrDefault()?.Id ?? string.Empty;
            _inspectorStateKey = string.Empty;
            _lastExportedPath = string.Empty;
            CancelTimelineInteraction();
            _timelineScrollPosition = Vector2.zero;
            ResetPreviewState();
        }

        private void ApplyEditorCommand(IGameAudioCommand command, bool showNotification, Action afterApply = null)
        {
            if (_editorSession == null)
            {
                throw new InvalidOperationException("Editor session is not initialized.");
            }

            _editorSession.Execute(command ?? throw new ArgumentNullException(nameof(command)));
            _project = _editorSession.CurrentProject;
            afterApply?.Invoke();
            RefreshDirtyState();
            PruneTimelineSelection();
            CancelTimelineInteraction();
            ResetPreviewState();
            RefreshView();

            if (showNotification)
            {
                ShowNotification(new GUIContent(command.DisplayName));
            }

            GameAudioDiagnosticLogger.Verbose("Edit", $"Executed command: {command.DisplayName}");
        }

        private void DrawTimelineGui()
        {
            GameAudioProject project = CurrentProject;
            if (project == null)
            {
                EditorGUILayout.LabelField(T("status.noProjectLoaded", "No project loaded."));
                return;
            }

            var metrics = new TimelineMetrics(project);
            Rect viewportRect = GUILayoutUtility.GetRect(10.0f, 100000.0f, TimelineViewportHeight, TimelineViewportHeight, GUILayout.ExpandWidth(true));
            GUI.Box(viewportRect, GUIContent.none, EditorStyles.helpBox);

            Rect contentRect = new Rect(0.0f, 0.0f, metrics.ContentWidth, metrics.ContentHeight);
            Event evt = Event.current;

            _timelineScrollPosition = GUI.BeginScrollView(viewportRect, _timelineScrollPosition, contentRect);
            Vector2 contentMouse = evt.mousePosition + _timelineScrollPosition - new Vector2(viewportRect.x, viewportRect.y);

            List<TimelineRenderedNote> renderedNotes = BuildRenderedNotes(project, metrics);
            HandleTimelinePointer(evt, contentMouse, viewportRect, metrics, renderedNotes);
            renderedNotes = BuildRenderedNotes(project, metrics);

            DrawTimelineBackground(project, metrics);
            DrawTimelineTrackHeaders(project, metrics);
            DrawTimelineNotes(renderedNotes);
            DrawCreatePreview(project, metrics);

            GUI.EndScrollView();
        }

        private List<TimelineRenderedNote> BuildRenderedNotes(GameAudioProject project, TimelineMetrics metrics)
        {
            var notes = new List<TimelineRenderedNote>();
            if (project.Tracks == null)
            {
                return notes;
            }

            for (int trackIndex = 0; trackIndex < project.Tracks.Count; trackIndex++)
            {
                GameAudioTrack track = project.Tracks[trackIndex];
                foreach (GameAudioNote note in track.Notes)
                {
                    int targetTrackIndex = trackIndex;
                    float startBeat = note.StartBeat;
                    float durationBeat = note.DurationBeat;

                    if (_timelineDragState != null
                        && _timelineDragState.Mode != TimelineInteractionMode.CreatingNote
                        && _timelineDragState.PreviewPlacements.TryGetValue(note.Id, out TimelinePreviewPlacement placement))
                    {
                        targetTrackIndex = placement.TrackIndex;
                        startBeat = placement.StartBeat;
                        durationBeat = placement.DurationBeat;
                    }

                    Rect noteRect = metrics.GetNoteRect(targetTrackIndex, startBeat, durationBeat);
                    notes.Add(new TimelineRenderedNote(
                        note.Id,
                        track.Id,
                        targetTrackIndex,
                        note.MidiNote,
                        startBeat,
                        durationBeat,
                        noteRect,
                        _selectedNoteIds.Contains(note.Id)));
                }
            }

            return notes
                .OrderBy(note => note.IsSelected ? 1 : 0)
                .ThenBy(note => note.StartBeat)
                .ThenBy(note => note.NoteId, StringComparer.Ordinal)
                .ToList();
        }

        private void DrawTimelineBackground(GameAudioProject project, TimelineMetrics metrics)
        {
            EditorGUI.DrawRect(new Rect(0.0f, 0.0f, metrics.ContentWidth, metrics.ContentHeight), new Color(0.10f, 0.10f, 0.10f));
            EditorGUI.DrawRect(new Rect(0.0f, 0.0f, metrics.HeaderWidth, metrics.ContentHeight), new Color(0.14f, 0.14f, 0.14f));
            EditorGUI.DrawRect(new Rect(0.0f, 0.0f, metrics.ContentWidth, metrics.RulerHeight), new Color(0.12f, 0.12f, 0.12f));

            for (int barIndex = 0; barIndex <= metrics.TotalBars; barIndex++)
            {
                float x = metrics.HeaderWidth + (barIndex * metrics.BeatsPerBar * metrics.PixelsPerBeat);
                EditorGUI.DrawRect(new Rect(x, 0.0f, 1.0f, metrics.ContentHeight), new Color(0.30f, 0.30f, 0.30f));
                if (barIndex < metrics.TotalBars)
                {
                    GUI.Label(
                        new Rect(x + 4.0f, 4.0f, 80.0f, metrics.RulerHeight - 8.0f),
                        TF("timeline.barLabel", "Bar {0:00}", barIndex + 1),
                        EditorStyles.miniBoldLabel);
                }
            }

            for (int beatIndex = 0; beatIndex <= metrics.TotalBeats; beatIndex++)
            {
                float x = metrics.HeaderWidth + (beatIndex * metrics.PixelsPerBeat);
                Color color = beatIndex % metrics.BeatsPerBar == 0
                    ? new Color(0.30f, 0.30f, 0.30f)
                    : new Color(0.22f, 0.22f, 0.22f);
                EditorGUI.DrawRect(new Rect(x, metrics.RulerHeight, 1.0f, metrics.ContentHeight - metrics.RulerHeight), color);
            }

            for (int trackIndex = 0; trackIndex < project.Tracks.Count; trackIndex++)
            {
                float y = metrics.GetTrackY(trackIndex);
                EditorGUI.DrawRect(new Rect(0.0f, y, metrics.ContentWidth, 1.0f), new Color(0.20f, 0.20f, 0.20f));
                EditorGUI.DrawRect(metrics.GetLaneRect(trackIndex), trackIndex % 2 == 0 ? new Color(0.11f, 0.11f, 0.11f) : new Color(0.13f, 0.13f, 0.13f));
            }
        }

        private void DrawPreviewWaveformGui()
        {
            Rect rect = GUILayoutUtility.GetRect(0.0f, PreviewWaveformHeight, GUILayout.ExpandWidth(true));
            GUI.Box(rect, GUIContent.none, EditorStyles.helpBox);

            Rect innerRect = new Rect(rect.x + 8.0f, rect.y + 8.0f, rect.width - 16.0f, rect.height - 16.0f);
            if (innerRect.width <= 4.0f || innerRect.height <= 4.0f)
            {
                return;
            }

            EditorGUI.DrawRect(innerRect, new Color(0.09f, 0.12f, 0.16f));

            GameAudioProject project = CurrentProject;
            GameAudioPreviewState previewState = _previewPlaybackService.State;
            if (project == null || !previewState.IsPreviewReady || previewState.RenderResult == null)
            {
                DrawWaveformPlaceholder(innerRect, T("preview.waveform.empty", "Render preview to see waveform."));
                return;
            }

            double waveformDurationSeconds = GetWaveformDurationSeconds(previewState);
            if (!previewState.LoopPlayback
                && previewState.OutputDurationSeconds > previewState.ProjectDurationSeconds
                && waveformDurationSeconds > 0.0d)
            {
                float projectWidth = (float)(innerRect.width * (previewState.ProjectDurationSeconds / waveformDurationSeconds));
                EditorGUI.DrawRect(
                    new Rect(innerRect.x + projectWidth, innerRect.y, innerRect.width - projectWidth, innerRect.height),
                    new Color(0.17f, 0.13f, 0.10f, 0.65f));
            }

            EditorGUI.DrawRect(
                new Rect(innerRect.x, innerRect.center.y, innerRect.width, 1.0f),
                new Color(0.18f, 0.30f, 0.40f, 0.8f));

            GameAudioPreviewWaveformData waveform = GameAudioPreviewWaveformBuilder.Build(
                previewState.RenderResult,
                Mathf.Max(32, Mathf.RoundToInt(innerRect.width)));

            if (waveform.Bins.Length == 0)
            {
                DrawWaveformPlaceholder(innerRect, T("preview.waveform.empty", "Render preview to see waveform."));
                return;
            }

            if (waveform.IsSilent)
            {
                DrawWaveformPlaceholder(innerRect, T("preview.waveform.silent", "Preview buffer is silent."));
            }
            else
            {
                DrawWaveformBins(innerRect, waveform);
            }

            DrawWaveformCursor(innerRect, previewState);
        }

        private void DrawTimelineTrackHeaders(GameAudioProject project, TimelineMetrics metrics)
        {
            for (int trackIndex = 0; trackIndex < project.Tracks.Count; trackIndex++)
            {
                GameAudioTrack track = project.Tracks[trackIndex];
                Rect headerRect = metrics.GetHeaderRect(trackIndex);
                bool isSelected = string.Equals(_selectedTrackId, track.Id, StringComparison.Ordinal);
                EditorGUI.DrawRect(headerRect, isSelected ? new Color(0.22f, 0.22f, 0.18f) : new Color(0.16f, 0.16f, 0.16f));

                Rect nameRect = new Rect(headerRect.x + 8.0f, headerRect.y + 6.0f, headerRect.width - 16.0f, 16.0f);
                Rect infoRect = new Rect(headerRect.x + 8.0f, headerRect.y + 22.0f, headerRect.width - 16.0f, 16.0f);
                GUI.Label(nameRect, track.Name, EditorStyles.boldLabel);
                GUI.Label(
                    infoRect,
                    TF("timeline.trackInfo", "Notes {0} / Pan {1:0.00}", track.Notes.Count, track.Pan),
                    EditorStyles.miniLabel);
            }

            DrawTimelineAddTrackFooter(project, metrics);
        }

        private void DrawTimelineAddTrackFooter(GameAudioProject project, TimelineMetrics metrics)
        {
            Rect footerRect = metrics.GetFooterRect(project.Tracks.Count);
            EditorGUI.DrawRect(footerRect, new Color(0.13f, 0.13f, 0.13f));

            Rect buttonRect = new Rect(
                footerRect.x + 8.0f,
                footerRect.y + 6.0f,
                footerRect.width - 16.0f,
                footerRect.height - 12.0f);

            bool canAddTrack = project.Tracks.Count < GameAudioToolInfo.MaxTrackCount;
            EditorGUI.BeginDisabledGroup(!canAddTrack);
            if (GUI.Button(buttonRect, T("timeline.addTrack", "+ Add Track")))
            {
                AddTrackFromTimeline();
            }

            EditorGUI.EndDisabledGroup();
        }

        private void DrawTimelineNotes(IEnumerable<TimelineRenderedNote> renderedNotes)
        {
            foreach (TimelineRenderedNote renderedNote in renderedNotes)
            {
                Color fillColor = renderedNote.IsSelected
                    ? new Color(0.95f, 0.62f, 0.18f)
                    : new Color(0.44f, 0.69f, 0.86f);
                Color outlineColor = renderedNote.IsSelected
                    ? new Color(1.0f, 0.87f, 0.38f)
                    : new Color(0.22f, 0.45f, 0.58f);

                EditorGUI.DrawRect(renderedNote.Rect, fillColor);
                EditorGUI.DrawRect(new Rect(renderedNote.Rect.x, renderedNote.Rect.y, renderedNote.Rect.width, 1.0f), outlineColor);
                EditorGUI.DrawRect(new Rect(renderedNote.Rect.x, renderedNote.Rect.yMax - 1.0f, renderedNote.Rect.width, 1.0f), outlineColor);
                EditorGUI.DrawRect(new Rect(renderedNote.Rect.x, renderedNote.Rect.y, 1.0f, renderedNote.Rect.height), outlineColor);
                EditorGUI.DrawRect(new Rect(renderedNote.Rect.xMax - 1.0f, renderedNote.Rect.y, 1.0f, renderedNote.Rect.height), outlineColor);

                GUI.Label(
                    new Rect(renderedNote.Rect.x + 4.0f, renderedNote.Rect.y + 2.0f, renderedNote.Rect.width - 8.0f, renderedNote.Rect.height - 4.0f),
                    TF("timeline.noteLabel", "MIDI {0}", renderedNote.MidiNote),
                    EditorStyles.whiteMiniLabel);
            }
        }

        private void DrawCreatePreview(GameAudioProject project, TimelineMetrics metrics)
        {
            if (_timelineDragState == null || _timelineDragState.Mode != TimelineInteractionMode.CreatingNote)
            {
                return;
            }

            int trackIndex = Mathf.Clamp(_timelineDragState.AnchorTrackIndex, 0, Math.Max(0, project.Tracks.Count - 1));
            float startBeat = Mathf.Min(_timelineDragState.AnchorBeat, _timelineDragState.CurrentBeat);
            float endBeat = Mathf.Max(_timelineDragState.AnchorBeat, _timelineDragState.CurrentBeat);
            float durationBeat = Mathf.Max(GameAudioTimelineGridUtility.GetBeatStep(_currentGridDivision), endBeat - startBeat);
            durationBeat = Mathf.Min(durationBeat, Math.Max(GameAudioToolInfo.MinNoteDurationBeat, metrics.TotalBeats - startBeat));
            Rect previewRect = metrics.GetNoteRect(trackIndex, startBeat, durationBeat);

            EditorGUI.DrawRect(previewRect, new Color(0.95f, 0.85f, 0.25f, 0.6f));
            EditorGUI.DrawRect(new Rect(previewRect.x, previewRect.y, previewRect.width, 1.0f), new Color(1.0f, 0.92f, 0.4f));
        }

        private void HandleTimelinePointer(Event evt, Vector2 contentMouse, Rect viewportRect, TimelineMetrics metrics, List<TimelineRenderedNote> renderedNotes)
        {
            switch (evt.type)
            {
                case EventType.MouseDown when evt.button == 0 && viewportRect.Contains(evt.mousePosition):
                    HandleTimelineMouseDown(evt, contentMouse, metrics, renderedNotes);
                    break;
                case EventType.MouseDrag when evt.button == 0 && _timelineDragState != null:
                    UpdateTimelineDragPreview(contentMouse, metrics, evt.shift);
                    evt.Use();
                    _timelineSurface.MarkDirtyRepaint();
                    break;
                case EventType.MouseUp when evt.button == 0 && _timelineDragState != null:
                    CommitTimelineDrag(metrics);
                    evt.Use();
                    break;
            }
        }

        private void HandleTimelineMouseDown(Event evt, Vector2 contentMouse, TimelineMetrics metrics, List<TimelineRenderedNote> renderedNotes)
        {
            if (contentMouse.y < 0.0f || contentMouse.y > metrics.ContentHeight)
            {
                return;
            }

            int trackIndex = metrics.GetTrackIndex(contentMouse.y);
            bool actionKey = evt.control || evt.command;
            TimelineHitInfo hitInfo = FindTimelineHit(renderedNotes, contentMouse);

            if (hitInfo.HasValue)
            {
                string trackId = CurrentProject.Tracks[hitInfo.TrackIndex].Id;

                if (actionKey)
                {
                    ToggleNoteSelection(hitInfo.NoteId, trackId);
                    evt.Use();
                    RefreshView();
                    return;
                }

                if (evt.shift)
                {
                    AddNoteSelection(hitInfo.NoteId, trackId);
                    evt.Use();
                    RefreshView();
                    return;
                }

                SelectOnlyNote(hitInfo.NoteId, trackId);
                if (hitInfo.Edge == TimelineHitEdge.Left || hitInfo.Edge == TimelineHitEdge.Right)
                {
                    BeginResizeInteraction(hitInfo, metrics);
                }
                else
                {
                    BeginMoveInteraction(hitInfo, metrics, contentMouse);
                }

                evt.Use();
                RefreshView();
                return;
            }

            if (trackIndex >= 0 && contentMouse.x < metrics.HeaderWidth)
            {
                _selectedTrackId = CurrentProject.Tracks[trackIndex].Id;
                _selectedNoteIds.Clear();
                CancelTimelineInteraction();
                evt.Use();
                RefreshView();
                return;
            }

            if (trackIndex < 0 || trackIndex >= CurrentProject.Tracks.Count || contentMouse.x < metrics.HeaderWidth)
            {
                return;
            }

            _selectedTrackId = CurrentProject.Tracks[trackIndex].Id;
            _selectedNoteIds.Clear();
            BeginCreateInteraction(trackIndex, metrics.GetBeatFromContentX(contentMouse.x));
            evt.Use();
            RefreshView();
        }

        private void BeginCreateInteraction(int trackIndex, float beat)
        {
            _timelineDragState = new TimelineDragState
            {
                Mode = TimelineInteractionMode.CreatingNote,
                AnchorTrackIndex = trackIndex,
                AnchorBeat = GameAudioTimelineGridUtility.SnapBeat(beat, _currentGridDivision),
                CurrentBeat = GameAudioTimelineGridUtility.SnapBeat(beat, _currentGridDivision)
            };
        }

        private void BeginMoveInteraction(TimelineHitInfo hitInfo, TimelineMetrics metrics, Vector2 contentMouse)
        {
            List<TimelineNoteOrigin> origins = BuildSelectedOrigins(hitInfo.NoteId);
            if (origins.Count == 0)
            {
                return;
            }

            _timelineDragState = new TimelineDragState
            {
                Mode = TimelineInteractionMode.MovingNotes,
                PrimaryNoteId = hitInfo.NoteId,
                AnchorTrackIndex = hitInfo.TrackIndex,
                AnchorBeat = metrics.GetBeatFromContentX(contentMouse.x),
                CurrentBeat = hitInfo.StartBeat,
                Origins = origins
            };

            UpdateTimelineDragPreview(contentMouse, metrics, false);
        }

        private void BeginResizeInteraction(TimelineHitInfo hitInfo, TimelineMetrics metrics)
        {
            SelectOnlyNote(hitInfo.NoteId, CurrentProject.Tracks[hitInfo.TrackIndex].Id);
            List<TimelineNoteOrigin> origins = BuildSelectedOrigins(hitInfo.NoteId);
            if (origins.Count == 0)
            {
                return;
            }

            TimelineNoteOrigin origin = origins[0];
            _timelineDragState = new TimelineDragState
            {
                Mode = hitInfo.Edge == TimelineHitEdge.Left ? TimelineInteractionMode.ResizingLeft : TimelineInteractionMode.ResizingRight,
                PrimaryNoteId = hitInfo.NoteId,
                AnchorTrackIndex = hitInfo.TrackIndex,
                AnchorBeat = hitInfo.Edge == TimelineHitEdge.Left ? origin.StartBeat : origin.StartBeat + origin.DurationBeat,
                CurrentBeat = hitInfo.Edge == TimelineHitEdge.Left ? origin.StartBeat : origin.StartBeat + origin.DurationBeat,
                Origins = origins
            };

            float edgeX = hitInfo.Edge == TimelineHitEdge.Left ? hitInfo.Rect.x : hitInfo.Rect.xMax;
            UpdateTimelineDragPreview(new Vector2(edgeX, hitInfo.Rect.center.y), metrics, false);
        }

        private void UpdateTimelineDragPreview(Vector2 contentMouse, TimelineMetrics metrics, bool disableSnap)
        {
            if (_timelineDragState == null)
            {
                return;
            }

            switch (_timelineDragState.Mode)
            {
                case TimelineInteractionMode.CreatingNote:
                    _timelineDragState.CurrentBeat = GameAudioTimelineGridUtility.SnapBeat(metrics.GetBeatFromContentX(contentMouse.x), _currentGridDivision);
                    _timelineDragState.CurrentBeat = Mathf.Clamp(_timelineDragState.CurrentBeat, 0.0f, metrics.TotalBeats);
                    break;
                case TimelineInteractionMode.MovingNotes:
                    UpdateMovePreview(contentMouse, metrics, disableSnap);
                    break;
                case TimelineInteractionMode.ResizingLeft:
                case TimelineInteractionMode.ResizingRight:
                    UpdateResizePreview(contentMouse, metrics, disableSnap);
                    break;
            }
        }

        private void UpdateMovePreview(Vector2 contentMouse, TimelineMetrics metrics, bool disableSnap)
        {
            TimelineNoteOrigin primaryOrigin = _timelineDragState.Origins[0];
            int hoverTrackIndex = metrics.GetTrackIndex(contentMouse.y);
            if (hoverTrackIndex < 0)
            {
                hoverTrackIndex = _timelineDragState.AnchorTrackIndex;
            }

            int requestedTrackDelta = hoverTrackIndex - _timelineDragState.AnchorTrackIndex;
            int minOriginTrack = _timelineDragState.Origins.Min(origin => origin.TrackIndex);
            int maxOriginTrack = _timelineDragState.Origins.Max(origin => origin.TrackIndex);
            int clampedTrackDelta = Mathf.Clamp(
                requestedTrackDelta,
                -minOriginTrack,
                Math.Max(0, CurrentProject.Tracks.Count - 1 - maxOriginTrack));

            float rawPrimaryStart = primaryOrigin.StartBeat + (metrics.GetBeatFromContentX(contentMouse.x) - _timelineDragState.AnchorBeat);
            float clampedPrimaryStart = Mathf.Clamp(rawPrimaryStart, 0.0f, Math.Max(0.0f, metrics.TotalBeats - primaryOrigin.DurationBeat));
            float snappedPrimaryStart = disableSnap
                ? clampedPrimaryStart
                : GameAudioTimelineGridUtility.SnapBeat(clampedPrimaryStart, _currentGridDivision);
            snappedPrimaryStart = Mathf.Clamp(snappedPrimaryStart, 0.0f, Math.Max(0.0f, metrics.TotalBeats - primaryOrigin.DurationBeat));
            float appliedBeatDelta = snappedPrimaryStart - primaryOrigin.StartBeat;

            _timelineDragState.PreviewPlacements.Clear();
            foreach (TimelineNoteOrigin origin in _timelineDragState.Origins)
            {
                int targetTrackIndex = origin.TrackIndex + clampedTrackDelta;
                string targetTrackId = CurrentProject.Tracks[targetTrackIndex].Id;
                float targetStartBeat = Mathf.Clamp(origin.StartBeat + appliedBeatDelta, 0.0f, Math.Max(0.0f, metrics.TotalBeats - origin.DurationBeat));
                _timelineDragState.PreviewPlacements[origin.NoteId] = new TimelinePreviewPlacement(origin.NoteId, targetTrackId, targetTrackIndex, targetStartBeat, origin.DurationBeat);
            }
        }

        private void UpdateResizePreview(Vector2 contentMouse, TimelineMetrics metrics, bool disableSnap)
        {
            TimelineNoteOrigin origin = _timelineDragState.Origins[0];
            float rawBeat = Mathf.Clamp(metrics.GetBeatFromContentX(contentMouse.x), 0.0f, metrics.TotalBeats);
            float minDuration = GameAudioToolInfo.MinNoteDurationBeat;
            float startBeat = origin.StartBeat;
            float durationBeat = origin.DurationBeat;

            if (_timelineDragState.Mode == TimelineInteractionMode.ResizingLeft)
            {
                float rightBeat = origin.StartBeat + origin.DurationBeat;
                float candidateStart = Mathf.Clamp(rawBeat, 0.0f, Math.Max(0.0f, rightBeat - minDuration));
                if (!disableSnap)
                {
                    candidateStart = GameAudioTimelineGridUtility.SnapBeat(candidateStart, _currentGridDivision);
                }

                candidateStart = Mathf.Clamp(candidateStart, 0.0f, Math.Max(0.0f, rightBeat - minDuration));
                startBeat = candidateStart;
                durationBeat = Math.Max(minDuration, rightBeat - candidateStart);
            }
            else
            {
                float minEndBeat = origin.StartBeat + minDuration;
                float candidateEnd = Mathf.Clamp(rawBeat, minEndBeat, metrics.TotalBeats);
                if (!disableSnap)
                {
                    candidateEnd = GameAudioTimelineGridUtility.SnapBeat(candidateEnd, _currentGridDivision);
                }

                candidateEnd = Mathf.Clamp(candidateEnd, minEndBeat, metrics.TotalBeats);
                durationBeat = Math.Max(minDuration, candidateEnd - origin.StartBeat);
            }

            _timelineDragState.PreviewPlacements.Clear();
            _timelineDragState.PreviewPlacements[origin.NoteId] = new TimelinePreviewPlacement(
                origin.NoteId,
                origin.TrackId,
                origin.TrackIndex,
                startBeat,
                durationBeat);
        }

        private void CommitTimelineDrag(TimelineMetrics metrics)
        {
            if (_timelineDragState == null)
            {
                return;
            }

            try
            {
                switch (_timelineDragState.Mode)
                {
                    case TimelineInteractionMode.CreatingNote:
                        CommitCreateNote(metrics);
                        break;
                    case TimelineInteractionMode.MovingNotes:
                    case TimelineInteractionMode.ResizingLeft:
                    case TimelineInteractionMode.ResizingRight:
                        CommitMoveOrResize();
                        break;
                }
            }
            catch (Exception exception)
            {
                ShowEditorException(exception, "Edit", "Timeline interaction commit failed");
                CancelTimelineInteraction();
                RefreshView();
            }
        }

        private void CommitCreateNote(TimelineMetrics metrics)
        {
            int trackIndex = Mathf.Clamp(_timelineDragState.AnchorTrackIndex, 0, Math.Max(0, CurrentProject.Tracks.Count - 1));
            float startBeat = Mathf.Min(_timelineDragState.AnchorBeat, _timelineDragState.CurrentBeat);
            float endBeat = Mathf.Max(_timelineDragState.AnchorBeat, _timelineDragState.CurrentBeat);
            startBeat = Mathf.Clamp(startBeat, 0.0f, Math.Max(0.0f, metrics.TotalBeats - GameAudioToolInfo.MinNoteDurationBeat));
            float minimumDuration = Math.Max(GameAudioToolInfo.MinNoteDurationBeat, GameAudioTimelineGridUtility.GetBeatStep(_currentGridDivision));
            float durationBeat = Math.Max(minimumDuration, endBeat - startBeat);
            durationBeat = Mathf.Min(durationBeat, Math.Max(GameAudioToolInfo.MinNoteDurationBeat, metrics.TotalBeats - startBeat));

            string newNoteId = $"note-{Guid.NewGuid():N}";
            GameAudioTrack track = CurrentProject.Tracks[trackIndex];
            ApplyEditorCommand(
                GameAudioProjectCommandFactory.AddNote(CurrentProject, track.Id, new GameAudioNote
                {
                    Id = newNoteId,
                    StartBeat = startBeat,
                    DurationBeat = durationBeat,
                    MidiNote = 60,
                    Velocity = 0.8f
                }),
                false,
                () => SelectOnlyNote(newNoteId, track.Id));
        }

        private void CommitMoveOrResize()
        {
            if (_timelineDragState.PreviewPlacements.Count == 0)
            {
                CancelTimelineInteraction();
                RefreshView();
                return;
            }

            bool changed = _timelineDragState.Origins.Any(origin =>
            {
                TimelinePreviewPlacement placement = _timelineDragState.PreviewPlacements[origin.NoteId];
                return placement.TrackId != origin.TrackId
                    || Math.Abs(placement.StartBeat - origin.StartBeat) > 0.0001f
                    || Math.Abs(placement.DurationBeat - origin.DurationBeat) > 0.0001f;
            });

            if (!changed)
            {
                CancelTimelineInteraction();
                RefreshView();
                return;
            }

            ApplyEditorCommand(
                GameAudioTimelineCommandFactory.MoveNotes(
                    CurrentProject,
                    _timelineDragState.PreviewPlacements.Values
                        .Select(placement => new GameAudioTimelineNotePlacement(placement.NoteId, placement.TrackId, placement.StartBeat, placement.DurationBeat))
                        .ToArray()),
                false);
        }

        private void CancelTimelineInteraction()
        {
            _timelineDragState = null;
        }

        private List<TimelineNoteOrigin> BuildSelectedOrigins(string primaryNoteId)
        {
            var origins = new List<TimelineNoteOrigin>();

            if (CurrentProject?.Tracks == null)
            {
                return origins;
            }

            if (_selectedNoteIds.Contains(primaryNoteId))
            {
                TryAddOrigin(CurrentProject, primaryNoteId, origins);
            }

            foreach (string noteId in _selectedNoteIds)
            {
                if (string.Equals(noteId, primaryNoteId, StringComparison.Ordinal))
                {
                    continue;
                }

                TryAddOrigin(CurrentProject, noteId, origins);
            }

            if (origins.Count == 0)
            {
                TryAddOrigin(CurrentProject, primaryNoteId, origins);
            }

            return origins;
        }

        private static void TryAddOrigin(GameAudioProject project, string noteId, List<TimelineNoteOrigin> destinations)
        {
            for (int trackIndex = 0; trackIndex < project.Tracks.Count; trackIndex++)
            {
                GameAudioTrack track = project.Tracks[trackIndex];
                GameAudioNote note = track.Notes.FirstOrDefault(candidate => string.Equals(candidate.Id, noteId, StringComparison.Ordinal));
                if (note == null)
                {
                    continue;
                }

                destinations.Add(new TimelineNoteOrigin(note.Id, track.Id, trackIndex, note.StartBeat, note.DurationBeat));
                return;
            }
        }

        private TimelineHitInfo FindTimelineHit(IEnumerable<TimelineRenderedNote> renderedNotes, Vector2 contentMouse)
        {
            foreach (TimelineRenderedNote renderedNote in renderedNotes.Reverse())
            {
                if (!renderedNote.Rect.Contains(contentMouse))
                {
                    continue;
                }

                TimelineHitEdge edge = TimelineHitEdge.Body;
                if (contentMouse.x <= renderedNote.Rect.x + TimelineResizeHandleWidth)
                {
                    edge = TimelineHitEdge.Left;
                }
                else if (contentMouse.x >= renderedNote.Rect.xMax - TimelineResizeHandleWidth)
                {
                    edge = TimelineHitEdge.Right;
                }

                return new TimelineHitInfo(renderedNote.NoteId, renderedNote.TrackId, renderedNote.TrackIndex, renderedNote.StartBeat, renderedNote.DurationBeat, renderedNote.Rect, edge);
            }

            return default;
        }

        private void ToggleNoteSelection(string noteId, string trackId)
        {
            if (_selectedNoteIds.Contains(noteId))
            {
                _selectedNoteIds.Remove(noteId);
            }
            else
            {
                _selectedNoteIds.Add(noteId);
            }

            _selectedTrackId = trackId;
        }

        private void AddNoteSelection(string noteId, string trackId)
        {
            _selectedNoteIds.Add(noteId);
            _selectedTrackId = trackId;
        }

        private void SelectOnlyNote(string noteId, string trackId)
        {
            _selectedNoteIds.Clear();
            _selectedNoteIds.Add(noteId);
            _selectedTrackId = trackId;
        }

        private void PruneTimelineSelection()
        {
            if (CurrentProject?.Tracks == null)
            {
                _selectedNoteIds.Clear();
                _selectedTrackId = string.Empty;
                return;
            }

            var existingIds = new HashSet<string>(
                CurrentProject.Tracks
                    .SelectMany(track => track.Notes)
                    .Select(note => note.Id),
                StringComparer.Ordinal);

            _selectedNoteIds.RemoveWhere(noteId => !existingIds.Contains(noteId));
            if (string.IsNullOrWhiteSpace(_selectedTrackId) && CurrentProject.Tracks.Count > 0)
            {
                _selectedTrackId = CurrentProject.Tracks[0].Id;
            }
        }

        private static void EnsureSampleProjects()
        {
            string targetDirectory = GetUserProjectFolderPath();
            Directory.CreateDirectory(targetDirectory);

            var bundledSamples = new (string Source, string Target)[]
            {
                ("BasicSE/basic-se.gats.json", "BasicSE.gats.json"),
                ("SimpleLoop/simple-loop.gats.json", "SimpleLoop.gats.json"),
                ("UIClick/ui-click.gats.json", "UIClick.gats.json"),
                ("UIConfirm/ui-confirm.gats.json", "UIConfirm.gats.json"),
                ("UICancel/ui-cancel.gats.json", "UICancel.gats.json"),
                ("CoinPickup/coin-pickup.gats.json", "CoinPickup.gats.json"),
                ("PowerUpRise/power-up-rise.gats.json", "PowerUpRise.gats.json"),
                ("LaserShot/laser-shot.gats.json", "LaserShot.gats.json"),
                ("ExplosionBurst/explosion-burst.gats.json", "ExplosionBurst.gats.json"),
                ("AlarmLoop/alarm-loop.gats.json", "AlarmLoop.gats.json")
            };

            foreach ((string source, string target) in bundledSamples)
            {
                CopySampleIfMissing(source, Path.Combine(targetDirectory, target));
            }
        }

        private static void CopySampleIfMissing(string sampleRelativePath, string targetPath)
        {
            if (File.Exists(targetPath))
            {
                return;
            }

            string sourcePath = Path.Combine(GetEmbeddedPackageRootPath(), "Samples~", sampleRelativePath);
            if (!File.Exists(sourcePath))
            {
                throw new FileNotFoundException($"Sample file was not found: {sourcePath}");
            }

            File.Copy(sourcePath, targetPath, false);
        }

        private static string GetEmbeddedPackageRootPath()
        {
            return Path.Combine(GetProjectRootPath(), "Packages", "com.sunmax.trusedison");
        }

        private static string GetProjectRootPath()
        {
            return Path.GetDirectoryName(UnityEngine.Application.dataPath) ?? Environment.CurrentDirectory;
        }

        private static string GetUserProjectFolderPath()
        {
            return Path.Combine(GetProjectRootPath(), "myAudioProjects");
        }

        private void ResetPreviewState()
        {
            _previewPlaybackService.InvalidatePreview();
            if (CurrentProject != null)
            {
                _previewPlaybackService.SetLoopPlayback(CurrentProject.LoopPlayback);
            }
        }

        private void RefreshView()
        {
            if (EnsureDisplayLanguageCurrent())
            {
                return;
            }

            GameAudioProject project = CurrentProject;
            titleContent = new GUIContent(_isDirty ? $"{GameAudioToolInfo.DisplayName}*" : GameAudioToolInfo.DisplayName);

            if (project == null)
            {
                return;
            }

            _nameValue.text = project.Name;
            _bpmValue.text = project.Bpm.ToString();
            _barsValue.text = project.TotalBars.ToString();
            _tracksValue.text = project.Tracks.Count.ToString();
            _pathValue.text = string.IsNullOrWhiteSpace(_projectPath) ? T("status.unsavedFile", "(unsaved)") : _projectPath;
            _statusValue.text = _isDirty ? T("status.unsaved", "Unsaved changes") : T("status.saved", "Saved");
            _toolbarBpmField?.SetValueWithoutNotify(project.Bpm);
            _toolbarGridField?.SetValueWithoutNotify(GameAudioTimelineGridUtility.NormalizeDivision(_currentGridDivision));
            _toolbarLoopToggle?.SetValueWithoutNotify(project.LoopPlayback);
            _timelineBarsField?.SetValueWithoutNotify(project.TotalBars);
            _loopToggle.SetValueWithoutNotify(project.LoopPlayback);
            if (_commonExportDirectoryField != null)
            {
                _commonExportDirectoryField.SetValueWithoutNotify(_commonConfig?.DefaultExportDirectory ?? "Exports/Audio");
            }

            if (_projectExportDirectoryField != null)
            {
                _projectExportDirectoryField.SetValueWithoutNotify(_projectConfig?.ExportDirectory ?? string.Empty);
            }

            _autoRefreshAfterExportToggle?.SetValueWithoutNotify(_projectConfig?.AutoRefreshAfterExport ?? true);
            _exportResolvedPathValue.text = GetResolvedExportDirectory();
            _exportFileNameValue.text = GameAudioExportUtility.NormalizeWaveFileName(project.Name);
            _exportLastResultValue.text = string.IsNullOrWhiteSpace(_lastExportedPath) ? T("status.notExported", "(not exported)") : _lastExportedPath;
            _exportQualityValue.text = BuildExportQualityText(_lastExportQualityReport);
            _normalizeExportToggle?.SetValueWithoutNotify(_normalizeExport);
            _exportNormalizeHeadroomField?.SetValueWithoutNotify(_exportNormalizeHeadroomDb);
            string exportQualityWarningText = BuildExportQualityWarningText(_lastExportQualityReport);
            if (_exportQualityHelpBox != null)
            {
                _exportQualityHelpBox.text = exportQualityWarningText;
                _exportQualityHelpBox.style.display = string.IsNullOrWhiteSpace(exportQualityWarningText)
                    ? DisplayStyle.None
                    : DisplayStyle.Flex;
            }

            _conversionSourceClipField?.SetValueWithoutNotify(_conversionSourceClip);
            _conversionOutputNameField?.SetValueWithoutNotify(_conversionOutputName);
            _conversionSampleRateField?.SetValueWithoutNotify(_conversionTargetSampleRate);
            _conversionChannelModeField?.SetValueWithoutNotify(_conversionChannelMode);
            _conversionLastResultValue.text = string.IsNullOrWhiteSpace(_lastConverted8BitPath)
                ? T("status.notExported", "(not exported)")
                : _lastConverted8BitPath;
            if (_conversionLastProjectValue != null)
            {
                _conversionLastProjectValue.text = string.IsNullOrWhiteSpace(_lastConverted8BitProjectPath)
                    ? T("status.notExported", "(not exported)")
                    : _lastConverted8BitProjectPath;
            }

            if (_undoButton != null)
            {
                _undoButton.SetEnabled(_editorSession != null && _editorSession.CanUndo);
            }

            if (_redoButton != null)
            {
                _redoButton.SetEnabled(_editorSession != null && _editorSession.CanRedo);
            }

            if (_gridButton != null)
            {
                _gridButton.text = TF("timeline.grid", "Grid {0}", _currentGridDivision);
            }

            GameAudioPreviewState previewState = _previewPlaybackService.State;
            GameAudioPreviewCursorState cursorState = GameAudioPreviewCursorCalculator.Calculate(project, previewState);
            _previewStateValue.text = LocalizePreviewStatus(previewState.StatusText);
            _previewBufferValue.text = BuildPreviewBufferText(previewState);
            _previewCursorValue.text = previewState.IsPreviewReady
                ? BuildPreviewCursorText(cursorState)
                : T("status.notRendered", "(not rendered)");
            _previewProgressBar.value = previewState.IsPreviewReady
                ? cursorState.MusicalProgress * 100.0f
                : 0.0f;
            _previewProgressBar.title = previewState.IsPreviewReady
                ? cursorState.IsInTail
                    ? TF("status.previewTail", "Playback tail +{0:0.00}s", cursorState.TailSeconds)
                    : TF("status.previewProgress", "Bar {0:00} / Beat {1:0.00}", cursorState.CurrentBar, cursorState.BeatInBar)
                : T("preview.cursorNotStarted", "Cursor not started");

            if (!string.IsNullOrWhiteSpace(previewState.ErrorText))
            {
                _previewHelpBox.text = previewState.ErrorText;
                _previewHelpBox.style.display = DisplayStyle.Flex;
            }
            else if (previewState.IsPreviewReady && !GameAudioEditorAudioUtility.IsAvailable)
            {
                _previewHelpBox.text = T("status.preview.readyButApiMissing", "Render is available, but UnityEditor preview playback API was not found in this editor build.");
                _previewHelpBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                _previewHelpBox.text = string.Empty;
                _previewHelpBox.style.display = DisplayStyle.None;
            }

            if (_loadWarnings.Count > 0)
            {
                _warningBox.text = string.Join("\n", _loadWarnings);
                _warningBox.style.display = DisplayStyle.Flex;
            }
            else
            {
                _warningBox.text = string.Empty;
                _warningBox.style.display = DisplayStyle.None;
            }

            RefreshInspectorPanel(project);
            _timelineSurface?.MarkDirtyRepaint();
            _previewWaveformView?.MarkDirtyRepaint();
            Repaint();
        }

        private bool EnsureDisplayLanguageCurrent()
        {
            GameAudioDisplayLanguage nextLanguage = GameAudioLocalization.ResolveLanguage(_commonConfig?.DisplayLanguage ?? GameAudioLanguageMode.Auto);
            if (nextLanguage == _displayLanguage)
            {
                return false;
            }

            _displayLanguage = nextLanguage;
            RequestUiRebuild();
            return true;
        }

        private void RequestUiRebuild()
        {
            if (_pendingUiRebuild)
            {
                return;
            }

            _pendingUiRebuild = true;
            EditorApplication.delayCall -= RebuildUiAfterDelay;
            EditorApplication.delayCall += RebuildUiAfterDelay;
        }

        private void RebuildUiAfterDelay()
        {
            EditorApplication.delayCall -= RebuildUiAfterDelay;

            if (this == null)
            {
                return;
            }

            _pendingUiRebuild = false;
            CreateGUI();
            rootVisualElement?.Focus();
            Repaint();
        }

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            if (ShouldIgnoreShortcutForFocusedElement(evt.target as VisualElement))
            {
                return;
            }

            bool actionKey = evt.ctrlKey || evt.commandKey;

            if (actionKey && evt.keyCode == KeyCode.N)
            {
                CreateNewProject();
                ConsumeShortcutEvent(evt);
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.O)
            {
                OpenProject();
                ConsumeShortcutEvent(evt);
                return;
            }

            if (actionKey && evt.shiftKey && evt.keyCode == KeyCode.S)
            {
                SaveProjectAs();
                ConsumeShortcutEvent(evt);
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.S)
            {
                SaveProject();
                ConsumeShortcutEvent(evt);
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.Z)
            {
                UndoLastEdit();
                ConsumeShortcutEvent(evt);
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.Y)
            {
                RedoLastEdit();
                ConsumeShortcutEvent(evt);
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.D)
            {
                if (_selectedNoteIds.Count > 0)
                {
                    DuplicateSelectedNotes();
                    ConsumeShortcutEvent(evt);
                }
                return;
            }

            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                if (_selectedNoteIds.Count > 0)
                {
                    DeleteSelectedNotes();
                    ConsumeShortcutEvent(evt);
                }
                return;
            }

            if (evt.keyCode == KeyCode.Home)
            {
                RewindPreview();
                ConsumeShortcutEvent(evt);
                return;
            }

            if (!actionKey && evt.keyCode == KeyCode.Space)
            {
                TogglePreviewPlaybackShortcut();
                ConsumeShortcutEvent(evt);
            }
        }

        private void TogglePreviewPlaybackShortcut()
        {
            GameAudioPreviewState previewState = _previewPlaybackService.State;
            if (previewState.IsPlaying)
            {
                PausePreview();
                return;
            }

            PlayPreview();
        }

        internal static bool ShouldIgnoreShortcutForFocusedElement(VisualElement element)
        {
            for (VisualElement current = element; current != null; current = current.parent)
            {
                if (current is TextField
                    || current is IntegerField
                    || current is FloatField
                    || current is DoubleField
                    || current is LongField
                    || current is Slider
                    || current is SliderInt
                    || current is Toggle
                    || current is Button)
                {
                    return true;
                }

                if (current.GetType().Name.StartsWith("PopupField", StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void FocusTimelineShortcutTarget()
        {
            if (_timelineSurface != null)
            {
                _timelineSurface.Focus();
                return;
            }

            rootVisualElement?.Focus();
        }

        private static void ConsumeShortcutEvent(EventBase evt)
        {
            if (evt == null)
            {
                return;
            }

            evt.StopImmediatePropagation();
            evt.StopPropagation();
        }

        private string BuildPreviewBufferText(GameAudioPreviewState previewState)
        {
            if (!previewState.IsPreviewReady || previewState.RenderResult == null)
            {
                return T("status.notRendered", "(not rendered)");
            }

            string channelLabel = previewState.ChannelCount == 1
                ? T("audio.mono", "Mono")
                : T("audio.stereo", "Stereo");
            return TF(
                "status.previewBuffer",
                "{0} Hz / {1} / project {2:0.00}s / output {3:0.00}s / peak {4:0.000}",
                previewState.SampleRate,
                channelLabel,
                previewState.ProjectDurationSeconds,
                previewState.OutputDurationSeconds,
                previewState.PeakAmplitude);
        }

        private string BuildPreviewCursorText(GameAudioPreviewCursorState cursorState)
        {
            string cursorText = TF(
                "status.previewCursor",
                "Bar {0:00} / Beat {1:0.00} ({2:0.00}s)",
                cursorState.CurrentBar,
                cursorState.BeatInBar,
                cursorState.MusicalSeconds);
            if (!cursorState.IsInTail)
            {
                return cursorText;
            }

            return TF("status.previewCursorTail", "{0} / tail +{1:0.00}s", cursorText, cursorState.TailSeconds);
        }

        private string BuildExportQualityText(GameAudioExportQualityReport report)
        {
            if (report == null)
            {
                return T("status.notExported", "(not exported)");
            }

            string channelLabel = report.ChannelCount == 1
                ? T("audio.mono", "Mono")
                : T("audio.stereo", "Stereo");
            string normalizeText = report.NormalizeEnabled
                ? report.NormalizeApplied
                    ? TF("export.quality.normalizeApplied", "normalize {0:+0.00;-0.00;0.00} dB", report.NormalizeGainDb)
                    : T("export.quality.normalizeSkipped", "normalize skipped")
                : T("export.quality.normalizeOff", "normalize off");
            string comparisonText = string.IsNullOrWhiteSpace(_lastExportComparisonText)
                ? string.Empty
                : $" / {_lastExportComparisonText}";
            return TF(
                "export.quality.summary",
                "{0} Hz / {1} / peak {2:0.000} (source {3:0.000}) / project {4:0.00}s / output {5:0.00}s / tail +{6:0.00}s / {7}{8}",
                report.SampleRate,
                channelLabel,
                report.OutputPeakAmplitude,
                report.SourcePeakAmplitude,
                report.ProjectDurationSeconds,
                report.OutputDurationSeconds,
                report.TailDurationSeconds,
                normalizeText,
                comparisonText);
        }

        private string BuildExportQualityWarningText(GameAudioExportQualityReport report)
        {
            if (report == null)
            {
                return string.Empty;
            }

            var messages = new List<string>();
            if (report.IsSilent)
            {
                messages.Add(T("export.quality.warningSilent", "Silent buffer: exported samples are effectively silent."));
            }
            else if (report.IsVeryLowPeak)
            {
                messages.Add(T("export.quality.warningLowPeak", "Very low peak: exported audio may be too quiet for distribution."));
            }

            if (report.HasClippingRisk)
            {
                messages.Add(T("export.quality.warningClip", "Clipping risk: output peak is at or above full scale."));
            }
            else if (report.SourceExceededFullScale && report.NormalizeApplied)
            {
                messages.Add(T("export.quality.warningNormalizedClip", "Source peak exceeded full scale; normalization reduced the exported output."));
            }

            return string.Join("\n", messages);
        }

        private string BuildExportComparisonText(GameAudioExportQualityReport previous, GameAudioExportQualityReport current)
        {
            if (previous == null || current == null)
            {
                return T("export.quality.firstExport", "first export this session");
            }

            return TF(
                "export.quality.compare",
                "delta peak {0:+0.000;-0.000;0.000}, delta duration {1:+0.00;-0.00;0.00}s",
                current.OutputPeakAmplitude - previous.OutputPeakAmplitude,
                current.OutputDurationSeconds - previous.OutputDurationSeconds);
        }

        private string LocalizePreviewStatus(string statusText)
        {
            return statusText switch
            {
                "Preview not rendered." => T("status.preview.notRendered", "Preview not rendered."),
                "Preview render failed." => T("status.preview.renderFailed", "Preview render failed."),
                "Loop preview playing." => T("status.preview.loopPlaying", "Loop preview playing."),
                "Preview playing." => T("status.preview.playing", "Preview playing."),
                "Loop preview paused." => T("status.preview.loopPaused", "Loop preview paused."),
                "Preview paused." => T("status.preview.paused", "Preview paused."),
                "Loop preview ready." => T("status.preview.loopReady", "Loop preview ready."),
                "Preview ready." => T("status.preview.ready", "Preview ready."),
                "Preview stopped." => T("status.preview.stopped", "Preview stopped."),
                "Preview rewound." => T("status.preview.rewound", "Preview rewound."),
                "Preview complete." => T("status.preview.complete", "Preview complete."),
                "Preview ready. Unity editor playback API is unavailable." => T("status.preview.apiUnavailable", "Preview ready. Unity editor playback API is unavailable."),
                "Preview ready (silent buffer)." => T("status.preview.silent", "Preview ready (silent buffer)."),
                "Preview start failed." => T("status.preview.startFailed", "Preview start failed."),
                "Preview loop restart failed." => T("status.preview.loopRestartFailed", "Preview loop restart failed."),
                _ => statusText
            };
        }

        private static double GetWaveformDurationSeconds(GameAudioPreviewState previewState)
        {
            if (previewState == null)
            {
                return 0.0d;
            }

            return previewState.LoopPlayback
                ? Math.Max(0.0d, previewState.ProjectDurationSeconds)
                : Math.Max(previewState.ProjectDurationSeconds, previewState.OutputDurationSeconds);
        }

        private static float CalculateWaveformCursorProgress(GameAudioPreviewState previewState)
        {
            double totalDurationSeconds = GetWaveformDurationSeconds(previewState);
            if (previewState == null || totalDurationSeconds <= 0.0d)
            {
                return 0.0f;
            }

            double playbackSeconds = previewState.LoopPlayback
                ? Math.Min(previewState.ProjectDurationSeconds, previewState.PlaybackSeconds)
                : Math.Min(totalDurationSeconds, previewState.PlaybackSeconds);
            return Mathf.Clamp01((float)(playbackSeconds / totalDurationSeconds));
        }

        private static void DrawWaveformBins(Rect rect, GameAudioPreviewWaveformData waveform)
        {
            float centerY = rect.center.y;
            float amplitudeHeight = (rect.height * 0.5f) - 2.0f;
            int binCount = waveform.Bins.Length;
            float binWidth = Mathf.Max(1.0f, rect.width / binCount);
            Color waveformColor = new Color(0.18f, 0.84f, 0.95f, 0.95f);

            for (int index = 0; index < binCount; index++)
            {
                GameAudioPreviewWaveformBin bin = waveform.Bins[index];
                float minY = centerY - (bin.Max * amplitudeHeight);
                float maxY = centerY - (bin.Min * amplitudeHeight);
                float x = rect.x + (index * binWidth);
                float height = Mathf.Max(1.0f, maxY - minY);
                EditorGUI.DrawRect(new Rect(x, minY, Mathf.Max(1.0f, binWidth), height), waveformColor);
            }
        }

        private static void DrawWaveformCursor(Rect rect, GameAudioPreviewState previewState)
        {
            float progress = CalculateWaveformCursorProgress(previewState);
            float cursorX = rect.x + (rect.width * progress);
            EditorGUI.DrawRect(new Rect(cursorX, rect.y, 2.0f, rect.height), new Color(1.0f, 0.88f, 0.35f, 0.95f));
        }

        private static void DrawWaveformPlaceholder(Rect rect, string label)
        {
            GUIStyle centeredLabel = new GUIStyle(EditorStyles.centeredGreyMiniLabel)
            {
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true
            };
            GUI.Label(rect, label, centeredLabel);
        }

        private void OnDisable()
        {
            EditorApplication.delayCall -= RebuildUiAfterDelay;
            _previewTicker?.Pause();
            _previewTicker = null;
            _previewPlaybackService.Dispose();
        }

        private readonly struct SelectedNoteContext
        {
            public SelectedNoteContext(GameAudioTrack track, GameAudioNote note)
            {
                Track = track;
                Note = note;
            }

            public GameAudioTrack Track { get; }

            public GameAudioNote Note { get; }
        }

        private enum WorkspacePage
        {
            File,
            Edit,
            Export,
            Settings
        }

        private sealed class TimelineDragState
        {
            public TimelineInteractionMode Mode;
            public string PrimaryNoteId = string.Empty;
            public int AnchorTrackIndex;
            public float AnchorBeat;
            public float CurrentBeat;
            public List<TimelineNoteOrigin> Origins = new List<TimelineNoteOrigin>();
            public Dictionary<string, TimelinePreviewPlacement> PreviewPlacements = new Dictionary<string, TimelinePreviewPlacement>(StringComparer.Ordinal);
        }

        private enum TimelineInteractionMode
        {
            None,
            CreatingNote,
            MovingNotes,
            ResizingLeft,
            ResizingRight
        }

        private enum TimelineHitEdge
        {
            Body,
            Left,
            Right
        }

        private readonly struct TimelineMetrics
        {
            public TimelineMetrics(GameAudioProject project)
            {
                int beatsPerBar = Math.Max(1, project?.TimeSignature?.Numerator ?? 4);
                int trackCount = Math.Max(1, project?.Tracks?.Count ?? 1);
                BeatsPerBar = beatsPerBar;
                TotalBars = Math.Max(1, project?.TotalBars ?? 1);
                TotalBeats = TotalBars * beatsPerBar;
                TrackCount = trackCount;
                HeaderWidth = TimelineHeaderWidth;
                RulerHeight = TimelineRulerHeight;
                RowHeight = TimelineRowHeight;
                FooterHeight = TimelineFooterHeight;
                NoteHeight = TimelineNoteHeight;
                PixelsPerBeat = TimelinePixelsPerBeat;
                TimelineWidth = Math.Max(640.0f, TotalBeats * PixelsPerBeat);
                ContentWidth = HeaderWidth + TimelineWidth + 24.0f;
                ContentHeight = RulerHeight + (trackCount * RowHeight) + FooterHeight + 8.0f;
            }

            public int BeatsPerBar { get; }

            public int TotalBars { get; }

            public int TotalBeats { get; }

            public int TrackCount { get; }

            public float HeaderWidth { get; }

            public float RulerHeight { get; }

            public float RowHeight { get; }

            public float FooterHeight { get; }

            public float NoteHeight { get; }

            public float PixelsPerBeat { get; }

            public float TimelineWidth { get; }

            public float ContentWidth { get; }

            public float ContentHeight { get; }

            public float GetTrackY(int trackIndex)
            {
                return RulerHeight + (trackIndex * RowHeight);
            }

            public Rect GetLaneRect(int trackIndex)
            {
                return new Rect(HeaderWidth, GetTrackY(trackIndex), TimelineWidth, RowHeight);
            }

            public Rect GetHeaderRect(int trackIndex)
            {
                return new Rect(0.0f, GetTrackY(trackIndex), HeaderWidth, RowHeight);
            }

            public Rect GetFooterRect(int trackCount)
            {
                return new Rect(0.0f, RulerHeight + (trackCount * RowHeight), HeaderWidth, FooterHeight);
            }

            public Rect GetNoteRect(int trackIndex, float startBeat, float durationBeat)
            {
                float x = HeaderWidth + (startBeat * PixelsPerBeat);
                float y = GetTrackY(trackIndex) + ((RowHeight - NoteHeight) * 0.5f);
                float width = Math.Max(6.0f, durationBeat * PixelsPerBeat);
                return new Rect(x, y, width, NoteHeight);
            }

            public float GetBeatFromContentX(float contentX)
            {
                return Mathf.Clamp((contentX - HeaderWidth) / PixelsPerBeat, 0.0f, TotalBeats);
            }

            public int GetTrackIndex(float contentY)
            {
                if (contentY < RulerHeight)
                {
                    return -1;
                }

                if (contentY >= RulerHeight + (TrackCount * RowHeight))
                {
                    return -1;
                }

                return Mathf.FloorToInt((contentY - RulerHeight) / RowHeight);
            }

        }

        private readonly struct TimelineRenderedNote
        {
            public TimelineRenderedNote(string noteId, string trackId, int trackIndex, int midiNote, float startBeat, float durationBeat, Rect rect, bool isSelected)
            {
                NoteId = noteId;
                TrackId = trackId;
                TrackIndex = trackIndex;
                MidiNote = midiNote;
                StartBeat = startBeat;
                DurationBeat = durationBeat;
                Rect = rect;
                IsSelected = isSelected;
            }

            public string NoteId { get; }

            public string TrackId { get; }

            public int TrackIndex { get; }

            public int MidiNote { get; }

            public float StartBeat { get; }

            public float DurationBeat { get; }

            public Rect Rect { get; }

            public bool IsSelected { get; }
        }

        private readonly struct TimelineNoteOrigin
        {
            public TimelineNoteOrigin(string noteId, string trackId, int trackIndex, float startBeat, float durationBeat)
            {
                NoteId = noteId;
                TrackId = trackId;
                TrackIndex = trackIndex;
                StartBeat = startBeat;
                DurationBeat = durationBeat;
            }

            public string NoteId { get; }

            public string TrackId { get; }

            public int TrackIndex { get; }

            public float StartBeat { get; }

            public float DurationBeat { get; }
        }

        private readonly struct TimelinePreviewPlacement
        {
            public TimelinePreviewPlacement(string noteId, string trackId, int trackIndex, float startBeat, float durationBeat)
            {
                NoteId = noteId;
                TrackId = trackId;
                TrackIndex = trackIndex;
                StartBeat = startBeat;
                DurationBeat = durationBeat;
            }

            public string NoteId { get; }

            public string TrackId { get; }

            public int TrackIndex { get; }

            public float StartBeat { get; }

            public float DurationBeat { get; }
        }

        private readonly struct TimelineHitInfo
        {
            public TimelineHitInfo(string noteId, string trackId, int trackIndex, float startBeat, float durationBeat, Rect rect, TimelineHitEdge edge)
            {
                NoteId = noteId;
                TrackId = trackId;
                TrackIndex = trackIndex;
                StartBeat = startBeat;
                DurationBeat = durationBeat;
                Rect = rect;
                Edge = edge;
            }

            public string NoteId { get; }

            public string TrackId { get; }

            public int TrackIndex { get; }

            public float StartBeat { get; }

            public float DurationBeat { get; }

            public Rect Rect { get; }

            public TimelineHitEdge Edge { get; }

            public bool HasValue => !string.IsNullOrWhiteSpace(NoteId);
        }
    }
}
