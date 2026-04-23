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
using TorusEdison.Editor.Persistence;
using TorusEdison.Editor.Utilities;
using UnityEditor;
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

        private readonly GameAudioCommonConfigSerializer _commonConfigSerializer = new GameAudioCommonConfigSerializer();
        private readonly GameAudioPreviewPlaybackService _previewPlaybackService = new GameAudioPreviewPlaybackService();
        private readonly GameAudioProjectSerializer _projectSerializer = new GameAudioProjectSerializer();
        private readonly GameAudioProjectConfigSerializer _projectConfigSerializer = new GameAudioProjectConfigSerializer();
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
        private IVisualElementScheduledItem _previewTicker;
        private TimelineDragState _timelineDragState;
        private Vector2 _timelineScrollPosition;
        private string _selectedTrackId = string.Empty;
        private string _currentGridDivision = "1/16";
        private WorkspacePage _currentWorkspacePage = WorkspacePage.File;

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
        private Label _timelineHintValue;
        private IMGUIContainer _timelineSurface;
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
            _currentGridDivision = GameAudioTimelineGridUtility.NormalizeDivision(_commonConfig.DefaultGridDivision);

            if (_project == null)
            {
                BindProject(CreateConfiguredProject(), true, string.Empty, Array.Empty<string>());
            }

            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.paddingLeft = 12;
            rootVisualElement.style.paddingRight = 12;
            rootVisualElement.style.paddingTop = 12;
            rootVisualElement.style.paddingBottom = 12;
            _workspaceTabButtons.Clear();
            _workspacePages.Clear();

            rootVisualElement.Add(BuildToolbar());
            rootVisualElement.Add(BuildNavigationBar());
            rootVisualElement.Add(BuildWorkspacePages());

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnRootKeyDown);

            _previewTicker?.Pause();
            _previewTicker = rootVisualElement.schedule.Execute(HandlePreviewTick).Every(50);

            SetWorkspacePage(_currentWorkspacePage);
            RefreshView();
        }

        private VisualElement BuildToolbar()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.alignItems = Align.Center;
            container.style.flexWrap = Wrap.Wrap;
            container.style.marginBottom = 12;

            container.Add(CreateToolbarButton("New", CreateNewProject));
            container.Add(CreateToolbarButton("Open", OpenProject));
            container.Add(CreateToolbarButton("Save", SaveProject));
            container.Add(CreateToolbarButton("Save As", SaveProjectAs));

            return container;
        }

        private VisualElement BuildNavigationBar()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexWrap = Wrap.Wrap;
            container.style.marginBottom = 12.0f;

            AddWorkspaceTabButton(container, WorkspacePage.File, "File");
            AddWorkspaceTabButton(container, WorkspacePage.Edit, "Edit");
            AddWorkspaceTabButton(container, WorkspacePage.Preview, "Preview");
            AddWorkspaceTabButton(container, WorkspacePage.Export, "Export");
            AddWorkspaceTabButton(container, WorkspacePage.Settings, "Settings");

            return container;
        }

        private VisualElement BuildWorkspacePages()
        {
            var container = new VisualElement();
            container.style.flexGrow = 1.0f;

            container.Add(CreateWorkspacePage(WorkspacePage.File, page =>
            {
                page.Add(BuildPageHeader("File", "Project files, current status, and sample workflows."));
                page.Add(BuildSummaryPanel());
                page.Add(BuildSamplePanel());
            }));

            container.Add(CreateWorkspacePage(WorkspacePage.Edit, page =>
            {
                page.Add(BuildPageHeader("Edit", "Timeline editing and selection-scoped note or track changes."));
                page.Add(BuildTimelinePanel());
                page.Add(BuildSelectionInspectorPanel());
            }));

            container.Add(CreateWorkspacePage(WorkspacePage.Preview, page =>
            {
                page.Add(BuildPageHeader("Preview", "Render and audition the current project without leaving the editor."));
                page.Add(BuildPreviewPanel());
            }));

            container.Add(CreateWorkspacePage(WorkspacePage.Export, page =>
            {
                page.Add(BuildPageHeader("Export", "Write WAV files and confirm the current output destination."));
                page.Add(BuildExportPanel());
            }));

            container.Add(CreateWorkspacePage(WorkspacePage.Settings, page =>
            {
                page.Add(BuildPageHeader("Settings", "Project-level settings and foundation diagnostics."));
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

            panel.Add(CreateSectionTitle("Current Project"));
            _nameValue = AddKeyValue(panel, "Name");
            _bpmValue = AddKeyValue(panel, "BPM");
            _barsValue = AddKeyValue(panel, "Bars");
            _tracksValue = AddKeyValue(panel, "Tracks");
            _pathValue = AddKeyValue(panel, "File");
            _statusValue = AddKeyValue(panel, "Status");

            return panel;
        }

        private VisualElement BuildTimelinePanel()
        {
            var panel = CreateSectionPanel(new Color(0.12f, 0.12f, 0.12f));

            panel.Add(CreateSectionTitle("Timeline Editing"));

            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.alignItems = Align.Center;
            actionRow.style.flexWrap = Wrap.Wrap;
            actionRow.style.marginBottom = 8;

            _undoButton = CreateToolbarButton("Undo", UndoLastEdit);
            _redoButton = CreateToolbarButton("Redo", RedoLastEdit);
            _gridButton = CreateToolbarButton("Grid 1/16", CycleGridDivision);

            actionRow.Add(_undoButton);
            actionRow.Add(_redoButton);
            actionRow.Add(_gridButton);

            panel.Add(actionRow);

            _timelineHintValue = new Label();
            _timelineHintValue.style.marginBottom = 8;
            _timelineHintValue.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(_timelineHintValue);

            _timelineSurface = new IMGUIContainer(DrawTimelineGui)
            {
                focusable = true
            };
            _timelineSurface.style.height = TimelineViewportHeight;
            _timelineSurface.style.marginBottom = 4;
            _timelineSurface.RegisterCallback<MouseDownEvent>(_ => _timelineSurface.Focus());
            panel.Add(_timelineSurface);

            return panel;
        }

        private VisualElement BuildSelectionInspectorPanel()
        {
            var panel = CreateSectionPanel(new Color(0.14f, 0.14f, 0.14f));

            panel.Add(CreateSectionTitle("Selection Inspector"));

            _selectionInspectorContainer = new VisualElement();
            _selectionInspectorContainer.style.marginBottom = 12.0f;
            panel.Add(_selectionInspectorContainer);

            return panel;
        }

        private VisualElement BuildProjectInspectorPanel()
        {
            var panel = CreateSectionPanel(new Color(0.14f, 0.14f, 0.14f));

            panel.Add(CreateSectionTitle("Project Inspector"));

            _projectInspectorContainer = new VisualElement();
            panel.Add(_projectInspectorContainer);

            return panel;
        }

        private VisualElement BuildPreviewPanel()
        {
            var panel = CreateSectionPanel(new Color(0.13f, 0.13f, 0.13f));

            panel.Add(CreateSectionTitle("Preview Playback"));

            var transportRow = new VisualElement();
            transportRow.style.flexDirection = FlexDirection.Row;
            transportRow.style.alignItems = Align.Center;
            transportRow.style.flexWrap = Wrap.Wrap;
            transportRow.style.marginBottom = 8;

            transportRow.Add(CreateToolbarButton("Render Preview", RenderPreview));
            transportRow.Add(CreateToolbarButton("Play", PlayPreview));
            transportRow.Add(CreateToolbarButton("Pause", PausePreview));
            transportRow.Add(CreateToolbarButton("Stop", StopPreview));
            transportRow.Add(CreateToolbarButton("Rewind", RewindPreview));

            _loopToggle = new Toggle("Loop");
            _loopToggle.style.marginLeft = 4;
            _loopToggle.RegisterValueChangedCallback(OnLoopPlaybackChanged);
            transportRow.Add(_loopToggle);

            panel.Add(transportRow);

            _previewStateValue = AddKeyValue(panel, "Preview");
            _previewBufferValue = AddKeyValue(panel, "Buffer");
            _previewCursorValue = AddKeyValue(panel, "Cursor");

            _previewProgressBar = new ProgressBar
            {
                title = "Cursor not started"
            };
            _previewProgressBar.lowValue = 0.0f;
            _previewProgressBar.highValue = 100.0f;
            _previewProgressBar.style.marginTop = 4;
            panel.Add(_previewProgressBar);

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

            panel.Add(CreateSectionTitle("WAV Export"));

            var actionRow = new VisualElement();
            actionRow.style.flexDirection = FlexDirection.Row;
            actionRow.style.alignItems = Align.Center;
            actionRow.style.flexWrap = Wrap.Wrap;
            actionRow.style.marginBottom = 8.0f;
            actionRow.Add(CreateToolbarButton("Export WAV", ExportWav));
            actionRow.Add(CreateToolbarButton("Open Export Folder", OpenExportFolder));
            panel.Add(actionRow);

            _exportResolvedPathValue = AddKeyValue(panel, "Resolved Folder");
            _exportFileNameValue = AddKeyValue(panel, "Export File");
            _exportLastResultValue = AddKeyValue(panel, "Last Export");

            _commonExportDirectoryField = new TextField
            {
                isDelayed = true
            };
            _commonExportDirectoryField.RegisterValueChangedCallback(OnCommonExportDirectoryChanged);
            panel.Add(CreateInspectorRow("Common Default Folder", _commonExportDirectoryField));

            _projectExportDirectoryField = new TextField
            {
                isDelayed = true
            };
            _projectExportDirectoryField.RegisterValueChangedCallback(OnProjectExportDirectoryChanged);
            panel.Add(CreateInspectorRow("Project Override Folder", _projectExportDirectoryField));

            _autoRefreshAfterExportToggle = new Toggle();
            _autoRefreshAfterExportToggle.RegisterValueChangedCallback(OnAutoRefreshAfterExportChanged);
            panel.Add(CreateInspectorRow("Auto Refresh Assets", _autoRefreshAfterExportToggle));

            return panel;
        }

        private VisualElement BuildSamplePanel()
        {
            var panel = CreateSectionPanel(new Color(0.15f, 0.15f, 0.15f));

            panel.Add(CreateSectionTitle("Samples And Workflow"));

            var sampleRow = new VisualElement();
            sampleRow.style.flexDirection = FlexDirection.Row;
            sampleRow.style.flexWrap = Wrap.Wrap;
            sampleRow.style.marginBottom = 8;

            sampleRow.Add(CreateToolbarButton("Create Samples", CreateSampleProjects));
            sampleRow.Add(CreateToolbarButton("Load Basic SE", LoadBasicSampleProject));
            sampleRow.Add(CreateToolbarButton("Load Simple Loop", LoadSimpleLoopSampleProject));
            sampleRow.Add(CreateToolbarButton("Open Folder", OpenSampleProjectsFolder));

            panel.Add(sampleRow);

            var sampleLocationLabel = new Label($"Sample files are stored under {GetUserProjectFolderPath()}");
            sampleLocationLabel.style.marginBottom = 4;
            panel.Add(sampleLocationLabel);

            var editingLabel = new Label("Timeline editing and inspector editing are now available. Use the Edit tab to create notes, move them, resize them, and adjust note or track parameters without leaving the editor.");
            editingLabel.style.whiteSpace = WhiteSpace.Normal;
            editingLabel.style.marginBottom = 4;
            panel.Add(editingLabel);

            var fieldsLabel = new Label("JSON is still useful for bulk edits, review, and version control, but file actions, preview, export, and settings are now separated into dedicated tabs.");
            fieldsLabel.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(fieldsLabel);

            return panel;
        }

        private VisualElement BuildInfoPanel()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Column;

            panel.Add(CreateSectionTitle("Foundation Status"));
            var currentScopeLabel = new Label("This window now separates file, edit, preview, export, and settings workflows while keeping the same project state, selection, playback, Undo / Redo, JSON save/load, and WAV export foundations.");
            currentScopeLabel.style.marginBottom = 4;
            panel.Add(currentScopeLabel);

            var nextScopeLabel = new Label("Release validation, documentation sync, and distribution packaging are the next layers to connect.");
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
                "{0}|{1}|{2}",
                RuntimeHelpers.GetHashCode(project),
                _selectedTrackId,
                selectionKey);

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

            _selectionInspectorContainer.Add(CreateInspectorGroupTitle("Selection"));

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
                    _selectionInspectorContainer.Add(CreateInspectorHelpBox("Select a track header or note in the timeline to start editing.", HelpBoxMessageType.Info));
                }
                else
                {
                    BuildTrackInspector(_selectionInspectorContainer, selectedTrack);
                }
            }

            _projectInspectorContainer.Add(CreateInspectorGroupTitle("Project"));
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
                ? $"Editing note {primarySelection.Note.Id} on {primarySelection.Track.Name}."
                : $"Editing {selectedNotes.Count} notes across {trackCount} tracks. Changes apply to every selected note.";
            parent.Add(CreateInspectorSummaryLabel(summaryText));

            AddInspectorFloatField(parent, "Start Beat", primarySelection.Note.StartBeat, requested =>
            {
                TryApplySelectedNotesChange(
                    "Set Note Start",
                    note => note.StartBeat = requested,
                    actualNotes => NotifyClamp("Start Beat", requested, actualNotes[0].Note.StartBeat));
            });

            AddInspectorFloatField(parent, "Duration Beat", primarySelection.Note.DurationBeat, requested =>
            {
                TryApplySelectedNotesChange(
                    "Set Note Duration",
                    note => note.DurationBeat = requested,
                    actualNotes => NotifyClamp("Duration Beat", requested, actualNotes[0].Note.DurationBeat));
            });

            AddInspectorIntegerField(parent, "MIDI Note", primarySelection.Note.MidiNote, requested =>
            {
                TryApplySelectedNotesChange(
                    "Set Note Pitch",
                    note => note.MidiNote = requested,
                    actualNotes => NotifyClamp("MIDI Note", requested, actualNotes[0].Note.MidiNote));
            });

            AddInspectorFloatField(parent, "Velocity", primarySelection.Note.Velocity, requested =>
            {
                TryApplySelectedNotesChange(
                    "Set Note Velocity",
                    note => note.Velocity = requested,
                    actualNotes => NotifyClamp("Velocity", requested, actualNotes[0].Note.Velocity));
            });

            bool allHaveOverride = selectedNotes.All(selection => selection.Note.VoiceOverride != null);
            bool anyHaveOverride = selectedNotes.Any(selection => selection.Note.VoiceOverride != null);
            AddInspectorToggleField(parent, "Use Voice Override", allHaveOverride, enabled =>
            {
                TryApplySelectedNotesChange(
                    enabled ? "Enable Voice Override" : "Disable Voice Override",
                    note => note.VoiceOverride = enabled ? note.VoiceOverride ?? GameAudioProjectFactory.CreateDefaultVoice() : null);
            });

            if (!allHaveOverride)
            {
                string message = anyHaveOverride
                    ? "Some selected notes still use the track default voice. Enable voice override to apply explicit note-level voice settings to the full selection."
                    : "Selected notes currently use each track's default voice. Enable voice override to edit per-note voice settings.";
                parent.Add(CreateInspectorHelpBox(message, HelpBoxMessageType.Info));
                return;
            }

            GameAudioVoiceSettings voice = primarySelection.Note.VoiceOverride ?? GameAudioProjectFactory.CreateDefaultVoice();
            AddVoiceInspector(
                parent,
                "Voice Override",
                voice,
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
                });
        }

        private void BuildTrackInspector(VisualElement parent, GameAudioTrack track)
        {
            parent.Add(CreateInspectorSummaryLabel($"Editing {track.Name}. Notes: {track.Notes.Count}."));

            AddInspectorTextField(parent, "Track Name", track.Name, requested =>
            {
                if (string.Equals(track.Name, requested, StringComparison.Ordinal))
                {
                    return;
                }

                TryApplyTrackChange("Rename Track", track.Id, current => current.Name = requested);
            });

            AddInspectorToggleField(parent, "Mute", track.Mute, requested =>
            {
                if (track.Mute == requested)
                {
                    return;
                }

                TryApplyTrackChange("Toggle Mute", track.Id, current => current.Mute = requested);
            });

            AddInspectorToggleField(parent, "Solo", track.Solo, requested =>
            {
                if (track.Solo == requested)
                {
                    return;
                }

                TryApplyTrackChange("Toggle Solo", track.Id, current => current.Solo = requested);
            });

            AddInspectorFloatField(parent, "Volume (dB)", track.VolumeDb, requested =>
            {
                TryApplyTrackChange(
                    "Set Track Volume",
                    track.Id,
                    current => current.VolumeDb = requested,
                    actualTrack => NotifyClamp("Track Volume", requested, actualTrack.VolumeDb));
            });

            AddInspectorFloatField(parent, "Pan", track.Pan, requested =>
            {
                TryApplyTrackChange(
                    "Set Track Pan",
                    track.Id,
                    current => current.Pan = requested,
                    actualTrack => NotifyClamp("Track Pan", requested, actualTrack.Pan));
            });

            AddVoiceInspector(
                parent,
                "Default Voice",
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
            parent.Add(CreateInspectorSummaryLabel("Transport, output, and render settings for the current project."));

            AddInspectorTextField(parent, "Project Name", project.Name, requested =>
            {
                if (string.Equals(project.Name, requested, StringComparison.Ordinal))
                {
                    return;
                }

                TryApplyProjectChange("Rename Project", current => current.Name = requested);
            });

            AddInspectorIntegerField(parent, "BPM", project.Bpm, requested =>
            {
                TryApplyProjectChange(
                    "Set BPM",
                    current => current.Bpm = requested,
                    actualProject => NotifyClamp("BPM", requested, actualProject.Bpm));
            });

            AddInspectorPopupField(parent, "Time Signature", GetSupportedTimeSignatureOptions(), FormatTimeSignature(project.TimeSignature), requested =>
            {
                TryApplyProjectChange(
                    "Set Time Signature",
                    current => current.TimeSignature = ParseTimeSignature(requested));
            });

            AddInspectorIntegerField(parent, "Total Bars", project.TotalBars, requested =>
            {
                TryApplyProjectChange(
                    "Set Total Bars",
                    current => current.TotalBars = requested,
                    actualProject => NotifyClamp("Total Bars", requested, actualProject.TotalBars));
            });

            AddInspectorPopupField(parent, "Sample Rate", GetSupportedSampleRateOptions(), FormatSampleRateOption(project.SampleRate), requested =>
            {
                TryApplyProjectChange(
                    "Set Sample Rate",
                    current => current.SampleRate = ParseSampleRateOption(requested));
            });

            AddInspectorPopupField(parent, "Channel Mode", GetSupportedChannelModeOptions(), project.ChannelMode.ToString(), requested =>
            {
                TryApplyProjectChange(
                    "Set Channel Mode",
                    current => current.ChannelMode = ParseChannelMode(requested));
            });

            AddInspectorFloatField(parent, "Master Gain (dB)", project.MasterGainDb, requested =>
            {
                TryApplyProjectChange(
                    "Set Master Gain",
                    current => current.MasterGainDb = requested,
                    actualProject => NotifyClamp("Master Gain", requested, actualProject.MasterGainDb));
            });
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

            AddInspectorPopupField(voiceFoldout, "Waveform", GetSupportedWaveformOptions(), voice.Waveform.ToString(), requested =>
            {
                applyVoiceChange("Set Waveform", current => current.Waveform = ParseWaveform(requested), null);
            });

            AddInspectorFloatField(voiceFoldout, "Pulse Width", voice.PulseWidth, requested =>
            {
                applyVoiceChange(
                    "Set Pulse Width",
                    current => current.PulseWidth = requested,
                    actualVoice => NotifyClamp("Pulse Width", requested, actualVoice.PulseWidth));
            });

            AddInspectorToggleField(voiceFoldout, "Noise Enabled", voice.NoiseEnabled, requested =>
            {
                applyVoiceChange("Toggle Noise", current => current.NoiseEnabled = requested, null);
            });

            AddInspectorPopupField(voiceFoldout, "Noise Type", GetSupportedNoiseTypeOptions(), voice.NoiseType.ToString(), requested =>
            {
                applyVoiceChange("Set Noise Type", current => current.NoiseType = ParseNoiseType(requested), null);
            });

            AddInspectorFloatField(voiceFoldout, "Noise Mix", voice.NoiseMix, requested =>
            {
                applyVoiceChange(
                    "Set Noise Mix",
                    current => current.NoiseMix = requested,
                    actualVoice => NotifyClamp("Noise Mix", requested, actualVoice.NoiseMix));
            });

            var envelopeFoldout = new Foldout
            {
                text = "Envelope",
                value = false
            };
            voiceFoldout.Add(envelopeFoldout);

            AddInspectorIntegerField(envelopeFoldout, "Attack (ms)", voice.Adsr.AttackMs, requested =>
            {
                applyVoiceChange(
                    "Set Attack",
                    current => current.Adsr.AttackMs = requested,
                    actualVoice => NotifyClamp("Attack", requested, actualVoice.Adsr.AttackMs));
            });

            AddInspectorIntegerField(envelopeFoldout, "Decay (ms)", voice.Adsr.DecayMs, requested =>
            {
                applyVoiceChange(
                    "Set Decay",
                    current => current.Adsr.DecayMs = requested,
                    actualVoice => NotifyClamp("Decay", requested, actualVoice.Adsr.DecayMs));
            });

            AddInspectorFloatField(envelopeFoldout, "Sustain", voice.Adsr.Sustain, requested =>
            {
                applyVoiceChange(
                    "Set Sustain",
                    current => current.Adsr.Sustain = requested,
                    actualVoice => NotifyClamp("Sustain", requested, actualVoice.Adsr.Sustain));
            });

            AddInspectorIntegerField(envelopeFoldout, "Release (ms)", voice.Adsr.ReleaseMs, requested =>
            {
                applyVoiceChange(
                    "Set Release",
                    current => current.Adsr.ReleaseMs = requested,
                    actualVoice => NotifyClamp("Release", requested, actualVoice.Adsr.ReleaseMs));
            });

            var effectFoldout = new Foldout
            {
                text = "Effect",
                value = false
            };
            voiceFoldout.Add(effectFoldout);

            AddInspectorFloatField(effectFoldout, "Volume (dB)", voice.Effect.VolumeDb, requested =>
            {
                applyVoiceChange(
                    "Set Voice Volume",
                    current => current.Effect.VolumeDb = requested,
                    actualVoice => NotifyClamp("Voice Volume", requested, actualVoice.Effect.VolumeDb));
            });

            AddInspectorFloatField(effectFoldout, "Pan", voice.Effect.Pan, requested =>
            {
                applyVoiceChange(
                    "Set Voice Pan",
                    current => current.Effect.Pan = requested,
                    actualVoice => NotifyClamp("Voice Pan", requested, actualVoice.Effect.Pan));
            });

            AddInspectorFloatField(effectFoldout, "Pitch (semitone)", voice.Effect.PitchSemitone, requested =>
            {
                applyVoiceChange(
                    "Set Voice Pitch",
                    current => current.Effect.PitchSemitone = requested,
                    actualVoice => NotifyClamp("Voice Pitch", requested, actualVoice.Effect.PitchSemitone));
            });

            AddInspectorIntegerField(effectFoldout, "Fade In (ms)", voice.Effect.FadeInMs, requested =>
            {
                applyVoiceChange(
                    "Set Fade In",
                    current => current.Effect.FadeInMs = requested,
                    actualVoice => NotifyClamp("Fade In", requested, actualVoice.Effect.FadeInMs));
            });

            AddInspectorIntegerField(effectFoldout, "Fade Out (ms)", voice.Effect.FadeOutMs, requested =>
            {
                applyVoiceChange(
                    "Set Fade Out",
                    current => current.Effect.FadeOutMs = requested,
                    actualVoice => NotifyClamp("Fade Out", requested, actualVoice.Effect.FadeOutMs));
            });

            var delayFoldout = new Foldout
            {
                text = "Delay",
                value = false
            };
            effectFoldout.Add(delayFoldout);

            AddInspectorToggleField(delayFoldout, "Enabled", voice.Effect.Delay.Enabled, requested =>
            {
                applyVoiceChange("Toggle Delay", current => current.Effect.Delay.Enabled = requested, null);
            });

            AddInspectorIntegerField(delayFoldout, "Time (ms)", voice.Effect.Delay.TimeMs, requested =>
            {
                applyVoiceChange(
                    "Set Delay Time",
                    current => current.Effect.Delay.TimeMs = requested,
                    actualVoice => NotifyClamp("Delay Time", requested, actualVoice.Effect.Delay.TimeMs));
            });

            AddInspectorFloatField(delayFoldout, "Feedback", voice.Effect.Delay.Feedback, requested =>
            {
                applyVoiceChange(
                    "Set Delay Feedback",
                    current => current.Effect.Delay.Feedback = requested,
                    actualVoice => NotifyClamp("Delay Feedback", requested, actualVoice.Effect.Delay.Feedback));
            });

            AddInspectorFloatField(delayFoldout, "Mix", voice.Effect.Delay.Mix, requested =>
            {
                applyVoiceChange(
                    "Set Delay Mix",
                    current => current.Effect.Delay.Mix = requested,
                    actualVoice => NotifyClamp("Delay Mix", requested, actualVoice.Effect.Delay.Mix));
            });
        }

        private void CreateNewProject()
        {
            if (!ConfirmDiscardIfDirty())
            {
                return;
            }

            BindProject(CreateConfiguredProject(), true, string.Empty, Array.Empty<string>());
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

        private static IEnumerable<string> GetSupportedChannelModeOptions()
        {
            yield return GameAudioChannelMode.Mono.ToString();
            yield return GameAudioChannelMode.Stereo.ToString();
        }

        private static IEnumerable<string> GetSupportedWaveformOptions()
        {
            return Enum.GetNames(typeof(GameAudioWaveformType));
        }

        private static IEnumerable<string> GetSupportedNoiseTypeOptions()
        {
            return Enum.GetNames(typeof(GameAudioNoiseType));
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

        private static int ParseSampleRateOption(string option)
        {
            string digits = new string((option ?? string.Empty).Where(char.IsDigit).ToArray());
            return int.TryParse(digits, NumberStyles.Integer, CultureInfo.InvariantCulture, out int parsed)
                ? parsed
                : GameAudioToolInfo.DefaultSampleRate;
        }

        private static GameAudioChannelMode ParseChannelMode(string option)
        {
            return string.Equals(option, GameAudioChannelMode.Mono.ToString(), StringComparison.Ordinal)
                ? GameAudioChannelMode.Mono
                : GameAudioChannelMode.Stereo;
        }

        private static GameAudioWaveformType ParseWaveform(string option)
        {
            return Enum.TryParse(option, true, out GameAudioWaveformType parsed)
                ? parsed
                : GameAudioWaveformType.Square;
        }

        private static GameAudioNoiseType ParseNoiseType(string option)
        {
            return Enum.TryParse(option, true, out GameAudioNoiseType parsed)
                ? parsed
                : GameAudioNoiseType.White;
        }

        private static bool IsFinite(float value)
        {
            return !float.IsNaN(value) && !float.IsInfinity(value);
        }

        private void NotifyClamp(string fieldName, int requestedValue, int actualValue)
        {
            if (requestedValue != actualValue)
            {
                ShowNotification(new GUIContent($"{fieldName} clamped to {actualValue}."));
            }
        }

        private void NotifyClamp(string fieldName, float requestedValue, float actualValue)
        {
            if (Math.Abs(requestedValue - actualValue) > 0.0001f)
            {
                ShowNotification(new GUIContent(string.Format(CultureInfo.InvariantCulture, "{0} clamped to {1:0.###}.", fieldName, actualValue)));
            }
        }

        private void ShowInvalidNumberMessage(string fieldName)
        {
            ShowNotification(new GUIContent($"{fieldName} requires a finite number."));
        }

        private void ShowEditorException(Exception exception)
        {
            EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
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
                ShowEditorException(exception);
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
                ShowEditorException(exception);
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
                ShowEditorException(exception);
            }
        }

        private void OpenProject()
        {
            if (!ConfirmDiscardIfDirty())
            {
                return;
            }

            string selectedPath = EditorUtility.OpenFilePanel("Open Game Audio Project", string.IsNullOrWhiteSpace(_projectPath) ? UnityEngine.Application.dataPath : _projectPath, "json");
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
                "Save Game Audio Project",
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
                _isDirty = false;
                ShowNotification(new GUIContent("Project saved."));
                RefreshView();
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
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
                "Unsaved changes will be lost. Continue?",
                "Discard Changes",
                "Cancel",
                "Save First");

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

        private void OnCommonExportDirectoryChanged(ChangeEvent<string> evt)
        {
            _commonConfig ??= _commonConfigSerializer.LoadOrDefault();
            string nextValue = evt.newValue?.Trim() ?? string.Empty;
            string normalizedValue = string.IsNullOrWhiteSpace(nextValue) ? "Exports/Audio" : nextValue;
            if (string.Equals(_commonConfig.DefaultExportDirectory, normalizedValue, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                _commonConfig.DefaultExportDirectory = normalizedValue;
                SaveCommonConfig();
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception);
            }
        }

        private void OnProjectExportDirectoryChanged(ChangeEvent<string> evt)
        {
            _projectConfig ??= _projectConfigSerializer.LoadOrDefault(GameAudioConfigPaths.GetProjectConfigPath(GetProjectRootPath()));
            string nextValue = evt.newValue?.Trim() ?? string.Empty;
            if (string.Equals(_projectConfig.ExportDirectory, nextValue, StringComparison.Ordinal))
            {
                return;
            }

            try
            {
                _projectConfig.ExportDirectory = nextValue;
                SaveProjectConfig();
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception);
            }
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
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception);
            }
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
                string exportPath = _wavExportService.Export(project, exportDirectory, project.Name);
                _lastExportedPath = exportPath;

                bool shouldRefresh = (_projectConfig?.AutoRefreshAfterExport ?? true)
                    && GameAudioExportUtility.ShouldRefreshAssetDatabase(exportPath, GetProjectRootPath());
                if (shouldRefresh)
                {
                    AssetDatabase.Refresh();
                }

                ShowNotification(new GUIContent(shouldRefresh ? "WAV exported and assets refreshed." : "WAV exported."));
                RefreshView();
            }
            catch (Exception exception)
            {
                ShowEditorException(exception);
            }
        }

        private void OpenExportFolder()
        {
            try
            {
                string exportDirectory = GetResolvedExportDirectory();
                Directory.CreateDirectory(exportDirectory);
                EditorUtility.RevealInFinder(exportDirectory);
            }
            catch (Exception exception)
            {
                ShowEditorException(exception);
            }
        }

        private void RenderPreview()
        {
            try
            {
                _previewPlaybackService.Prepare(CurrentProject);
                RefreshView();
                ShowNotification(new GUIContent("Preview rendered."));
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
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
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
            }
        }

        private void StopPreview()
        {
            _previewPlaybackService.Stop();
            RefreshView();
        }

        private void PausePreview()
        {
            _previewPlaybackService.Pause();
            RefreshView();
        }

        private void RewindPreview()
        {
            _previewPlaybackService.Rewind();
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
                ShowEditorException(exception);
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
            _isDirty = true;
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
            _isDirty = true;
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
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
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
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
            }
        }

        private void CreateSampleProjects()
        {
            try
            {
                EnsureSampleProjects();
                ShowNotification(new GUIContent("Sample projects created."));
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
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
                EditorUtility.RevealInFinder(GetUserProjectFolderPath());
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
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
                LoadProjectFromPath(Path.Combine(GetUserProjectFolderPath(), fileName));
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
            }
        }

        private void LoadProjectFromPath(string path)
        {
            try
            {
                GameAudioProjectLoadResult loadResult = _projectSerializer.LoadFromFile(path);
                BindProject(loadResult.Project, false, path, loadResult.Warnings);
                ShowNotification(new GUIContent("Project loaded."));
                RefreshView();
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
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
            _isDirty = isDirty;
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
            _isDirty = true;
            afterApply?.Invoke();
            PruneTimelineSelection();
            CancelTimelineInteraction();
            ResetPreviewState();
            RefreshView();

            if (showNotification)
            {
                ShowNotification(new GUIContent(command.DisplayName));
            }
        }

        private void DrawTimelineGui()
        {
            GameAudioProject project = CurrentProject;
            if (project == null)
            {
                EditorGUILayout.LabelField("No project loaded.");
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

        private static void DrawTimelineBackground(GameAudioProject project, TimelineMetrics metrics)
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
                    GUI.Label(new Rect(x + 4.0f, 4.0f, 80.0f, metrics.RulerHeight - 8.0f), $"Bar {barIndex + 1:00}", EditorStyles.miniBoldLabel);
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
                GUI.Label(infoRect, $"Notes {track.Notes.Count} / Pan {track.Pan:0.00}", EditorStyles.miniLabel);
            }
        }

        private static void DrawTimelineNotes(IEnumerable<TimelineRenderedNote> renderedNotes)
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

                GUI.Label(new Rect(renderedNote.Rect.x + 4.0f, renderedNote.Rect.y + 2.0f, renderedNote.Rect.width - 8.0f, renderedNote.Rect.height - 4.0f), $"MIDI {renderedNote.MidiNote}", EditorStyles.whiteMiniLabel);
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
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
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

            CopySampleIfMissing("BasicSE/basic-se.gats.json", Path.Combine(targetDirectory, "BasicSE.gats.json"));
            CopySampleIfMissing("SimpleLoop/simple-loop.gats.json", Path.Combine(targetDirectory, "SimpleLoop.gats.json"));
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
            _pathValue.text = string.IsNullOrWhiteSpace(_projectPath) ? "(unsaved)" : _projectPath;
            _statusValue.text = _isDirty ? "Unsaved changes" : "Saved";
            _toolbarBpmField?.SetValueWithoutNotify(project.Bpm);
            _toolbarGridField?.SetValueWithoutNotify(GameAudioTimelineGridUtility.NormalizeDivision(_currentGridDivision));
            _toolbarLoopToggle?.SetValueWithoutNotify(project.LoopPlayback);
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
            _exportLastResultValue.text = string.IsNullOrWhiteSpace(_lastExportedPath) ? "(not exported)" : _lastExportedPath;

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
                _gridButton.text = $"Grid {_currentGridDivision}";
            }

            if (_timelineHintValue != null)
            {
                _timelineHintValue.text = $"Grid {_currentGridDivision} | Selected {_selectedNoteIds.Count} note(s) | Drag empty lane to create | Drag note to move | Drag edge to resize | Ctrl+D duplicate | Delete remove | Ctrl+Z / Ctrl+Y undo redo";
            }

            GameAudioPreviewState previewState = _previewPlaybackService.State;
            GameAudioPreviewCursorState cursorState = GameAudioPreviewCursorCalculator.Calculate(project, previewState);
            _previewStateValue.text = previewState.StatusText;
            _previewBufferValue.text = BuildPreviewBufferText(previewState);
            _previewCursorValue.text = previewState.IsPreviewReady
                ? BuildPreviewCursorText(cursorState)
                : "(not rendered)";
            _previewProgressBar.value = previewState.IsPreviewReady
                ? cursorState.MusicalProgress * 100.0f
                : 0.0f;
            _previewProgressBar.title = previewState.IsPreviewReady
                ? cursorState.IsInTail
                    ? $"Playback tail +{cursorState.TailSeconds:0.00}s"
                    : $"Bar {cursorState.CurrentBar:00} / Beat {cursorState.BeatInBar:0.00}"
                : "Cursor not started";

            if (!string.IsNullOrWhiteSpace(previewState.ErrorText))
            {
                _previewHelpBox.text = previewState.ErrorText;
                _previewHelpBox.style.display = DisplayStyle.Flex;
            }
            else if (previewState.IsPreviewReady && !GameAudioEditorAudioUtility.IsAvailable)
            {
                _previewHelpBox.text = "Render is available, but UnityEditor preview playback API was not found in this editor build.";
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
            Repaint();
        }

        private void OnRootKeyDown(KeyDownEvent evt)
        {
            bool actionKey = evt.ctrlKey || evt.commandKey;

            if (actionKey && evt.keyCode == KeyCode.Z)
            {
                UndoLastEdit();
                evt.StopPropagation();
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.Y)
            {
                RedoLastEdit();
                evt.StopPropagation();
                return;
            }

            if (actionKey && evt.keyCode == KeyCode.D)
            {
                if (_selectedNoteIds.Count > 0)
                {
                    DuplicateSelectedNotes();
                    evt.StopPropagation();
                }
                return;
            }

            if (evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace)
            {
                if (_selectedNoteIds.Count > 0)
                {
                    DeleteSelectedNotes();
                    evt.StopPropagation();
                }
                return;
            }

            if (evt.keyCode == KeyCode.Home)
            {
                RewindPreview();
                evt.StopPropagation();
            }
        }

        private static string BuildPreviewBufferText(GameAudioPreviewState previewState)
        {
            if (!previewState.IsPreviewReady || previewState.RenderResult == null)
            {
                return "(not rendered)";
            }

            string channelLabel = previewState.ChannelCount == 1 ? "Mono" : "Stereo";
            return $"{previewState.SampleRate} Hz / {channelLabel} / project {previewState.ProjectDurationSeconds:0.00}s / output {previewState.OutputDurationSeconds:0.00}s / peak {previewState.PeakAmplitude:0.000}";
        }

        private static string BuildPreviewCursorText(GameAudioPreviewCursorState cursorState)
        {
            string cursorText = $"Bar {cursorState.CurrentBar:00} / Beat {cursorState.BeatInBar:0.00} ({cursorState.MusicalSeconds:0.00}s)";
            if (!cursorState.IsInTail)
            {
                return cursorText;
            }

            return $"{cursorText} / tail +{cursorState.TailSeconds:0.00}s";
        }

        private void OnDisable()
        {
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
            Preview,
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
                BeatsPerBar = beatsPerBar;
                TotalBars = Math.Max(1, project?.TotalBars ?? 1);
                TotalBeats = TotalBars * beatsPerBar;
                HeaderWidth = TimelineHeaderWidth;
                RulerHeight = TimelineRulerHeight;
                RowHeight = TimelineRowHeight;
                NoteHeight = TimelineNoteHeight;
                PixelsPerBeat = TimelinePixelsPerBeat;
                TimelineWidth = Math.Max(640.0f, TotalBeats * PixelsPerBeat);
                ContentWidth = HeaderWidth + TimelineWidth + 24.0f;
                ContentHeight = RulerHeight + Math.Max(1, project?.Tracks?.Count ?? 1) * RowHeight + 8.0f;
            }

            public int BeatsPerBar { get; }

            public int TotalBars { get; }

            public int TotalBeats { get; }

            public float HeaderWidth { get; }

            public float RulerHeight { get; }

            public float RowHeight { get; }

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
