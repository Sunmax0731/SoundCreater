namespace TorusEdison.Editor.Audio
{
    public sealed class GameAudioPreviewCursorState
    {
        public GameAudioPreviewCursorState(
            double musicalSeconds,
            float musicalProgress,
            int currentBar,
            double beatInBar,
            bool isInTail,
            double tailSeconds)
        {
            MusicalSeconds = musicalSeconds;
            MusicalProgress = musicalProgress;
            CurrentBar = currentBar;
            BeatInBar = beatInBar;
            IsInTail = isInTail;
            TailSeconds = tailSeconds;
        }

        public double MusicalSeconds { get; }

        public float MusicalProgress { get; }

        public int CurrentBar { get; }

        public double BeatInBar { get; }

        public bool IsInTail { get; }

        public double TailSeconds { get; }
    }
}
