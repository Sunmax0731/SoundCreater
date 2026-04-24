using System;
using System.IO;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Audio
{
    internal sealed class GameAudioWavExportService
    {
        private const float MinimumNormalizeHeadroomDb = -12.0f;
        private const float MaximumNormalizeHeadroomDb = 0.0f;

        private readonly GameAudioProjectRenderer _renderer = new GameAudioProjectRenderer();

        public string Export(GameAudioProject project, string exportDirectory, string projectName)
        {
            return ExportWithResult(project, exportDirectory, projectName).WaveFilePath;
        }

        public GameAudioWavExportResult ExportWithResult(
            GameAudioProject project,
            string exportDirectory,
            string projectName,
            GameAudioWavExportOptions options = null)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            options ??= GameAudioWavExportOptions.Default;
            string filePath = GameAudioExportUtility.BuildWaveFilePath(exportDirectory, projectName);
            string directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new InvalidOperationException("Export directory could not be resolved.");
            }

            Directory.CreateDirectory(directoryPath);

            GameAudioRenderResult renderResult = _renderer.Render(project, new GameAudioRenderSettings { ClampSamples = false });
            float[] outputSamples = (float[])renderResult.Samples.Clone();
            GameAudioNormalizeResult normalizeResult = ApplyNormalization(outputSamples, renderResult.PeakAmplitude, options);
            float outputPeakAmplitude = CalculatePeakAmplitude(outputSamples);
            byte[] wavBytes = GameAudioWavEncoder.EncodePcm16(outputSamples, renderResult.SampleRate, renderResult.ChannelCount);
            File.WriteAllBytes(filePath, wavBytes);
            var qualityReport = new GameAudioExportQualityReport(
                renderResult.PeakAmplitude,
                outputPeakAmplitude,
                renderResult.SampleRate,
                renderResult.ChannelCount,
                renderResult.FrameCount,
                renderResult.ProjectFrameCount,
                options.Normalize,
                normalizeResult.Applied,
                normalizeResult.GainDb);
            GameAudioDiagnosticLogger.Verbose(
                "Export",
                $"Encoded {wavBytes.Length} bytes for {projectName}. SourcePeak={qualityReport.SourcePeakAmplitude:0.000}; OutputPeak={qualityReport.OutputPeakAmplitude:0.000}; NormalizeApplied={qualityReport.NormalizeApplied}.");
            return new GameAudioWavExportResult(filePath, qualityReport);
        }

        private static GameAudioNormalizeResult ApplyNormalization(float[] samples, float peakAmplitude, GameAudioWavExportOptions options)
        {
            if (samples == null
                || samples.Length == 0
                || !options.Normalize
                || peakAmplitude <= GameAudioExportQualityReport.SilentPeakThreshold)
            {
                return GameAudioNormalizeResult.NotApplied;
            }

            float headroomDb = GameAudioValidationUtility.ClampFloat(options.HeadroomDb, MinimumNormalizeHeadroomDb, MaximumNormalizeHeadroomDb);
            float targetPeak = DbToLinear(headroomDb);
            float gain = targetPeak / peakAmplitude;
            for (int index = 0; index < samples.Length; index++)
            {
                samples[index] *= gain;
            }

            return new GameAudioNormalizeResult(true, LinearToDb(gain));
        }

        private static float CalculatePeakAmplitude(float[] samples)
        {
            float peak = 0.0f;
            if (samples == null)
            {
                return peak;
            }

            for (int index = 0; index < samples.Length; index++)
            {
                peak = Math.Max(peak, Math.Abs(samples[index]));
            }

            return peak;
        }

        private static float DbToLinear(float db)
        {
            return (float)Math.Pow(10.0d, db / 20.0d);
        }

        private static float LinearToDb(float linear)
        {
            return linear <= 0.0f ? -120.0f : (float)(20.0d * Math.Log10(linear));
        }
    }

    internal sealed class GameAudioWavExportOptions
    {
        public static GameAudioWavExportOptions Default => new GameAudioWavExportOptions();

        public bool Normalize { get; set; }

        public float HeadroomDb { get; set; } = -1.0f;
    }

    internal sealed class GameAudioWavExportResult
    {
        public GameAudioWavExportResult(string waveFilePath, GameAudioExportQualityReport qualityReport)
        {
            WaveFilePath = waveFilePath ?? string.Empty;
            QualityReport = qualityReport ?? throw new ArgumentNullException(nameof(qualityReport));
        }

        public string WaveFilePath { get; }

        public GameAudioExportQualityReport QualityReport { get; }
    }

    internal readonly struct GameAudioNormalizeResult
    {
        public static GameAudioNormalizeResult NotApplied => new GameAudioNormalizeResult(false, 0.0f);

        public GameAudioNormalizeResult(bool applied, float gainDb)
        {
            Applied = applied;
            GainDb = gainDb;
        }

        public bool Applied { get; }

        public float GainDb { get; }
    }
}
