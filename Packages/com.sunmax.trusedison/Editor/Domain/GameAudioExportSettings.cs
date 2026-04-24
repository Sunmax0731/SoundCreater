namespace TorusEdison.Editor.Domain
{
    public enum GameAudioExportDurationMode
    {
        ProjectBars,
        Seconds,
        AutoTrim
    }

    public sealed class GameAudioExportSettings
    {
        public GameAudioExportDurationMode DurationMode { get; set; } = GameAudioExportDurationMode.ProjectBars;

        public float DurationSeconds { get; set; } = 1.0f;

        public bool IncludeTail { get; set; } = true;
    }
}
