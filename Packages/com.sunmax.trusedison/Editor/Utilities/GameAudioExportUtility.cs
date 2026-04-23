using System;
using System.IO;

namespace TorusEdison.Editor.Utilities
{
    internal static class GameAudioExportUtility
    {
        public static string NormalizeExportDirectory(string exportDirectory, string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new ArgumentException("Project root is required.", nameof(projectRoot));
            }

            if (string.IsNullOrWhiteSpace(exportDirectory))
            {
                return Path.Combine(projectRoot, "Exports", "Audio");
            }

            return Path.IsPathRooted(exportDirectory)
                ? exportDirectory
                : Path.Combine(projectRoot, exportDirectory);
        }

        public static string NormalizeWaveFileName(string projectName)
        {
            string sanitizedName = GameAudioValidationUtility.SanitizeExportFileName(projectName);
            if (string.IsNullOrWhiteSpace(sanitizedName))
            {
                sanitizedName = "GameAudio";
            }

            return sanitizedName.EndsWith(".wav", StringComparison.OrdinalIgnoreCase)
                ? sanitizedName
                : $"{sanitizedName}.wav";
        }

        public static string BuildWaveFilePath(string exportDirectory, string projectName)
        {
            if (string.IsNullOrWhiteSpace(exportDirectory))
            {
                throw new ArgumentException("Export directory is required.", nameof(exportDirectory));
            }

            return Path.Combine(exportDirectory, NormalizeWaveFileName(projectName));
        }

        public static bool ShouldRefreshAssetDatabase(string filePath, string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(filePath) || string.IsNullOrWhiteSpace(projectRoot))
            {
                return false;
            }

            string fullFilePath = Path.GetFullPath(filePath);
            string assetsDirectory = Path.GetFullPath(Path.Combine(projectRoot, "Assets"));
            string assetsDirectoryWithSeparator = assetsDirectory.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar)
                + Path.DirectorySeparatorChar;

            return fullFilePath.StartsWith(assetsDirectoryWithSeparator, StringComparison.OrdinalIgnoreCase);
        }
    }
}
