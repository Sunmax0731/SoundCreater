using System.IO;
using NUnit.Framework;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Config;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Localization;
using TorusEdison.Editor.Persistence;
using TorusEdison.Editor.Utilities;
using UnityEngine;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioConfigSerializerTests
    {
        [Test]
        public void CommonConfigSerializer_RoundTrips()
        {
            var serializer = new GameAudioCommonConfigSerializer();
            var config = new GameAudioCommonConfig
            {
                DefaultSampleRate = 44100,
                DefaultChannelMode = GameAudioChannelMode.Mono,
                DefaultExportDirectory = "Custom/Exports",
                ShowStartupGuide = false,
                RememberLastProject = false,
                LastProjectPath = "D:/AudioProjects/last.gats.json",
                DefaultGridDivision = "1/8",
                UndoHistoryLimit = 240,
                DisplayLanguage = GameAudioLanguageMode.Chinese,
                EnableDiagnosticLogging = true,
                DiagnosticLogLevel = GameAudioDiagnosticLogLevel.Verbose
            };

            string json = serializer.Serialize(config);
            GameAudioCommonConfig result = serializer.Deserialize(json);

            Assert.That(result.DefaultSampleRate, Is.EqualTo(44100));
            Assert.That(result.DefaultChannelMode, Is.EqualTo(GameAudioChannelMode.Mono));
            Assert.That(result.DefaultExportDirectory, Is.EqualTo("Custom/Exports"));
            Assert.That(result.ShowStartupGuide, Is.False);
            Assert.That(result.RememberLastProject, Is.False);
            Assert.That(result.LastProjectPath, Is.EqualTo("D:/AudioProjects/last.gats.json"));
            Assert.That(result.DefaultGridDivision, Is.EqualTo("1/8"));
            Assert.That(result.UndoHistoryLimit, Is.EqualTo(240));
            Assert.That(result.DisplayLanguage, Is.EqualTo(GameAudioLanguageMode.Chinese));
            Assert.That(result.EnableDiagnosticLogging, Is.True);
            Assert.That(result.DiagnosticLogLevel, Is.EqualTo(GameAudioDiagnosticLogLevel.Verbose));
        }

        [Test]
        public void ProjectConfigSerializer_RoundTripsNullableOverrides()
        {
            var serializer = new GameAudioProjectConfigSerializer();
            var config = new GameAudioProjectConfig
            {
                ExportDirectory = "D:/Exports",
                AutoRefreshAfterExport = false,
                PreferredSampleRate = 44100,
                PreferredChannelMode = GameAudioChannelMode.Mono
            };

            string json = serializer.Serialize(config);
            GameAudioProjectConfig result = serializer.Deserialize(json);

            Assert.That(result.ExportDirectory, Is.EqualTo("D:/Exports"));
            Assert.That(result.AutoRefreshAfterExport, Is.False);
            Assert.That(result.PreferredSampleRate, Is.EqualTo(44100));
            Assert.That(result.PreferredChannelMode, Is.EqualTo(GameAudioChannelMode.Mono));
        }

        [Test]
        public void ConfigResolver_UsesProjectOverridesBeforeCommonDefaults()
        {
            var commonConfig = new GameAudioCommonConfig
            {
                DefaultSampleRate = 48000,
                DefaultChannelMode = GameAudioChannelMode.Stereo,
                DefaultExportDirectory = "Exports/Audio"
            };

            var projectConfig = new GameAudioProjectConfig
            {
                ExportDirectory = "D:/Audio/Out",
                PreferredSampleRate = 44100,
                PreferredChannelMode = GameAudioChannelMode.Mono
            };

            Assert.That(GameAudioConfigResolver.ResolveSampleRate(commonConfig, projectConfig), Is.EqualTo(44100));
            Assert.That(GameAudioConfigResolver.ResolveChannelMode(commonConfig, projectConfig), Is.EqualTo(GameAudioChannelMode.Mono));
            Assert.That(
                GameAudioConfigResolver.ResolveExportDirectory(commonConfig, projectConfig, "D:/Project").Replace('\\', '/'),
                Is.EqualTo("D:/Audio/Out"));
        }

        [Test]
        public void ProjectConfigPath_UsesProjectSettingsFolder()
        {
            string path = GameAudioConfigPaths.GetProjectConfigPath("D:/Repo");
            Assert.That(path.Replace('\\', '/'), Is.EqualTo("D:/Repo/ProjectSettings/GameAudioToolSettings.json"));
        }

        [Test]
        public void CommonConfigSerializer_SaveCreatesDirectory()
        {
            var serializer = new GameAudioCommonConfigSerializer();
            string root = Path.Combine(Path.GetTempPath(), "GameAudioToolTests", Path.GetRandomFileName());
            string configPath = Path.Combine(root, "nested", "config.json");

            try
            {
                serializer.Save(new GameAudioCommonConfig(), configPath);
                Assert.That(File.Exists(configPath), Is.True);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [Test]
        public void CommonConfigSerializer_LoadOrDefault_ReturnsDefaultsForInvalidJson()
        {
            var serializer = new GameAudioCommonConfigSerializer();
            string root = Path.Combine(Path.GetTempPath(), "GameAudioToolTests", Path.GetRandomFileName());
            string configPath = Path.Combine(root, "config.json");

            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(configPath, "{ invalid json");

                GameAudioCommonConfig result = serializer.LoadOrDefault(configPath);

                Assert.That(result.DefaultSampleRate, Is.EqualTo(48000));
                Assert.That(result.DefaultChannelMode, Is.EqualTo(GameAudioChannelMode.Stereo));
                Assert.That(result.UndoHistoryLimit, Is.EqualTo(100));
                Assert.That(result.DisplayLanguage, Is.EqualTo(GameAudioLanguageMode.Auto));
                Assert.That(result.EnableDiagnosticLogging, Is.False);
                Assert.That(result.DiagnosticLogLevel, Is.EqualTo(GameAudioDiagnosticLogLevel.Info));
                Assert.That(result.LastProjectPath, Is.EqualTo(string.Empty));
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [Test]
        public void ProjectConfigSerializer_LoadOrDefault_ReturnsDefaultsForInvalidJson()
        {
            var serializer = new GameAudioProjectConfigSerializer();
            string root = Path.Combine(Path.GetTempPath(), "GameAudioToolTests", Path.GetRandomFileName());
            string configPath = Path.Combine(root, "config.json");

            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(configPath, "{ invalid json");

                GameAudioProjectConfig result = serializer.LoadOrDefault(configPath);

                Assert.That(result.ExportDirectory, Is.EqualTo(string.Empty));
                Assert.That(result.AutoRefreshAfterExport, Is.True);
                Assert.That(result.PreferredSampleRate, Is.Null);
                Assert.That(result.PreferredChannelMode, Is.Null);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [Test]
        public void ProjectFactory_UsesResolvedConfigDefaults()
        {
            var commonConfig = new GameAudioCommonConfig
            {
                DefaultSampleRate = 44100,
                DefaultChannelMode = GameAudioChannelMode.Mono
            };

            var projectConfig = new GameAudioProjectConfig();
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject(
                GameAudioConfigResolver.ResolveSampleRate(commonConfig, projectConfig),
                GameAudioConfigResolver.ResolveChannelMode(commonConfig, projectConfig));

            Assert.That(project.SampleRate, Is.EqualTo(44100));
            Assert.That(project.ChannelMode, Is.EqualTo(GameAudioChannelMode.Mono));
        }

        [Test]
        public void ProjectFactory_UsesProjectConfigOverridesForNewProjectDefaults()
        {
            var commonConfig = new GameAudioCommonConfig
            {
                DefaultSampleRate = 48000,
                DefaultChannelMode = GameAudioChannelMode.Stereo
            };

            var projectConfig = new GameAudioProjectConfig
            {
                PreferredSampleRate = 44100,
                PreferredChannelMode = GameAudioChannelMode.Mono
            };

            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject(
                GameAudioConfigResolver.ResolveSampleRate(commonConfig, projectConfig),
                GameAudioConfigResolver.ResolveChannelMode(commonConfig, projectConfig));

            Assert.That(project.SampleRate, Is.EqualTo(44100));
            Assert.That(project.ChannelMode, Is.EqualTo(GameAudioChannelMode.Mono));
        }

        [Test]
        public void Localization_ResolveLanguage_UsesOverrideBeforeUnityLanguage()
        {
            Assert.That(GameAudioLocalization.ResolveLanguage(GameAudioLanguageMode.Japanese), Is.EqualTo(GameAudioDisplayLanguage.Japanese));
            Assert.That(GameAudioLocalization.ResolveLanguage(GameAudioLanguageMode.English), Is.EqualTo(GameAudioDisplayLanguage.English));
            Assert.That(GameAudioLocalization.ResolveLanguage(GameAudioLanguageMode.Chinese), Is.EqualTo(GameAudioDisplayLanguage.Chinese));
        }

        [Test]
        public void Localization_MapSystemLanguage_FallsBackToEnglish()
        {
            Assert.That(GameAudioLocalization.MapSystemLanguage(SystemLanguage.Japanese), Is.EqualTo(GameAudioDisplayLanguage.Japanese));
            Assert.That(GameAudioLocalization.MapSystemLanguage(SystemLanguage.ChineseTraditional), Is.EqualTo(GameAudioDisplayLanguage.Chinese));
            Assert.That(GameAudioLocalization.MapSystemLanguage(SystemLanguage.Korean), Is.EqualTo(GameAudioDisplayLanguage.English));
        }

        [Test]
        public void Localization_ReturnsLocalizedLabelsForSupportedUiEnums()
        {
            Assert.That(
                GameAudioLocalization.GetLanguageModeLabel(GameAudioDisplayLanguage.Japanese, GameAudioLanguageMode.Auto),
                Is.EqualTo("自動"));
            Assert.That(
                GameAudioLocalization.GetChannelModeLabel(GameAudioDisplayLanguage.Chinese, GameAudioChannelMode.Stereo),
                Is.EqualTo("立体声"));
            Assert.That(
                GameAudioLocalization.GetWaveformLabel(GameAudioDisplayLanguage.Japanese, GameAudioWaveformType.Pulse),
                Is.EqualTo("パルス波"));
            Assert.That(
                GameAudioLocalization.GetNoiseTypeLabel(GameAudioDisplayLanguage.Chinese, GameAudioNoiseType.White),
                Is.EqualTo("白噪声"));
        }
    }
}
