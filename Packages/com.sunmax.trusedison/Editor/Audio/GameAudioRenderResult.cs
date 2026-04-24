using System;
using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Audio
{
    public sealed class GameAudioRenderResult
    {
        public GameAudioRenderResult(float[] samples, int sampleRate, int channelCount, int frameCount, int projectFrameCount, float peakAmplitude)
            : this(samples, sampleRate, channelCount, frameCount, projectFrameCount, projectFrameCount, GameAudioExportDurationMode.ProjectBars, true, peakAmplitude)
        {
        }

        public GameAudioRenderResult(
            float[] samples,
            int sampleRate,
            int channelCount,
            int frameCount,
            int projectFrameCount,
            int targetFrameCount,
            GameAudioExportDurationMode durationMode,
            bool includeTail,
            float peakAmplitude)
        {
            Samples = samples ?? Array.Empty<float>();
            SampleRate = sampleRate;
            ChannelCount = channelCount;
            FrameCount = frameCount;
            ProjectFrameCount = projectFrameCount;
            TargetFrameCount = targetFrameCount;
            DurationMode = durationMode;
            IncludeTail = includeTail;
            PeakAmplitude = peakAmplitude;
        }

        public float[] Samples { get; }

        public int SampleRate { get; }

        public int ChannelCount { get; }

        public int FrameCount { get; }

        public int ProjectFrameCount { get; }

        public int TargetFrameCount { get; }

        public GameAudioExportDurationMode DurationMode { get; }

        public bool IncludeTail { get; }

        public float PeakAmplitude { get; }

        public double DurationSeconds => SampleRate <= 0 ? 0.0 : FrameCount / (double)SampleRate;

        public double TargetDurationSeconds => SampleRate <= 0 ? 0.0 : TargetFrameCount / (double)SampleRate;
    }
}
