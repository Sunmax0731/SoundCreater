namespace TorusEdison.Editor.Audio
{
    internal sealed class GameAudioAudioClipConversionExportResult
    {
        public GameAudioAudioClipConversionExportResult(string waveFilePath, string projectFilePath)
        {
            WaveFilePath = waveFilePath ?? string.Empty;
            ProjectFilePath = projectFilePath ?? string.Empty;
        }

        public string WaveFilePath { get; }

        public string ProjectFilePath { get; }
    }
}
