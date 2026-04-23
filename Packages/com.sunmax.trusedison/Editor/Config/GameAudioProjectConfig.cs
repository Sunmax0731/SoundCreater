using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Config
{
    public sealed class GameAudioProjectConfig
    {
        public string ExportDirectory { get; set; } = string.Empty;

        public bool AutoRefreshAfterExport { get; set; } = true;

        public int? PreferredSampleRate { get; set; }

        public GameAudioChannelMode? PreferredChannelMode { get; set; }
    }
}
