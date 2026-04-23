using System;
using GameAudioTool.Editor.Domain;
using UnityEditor;
using UnityEngine;

namespace GameAudioTool.Editor.Audio
{
    public sealed class GameAudioPreviewPlaybackService : IDisposable
    {
        private readonly GameAudioPreviewClipAssetCache _clipAssetCache = new GameAudioPreviewClipAssetCache();
        private readonly GameAudioProjectRenderer _renderer = new GameAudioProjectRenderer();

        private GameAudioProject _boundProject;
        private GameAudioRenderResult _renderResult;
        private AudioClip _outputClip;
        private AudioClip _loopClip;
        private AudioClip _activeClip;
        private bool _previewDirty = true;
        private bool _isPlaying;
        private bool _isPaused;
        private bool _loopPlayback;
        private double _playbackStartedAt;
        private double _playbackSeconds;
        private string _statusText = "Preview not rendered.";
        private string _errorText = string.Empty;

        public GameAudioPreviewState State => new GameAudioPreviewState(
            _renderResult != null,
            _isPlaying,
            _isPaused,
            _loopPlayback,
            _playbackSeconds,
            _renderResult,
            _statusText,
            _errorText);

        public void Dispose()
        {
            StopActiveClip();
            DestroyPreviewClips();
            _renderResult = null;
        }

        public void InvalidatePreview()
        {
            StopInternal(true, "Preview not rendered.");
            _previewDirty = true;
            _renderResult = null;
            _errorText = string.Empty;
            DestroyPreviewClips();
        }

        public GameAudioPreviewState Prepare(GameAudioProject project)
        {
            EnsurePreview(project);
            return State;
        }

        public GameAudioPreviewState Play(GameAudioProject project, bool loopPlayback)
        {
            _loopPlayback = loopPlayback;
            EnsurePreview(project);

            if (_renderResult == null || _outputClip == null)
            {
                _statusText = "Preview render failed.";
                return State;
            }

            AudioClip clipToPlay = _loopPlayback ? (_loopClip ?? _outputClip) : _outputClip;
            double resumeSeconds = _isPaused ? _playbackSeconds : 0.0d;
            int startSample = _isPaused ? GetResumeStartSample(clipToPlay, resumeSeconds) : 0;

            try
            {
                StopActiveClip();
                GameAudioEditorAudioUtility.PlayPreviewClip(clipToPlay, _loopPlayback, startSample);
                _activeClip = clipToPlay;
                _isPlaying = true;
                _isPaused = false;
                _playbackStartedAt = EditorApplication.timeSinceStartup - resumeSeconds;
                _playbackSeconds = resumeSeconds;
                _errorText = string.Empty;
                _statusText = _loopPlayback
                    ? "Loop preview playing."
                    : "Preview playing.";
            }
            catch (Exception exception)
            {
                StopInternal(true, "Preview start failed.");
                _errorText = exception.Message;
            }

            return State;
        }

        public GameAudioPreviewState Pause()
        {
            if (_renderResult == null)
            {
                _statusText = "Preview not rendered.";
                return State;
            }

            if (!_isPlaying)
            {
                _statusText = _isPaused
                    ? (_loopPlayback ? "Loop preview paused." : "Preview paused.")
                    : (_loopPlayback ? "Loop preview ready." : "Preview ready.");
                return State;
            }

            _playbackSeconds = CalculatePlaybackSeconds(Math.Max(0.0d, EditorApplication.timeSinceStartup - _playbackStartedAt));
            StopInternal(false, _loopPlayback ? "Loop preview paused." : "Preview paused.", true);
            return State;
        }

        public GameAudioPreviewState Stop()
        {
            if (_renderResult == null)
            {
                _statusText = "Preview not rendered.";
                return State;
            }

            StopInternal(true, "Preview stopped.");
            return State;
        }

        public GameAudioPreviewState Rewind()
        {
            if (_renderResult == null)
            {
                _statusText = "Preview not rendered.";
                return State;
            }

            StopInternal(true, "Preview rewound.");
            return State;
        }

        public GameAudioPreviewState SetLoopPlayback(bool loopPlayback)
        {
            bool loopChanged = _loopPlayback != loopPlayback;
            _loopPlayback = loopPlayback;

            if (_renderResult == null)
            {
                return State;
            }

            if (_isPlaying && loopChanged && _boundProject != null)
            {
                return Play(_boundProject, _loopPlayback);
            }

            if (_isPaused)
            {
                _statusText = _loopPlayback
                    ? "Loop preview paused."
                    : "Preview paused.";
            }
            else
            {
                _statusText = _loopPlayback
                    ? "Loop preview ready."
                    : "Preview ready.";
            }
            return State;
        }

        public bool Update()
        {
            if (!_isPlaying || _renderResult == null)
            {
                return false;
            }

            double elapsedSeconds = Math.Max(0.0d, EditorApplication.timeSinceStartup - _playbackStartedAt);
            double previousSeconds = _playbackSeconds;

            if (_loopPlayback)
            {
                double loopDurationSeconds = State.ProjectDurationSeconds;
                if (loopDurationSeconds <= 0.0d)
                {
                    StopInternal(true, "Preview ready.");
                    return true;
                }

                if (!GameAudioEditorAudioUtility.SupportsNativeLooping && elapsedSeconds >= loopDurationSeconds)
                {
                    double completedLoops = Math.Floor(elapsedSeconds / loopDurationSeconds);
                    if (completedLoops >= 1.0d)
                    {
                        _playbackStartedAt += completedLoops * loopDurationSeconds;
                        elapsedSeconds = Math.Max(0.0d, elapsedSeconds - (completedLoops * loopDurationSeconds));

                        try
                        {
                            StopActiveClip();
                            GameAudioEditorAudioUtility.PlayPreviewClip(_loopClip ?? _outputClip, false);
                            _activeClip = _loopClip ?? _outputClip;
                        }
                        catch (Exception exception)
                        {
                            StopInternal(true, "Preview loop restart failed.");
                            _errorText = exception.Message;
                            return true;
                        }
                    }
                }

                _playbackSeconds = CalculatePlaybackSeconds(elapsedSeconds);
                return Math.Abs(previousSeconds - _playbackSeconds) > 0.0005d;
            }

            double outputDurationSeconds = State.OutputDurationSeconds;
            _playbackSeconds = CalculatePlaybackSeconds(elapsedSeconds);
            if (elapsedSeconds >= outputDurationSeconds)
            {
                StopActiveClip();
                _isPlaying = false;
                _isPaused = false;
                _statusText = "Preview complete.";
                return true;
            }

            return Math.Abs(previousSeconds - _playbackSeconds) > 0.0005d;
        }

        private void EnsurePreview(GameAudioProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            _boundProject = project;

            if (!_previewDirty && _renderResult != null && _outputClip != null)
            {
                if (!_isPlaying)
                {
                    _statusText = _isPaused
                        ? (_loopPlayback ? "Loop preview paused." : "Preview paused.")
                        : (_loopPlayback ? "Loop preview ready." : "Preview ready.");
                }

                return;
            }

            StopActiveClip();
            DestroyPreviewClips();

            _renderResult = _renderer.Render(project);
            _outputClip = _clipAssetCache.CreateOutputClip(project.Name, _renderResult);

            if (_renderResult.ProjectFrameCount < _renderResult.FrameCount)
            {
                int loopSampleCount = _renderResult.ProjectFrameCount * _renderResult.ChannelCount;
                float[] loopSamples = new float[loopSampleCount];
                Array.Copy(_renderResult.Samples, loopSamples, loopSampleCount);
                _loopClip = _clipAssetCache.CreateLoopClip(project.Name, loopSamples, _renderResult.ProjectFrameCount, _renderResult.ChannelCount, _renderResult.SampleRate);
            }
            else
            {
                _loopClip = _outputClip;
            }

            if (_outputClip == null)
            {
                throw new InvalidOperationException("Preview clip asset could not be created.");
            }

            _previewDirty = false;
            _isPlaying = false;
            _isPaused = false;
            _playbackSeconds = 0.0d;
            _errorText = string.Empty;
            _statusText = !GameAudioEditorAudioUtility.IsAvailable
                ? "Preview ready. Unity editor playback API is unavailable."
                : _renderResult.PeakAmplitude <= 0.0001f
                    ? "Preview ready (silent buffer)."
                    : "Preview ready.";
        }

        private static double NormalizeLoopSeconds(double elapsedSeconds, double loopDurationSeconds)
        {
            if (loopDurationSeconds <= 0.0d)
            {
                return 0.0d;
            }

            double normalized = elapsedSeconds % loopDurationSeconds;
            return normalized < 0.0d
                ? normalized + loopDurationSeconds
                : normalized;
        }

        private double CalculatePlaybackSeconds(double elapsedSeconds)
        {
            if (_renderResult == null)
            {
                return 0.0d;
            }

            if (_loopPlayback)
            {
                return NormalizeLoopSeconds(elapsedSeconds, State.ProjectDurationSeconds);
            }

            return Math.Min(State.OutputDurationSeconds, elapsedSeconds);
        }

        private static int GetResumeStartSample(AudioClip clip, double resumeSeconds)
        {
            if (clip == null || clip.frequency <= 0 || clip.samples <= 0)
            {
                return 0;
            }

            int startSample = (int)Math.Round(Math.Max(0.0d, resumeSeconds) * clip.frequency);
            return Math.Clamp(startSample, 0, Math.Max(0, clip.samples - 1));
        }

        private void StopInternal(bool resetCursor, string statusText, bool markPaused = false)
        {
            StopActiveClip();
            _isPlaying = false;
            _isPaused = markPaused;
            if (resetCursor)
            {
                _playbackSeconds = 0.0d;
            }

            _statusText = statusText;
        }

        private void StopActiveClip()
        {
            if (_activeClip == null)
            {
                return;
            }

            GameAudioEditorAudioUtility.StopPreviewClip(_activeClip);
            _activeClip = null;
        }

        private void DestroyPreviewClips()
        {
            _outputClip = null;
            _loopClip = null;
            _clipAssetCache.Clear();
        }
    }
}
