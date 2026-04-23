using System;
using System.IO;

namespace TorusEdison.Editor.Utilities
{
    internal static class GameAudioConfigPaths
    {
        public static string GetCommonConfigPath()
        {
            string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
            return Path.Combine(localAppData, "GameAudioTool", "config.json");
        }

        public static string GetProjectConfigPath(string projectRoot = null)
        {
            string resolvedRoot = string.IsNullOrWhiteSpace(projectRoot)
                ? Directory.GetCurrentDirectory()
                : projectRoot;

            return Path.Combine(resolvedRoot, "ProjectSettings", "GameAudioToolSettings.json");
        }
    }
}
