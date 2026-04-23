using NUnit.Framework;
using TorusEdison.Editor.Audio;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioPreviewWaveformBuilderTests
    {
        [Test]
        public void Build_ReturnsEmptySilentData_WhenRenderResultIsMissing()
        {
            GameAudioPreviewWaveformData waveform = GameAudioPreviewWaveformBuilder.Build(null, 64);

            Assert.That(waveform.IsSilent, Is.True);
            Assert.That(waveform.Bins, Is.Empty);
        }

        [Test]
        public void Build_CapturesMinAndMaxAcrossBins()
        {
            var renderResult = new GameAudioRenderResult(
                new[]
                {
                    -1.0f, 0.4f,
                    -0.5f, 0.1f,
                    0.25f, -0.2f,
                    0.75f, 0.6f
                },
                48000,
                2,
                4,
                4,
                1.0f);

            GameAudioPreviewWaveformData waveform = GameAudioPreviewWaveformBuilder.Build(renderResult, 2);

            Assert.That(waveform.IsSilent, Is.False);
            Assert.That(waveform.Bins.Length, Is.EqualTo(2));
            Assert.That(waveform.Bins[0].Min, Is.EqualTo(-1.0f).Within(0.0001f));
            Assert.That(waveform.Bins[0].Max, Is.EqualTo(0.4f).Within(0.0001f));
            Assert.That(waveform.Bins[1].Min, Is.EqualTo(-0.2f).Within(0.0001f));
            Assert.That(waveform.Bins[1].Max, Is.EqualTo(0.75f).Within(0.0001f));
        }

        [Test]
        public void Build_MarksSilentWaveform_WhenPeakAmplitudeIsNearZero()
        {
            var renderResult = new GameAudioRenderResult(
                new float[8],
                48000,
                2,
                4,
                4,
                0.0f);

            GameAudioPreviewWaveformData waveform = GameAudioPreviewWaveformBuilder.Build(renderResult, 4);

            Assert.That(waveform.IsSilent, Is.True);
            Assert.That(waveform.Bins.Length, Is.EqualTo(4));
        }
    }
}
