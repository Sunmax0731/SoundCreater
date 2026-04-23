using System;
using System.IO;
using NUnit.Framework;
using TorusEdison.Editor.Audio;
using TorusEdison.Editor.Persistence;
using UnityEngine;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioAudioClipConversionServiceTests
    {
        [Test]
        public void ConvertSamples_DownmixesStereoToMono()
        {
            float[] converted = GameAudioAudioClipConversionService.ConvertSamples(
                new[]
                {
                    1.0f, 1.0f,
                    -1.0f, -1.0f
                },
                2,
                2,
                22050,
                22050,
                GameAudioConversionChannelMode.Mono,
                out int outputChannels);

            Assert.That(outputChannels, Is.EqualTo(1));
            Assert.That(converted, Is.EqualTo(new[] { 1.0f, -1.0f }).Within(0.0001f));
        }

        [Test]
        public void ConvertSamples_DuplicatesMonoToStereo()
        {
            float[] converted = GameAudioAudioClipConversionService.ConvertSamples(
                new[]
                {
                    0.25f,
                    -0.75f
                },
                2,
                1,
                22050,
                22050,
                GameAudioConversionChannelMode.Stereo,
                out int outputChannels);

            Assert.That(outputChannels, Is.EqualTo(2));
            Assert.That(converted, Is.EqualTo(new[] { 0.25f, 0.25f, -0.75f, -0.75f }).Within(0.0001f));
        }

        [Test]
        public void ExportAsPcm8_WritesWaveAndGeneratedProjectFilesToDisk()
        {
            string root = Path.Combine(Path.GetTempPath(), "TorusEdisonTests", Path.GetRandomFileName());
            AudioClip clip = null;

            try
            {
                clip = AudioClip.Create("source-clip", 4, 1, 22050, false);
                clip.SetData(new[] { -1.0f, 0.0f, 1.0f, 0.5f }, 0);

                var service = new GameAudioAudioClipConversionService();
                GameAudioAudioClipConversionExportResult exportResult = service.ExportAsPcm8(
                    clip,
                    root,
                    "Converted:Clip",
                    22050,
                    GameAudioConversionChannelMode.Preserve);

                Assert.That(File.Exists(exportResult.WaveFilePath), Is.True);
                Assert.That(Path.GetFileName(exportResult.WaveFilePath), Is.EqualTo("Converted_Clip.wav"));
                Assert.That(File.Exists(exportResult.ProjectFilePath), Is.True);
                Assert.That(Path.GetFileName(exportResult.ProjectFilePath), Is.EqualTo("Converted_Clip.gats.json"));

                byte[] bytes = File.ReadAllBytes(exportResult.WaveFilePath);
                Assert.That(bytes.Length, Is.GreaterThan(44));
                Assert.That(BitConverter.ToInt16(bytes, 34), Is.EqualTo(8));

                var serializer = new GameAudioProjectSerializer();
                GameAudioProjectLoadResult projectResult = serializer.LoadFromFile(exportResult.ProjectFilePath);
                Assert.That(projectResult.Project.Name, Is.EqualTo("Converted:Clip"));
                Assert.That(projectResult.Project.SampleRate, Is.EqualTo(22050));
                Assert.That(projectResult.Project.ImportedAudioConversion, Is.Not.Null);
                Assert.That(projectResult.Project.ImportedAudioConversion.SourceClipName, Is.EqualTo("source-clip"));
                Assert.That(projectResult.Project.ImportedAudioConversion.TargetChannelMode, Is.EqualTo("Preserve"));
                Assert.That(projectResult.Project.ImportedAudioConversion.OutputWaveFileName, Is.EqualTo("Converted_Clip.wav"));
            }
            finally
            {
                if (clip != null)
                {
                    UnityEngine.Object.DestroyImmediate(clip);
                }

                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }
    }
}
