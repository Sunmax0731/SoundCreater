namespace TorusEdison.Editor.Domain
{
    public sealed class GameAudioImportedAudioConversion
    {
        public string SourceClipName { get; set; } = string.Empty;

        public string SourceAssetPath { get; set; } = string.Empty;

        public int SourceSampleRate { get; set; }

        public int SourceChannelCount { get; set; }

        public float SourceDurationSeconds { get; set; }

        public int TargetSampleRate { get; set; }

        public string TargetChannelMode { get; set; } = "Preserve";

        public int OutputChannelCount { get; set; }

        public string OutputWaveFileName { get; set; } = string.Empty;
    }
}
