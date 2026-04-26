using System.IO;
using TorusEdison.Editor.Config;
using TorusEdison.Editor.Localization;
using TorusEdison.Editor.Persistence;
using TorusEdison.Editor.Utilities;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace TorusEdison.Editor.Windows
{
    internal sealed class TorusEdisonVersionLicenseWindow : EditorWindow
    {
        private const float HeroHeight = 220.0f;
        private const string HeroImageFileName = "TorusEdisonHeroImage.png";

        private readonly GameAudioCommonConfigSerializer _commonConfigSerializer = new GameAudioCommonConfigSerializer();
        private GameAudioDisplayLanguage _displayLanguage = GameAudioDisplayLanguage.English;
        private InfoMode _mode = InfoMode.Version;

        [MenuItem("Tools/Torus Edison/ライセンス")]
        public static void OpenLicense()
        {
            OpenWindow(InfoMode.License);
        }

        [MenuItem("Tools/Torus Edison/バージョン情報")]
        public static void OpenVersionInfo()
        {
            OpenWindow(InfoMode.Version);
        }

        private static void OpenWindow(InfoMode mode)
        {
            var window = GetWindow<TorusEdisonVersionLicenseWindow>();
            window._mode = mode;
            window.titleContent = new GUIContent(mode == InfoMode.License ? "ライセンス" : "バージョン情報");
            window.minSize = new Vector2(760.0f, 620.0f);
            window.CreateGUI();
            window.Show();
            window.Focus();
        }

        private void CreateGUI()
        {
            GameAudioCommonConfig commonConfig = _commonConfigSerializer.LoadOrDefault();
            _displayLanguage = GameAudioLocalization.ResolveLanguage(commonConfig.DisplayLanguage);

            rootVisualElement.Clear();
            rootVisualElement.style.paddingLeft = 14.0f;
            rootVisualElement.style.paddingRight = 14.0f;
            rootVisualElement.style.paddingTop = 14.0f;
            rootVisualElement.style.paddingBottom = 14.0f;

            var scrollView = new ScrollView();
            scrollView.style.flexGrow = 1.0f;
            rootVisualElement.Add(scrollView);

            var hero = new IMGUIContainer(DrawHeroBanner);
            hero.style.height = HeroHeight;
            hero.style.marginBottom = 12.0f;
            scrollView.Add(hero);

            if (_mode == InfoMode.Version)
            {
                scrollView.Add(BuildSection(
                    T("about.version", "Version"),
                    new[]
                    {
                        (T("about.toolVersion", "Tool Version"), GameAudioToolInfo.ToolVersion),
                        (T("about.packageId", "Package ID"), GameAudioToolInfo.PackageIdentifier),
                        ("検証済み Unity", "6000.4.0f1"),
                        ("メニュー", "Tools > Torus Edison > メイン画面"),
                        ("ライセンス", "MIT License"),
                        (T("about.sessionFormat", "Session Format"), GameAudioToolInfo.SessionFileExtension),
                        (T("about.supportedEnv", "Supported Environment"), "Unity 6000.0+ / Windows / Offline"),
                        (T("about.capabilities", "Current Scope"), $"32 tracks / {GameAudioToolInfo.MaxTotalBars} bars / WAV export / Localized UI")
                    }));
            }
            else
            {
                scrollView.Add(BuildSection(
                    T("about.license", "License"),
                    new[]
                    {
                        ("ライセンス種別", "MIT License"),
                        ("対象パッケージ", GameAudioToolInfo.PackageIdentifier),
                        ("LICENSE", $"Packages/{GameAudioToolInfo.PackageIdentifier}/LICENSE.md")
                    }));
                scrollView.Add(BuildTextSection(
                    T("about.license", "License"),
                    "本 Unity エディタ拡張は MIT License で提供されます。利用、改変、再配布、商用利用が可能です。再配布時は、パッケージに含まれる LICENSE.md の著作権表示とライセンス本文を保持してください。"));
            }

            scrollView.Add(BuildLinksSection());
            scrollView.Add(BuildTextSection(
                T("about.support", "Support"),
                T("about.supportBody", "When contacting support, share your Unity version, Torus Edison version, the loaded .gats.json file name, reproduction steps, and any Console warnings or errors.")));
        }

        private string T(string key, string englishText)
        {
            return GameAudioLocalization.Get(_displayLanguage, key, englishText);
        }

        private static VisualElement BuildSection(string title, (string Label, string Value)[] rows)
        {
            var section = CreateSectionContainer(title);
            foreach ((string label, string value) in rows)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.marginBottom = 6.0f;

                var keyLabel = new Label(label);
                keyLabel.style.minWidth = 170.0f;
                keyLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
                row.Add(keyLabel);

                var valueLabel = new Label(value);
                valueLabel.style.whiteSpace = WhiteSpace.Normal;
                valueLabel.style.flexGrow = 1.0f;
                row.Add(valueLabel);

                section.Add(row);
            }

            return section;
        }

        private VisualElement BuildLinksSection()
        {
            var section = CreateSectionContainer(T("about.links", "Quick Links"));
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.flexWrap = Wrap.Wrap;

            row.Add(CreateLinkButton(T("about.manualJa", "Open Manual (JA)"), () => OpenExternal(GetDocumentationPath("Manual.ja.md"))));
            row.Add(CreateLinkButton(T("about.manualEn", "Open Manual (EN)"), () => OpenExternal(GetDocumentationPath("Manual.md"))));
            row.Add(CreateLinkButton(T("about.terms", "Open Terms"), () => OpenExternal(GetDocumentationPath("TermsOfUse.md"))));
            row.Add(CreateLinkButton(T("about.releaseNotes", "Open Release Notes"), () => OpenExternal(GetDocumentationPath("ReleaseNotes.md"))));
            row.Add(CreateLinkButton(T("about.licenseFile", "Open License"), () => OpenExternal(GetPackageFilePath("LICENSE.md"))));
            row.Add(CreateLinkButton(T("about.github", "Open GitHub Releases"), () => UnityEngine.Application.OpenURL($"{GameAudioToolInfo.RepositoryUrl}/releases")));

            section.Add(row);
            return section;
        }

        private VisualElement BuildTextSection(string title, string body)
        {
            var section = CreateSectionContainer(title);
            var bodyLabel = new Label(body);
            bodyLabel.style.whiteSpace = WhiteSpace.Normal;
            section.Add(bodyLabel);
            return section;
        }

        private static VisualElement CreateSectionContainer(string title)
        {
            var section = new VisualElement();
            section.style.backgroundColor = new Color(0.11f, 0.14f, 0.19f);
            section.style.paddingLeft = 12.0f;
            section.style.paddingRight = 12.0f;
            section.style.paddingTop = 12.0f;
            section.style.paddingBottom = 12.0f;
            section.style.marginBottom = 10.0f;
            section.style.borderTopLeftRadius = 8.0f;
            section.style.borderTopRightRadius = 8.0f;
            section.style.borderBottomLeftRadius = 8.0f;
            section.style.borderBottomRightRadius = 8.0f;

            var titleLabel = new Label(title);
            titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleLabel.style.fontSize = 14.0f;
            titleLabel.style.marginBottom = 8.0f;
            section.Add(titleLabel);

            return section;
        }

        private Button CreateLinkButton(string text, System.Action onClick)
        {
            var button = new Button(() => onClick?.Invoke())
            {
                text = text
            };
            button.style.minWidth = 170.0f;
            button.style.marginRight = 8.0f;
            button.style.marginBottom = 6.0f;
            return button;
        }

        private static void OpenExternal(string path)
        {
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog(GameAudioToolInfo.DisplayName, $"File not found:\n{path}", "OK");
                return;
            }

            EditorUtility.OpenWithDefaultApp(path);
        }

        private static string GetProjectRootPath()
        {
            return Path.GetDirectoryName(UnityEngine.Application.dataPath) ?? System.Environment.CurrentDirectory;
        }

        private static string GetPackageRootPath()
        {
            return Path.Combine(GetProjectRootPath(), "Packages", GameAudioToolInfo.PackageIdentifier);
        }

        private static string GetDocumentationPath(string fileName)
        {
            return Path.Combine(GetPackageRootPath(), "Documentation~", fileName);
        }

        private static string GetPackageFilePath(string fileName)
        {
            return Path.Combine(GetPackageRootPath(), fileName);
        }

        private static string GetHeroImageAssetPath()
        {
            return $"Packages/{GameAudioToolInfo.PackageIdentifier}/Editor/Resources/{HeroImageFileName}";
        }

        private static Texture2D LoadHeroImage()
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(GetHeroImageAssetPath());
        }

        private static void DrawHeroBanner()
        {
            Rect rect = GUILayoutUtility.GetRect(0.0f, HeroHeight, GUILayout.ExpandWidth(true));
            Texture2D heroImage = LoadHeroImage();
            if (heroImage != null)
            {
                DrawHeroImage(rect, heroImage);
                return;
            }

            for (int index = 0; index < 18; index++)
            {
                float t = index / 17.0f;
                Color color = Color.Lerp(new Color(0.02f, 0.08f, 0.20f), new Color(0.02f, 0.21f, 0.47f), t);
                EditorGUI.DrawRect(new Rect(rect.x, rect.y + (rect.height * t), rect.width, (rect.height / 17.0f) + 1.0f), color);
            }

            Rect glowRect = new Rect(rect.x, rect.y, rect.width, rect.height * 0.58f);
            EditorGUI.DrawRect(glowRect, new Color(0.10f, 0.50f, 0.95f, 0.14f));

            DrawWaveformSide(rect, true);
            DrawWaveformSide(rect, false);
            DrawLogo(rect);
            DrawBottomTimeline(rect);

            GUIStyle titleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 54,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleLeft,
                normal =
                {
                    textColor = Color.white
                }
            };

            GUIStyle subtitleStyle = new GUIStyle(EditorStyles.label)
            {
                fontSize = 16,
                alignment = TextAnchor.UpperLeft,
                normal =
                {
                    textColor = new Color(0.80f, 0.92f, 1.0f, 0.92f)
                }
            };

            Rect titleRect = new Rect(rect.x + 220.0f, rect.y + 42.0f, rect.width - 260.0f, 80.0f);
            Rect subtitleRect = new Rect(rect.x + 224.0f, rect.y + 120.0f, rect.width - 280.0f, 26.0f);
            GUI.Label(titleRect, GameAudioToolInfo.DisplayName, titleStyle);
            GUI.Label(subtitleRect, "License / Version Info", subtitleStyle);
        }

        private static void DrawHeroImage(Rect rect, Texture2D heroImage)
        {
            GUI.DrawTexture(rect, heroImage, ScaleMode.ScaleAndCrop);

            Rect overlayRect = new Rect(rect.x, rect.yMax - 44.0f, rect.width, 44.0f);
            EditorGUI.DrawRect(overlayRect, new Color(0.01f, 0.05f, 0.12f, 0.72f));

            GUIStyle overlayLabel = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize = 18,
                alignment = TextAnchor.MiddleRight,
                normal =
                {
                    textColor = Color.white
                }
            };

            GUI.Label(
                new Rect(rect.x + 24.0f, rect.yMax - 38.0f, rect.width - 48.0f, 28.0f),
                "License / Version Info",
                overlayLabel);
        }

        private static void DrawWaveformSide(Rect rect, bool left)
        {
            float anchorX = left ? rect.x + 56.0f : rect.xMax - 56.0f;
            for (int index = 0; index < 18; index++)
            {
                float distance = (index / 17.0f) * 70.0f;
                float amplitude = Mathf.Sin(index * 0.7f) * 18.0f;
                float barHeight = Mathf.Abs(amplitude) + 6.0f;
                float x = left ? anchorX - distance : anchorX + distance;
                Rect bar = new Rect(x, rect.center.y - (barHeight * 0.5f), 2.0f, barHeight);
                EditorGUI.DrawRect(bar, new Color(0.14f, 0.80f, 1.0f, 0.55f));
            }
        }

        private static void DrawLogo(Rect rect)
        {
            Vector2 center = new Vector2(rect.x + 128.0f, rect.y + 100.0f);
            Handles.BeginGUI();
            Handles.color = new Color(0.15f, 0.88f, 1.0f, 0.95f);
            Handles.DrawSolidDisc(center, Vector3.forward, 48.0f);
            Handles.color = new Color(0.02f, 0.17f, 0.32f, 1.0f);
            Handles.DrawSolidDisc(center, Vector3.forward, 25.0f);
            Handles.color = new Color(0.58f, 0.96f, 1.0f, 0.95f);
            Handles.DrawWireDisc(center, Vector3.forward, 48.0f);
            Handles.DrawWireDisc(center, Vector3.forward, 38.0f);
            Handles.EndGUI();

            for (int index = -3; index <= 3; index++)
            {
                float barHeight = 8.0f + (Mathf.Abs(index) * 6.0f);
                Rect bar = new Rect(center.x + (index * 6.0f) - 1.0f, center.y - (barHeight * 0.5f), 2.0f, barHeight);
                EditorGUI.DrawRect(bar, new Color(0.55f, 0.97f, 1.0f, 0.95f));
            }
        }

        private static void DrawBottomTimeline(Rect rect)
        {
            Rect panelRect = new Rect(rect.x + 24.0f, rect.yMax - 86.0f, rect.width - 48.0f, 66.0f);
            EditorGUI.DrawRect(panelRect, new Color(0.03f, 0.12f, 0.24f, 0.92f));
            EditorGUI.DrawRect(new Rect(panelRect.x, panelRect.y, panelRect.width, 1.0f), new Color(0.19f, 0.45f, 0.76f, 0.85f));

            for (int lineIndex = 1; lineIndex < 6; lineIndex++)
            {
                float y = panelRect.y + (lineIndex * 16.0f);
                EditorGUI.DrawRect(new Rect(panelRect.x, y, panelRect.width, 1.0f), new Color(0.10f, 0.23f, 0.39f, 0.85f));
            }

            for (int column = 1; column < 8; column++)
            {
                float x = panelRect.x + (column * (panelRect.width / 8.0f));
                EditorGUI.DrawRect(new Rect(x, panelRect.y, 1.0f, panelRect.height), new Color(0.09f, 0.22f, 0.38f, 0.75f));
            }

            DrawTimelineBlock(panelRect, new Rect(panelRect.x + 190.0f, panelRect.y + 9.0f, 180.0f, 22.0f), new Color(0.13f, 0.82f, 1.0f, 0.85f));
            DrawTimelineBlock(panelRect, new Rect(panelRect.x + 420.0f, panelRect.y + 9.0f, 150.0f, 22.0f), new Color(0.13f, 0.82f, 1.0f, 0.85f));
            DrawTimelineBlock(panelRect, new Rect(panelRect.x + 250.0f, panelRect.y + 39.0f, 260.0f, 20.0f), new Color(0.55f, 0.35f, 1.0f, 0.78f));

            float cursorX = panelRect.x + 312.0f;
            EditorGUI.DrawRect(new Rect(cursorX, panelRect.y, 2.0f, panelRect.height), new Color(0.66f, 0.97f, 1.0f, 0.95f));
        }

        private static void DrawTimelineBlock(Rect panelRect, Rect blockRect, Color color)
        {
            EditorGUI.DrawRect(blockRect, color);
            EditorGUI.DrawRect(new Rect(blockRect.x, blockRect.y, blockRect.width, 1.0f), Color.white * 0.65f);

            for (int index = 0; index < 16; index++)
            {
                float normalized = index / 15.0f;
                float wave = Mathf.Abs(Mathf.Sin((index + 1) * 0.9f)) * (blockRect.height * 0.32f);
                float x = Mathf.Lerp(blockRect.x + 6.0f, blockRect.xMax - 6.0f, normalized);
                Rect line = new Rect(x, blockRect.center.y - (wave * 0.5f), 2.0f, Mathf.Max(2.0f, wave));
                EditorGUI.DrawRect(line, new Color(1.0f, 1.0f, 1.0f, 0.45f));
            }
        }

        private enum InfoMode
        {
            Version,
            License
        }
    }
}
