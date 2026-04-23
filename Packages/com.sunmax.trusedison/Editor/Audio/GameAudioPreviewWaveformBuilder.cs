using System;

namespace TorusEdison.Editor.Audio
{
    public static class GameAudioPreviewWaveformBuilder
    {
        public static GameAudioPreviewWaveformData Build(GameAudioRenderResult renderResult, int desiredBinCount)
        {
            if (renderResult == null
                || renderResult.FrameCount <= 0
                || renderResult.ChannelCount <= 0
                || renderResult.Samples == null
                || renderResult.Samples.Length == 0)
            {
                return new GameAudioPreviewWaveformData(Array.Empty<GameAudioPreviewWaveformBin>(), true);
            }

            int binCount = Math.Max(1, desiredBinCount);
            var bins = new GameAudioPreviewWaveformBin[binCount];
            int framesPerBin = Math.Max(1, (int)Math.Ceiling(renderResult.FrameCount / (double)binCount));

            for (int binIndex = 0; binIndex < binCount; binIndex++)
            {
                int startFrame = binIndex * framesPerBin;
                if (startFrame >= renderResult.FrameCount)
                {
                    bins[binIndex] = new GameAudioPreviewWaveformBin(0.0f, 0.0f);
                    continue;
                }

                int endFrame = Math.Min(renderResult.FrameCount, startFrame + framesPerBin);
                float min = 0.0f;
                float max = 0.0f;

                for (int frameIndex = startFrame; frameIndex < endFrame; frameIndex++)
                {
                    int sampleOffset = frameIndex * renderResult.ChannelCount;
                    for (int channelIndex = 0; channelIndex < renderResult.ChannelCount; channelIndex++)
                    {
                        float sample = renderResult.Samples[sampleOffset + channelIndex];
                        if (sample < min)
                        {
                            min = sample;
                        }

                        if (sample > max)
                        {
                            max = sample;
                        }
                    }
                }

                bins[binIndex] = new GameAudioPreviewWaveformBin(min, max);
            }

            return new GameAudioPreviewWaveformData(bins, renderResult.PeakAmplitude <= 0.0001f);
        }
    }
}
