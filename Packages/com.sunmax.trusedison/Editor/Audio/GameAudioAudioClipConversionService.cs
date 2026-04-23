using System;
using System.IO;
using TorusEdison.Editor.Utilities;
using UnityEngine;

namespace TorusEdison.Editor.Audio
{
    internal sealed class GameAudioAudioClipConversionService
    {
        public string ExportAsPcm8(
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
            GameAudioDiagnosticLogger.Verbose("Conversion", $"Encoded 8-bit WAV {filePath} ({resolvedSampleRate} Hz / {outputChannelCount} ch).");
            return filePath;
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
    }
}
