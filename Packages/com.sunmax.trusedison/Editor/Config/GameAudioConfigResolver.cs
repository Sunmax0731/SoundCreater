using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;

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
                return GameAudioExportUtility.NormalizeExportDirectory(projectConfig.ExportDirectory, projectRoot);
            }

            if (commonConfig != null && !string.IsNullOrWhiteSpace(commonConfig.DefaultExportDirectory))
            {
                return GameAudioExportUtility.NormalizeExportDirectory(commonConfig.DefaultExportDirectory, projectRoot);
            }

            return GameAudioExportUtility.NormalizeExportDirectory(string.Empty, projectRoot);
        }
    }
}
