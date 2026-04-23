namespace GameAudioTool.Editor.Audio
{
    public sealed class GameAudioPreviewState
    {
        public GameAudioPreviewState(
            bool isPreviewReady,
            bool isPlaying,
            bool loopPlayback,
            double playbackSeconds,
            GameAudioRenderResult renderResult,
            string statusText,
            string errorText)
        {
            IsPreviewReady = isPreviewReady;
            IsPlaying = isPlaying;
            LoopPlayback = loopPlayback;
            PlaybackSeconds = playbackSeconds;
            RenderResult = renderResult;
            StatusText = statusText ?? string.Empty;
            ErrorText = errorText ?? string.Empty;
        }

        public bool IsPreviewReady { get; }

        public bool IsPlaying { get; }

        public bool LoopPlayback { get; }

        public double PlaybackSeconds { get; }

        public GameAudioRenderResult RenderResult { get; }

        public string StatusText { get; }

        public string ErrorText { get; }

        public int SampleRate => RenderResult?.SampleRate ?? 0;

        public int ChannelCount => RenderResult?.ChannelCount ?? 0;

        public float PeakAmplitude => RenderResult?.PeakAmplitude ?? 0.0f;

        public double ProjectDurationSeconds => RenderResult == null || RenderResult.SampleRate <= 0
            ? 0.0d
            : RenderResult.ProjectFrameCount / (double)RenderResult.SampleRate;

        public double OutputDurationSeconds => RenderResult?.DurationSeconds ?? 0.0d;
    }
}
