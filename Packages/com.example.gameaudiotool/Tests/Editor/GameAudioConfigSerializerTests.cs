using System.IO;
using NUnit.Framework;
using GameAudioTool.Editor.Config;
using GameAudioTool.Editor.Domain;
using GameAudioTool.Editor.Persistence;
using GameAudioTool.Editor.Utilities;

namespace GameAudioTool.Editor.Tests
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
                DefaultGridDivision = "1/8",
                UndoHistoryLimit = 240
            };

            string json = serializer.Serialize(config);
            GameAudioCommonConfig result = serializer.Deserialize(json);

            Assert.That(result.DefaultSampleRate, Is.EqualTo(44100));
            Assert.That(result.DefaultChannelMode, Is.EqualTo(GameAudioChannelMode.Mono));
            Assert.That(result.DefaultExportDirectory, Is.EqualTo("Custom/Exports"));
            Assert.That(result.ShowStartupGuide, Is.False);
            Assert.That(result.RememberLastProject, Is.False);
            Assert.That(result.DefaultGridDivision, Is.EqualTo("1/8"));
            Assert.That(result.UndoHistoryLimit, Is.EqualTo(240));
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
            Assert.That(GameAudioConfigResolver.ResolveExportDirectory(commonConfig, projectConfig, "D:/Project"), Is.EqualTo("D:/Audio/Out"));
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
    }
}
