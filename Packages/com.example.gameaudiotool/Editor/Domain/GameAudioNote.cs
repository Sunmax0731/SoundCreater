namespace GameAudioTool.Editor.Domain
{
    public sealed class GameAudioNote
    {
        public string Id { get; set; } = string.Empty;

        public float StartBeat { get; set; }

        public float DurationBeat { get; set; } = 1.0f;

        public int MidiNote { get; set; } = 60;

        public float Velocity { get; set; } = 0.8f;

        public GameAudioVoiceSettings VoiceOverride { get; set; }
    }
}
