using System;
using System.Linq;
using TorusEdison.Editor.Localization;
using TorusEdison.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TorusEdison.Editor.Windows
{
    internal sealed class GameAudioTimelineHelpWindow : EditorWindow
    {
        private GameAudioDisplayLanguage _displayLanguage = GameAudioDisplayLanguage.English;
        private string _hintText = string.Empty;

        public static void Open(GameAudioDisplayLanguage displayLanguage, string hintText)
        {
            var window = CreateInstance<GameAudioTimelineHelpWindow>();
            window._displayLanguage = displayLanguage;
            window._hintText = hintText ?? string.Empty;
            window.titleContent = new GUIContent(GameAudioLocalization.Get(displayLanguage, "timeline.help.title", "Timeline Editing Help"));
            window.minSize = new Vector2(440.0f, 300.0f);
            window.maxSize = new Vector2(560.0f, 420.0f);
            window.ShowModalUtility();
        }

        private void CreateGUI()
        {
            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 16.0f;
            rootVisualElement.style.paddingRight = 16.0f;
            rootVisualElement.style.paddingTop = 16.0f;
            rootVisualElement.style.paddingBottom = 16.0f;
            rootVisualElement.style.flexDirection = FlexDirection.Column;

            var titleLabel = new Label(T("timeline.help.title", "Timeline Editing Help"));
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 16.0f;
            titleLabel.style.marginBottom = 8.0f;
            rootVisualElement.Add(titleLabel);

            var summaryLabel = new Label(T("timeline.help.summary", "Open this when you need a quick reminder of the current timeline mouse and keyboard shortcuts."));
            summaryLabel.style.whiteSpace = WhiteSpace.Normal;
            summaryLabel.style.marginBottom = 12.0f;
            rootVisualElement.Add(summaryLabel);

            var instructionsContainer = new ScrollView();
            instructionsContainer.style.flexGrow = 1.0f;
            instructionsContainer.style.marginBottom = 12.0f;
            rootVisualElement.Add(instructionsContainer);

            string[] segments = (_hintText ?? string.Empty)
                .Split(new[] { '|' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(segment => segment.Trim())
                .Where(segment => !string.IsNullOrWhiteSpace(segment))
                .ToArray();

            if (segments.Length == 0)
            {
                instructionsContainer.Add(BuildInstructionLabel(T("timeline.help.empty", "Timeline help is unavailable until the editor UI is ready."), false));
            }
            else
            {
                for (int index = 0; index < segments.Length; index++)
                {
                    instructionsContainer.Add(BuildInstructionLabel(segments[index], index > 0));
                }
            }

            var closeButton = new Button(Close)
            {
                text = T("timeline.help.close", "Close")
            };
            closeButton.style.alignSelf = Align.FlexEnd;
            closeButton.style.minWidth = 120.0f;
            rootVisualElement.Add(closeButton);
        }

        private string T(string key, string englishText)
        {
            return GameAudioLocalization.Get(_displayLanguage, key, englishText);
        }

        private static VisualElement BuildInstructionLabel(string text, bool addTopMargin)
        {
            var label = new Label($"- {text}")
            {
                style =
                {
                    whiteSpace = WhiteSpace.Normal
                }
            };

            if (addTopMargin)
            {
                label.style.marginTop = 4.0f;
            }

            return label;
        }
    }
}
