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
                return Path.GetFullPath(Path.Combine(projectRoot, "Exports", "Audio"));
            }

            return Path.IsPathRooted(exportDirectory)
                ? Path.GetFullPath(exportDirectory)
                : Path.GetFullPath(Path.Combine(projectRoot, exportDirectory));
        }

        public static string NormalizeStoredExportDirectory(string exportDirectory, string projectRoot)
        {
            if (string.IsNullOrWhiteSpace(projectRoot))
            {
                throw new ArgumentException("Project root is required.", nameof(projectRoot));
            }

            if (string.IsNullOrWhiteSpace(exportDirectory))
            {
                return string.Empty;
            }

            string trimmed = exportDirectory.Trim();
            if (!Path.IsPathRooted(trimmed))
            {
                return NormalizeStoredRelativeDirectory(trimmed);
            }

            string projectRootFullPath = TrimEndingDirectorySeparators(Path.GetFullPath(projectRoot));
            string exportFullPath = TrimEndingDirectorySeparators(Path.GetFullPath(trimmed));
            string projectRootWithSeparator = projectRootFullPath + Path.DirectorySeparatorChar;

            if (string.Equals(exportFullPath, projectRootFullPath, StringComparison.OrdinalIgnoreCase))
            {
                return ".";
            }

            if (exportFullPath.StartsWith(projectRootWithSeparator, StringComparison.OrdinalIgnoreCase))
            {
                return NormalizeStoredRelativeDirectory(Path.GetRelativePath(projectRootFullPath, exportFullPath));
            }

            return exportFullPath;
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

        private static string NormalizeStoredRelativeDirectory(string relativeDirectory)
        {
            if (string.IsNullOrWhiteSpace(relativeDirectory))
            {
                return string.Empty;
            }

            string normalized = relativeDirectory
                .Trim()
                .Replace(Path.DirectorySeparatorChar, '/')
                .Replace(Path.AltDirectorySeparatorChar, '/');

            normalized = normalized.TrimEnd('/');
            return string.IsNullOrWhiteSpace(normalized)
                ? string.Empty
                : normalized;
        }

        private static string TrimEndingDirectorySeparators(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                return string.Empty;
            }

            string fullPath = Path.GetFullPath(path);
            string root = Path.GetPathRoot(fullPath) ?? string.Empty;
            string trimmed = fullPath.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.IsNullOrWhiteSpace(trimmed) || string.Equals(trimmed, root.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar), StringComparison.OrdinalIgnoreCase)
                ? root
                : trimmed;
        }
    }
}
