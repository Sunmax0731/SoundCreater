using System;
using System.IO;
using NUnit.Framework;
using TorusEdison.Editor.Audio;
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
        public void ExportAsPcm8_WritesWaveFileToDisk()
        {
            string root = Path.Combine(Path.GetTempPath(), "TorusEdisonTests", Path.GetRandomFileName());
            AudioClip clip = null;

            try
            {
                clip = AudioClip.Create("source-clip", 4, 1, 22050, false);
                clip.SetData(new[] { -1.0f, 0.0f, 1.0f, 0.5f }, 0);

                var service = new GameAudioAudioClipConversionService();
                string filePath = service.ExportAsPcm8(
                    clip,
                    root,
                    "Converted:Clip",
                    22050,
                    GameAudioConversionChannelMode.Preserve);

                Assert.That(File.Exists(filePath), Is.True);
                Assert.That(Path.GetFileName(filePath), Is.EqualTo("Converted_Clip.wav"));

                byte[] bytes = File.ReadAllBytes(filePath);
                Assert.That(bytes.Length, Is.GreaterThan(44));
                Assert.That(BitConverter.ToInt16(bytes, 34), Is.EqualTo(8));
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
