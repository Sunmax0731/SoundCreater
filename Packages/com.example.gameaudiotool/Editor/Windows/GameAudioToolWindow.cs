using System;
using System.Collections.Generic;
using GameAudioTool.Editor.Application;
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
        private readonly GameAudioProjectSerializer _projectSerializer = new GameAudioProjectSerializer();
        private readonly GameAudioProjectConfigSerializer _projectConfigSerializer = new GameAudioProjectConfigSerializer();

        private GameAudioProject _project;
        private string _projectPath = string.Empty;
        private bool _isDirty;
        private List<string> _loadWarnings = new List<string>();

        private Label _nameValue;
        private Label _bpmValue;
        private Label _barsValue;
        private Label _tracksValue;
        private Label _pathValue;
        private Label _statusValue;
        private HelpBox _warningBox;

        [MenuItem("Tools/Torus Edison/Open Editor")]
        public static void OpenWindow()
        {
            var window = GetWindow<GameAudioToolWindow>();
            window.titleContent = new GUIContent(GameAudioToolInfo.DisplayName);
            window.minSize = new Vector2(560.0f, 340.0f);
            window.Show();
        }

        private void CreateGUI()
        {
            if (_project == null)
            {
                _project = CreateConfiguredProject();
                _isDirty = true;
            }

            rootVisualElement.Clear();
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            rootVisualElement.style.paddingLeft = 12;
            rootVisualElement.style.paddingRight = 12;
            rootVisualElement.style.paddingTop = 12;
            rootVisualElement.style.paddingBottom = 12;

            rootVisualElement.Add(BuildToolbar());
            rootVisualElement.Add(BuildSummaryPanel());
            rootVisualElement.Add(BuildInfoPanel());

            RefreshView();
        }

        private VisualElement BuildToolbar()
        {
            var container = new VisualElement();
            container.style.flexDirection = FlexDirection.Row;
            container.style.gap = 8;
            container.style.marginBottom = 12;

            container.Add(CreateToolbarButton("New", CreateNewProject));
            container.Add(CreateToolbarButton("Open", OpenProject));
            container.Add(CreateToolbarButton("Save", SaveProject));
            container.Add(CreateToolbarButton("Save As", SaveProjectAs));

            return container;
        }

        private VisualElement BuildSummaryPanel()
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
            panel.style.backgroundColor = new Color(0.16f, 0.16f, 0.16f);

            panel.Add(CreateSectionTitle("Current Project"));
            _nameValue = AddKeyValue(panel, "Name");
            _bpmValue = AddKeyValue(panel, "BPM");
            _barsValue = AddKeyValue(panel, "Bars");
            _tracksValue = AddKeyValue(panel, "Tracks");
            _pathValue = AddKeyValue(panel, "File");
            _statusValue = AddKeyValue(panel, "Status");

            return panel;
        }

        private VisualElement BuildInfoPanel()
        {
            var panel = new VisualElement();
            panel.style.flexDirection = FlexDirection.Column;
            panel.style.gap = 8;

            panel.Add(CreateSectionTitle("Foundation Status"));
            panel.Add(new Label("This initial window wires up project creation, JSON save/load, and package scaffolding."));
            panel.Add(new Label("Timeline editing, playback, WAV export, and Undo/Redo are the next layers to connect."));

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
            return button;
        }

        private Label AddKeyValue(VisualElement parent, string key)
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

        private Label CreateSectionTitle(string text)
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
            RefreshView();
        }

        private void OpenProject()
        {
            if (!ConfirmDiscardIfDirty())
            {
                return;
            }

            string selectedPath = EditorUtility.OpenFilePanel("Open Game Audio Project", string.IsNullOrWhiteSpace(_projectPath) ? Application.dataPath : _projectPath, "json");
            if (string.IsNullOrWhiteSpace(selectedPath))
            {
                return;
            }

            try
            {
                GameAudioProjectLoadResult loadResult = _projectSerializer.LoadFromFile(selectedPath);
                _project = loadResult.Project;
                _projectPath = selectedPath;
                _isDirty = false;
                _loadWarnings = new List<string>(loadResult.Warnings);
                ShowNotification(new GUIContent("Project loaded."));
                RefreshView();
            }
            catch (Exception exception)
            {
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, exception.Message, "OK");
            }
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
                string.IsNullOrWhiteSpace(_projectPath) ? Application.dataPath : System.IO.Path.GetDirectoryName(_projectPath),
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
    }
}
