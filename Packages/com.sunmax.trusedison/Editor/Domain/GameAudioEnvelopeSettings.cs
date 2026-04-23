namespace TorusEdison.Editor.Domain
{
    public sealed class GameAudioEnvelopeSettings
    {
        public int AttackMs { get; set; } = 5;

        public int DecayMs { get; set; } = 80;

        public float Sustain { get; set; } = 0.7f;

        public int ReleaseMs { get; set; } = 120;
    }
}
