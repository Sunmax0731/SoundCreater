using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
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
        private readonly HashSet<string> _selectedNoteIds = new HashSet<string>(StringComparer.Ordinal);

        private GameAudioEditorSession _editorSession;
        private GameAudioProject _project;
        private string _projectPath = string.Empty;
        private bool _isDirty;
        private List<string> _loadWarnings = new List<string>();
        private IVisualElementScheduledItem _previewTicker;
        private TimelineDragState _timelineDragState;
        private Vector2 _timelineScrollPosition;
        private string _selectedTrackId = string.Empty;
        private string _currentGridDivision = "1/16";

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
        private Toggle _loopToggle;
        private HelpBox _previewHelpBox;
        private Button _undoButton;
        private Button _redoButton;
        private Button _gridButton;
        private Label _timelineHintValue;
        private IMGUIContainer _timelineSurface;

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
            GameAudioCommonConfig commonConfig = _commonConfigSerializer.LoadOrDefault();
            _currentGridDivision = GameAudioTimelineGridUtility.NormalizeDivision(commonConfig.DefaultGridDivision);

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

            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1.0f;
            rootVisualElement.Add(scrollView);

            scrollView.Add(BuildToolbar());
            scrollView.Add(BuildSummaryPanel());
            scrollView.Add(BuildTimelinePanel());
            scrollView.Add(BuildSamplePanel());
            scrollView.Add(BuildPreviewPanel());
            scrollView.Add(BuildInfoPanel());

            rootVisualElement.RegisterCallback<KeyDownEvent>(OnRootKeyDown);

            _previewTicker?.Pause();
            _previewTicker = rootVisualElement.schedule.Execute(HandlePreviewTick).Every(50);

            RefreshView();
        }

        private VisualElement BuildToolbar()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.flexWrap = Wrap.Wrap;
            container.style.marginBottom = 12;

            container.Add(CreateToolbarButton("New", CreateNewProject));
            container.Add(CreateToolbarButton("Open", OpenProject));
            container.Add(CreateToolbarButton("Save", SaveProject));
            container.Add(CreateToolbarButton("Save As", SaveProjectAs));

            return container;
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

            var editingLabel = new Label("Timeline note editing is now available. Drag on an empty lane to create a note, drag a note to move it, and drag the left or right edge to resize it. Exact pitch and voice parameter editing still falls back to JSON until the Inspector UI is implemented.");
            editingLabel.style.whiteSpace = WhiteSpace.Normal;
            editingLabel.style.marginBottom = 4;
            panel.Add(editingLabel);

            var fieldsLabel = new Label("Useful fields to edit in JSON for now: note MIDI / velocity / voiceOverride, track defaultVoice / pan / volume, project BPM / Total Bars / sampleRate / channelMode.");
            fieldsLabel.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(fieldsLabel);

            return panel;
        }

        private VisualElement BuildInfoPanel()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Column;

            panel.Add(CreateSectionTitle("Foundation Status"));
            var currentScopeLabel = new Label("This window now wires up project creation, timeline note editing, Undo / Redo, JSON save/load, offline preview rendering, and editor playback.");
            currentScopeLabel.style.marginBottom = 4;
            panel.Add(currentScopeLabel);

            var nextScopeLabel = new Label("Inspector editing, WAV export, and release packaging are the next layers to connect.");
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

        private void CreateNewProject()
        {
            if (!ConfirmDiscardIfDirty())
            {
                return;
            }

            BindProject(CreateConfiguredProject(), true, string.Empty, Array.Empty<string>());
            RefreshView();
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
            GameAudioCommonConfig commonConfig = _commonConfigSerializer.LoadOrDefault();
            GameAudioProjectConfig projectConfig = _projectConfigSerializer.LoadOrDefault();
            int sampleRate = GameAudioConfigResolver.ResolveSampleRate(commonConfig, projectConfig);
            GameAudioChannelMode channelMode = GameAudioConfigResolver.ResolveChannelMode(commonConfig, projectConfig);
            _currentGridDivision = GameAudioTimelineGridUtility.NormalizeDivision(commonConfig.DefaultGridDivision);
            return GameAudioProjectFactory.CreateDefaultProject(sampleRate, channelMode);
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
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
            }
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
            _currentGridDivision = divisions[nextIndex];
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
            _editorSession = new GameAudioEditorSession(_project, _commonConfigSerializer.LoadOrDefault().UndoHistoryLimit);
            _selectedNoteIds.Clear();
            _selectedTrackId = _project.Tracks.FirstOrDefault()?.Id ?? string.Empty;
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
            _loopToggle.SetValueWithoutNotify(project.LoopPlayback);

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
