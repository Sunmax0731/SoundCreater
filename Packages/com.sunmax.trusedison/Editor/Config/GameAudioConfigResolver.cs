using System.IO;
using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Config
{
    public static class GameAudioConfigResolver
    {
        public static int ResolveSampleRate(GameAudioCommonConfig commonConfig, GameAudioProjectConfig projectConfig)
        {
            if (projectConfig != null && projectConfig.PreferredSampleRate.HasValue)
            {
                return projectConfig.PreferredSampleRate.Value;
            }

            return commonConfig?.DefaultSampleRate ?? 48000;
        }

        public static GameAudioChannelMode ResolveChannelMode(GameAudioCommonConfig commonConfig, GameAudioProjectConfig projectConfig)
        {
            if (projectConfig != null && projectConfig.PreferredChannelMode.HasValue)
            {
                return projectConfig.PreferredChannelMode.Value;
            }

            return commonConfig?.DefaultChannelMode ?? GameAudioChannelMode.Stereo;
        }

        public static string ResolveExportDirectory(GameAudioCommonConfig commonConfig, GameAudioProjectConfig projectConfig, string projectRoot)
        {
            if (projectConfig != null && !string.IsNullOrWhiteSpace(projectConfig.ExportDirectory))
            {
                return projectConfig.ExportDirectory;
            }

            if (commonConfig != null && !string.IsNullOrWhiteSpace(commonConfig.DefaultExportDirectory))
            {
                return Path.IsPathRooted(commonConfig.DefaultExportDirectory)
                    ? commonConfig.DefaultExportDirectory
                    : Path.Combine(projectRoot, commonConfig.DefaultExportDirectory);
            }

            return Path.Combine(projectRoot, "Exports", "Audio");
        }
    }
}
