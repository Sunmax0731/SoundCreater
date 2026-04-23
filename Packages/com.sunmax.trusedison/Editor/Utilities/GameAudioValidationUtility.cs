using System;
using System.Globalization;
using System.IO;

namespace TorusEdison.Editor.Utilities
{
    internal static class GameAudioValidationUtility
    {
        public static int ClampInt(int value, int min, int max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static float ClampFloat(float value, float min, float max)
        {
            return Math.Min(Math.Max(value, min), max);
        }

        public static bool IsSupportedSampleRate(int sampleRate)
        {
            return sampleRate == GameAudioToolInfo.DefaultSampleRate || sampleRate == GameAudioToolInfo.AlternateSampleRate;
        }

        public static bool TryGetFormatMajor(string formatVersion, out int majorVersion)
        {
            majorVersion = 0;
            if (string.IsNullOrWhiteSpace(formatVersion))
            {
                return false;
            }

            string[] parts = formatVersion.Split('.');
            return parts.Length > 0 && int.TryParse(parts[0], NumberStyles.Integer, CultureInfo.InvariantCulture, out majorVersion);
        }

        public static string SanitizeExportFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return "GameAudio";
            }

            char[] invalidChars = Path.GetInvalidFileNameChars();
            string sanitized = name;
            foreach (char invalidChar in invalidChars)
            {
                sanitized = sanitized.Replace(invalidChar, '_');
            }

            return sanitized.Trim();
        }
    }
}
