using System;
using System.IO;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Persistence
{
    internal static class GameAudioProjectFileUtility
    {
        public static string NormalizeSavePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A target path is required.", nameof(path));
            }

            if (HasSessionFileExtension(path))
            {
                return SanitizeSessionFilePath(path);
            }

            string directoryPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "GameAudio";
            }

            fileName = GameAudioValidationUtility.SanitizeExportFileName(fileName);
            return string.IsNullOrWhiteSpace(directoryPath)
                ? $"{fileName}{GameAudioToolInfo.SessionFileExtension}"
                : Path.Combine(directoryPath, $"{fileName}{GameAudioToolInfo.SessionFileExtension}");
        }

        public static void EnsureSessionFileExtension(string path)
        {
            if (!HasSessionFileExtension(path))
            {
                throw new GameAudioPersistenceException($"Project files must use the {GameAudioToolInfo.SessionFileExtension} extension.");
            }
        }

        private static bool HasSessionFileExtension(string path)
        {
            return !string.IsNullOrWhiteSpace(path)
                && path.EndsWith(GameAudioToolInfo.SessionFileExtension, StringComparison.OrdinalIgnoreCase);
        }

        private static string SanitizeSessionFilePath(string path)
        {
            string directoryPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileName(path);
            string baseName = fileName.Substring(0, fileName.Length - GameAudioToolInfo.SessionFileExtension.Length);
            string sanitizedFileName = $"{GameAudioValidationUtility.SanitizeExportFileName(baseName)}{GameAudioToolInfo.SessionFileExtension}";

            return string.IsNullOrWhiteSpace(directoryPath)
                ? sanitizedFileName
                : Path.Combine(directoryPath, sanitizedFileName);
        }
    }
}
