using System;
using System.Collections.Generic;
using System.IO;
using GameAudioTool.Editor.Application;
using GameAudioTool.Editor.Audio;
using GameAudioTool.Editor.Config;
using GameAudioTool.Editor.Domain;
using GameAudioTool.Editor.Persistence;
using GameAudioTool.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace GameAudioTool.Editor.Windows
{
    public sealed class GameAudioToolWindow : EditorWindow
    {
        private readonly GameAudioCommonConfigSerializer _commonConfigSerializer = new GameAudioCommonConfigSerializer();
        private readonly GameAudioPreviewPlaybackService _previewPlaybackService = new GameAudioPreviewPlaybackService();
        private readonly GameAudioProjectSerializer _projectSerializer = new GameAudioProjectSerializer();
        private readonly GameAudioProjectConfigSerializer _projectConfigSerializer = new GameAudioProjectConfigSerializer();

        private GameAudioProject _project;
        private string _projectPath = string.Empty;
        private bool _isDirty;
        private List<string> _loadWarnings = new List<string>();
        private IVisualElementScheduledItem _previewTicker;

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

        [MenuItem("Tools/Torus Edison/Open Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<GameAudioToolWindow>();
            window.titleContent = new GUIContent(GameAudioToolInfo.DisplayName);
            window.minSize = new Vector2(640.0f, 420.0f);
            window.Show();
        }

        private void CreateGUI()
        {
            if (_project == null)
            {
                _project = CreateConfiguredProject();
                _isDirty = true;
                ResetPreviewState();
            }

            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.paddingLeft = 12;
            rootVisualElement.style.paddingRight = 12;
            rootVisualElement.style.paddingTop = 12;
            rootVisualElement.style.paddingBottom = 12;

            rootVisualElement.Add(BuildToolbar());
            rootVisualElement.Add(BuildSummaryPanel());
            rootVisualElement.Add(BuildSamplePanel());
            rootVisualElement.Add(BuildPreviewPanel());
            rootVisualElement.Add(BuildInfoPanel());

            _previewTicker?.Pause();
            _previewTicker = rootVisualElement.schedule.Execute(HandlePreviewTick).Every(50);

            RefreshView();
        }

        private VisualElement BuildToolbar()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
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

        private VisualElement BuildPreviewPanel()
        {
            var panel = CreateSectionPanel(new Color(0.13f, 0.13f, 0.13f));

            panel.Add(CreateSectionTitle("Preview Playback"));

            var transportRow = new VisualElement();
            transportRow.style.flexDirection = FlexDirection.Row;
            transportRow.style.alignItems = Align.Center;
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

            panel.Add(CreateSectionTitle("Samples And Editing"));

            var sampleRow = new VisualElement();
            sampleRow.style.flexDirection = FlexDirection.Row;
            sampleRow.style.marginBottom = 8;

            sampleRow.Add(CreateToolbarButton("Create Samples", CreateSampleProjects));
            sampleRow.Add(CreateToolbarButton("Load Basic SE", LoadBasicSampleProject));
            sampleRow.Add(CreateToolbarButton("Load Simple Loop", LoadSimpleLoopSampleProject));
            sampleRow.Add(CreateToolbarButton("Open Folder", OpenSampleProjectsFolder));

            panel.Add(sampleRow);

            var sampleLocationLabel = new Label($"Sample files are stored under {GetUserProjectFolderPath()}");
            sampleLocationLabel.style.marginBottom = 4;
            panel.Add(sampleLocationLabel);

            var editingLabel = new Label("Current editing method: timeline and inspector editing UI are not implemented yet. For now, open a .gats.json file and edit the JSON in your text editor, then reopen it in Torus Edison.");
            editingLabel.style.whiteSpace = WhiteSpace.Normal;
            editingLabel.style.marginBottom = 4;
            panel.Add(editingLabel);

            var fieldsLabel = new Label("Useful fields to edit first: project.name, project.bpm, project.totalBars, project.loopPlayback, track volume/pan, note startBeat/durationBeat/midiNote/velocity.");
            fieldsLabel.style.whiteSpace = WhiteSpace.Normal;
            panel.Add(fieldsLabel);

            return panel;
        }

        private VisualElement BuildInfoPanel()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Column;

            panel.Add(CreateSectionTitle("Foundation Status"));
            var currentScopeLabel = new Label("This window now wires up project creation, JSON save/load, offline preview rendering, and editor playback.");
            currentScopeLabel.style.marginBottom = 4;
            panel.Add(currentScopeLabel);

            var nextScopeLabel = new Label("Timeline editing, WAV export, and Undo/Redo are the next layers to connect.");
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

            _project = CreateConfiguredProject();
            _projectPath = string.Empty;
            _isDirty = true;
            _loadWarnings.Clear();
            ResetPreviewState();
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
            string projectName = _project == null ? "New Audio Project" : _project.Name;
            string defaultFileName = $"{GameAudioValidationUtility.SanitizeExportFileName(projectName)}{GameAudioToolInfo.SessionFileExtension}";
            string selectedPath = EditorUtility.SaveFilePanel(
                "Save Game Audio Project",
                string.IsNullOrWhiteSpace(_projectPath) ? UnityEngine.Application.dataPath : System.IO.Path.GetDirectoryName(_projectPath),
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
                _projectSerializer.SaveToFile(resolvedPath, _project);
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
            return GameAudioProjectFactory.CreateDefaultProject(sampleRate, channelMode);
        }

        private void RenderPreview()
        {
            try
            {
                _previewPlaybackService.Prepare(_project);
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
                _previewPlaybackService.Play(_project, _project != null && _project.LoopPlayback);
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
            if (_project == null || _project.LoopPlayback == evt.newValue)
            {
                return;
            }

            _project.LoopPlayback = evt.newValue;
            _isDirty = true;
            _previewPlaybackService.SetLoopPlayback(evt.newValue);
            RefreshView();
        }

        private void HandlePreviewTick()
        {
            if (_previewPlaybackService.Update())
            {
                RefreshView();
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
                _project = loadResult.Project;
                _projectPath = path;
                _isDirty = false;
                _loadWarnings = new List<string>(loadResult.Warnings);
                ResetPreviewState();
                ShowNotification(new GUIContent("Project loaded."));
                RefreshView();
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
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
            return Path.Combine(GetProjectRootPath(), "Packages", "com.example.gameaudiotool");
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
            if (_project != null)
            {
                _previewPlaybackService.SetLoopPlayback(_project.LoopPlayback);
            }
        }

        private void RefreshView()
        {
            titleContent = new GUIContent(_isDirty ? $"{GameAudioToolInfo.DisplayName}*" : GameAudioToolInfo.DisplayName);

            if (_project == null)
            {
                return;
            }

            _nameValue.text = _project.Name;
            _bpmValue.text = _project.Bpm.ToString();
            _barsValue.text = _project.TotalBars.ToString();
            _tracksValue.text = _project.Tracks.Count.ToString();
            _pathValue.text = string.IsNullOrWhiteSpace(_projectPath) ? "(unsaved)" : _projectPath;
            _statusValue.text = _isDirty ? "Unsaved changes" : "Saved";
            _loopToggle.SetValueWithoutNotify(_project.LoopPlayback);

            GameAudioPreviewState previewState = _previewPlaybackService.State;
            GameAudioPreviewCursorState cursorState = GameAudioPreviewCursorCalculator.Calculate(_project, previewState);
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
    }
}
