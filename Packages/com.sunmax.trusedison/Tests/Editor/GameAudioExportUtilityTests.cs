using System.IO;
using System.Text;
using NUnit.Framework;
using TorusEdison.Editor.Audio;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Config;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioExportUtilityTests
    {
        [Test]
        public void ResolveExportDirectory_UsesRelativeProjectOverrideWithinProjectRoot()
        {
            var commonConfig = new GameAudioCommonConfig
            {
                DefaultExportDirectory = "Exports/Common"
            };

            var projectConfig = new GameAudioProjectConfig
            {
                ExportDirectory = "Exports/Project"
            };

            string resolved = GameAudioConfigResolver.ResolveExportDirectory(commonConfig, projectConfig, "D:/ProjectRoot");

            Assert.That(resolved.Replace('\\', '/'), Is.EqualTo("D:/ProjectRoot/Exports/Project"));
        }

        [Test]
        public void BuildWaveFilePath_SanitizesNameAndAppendsExtension()
        {
            string path = GameAudioExportUtility.BuildWaveFilePath("D:/Exports", "Boss:SE?");

            Assert.That(path.Replace('\\', '/'), Is.EqualTo("D:/Exports/Boss_SE_.wav"));
        }

        [Test]
        public void NormalizeStoredExportDirectory_ConvertsProjectSubfolderToRelativePath()
        {
            string stored = GameAudioExportUtility.NormalizeStoredExportDirectory("D:/ProjectRoot/Assets/Exports/Audio", "D:/ProjectRoot");

            Assert.That(stored, Is.EqualTo("Assets/Exports/Audio"));
        }

        [Test]
        public void NormalizeStoredExportDirectory_KeepsExternalFolderAbsolute()
        {
            string stored = GameAudioExportUtility.NormalizeStoredExportDirectory("D:/External/Audio", "D:/ProjectRoot");

            Assert.That(stored.Replace('\\', '/'), Is.EqualTo("D:/External/Audio"));
        }

        [Test]
        public void NormalizeStoredExportDirectory_UsesDotForProjectRoot()
        {
            string stored = GameAudioExportUtility.NormalizeStoredExportDirectory("D:/ProjectRoot", "D:/ProjectRoot");

            Assert.That(stored, Is.EqualTo("."));
        }

        [Test]
        public void ShouldRefreshAssetDatabase_ReturnsTrueOnlyForAssetsSubpaths()
        {
            Assert.That(GameAudioExportUtility.ShouldRefreshAssetDatabase("D:/Project/Assets/Exports/test.wav", "D:/Project"), Is.True);
            Assert.That(GameAudioExportUtility.ShouldRefreshAssetDatabase("D:/Project/Exports/test.wav", "D:/Project"), Is.False);
        }

        [Test]
        public void EncodePcm16_WritesExpectedWaveHeader()
        {
            byte[] wavBytes = GameAudioWavEncoder.EncodePcm16(new[] { -1.0f, 0.0f, 1.0f, 0.5f }, 48000, 2);

            using var stream = new MemoryStream(wavBytes);
            using var reader = new BinaryReader(stream, Encoding.ASCII);

            string riff = new string(reader.ReadChars(4));
            int riffChunkSize = reader.ReadInt32();
            string wave = new string(reader.ReadChars(4));
            string fmt = new string(reader.ReadChars(4));
            int fmtChunkSize = reader.ReadInt32();
            short audioFormat = reader.ReadInt16();
            short channelCount = reader.ReadInt16();
            int sampleRate = reader.ReadInt32();
            int byteRate = reader.ReadInt32();
            short blockAlign = reader.ReadInt16();
            short bitsPerSample = reader.ReadInt16();
            string data = new string(reader.ReadChars(4));
            int dataLength = reader.ReadInt32();

            Assert.That(riff, Is.EqualTo("RIFF"));
            Assert.That(riffChunkSize, Is.EqualTo(wavBytes.Length - 8));
            Assert.That(wave, Is.EqualTo("WAVE"));
            Assert.That(fmt, Is.EqualTo("fmt "));
            Assert.That(fmtChunkSize, Is.EqualTo(16));
            Assert.That(audioFormat, Is.EqualTo(1));
            Assert.That(channelCount, Is.EqualTo(2));
            Assert.That(sampleRate, Is.EqualTo(48000));
            Assert.That(byteRate, Is.EqualTo(192000));
            Assert.That(blockAlign, Is.EqualTo(4));
            Assert.That(bitsPerSample, Is.EqualTo(16));
            Assert.That(data, Is.EqualTo("data"));
            Assert.That(dataLength, Is.EqualTo(8));
        }

        [Test]
        public void Export_WritesWaveFileToDisk()
        {
            string root = Path.Combine(Path.GetTempPath(), "TorusEdisonTests", Path.GetRandomFileName());

            try
            {
                GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
                project.Tracks[0].Notes.Add(new GameAudioNote
                {
                    Id = "note-a",
                    StartBeat = 0.0f,
                    DurationBeat = 0.5f,
                    MidiNote = 72,
                    Velocity = 0.8f
                });

                var service = new GameAudioWavExportService();
                string filePath = service.Export(project, root, "Export:Test");

                Assert.That(File.Exists(filePath), Is.True);
                byte[] bytes = File.ReadAllBytes(filePath);
                Assert.That(bytes.Length, Is.GreaterThan(44));
                Assert.That(Encoding.ASCII.GetString(bytes, 0, 4), Is.EqualTo("RIFF"));
                Assert.That(Path.GetFileName(filePath), Is.EqualTo("Export_Test.wav"));
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
