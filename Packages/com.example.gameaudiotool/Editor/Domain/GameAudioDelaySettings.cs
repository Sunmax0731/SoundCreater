namespace GameAudioTool.Editor.Domain
{
    public sealed class GameAudioDelaySettings
    {
        public bool Enabled { get; set; }

        public int TimeMs { get; set; } = 180;

        public float Feedback { get; set; } = 0.25f;

        public float Mix { get; set; } = 0.2f;
    }
}
