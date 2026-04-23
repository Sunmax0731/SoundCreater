namespace TorusEdison.Editor.Domain
{
    public sealed class GameAudioEffectSettings
    {
        public float VolumeDb { get; set; }

        public float Pan { get; set; }

        public float PitchSemitone { get; set; }

        public int FadeInMs { get; set; }

        public int FadeOutMs { get; set; }

        public GameAudioDelaySettings Delay { get; set; } = new GameAudioDelaySettings();
    }
}
