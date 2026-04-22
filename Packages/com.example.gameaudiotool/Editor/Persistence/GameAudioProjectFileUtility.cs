using System;
using System.IO;
using GameAudioTool.Editor.Utilities;

namespace GameAudioTool.Editor.Persistence
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
                return path;
            }

            string directoryPath = Path.GetDirectoryName(path);
            string fileName = Path.GetFileNameWithoutExtension(path);
            if (string.IsNullOrWhiteSpace(fileName))
            {
                fileName = "GameAudio";
            }

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
    }
}
