using System;

namespace GameAudioTool.Editor.Audio
{
    public sealed class GameAudioRenderResult
    {
        public GameAudioRenderResult(float[] samples, int sampleRate, int channelCount, int frameCount, int projectFrameCount, float peakAmplitude)
        {
            Samples = samples ?? Array.Empty<float>();
            SampleRate = sampleRate;
            ChannelCount = channelCount;
            FrameCount = frameCount;
            ProjectFrameCount = projectFrameCount;
            PeakAmplitude = peakAmplitude;
        }

        public float[] Samples { get; }

        public int SampleRate { get; }

        public int ChannelCount { get; }

        public int FrameCount { get; }

        public int ProjectFrameCount { get; }

        public float PeakAmplitude { get; }

        public double DurationSeconds => SampleRate <= 0 ? 0.0 : FrameCount / (double)SampleRate;
    }
}
