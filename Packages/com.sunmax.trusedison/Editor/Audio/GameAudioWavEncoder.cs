using System;
using System.IO;

namespace TorusEdison.Editor.Audio
{
    internal static class GameAudioWavEncoder
    {
        private const short BitsPerSample = 16;
        private const short BytesPerSample = BitsPerSample / 8;

        public static byte[] EncodePcm16(float[] samples, int sampleRate, int channelCount)
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

            int dataLength = samples.Length * BytesPerSample;
            int byteRate = sampleRate * channelCount * BytesPerSample;
            short blockAlign = (short)(channelCount * BytesPerSample);

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
                writer.Write(BitsPerSample);
                writer.Write(new[] { 'd', 'a', 't', 'a' });
                writer.Write(dataLength);

                for (int index = 0; index < samples.Length; index++)
                {
                    float clamped = Math.Clamp(samples[index], -1.0f, 1.0f);
                    short value = (short)Math.Round(clamped * short.MaxValue);
                    writer.Write(value);
                }

                writer.Flush();
                return stream.ToArray();
            }
        }
    }
}
