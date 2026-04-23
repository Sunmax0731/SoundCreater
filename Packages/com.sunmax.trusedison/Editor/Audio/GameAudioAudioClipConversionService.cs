using System;
using System.IO;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Persistence;
using TorusEdison.Editor.Utilities;
using UnityEditor;
using UnityEngine;

namespace TorusEdison.Editor.Audio
{
    internal sealed class GameAudioAudioClipConversionService
    {
        private readonly GameAudioProjectSerializer _projectSerializer = new GameAudioProjectSerializer();

        public GameAudioAudioClipConversionExportResult ExportAsPcm8(
            AudioClip sourceClip,
            string exportDirectory,
            string outputName,
            int targetSampleRate,
            GameAudioConversionChannelMode channelMode)
        {
            if (sourceClip == null)
            {
                throw new ArgumentNullException(nameof(sourceClip));
            }

            if (string.IsNullOrWhiteSpace(exportDirectory))
            {
                throw new ArgumentException("Export directory is required.", nameof(exportDirectory));
            }

            int resolvedSampleRate = targetSampleRate > 0 ? targetSampleRate : sourceClip.frequency;
            if (resolvedSampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetSampleRate));
            }

            int sourceFrameCount = sourceClip.samples;
            int sourceChannelCount = sourceClip.channels;
            if (sourceFrameCount <= 0 || sourceChannelCount <= 0)
            {
                throw new InvalidOperationException("Source clip is empty.");
            }

            var sourceSamples = new float[sourceFrameCount * sourceChannelCount];
            if (!sourceClip.GetData(sourceSamples, 0))
            {
                throw new InvalidOperationException("Source clip samples could not be read.");
            }

            int outputChannelCount;
            float[] convertedSamples = ConvertSamples(
                sourceSamples,
                sourceFrameCount,
                sourceChannelCount,
                sourceClip.frequency,
                resolvedSampleRate,
                channelMode,
                out outputChannelCount);

            string filePath = GameAudioExportUtility.BuildWaveFilePath(exportDirectory, outputName);
            string directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directoryPath))
            {
                throw new InvalidOperationException("Export directory could not be resolved.");
            }

            Directory.CreateDirectory(directoryPath);
            byte[] wavBytes = GameAudioWavEncoder.EncodePcm8(convertedSamples, resolvedSampleRate, outputChannelCount);
            File.WriteAllBytes(filePath, wavBytes);
            string waveFileName = Path.GetFileName(filePath);

            GameAudioProject conversionProject = CreateConversionProject(
                sourceClip,
                outputName,
                resolvedSampleRate,
                channelMode,
                outputChannelCount,
                waveFileName);

            string projectFilePath = GameAudioExportUtility.BuildProjectFilePath(exportDirectory, outputName);
            _projectSerializer.SaveToFile(projectFilePath, conversionProject);
            GameAudioDiagnosticLogger.Verbose(
                "Conversion",
                $"Encoded 8-bit WAV {filePath} ({resolvedSampleRate} Hz / {outputChannelCount} ch) and generated conversion project {projectFilePath}.");
            return new GameAudioAudioClipConversionExportResult(filePath, projectFilePath);
        }

        internal static float[] ConvertSamples(
            float[] sourceSamples,
            int sourceFrameCount,
            int sourceChannelCount,
            int sourceSampleRate,
            int targetSampleRate,
            GameAudioConversionChannelMode channelMode,
            out int outputChannelCount)
        {
            if (sourceSamples == null)
            {
                throw new ArgumentNullException(nameof(sourceSamples));
            }

            if (sourceFrameCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceFrameCount));
            }

            if (sourceChannelCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceChannelCount));
            }

            if (sourceSampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sourceSampleRate));
            }

            if (targetSampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(targetSampleRate));
            }

            if (sourceSamples.Length < sourceFrameCount * sourceChannelCount)
            {
                throw new ArgumentException("Source sample buffer is shorter than the declared frame range.", nameof(sourceSamples));
            }

            outputChannelCount = ResolveOutputChannelCount(sourceChannelCount, channelMode);
            int outputFrameCount = Math.Max(1, (int)Math.Round(sourceFrameCount * (double)targetSampleRate / sourceSampleRate));
            var outputSamples = new float[outputFrameCount * outputChannelCount];

            for (int outputFrame = 0; outputFrame < outputFrameCount; outputFrame++)
            {
                double sourcePosition = outputFrame * (double)sourceSampleRate / targetSampleRate;
                int sourceFrameA = Math.Clamp((int)Math.Floor(sourcePosition), 0, sourceFrameCount - 1);
                int sourceFrameB = Math.Min(sourceFrameA + 1, sourceFrameCount - 1);
                float blend = (float)(sourcePosition - sourceFrameA);

                for (int channelIndex = 0; channelIndex < outputChannelCount; channelIndex++)
                {
                    float sampleA = GetConvertedSample(sourceSamples, sourceFrameA, sourceChannelCount, channelMode, channelIndex);
                    float sampleB = GetConvertedSample(sourceSamples, sourceFrameB, sourceChannelCount, channelMode, channelIndex);
                    outputSamples[(outputFrame * outputChannelCount) + channelIndex] = Mathf.Lerp(sampleA, sampleB, blend);
                }
            }

            return outputSamples;
        }

        private static int ResolveOutputChannelCount(int sourceChannelCount, GameAudioConversionChannelMode channelMode)
        {
            return channelMode switch
            {
                GameAudioConversionChannelMode.Mono => 1,
                GameAudioConversionChannelMode.Stereo => 2,
                _ => sourceChannelCount
            };
        }

        private static float GetConvertedSample(
            float[] sourceSamples,
            int sourceFrame,
            int sourceChannelCount,
            GameAudioConversionChannelMode channelMode,
            int outputChannelIndex)
        {
            int frameOffset = sourceFrame * sourceChannelCount;
            switch (channelMode)
            {
                case GameAudioConversionChannelMode.Mono:
                    return AverageChannels(sourceSamples, frameOffset, sourceChannelCount);

                case GameAudioConversionChannelMode.Stereo:
                    if (sourceChannelCount == 1)
                    {
                        return sourceSamples[frameOffset];
                    }

                    if (sourceChannelCount == 2)
                    {
                        return sourceSamples[frameOffset + Math.Clamp(outputChannelIndex, 0, 1)];
                    }

                    return outputChannelIndex == 0
                        ? AverageAlternatingChannels(sourceSamples, frameOffset, sourceChannelCount, 0)
                        : AverageAlternatingChannels(sourceSamples, frameOffset, sourceChannelCount, 1);

                default:
                    int sourceChannelIndex = Math.Clamp(outputChannelIndex, 0, sourceChannelCount - 1);
                    return sourceSamples[frameOffset + sourceChannelIndex];
            }
        }

        private static float AverageChannels(float[] sourceSamples, int frameOffset, int sourceChannelCount)
        {
            float total = 0.0f;
            for (int channelIndex = 0; channelIndex < sourceChannelCount; channelIndex++)
            {
                total += sourceSamples[frameOffset + channelIndex];
            }

            return total / sourceChannelCount;
        }

        private static float AverageAlternatingChannels(float[] sourceSamples, int frameOffset, int sourceChannelCount, int startIndex)
        {
            float total = 0.0f;
            int count = 0;
            for (int channelIndex = startIndex; channelIndex < sourceChannelCount; channelIndex += 2)
            {
                total += sourceSamples[frameOffset + channelIndex];
                count++;
            }

            if (count == 0)
            {
                return AverageChannels(sourceSamples, frameOffset, sourceChannelCount);
            }

            return total / count;
        }

        private static GameAudioProject CreateConversionProject(
            AudioClip sourceClip,
            string outputName,
            int targetSampleRate,
            GameAudioConversionChannelMode channelMode,
            int outputChannelCount,
            string outputWaveFileName)
        {
            string projectName = ResolveProjectName(sourceClip, outputName);
            var project = GameAudioProjectFactory.CreateDefaultProject(
                targetSampleRate,
                outputChannelCount == 1 ? GameAudioChannelMode.Mono : GameAudioChannelMode.Stereo);

            project.Name = projectName;
            project.SampleRate = targetSampleRate;
            project.TotalBars = CalculateTotalBars(sourceClip.length, project.Bpm, project.TimeSignature?.Numerator ?? 4, project.TimeSignature?.Denominator ?? 4);
            if (project.Tracks.Count > 0)
            {
                project.Tracks[0].Name = "Imported Audio";
            }

            string assetPath = AssetDatabase.GetAssetPath(sourceClip);
            project.ImportedAudioConversion = new GameAudioImportedAudioConversion
            {
                SourceClipName = sourceClip.name ?? string.Empty,
                SourceAssetPath = string.IsNullOrWhiteSpace(assetPath) ? "(not imported)" : assetPath,
                SourceSampleRate = sourceClip.frequency,
                SourceChannelCount = sourceClip.channels,
                SourceDurationSeconds = sourceClip.length,
                TargetSampleRate = targetSampleRate,
                TargetChannelMode = channelMode.ToString(),
                OutputChannelCount = outputChannelCount,
                OutputWaveFileName = outputWaveFileName ?? string.Empty
            };

            return project;
        }

        private static string ResolveProjectName(AudioClip sourceClip, string outputName)
        {
            if (!string.IsNullOrWhiteSpace(outputName))
            {
                return outputName.Trim();
            }

            if (sourceClip != null && !string.IsNullOrWhiteSpace(sourceClip.name))
            {
                return $"{sourceClip.name}_8bit";
            }

            return "Converted8Bit";
        }

        private static int CalculateTotalBars(float durationSeconds, int bpm, int numerator, int denominator)
        {
            if (durationSeconds <= 0.0f || bpm <= 0)
            {
                return GameAudioToolInfo.DefaultTotalBars;
            }

            float beatsPerBar = Mathf.Max(1.0f, numerator * (4.0f / Mathf.Max(1, denominator)));
            double totalBeats = durationSeconds * bpm / 60.0;
            int totalBars = Math.Max(1, (int)Math.Ceiling(totalBeats / beatsPerBar));
            return Math.Clamp(totalBars, 1, GameAudioToolInfo.MaxTotalBars);
        }
    }
}
