using System;

namespace TorusEdison.Editor.Audio
{
    public sealed class GameAudioExportQualityReport
    {
        public const float SilentPeakThreshold = 0.0001f;
        public const float VeryLowPeakThreshold = 0.05f;
        public const float ClippingRiskThreshold = 0.999f;

        public GameAudioExportQualityReport(
            float sourcePeakAmplitude,
            float outputPeakAmplitude,
            int sampleRate,
            int channelCount,
            int frameCount,
            int projectFrameCount,
            bool normalizeEnabled,
            bool normalizeApplied,
            float normalizeGainDb)
        {
            SourcePeakAmplitude = Math.Max(0.0f, sourcePeakAmplitude);
            OutputPeakAmplitude = Math.Max(0.0f, outputPeakAmplitude);
            SampleRate = sampleRate;
            ChannelCount = channelCount;
            FrameCount = Math.Max(0, frameCount);
            ProjectFrameCount = Math.Max(0, projectFrameCount);
            NormalizeEnabled = normalizeEnabled;
            NormalizeApplied = normalizeApplied;
            NormalizeGainDb = normalizeGainDb;
        }

        public float SourcePeakAmplitude { get; }

        public float OutputPeakAmplitude { get; }

        public int SampleRate { get; }

        public int ChannelCount { get; }

        public int FrameCount { get; }

        public int ProjectFrameCount { get; }

        public bool NormalizeEnabled { get; }

        public bool NormalizeApplied { get; }

        public float NormalizeGainDb { get; }

        public double OutputDurationSeconds => SampleRate <= 0 ? 0.0d : FrameCount / (double)SampleRate;

        public double ProjectDurationSeconds => SampleRate <= 0 ? 0.0d : ProjectFrameCount / (double)SampleRate;

        public double TailDurationSeconds => Math.Max(0.0d, OutputDurationSeconds - ProjectDurationSeconds);

        public bool IsSilent => SourcePeakAmplitude <= SilentPeakThreshold;

        public bool IsVeryLowPeak => !IsSilent && SourcePeakAmplitude < VeryLowPeakThreshold;

        public bool SourceExceededFullScale => SourcePeakAmplitude > 1.0f;

        public bool HasClippingRisk => !IsSilent
            && (OutputPeakAmplitude >= ClippingRiskThreshold
                || (!NormalizeApplied && SourceExceededFullScale));
    }
}
