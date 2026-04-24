using System;
using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Audio
{
    public static class GameAudioPreviewCursorCalculator
    {
        public static GameAudioPreviewCursorState Calculate(GameAudioProject project, GameAudioPreviewState previewState)
        {
            if (project == null || previewState?.RenderResult == null || previewState.SampleRate <= 0)
            {
                return new GameAudioPreviewCursorState(0.0d, 0.0f, 1, 1.0d, false, 0.0d);
            }

            double targetDurationSeconds = Math.Max(0.0d, previewState.TargetDurationSeconds);
            double outputDurationSeconds = Math.Max(targetDurationSeconds, previewState.OutputDurationSeconds);
            double playbackSeconds = Clamp(previewState.PlaybackSeconds, 0.0d, outputDurationSeconds);
            double musicalSeconds = Math.Min(targetDurationSeconds, playbackSeconds);
            bool isInTail = !previewState.LoopPlayback && playbackSeconds > targetDurationSeconds;
            double tailSeconds = Math.Max(0.0d, playbackSeconds - targetDurationSeconds);
            float musicalProgress = targetDurationSeconds <= 0.0d
                ? 0.0f
                : (float)(musicalSeconds / targetDurationSeconds);

            double secondsPerBeat = 60.0d / Math.Max(1, project.Bpm);
            double beatsPerBar = GetBeatsPerBar(project.TimeSignature);
            int totalBars = Math.Max(1, project.TotalBars);
            double totalBeats = Math.Max(beatsPerBar, totalBars * beatsPerBar);
            double currentBeatPosition = secondsPerBeat <= 0.0d
                ? 0.0d
                : Math.Min(totalBeats, musicalSeconds / secondsPerBeat);

            if (currentBeatPosition >= totalBeats)
            {
                return new GameAudioPreviewCursorState(
                    musicalSeconds,
                    Clamp(musicalProgress, 0.0f, 1.0f),
                    totalBars,
                    beatsPerBar,
                    isInTail,
                    tailSeconds);
            }

            int zeroBasedBar = Math.Min(totalBars - 1, (int)Math.Floor(currentBeatPosition / beatsPerBar));
            double beatInBar = (currentBeatPosition - (zeroBasedBar * beatsPerBar)) + 1.0d;
            return new GameAudioPreviewCursorState(
                musicalSeconds,
                Clamp(musicalProgress, 0.0f, 1.0f),
                zeroBasedBar + 1,
                beatInBar,
                isInTail,
                tailSeconds);
        }

        private static double GetBeatsPerBar(GameAudioTimeSignature timeSignature)
        {
            if (timeSignature == null || timeSignature.Denominator <= 0)
            {
                return 4.0d;
            }

            return Math.Max(1.0d, timeSignature.Numerator * (4.0d / timeSignature.Denominator));
        }

        private static double Clamp(double value, double min, double max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }

        private static float Clamp(float value, float min, float max)
        {
            if (value < min)
            {
                return min;
            }

            if (value > max)
            {
                return max;
            }

            return value;
        }
    }
}
