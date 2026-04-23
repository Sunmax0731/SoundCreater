namespace TorusEdison.Editor.Domain
{
    public sealed class GameAudioVoiceSettings
    {
        public GameAudioWaveformType Waveform { get; set; } = GameAudioWaveformType.Square;

        public float PulseWidth { get; set; } = 0.5f;

        public bool NoiseEnabled { get; set; }

        public GameAudioNoiseType NoiseType { get; set; } = GameAudioNoiseType.White;

        public float NoiseMix { get; set; }

        public GameAudioEnvelopeSettings Adsr { get; set; } = new GameAudioEnvelopeSettings();

        public GameAudioEffectSettings Effect { get; set; } = new GameAudioEffectSettings();
    }
}
