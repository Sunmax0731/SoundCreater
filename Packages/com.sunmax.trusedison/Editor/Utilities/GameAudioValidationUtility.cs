using System;
using System.Globalization;
using System.IO;

namespace TorusEdison.Editor.Utilities
{
    internal static class GameAudioValidationUtility
    {
        private static readonly string[] WindowsReservedFileNames =
        {
            "CON",
            "PRN",
            "AUX",
            "NUL",
            "CONIN$",
            "CONOUT$",
            "COM1",
            "COM2",
            "COM3",
            "COM4",
            "COM5",
            "COM6",
            "COM7",
            "COM8",
            "COM9",
            "LPT1",
            "LPT2",
            "LPT3",
            "LPT4",
            "LPT5",
            "LPT6",
            "LPT7",
            "LPT8",
            "LPT9"
        };

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

            sanitized = sanitized.Trim().Trim('.');
            if (string.IsNullOrWhiteSpace(sanitized))
            {
                return "GameAudio";
            }

            int firstDotIndex = sanitized.IndexOf('.');
            string baseName = firstDotIndex >= 0
                ? sanitized.Substring(0, firstDotIndex).Trim()
                : sanitized.Trim();

            if (IsWindowsReservedFileName(baseName))
            {
                string extensionPart = firstDotIndex >= 0 ? sanitized.Substring(firstDotIndex) : string.Empty;
                sanitized = $"{baseName}_{extensionPart}";
            }

            return string.IsNullOrWhiteSpace(sanitized)
                ? "GameAudio"
                : sanitized;
        }

        private static bool IsWindowsReservedFileName(string name)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                return false;
            }

            for (int index = 0; index < WindowsReservedFileNames.Length; index++)
            {
                if (string.Equals(name, WindowsReservedFileNames[index], StringComparison.OrdinalIgnoreCase))
                {
                    return true;
                }
            }

            return false;
        }
    }
}
