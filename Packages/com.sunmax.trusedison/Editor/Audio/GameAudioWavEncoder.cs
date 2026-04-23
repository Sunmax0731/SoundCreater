using System;
using System.IO;

namespace TorusEdison.Editor.Audio
{
    internal static class GameAudioWavEncoder
    {
        private const short Pcm16BitsPerSample = 16;
        private const short Pcm16BytesPerSample = Pcm16BitsPerSample / 8;
        private const short Pcm8BitsPerSample = 8;
        private const short Pcm8BytesPerSample = Pcm8BitsPerSample / 8;

        public static byte[] EncodePcm16(float[] samples, int sampleRate, int channelCount)
        {
            return Encode(samples, sampleRate, channelCount, Pcm16BitsPerSample, Pcm16BytesPerSample, WritePcm16Sample);
        }

        public static byte[] EncodePcm8(float[] samples, int sampleRate, int channelCount)
        {
            return Encode(samples, sampleRate, channelCount, Pcm8BitsPerSample, Pcm8BytesPerSample, WritePcm8Sample);
        }

        private static byte[] Encode(
            float[] samples,
            int sampleRate,
            int channelCount,
            short bitsPerSample,
            short bytesPerSample,
            Action<BinaryWriter, float> writeSample)
        {
            if (samples == null)
            {
                throw new ArgumentNullException(nameof(samples));
            }

            if (sampleRate <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(sampleRate));
            }

            if (channelCount <= 0)
            {
                throw new ArgumentOutOfRangeException(nameof(channelCount));
            }

            if (writeSample == null)
            {
                throw new ArgumentNullException(nameof(writeSample));
            }

            int dataLength = samples.Length * bytesPerSample;
            int byteRate = sampleRate * channelCount * bytesPerSample;
            short blockAlign = (short)(channelCount * bytesPerSample);

            using (var stream = new MemoryStream(44 + dataLength))
            using (var writer = new BinaryWriter(stream))
            {
                writer.Write(new[] { 'R', 'I', 'F', 'F' });
                writer.Write(36 + dataLength);
                writer.Write(new[] { 'W', 'A', 'V', 'E' });
                writer.Write(new[] { 'f', 'm', 't', ' ' });
                writer.Write(16);
                writer.Write((short)1);
                writer.Write((short)channelCount);
                writer.Write(sampleRate);
                writer.Write(byteRate);
                writer.Write(blockAlign);
                writer.Write(bitsPerSample);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataLength);

                for (int index = 0; index < samples.Length; index++)
                {
                    writeSample(writer, samples[index]);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }

        private static void WritePcm16Sample(BinaryWriter writer, float sample)
        {
            float clamped = Math.Clamp(sample, -1.0f, 1.0f);
            short value = (short)Math.Round(clamped * short.MaxValue);
            writer.Write(value);
        }

        private static void WritePcm8Sample(BinaryWriter writer, float sample)
        {
            float clamped = Math.Clamp(sample, -1.0f, 1.0f);
            byte value = (byte)Math.Clamp((int)Math.Round((clamped + 1.0f) * 127.5f), 0, byte.MaxValue);
            writer.Write(value);
        }
    }
}
