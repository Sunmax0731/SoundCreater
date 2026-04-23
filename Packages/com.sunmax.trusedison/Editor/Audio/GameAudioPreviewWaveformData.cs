namespace TorusEdison.Editor.Audio
{
    public readonly struct GameAudioPreviewWaveformBin
    {
        public GameAudioPreviewWaveformBin(float min, float max)
        {
            Min = min;
            Max = max;
        }

        public float Min { get; }

        public float Max { get; }
    }

    public sealed class GameAudioPreviewWaveformData
    {
        public GameAudioPreviewWaveformData(GameAudioPreviewWaveformBin[] bins, bool isSilent)
        {
            Bins = bins ?? System.Array.Empty<GameAudioPreviewWaveformBin>();
            IsSilent = isSilent;
        }

        public GameAudioPreviewWaveformBin[] Bins { get; }

        public bool IsSilent { get; }
    }
}
