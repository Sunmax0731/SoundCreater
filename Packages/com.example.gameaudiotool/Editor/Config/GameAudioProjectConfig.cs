using GameAudioTool.Editor.Domain;

namespace GameAudioTool.Editor.Config
{
    public sealed class GameAudioProjectConfig
    {
        public string ExportDirectory { get; set; } = string.Empty;

        public bool AutoRefreshAfterExport { get; set; } = true;

        public int? PreferredSampleRate { get; set; }

        public GameAudioChannelMode? PreferredChannelMode { get; set; }
    }
}
