using System;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Audio
{
    public sealed class GameAudioProjectRenderer
    {
        private const float DelayTailThreshold = 0.0001f;
        private const int MaxDelayRepeats = 64;

        public GameAudioRenderResult Render(GameAudioProject project, GameAudioRenderSettings settings = null)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            settings ??= new GameAudioRenderSettings();

            int sampleRate = GameAudioValidationUtility.IsSupportedSampleRate(project.SampleRate)
                ? project.SampleRate
                : GameAudioToolInfo.DefaultSampleRate;

            int channelCount = project.ChannelMode == GameAudioChannelMode.Mono ? 1 : 2;
            double secondsPerBeat = 60.0d / Math.Max(1, project.Bpm);
            double beatsPerBar = GetBeatsPerBar(project.TimeSignature);
            int projectFrameCount = Math.Max(1, SecondsToFrames(project.TotalBars * beatsPerBar * secondsPerBeat, sampleRate));
            int renderFrameCount = settings.ExtendTail
                ? Math.Max(projectFrameCount, CalculateTailExtendedFrameCount(project, sampleRate, secondsPerBeat))
                : projectFrameCount;

            float[] samples = new float[renderFrameCount * channelCount];
            bool hasSoloTrack = HasSoloTrack(project);

            if (project.Tracks == null)
            {
                float emptyPeakAmplitude = ApplyMasterGainAndClamp(project, samples, settings.ClampSamples);
                return new GameAudioRenderResult(samples, sampleRate, channelCount, renderFrameCount, projectFrameCount, emptyPeakAmplitude);
            }

            foreach (GameAudioTrack track in project.Tracks)
            {
                if (!IsTrackAudible(track, hasSoloTrack))
                {
                    continue;
                }

                RenderTrack(project, track, sampleRate, channelCount, secondsPerBeat, renderFrameCount, samples);
            }

            float peakAmplitude = ApplyMasterGainAndClamp(project, samples, settings.ClampSamples);
            return new GameAudioRenderResult(samples, sampleRate, channelCount, renderFrameCount, projectFrameCount, peakAmplitude);
        }

        private static void RenderTrack(GameAudioProject project, GameAudioTrack track, int sampleRate, int channelCount, double secondsPerBeat, int renderFrameCount, float[] destination)
        {
            float trackGain = DbToLinear(GameAudioValidationUtility.ClampFloat(track.VolumeDb, -48.0f, 6.0f));
            float trackPan = GameAudioValidationUtility.ClampFloat(track.Pan, -1.0f, 1.0f);

            if (track.Notes == null)
            {
                return;
            }

            foreach (GameAudioNote note in track.Notes)
            {
                if (note == null)
                {
                    continue;
                }

                int startFrame = Math.Max(0, SecondsToFrames(note.StartBeat * secondsPerBeat, sampleRate));
                if (startFrame >= renderFrameCount)
                {
                    continue;
                }

                GameAudioVoiceSettings voice = ResolveVoice(track, note);
                float[] noteBuffer = RenderNote(project, track, note, voice, sampleRate, channelCount, secondsPerBeat);
                MixNoteIntoTrack(noteBuffer, startFrame, destination, renderFrameCount, channelCount, trackGain, trackPan);
            }
        }

        private static float[] RenderNote(GameAudioProject project, GameAudioTrack track, GameAudioNote note, GameAudioVoiceSettings voice, int sampleRate, int channelCount, double secondsPerBeat)
        {
            GameAudioEnvelopeSettings adsr = voice.Adsr ?? GameAudioProjectFactory.CreateDefaultEnvelope();
            GameAudioEffectSettings effect = voice.Effect ?? GameAudioProjectFactory.CreateDefaultEffect();
            GameAudioDelaySettings delay = effect.Delay ?? GameAudioProjectFactory.CreateDefaultDelay();

            int bodyFrameCount = Math.Max(1, SecondsToFrames(Math.Max(GameAudioToolInfo.MinNoteDurationBeat, note.DurationBeat) * secondsPerBeat, sampleRate));
            int releaseFrameCount = MillisecondsToFrames(GameAudioValidationUtility.ClampInt(adsr.ReleaseMs, 0, 5000), sampleRate);
            int totalFrameCount = bodyFrameCount + releaseFrameCount;

            int delayFrameCount = delay.Enabled
                ? Math.Max(1, MillisecondsToFrames(GameAudioValidationUtility.ClampInt(delay.TimeMs, 20, 1000), sampleRate))
                : 0;

            int delayRepeatCount = CalculateDelayRepeatCount(delay);
            int renderedFrameCount = totalFrameCount + (delayFrameCount * delayRepeatCount);
            float[] noteBuffer = new float[renderedFrameCount * channelCount];

            int attackFrameCount = MillisecondsToFrames(GameAudioValidationUtility.ClampInt(adsr.AttackMs, 0, 5000), sampleRate);
            int decayFrameCount = MillisecondsToFrames(GameAudioValidationUtility.ClampInt(adsr.DecayMs, 0, 5000), sampleRate);
            CompressAttackAndDecay(bodyFrameCount, ref attackFrameCount, ref decayFrameCount);

            int fadeInFrameCount = MillisecondsToFrames(GameAudioValidationUtility.ClampInt(effect.FadeInMs, 0, 3000), sampleRate);
            int fadeOutFrameCount = MillisecondsToFrames(GameAudioValidationUtility.ClampInt(effect.FadeOutMs, 0, 3000), sampleRate);
            float noteGain = DbToLinear(GameAudioValidationUtility.ClampFloat(effect.VolumeDb, -48.0f, 6.0f))
                * GameAudioValidationUtility.ClampFloat(note.Velocity, 0.0f, 1.0f);

            float notePan = GameAudioValidationUtility.ClampFloat(effect.Pan, -1.0f, 1.0f);
            double frequency = MidiNoteToFrequency(
                GameAudioValidationUtility.ClampInt(note.MidiNote, 0, 127)
                + GameAudioValidationUtility.ClampFloat(effect.PitchSemitone, -24.0f, 24.0f));

            float pulseWidth = GameAudioValidationUtility.ClampFloat(voice.PulseWidth, 0.10f, 0.90f);
            float noiseMix = voice.NoiseEnabled
                ? GameAudioValidationUtility.ClampFloat(voice.NoiseMix, 0.0f, 1.0f)
                : 0.0f;

            var random = new Random(ComputeNoiseSeed(project.Id, track.Id, note.Id));

            for (int frameIndex = 0; frameIndex < totalFrameCount; frameIndex++)
            {
                float envelope = GetEnvelope(frameIndex, bodyFrameCount, attackFrameCount, decayFrameCount, GameAudioValidationUtility.ClampFloat(adsr.Sustain, 0.0f, 1.0f), releaseFrameCount);
                if (envelope <= 0.0f)
                {
                    continue;
                }

                float fade = GetFadeGain(frameIndex, totalFrameCount, fadeInFrameCount, fadeOutFrameCount);
                if (fade <= 0.0f)
                {
                    continue;
                }

                double phase = frequency * (frameIndex / (double)sampleRate);
                float waveformSample = SampleWaveform(voice.Waveform, phase, pulseWidth);
                if (noiseMix > 0.0f)
                {
                    float whiteNoise = (float)((random.NextDouble() * 2.0d) - 1.0d);
                    waveformSample = (waveformSample * (1.0f - noiseMix)) + (whiteNoise * noiseMix);
                }

                float sample = waveformSample * envelope * fade * noteGain;
                WriteSample(noteBuffer, frameIndex, channelCount, sample, notePan);
            }

            if (delay.Enabled && delayFrameCount > 0 && delayRepeatCount > 0)
            {
                ApplyDelay(noteBuffer, renderedFrameCount, channelCount, delayFrameCount, GameAudioValidationUtility.ClampFloat(delay.Feedback, 0.0f, 0.70f), GameAudioValidationUtility.ClampFloat(delay.Mix, 0.0f, 1.0f));
            }

            return noteBuffer;
        }

        private static void MixNoteIntoTrack(float[] noteBuffer, int startFrame, float[] destination, int renderFrameCount, int channelCount, float trackGain, float trackPan)
        {
            float leftGain = 1.0f;
            float rightGain = 1.0f;
            if (channelCount == 2)
            {
                GetTrackPanGains(trackPan, out leftGain, out rightGain);
            }

            int noteFrameCount = noteBuffer.Length / channelCount;
            for (int frameIndex = 0; frameIndex < noteFrameCount; frameIndex++)
            {
                int targetFrame = startFrame + frameIndex;
                if (targetFrame >= renderFrameCount)
                {
                    break;
                }

                int targetOffset = targetFrame * channelCount;
                int sourceOffset = frameIndex * channelCount;
                if (channelCount == 1)
                {
                    destination[targetOffset] += noteBuffer[sourceOffset] * trackGain;
                    continue;
                }

                destination[targetOffset] += noteBuffer[sourceOffset] * trackGain * leftGain;
                destination[targetOffset + 1] += noteBuffer[sourceOffset + 1] * trackGain * rightGain;
            }
        }

        private static float ApplyMasterGainAndClamp(GameAudioProject project, float[] samples, bool clampSamples)
        {
            float masterGain = DbToLinear(GameAudioValidationUtility.ClampFloat(project.MasterGainDb, -24.0f, 6.0f));
            float peakAmplitude = 0.0f;

            for (int index = 0; index < samples.Length; index++)
            {
                float sample = samples[index] * masterGain;
                if (clampSamples)
                {
                    sample = GameAudioValidationUtility.ClampFloat(sample, -1.0f, 1.0f);
                }

                samples[index] = sample;
                peakAmplitude = Math.Max(peakAmplitude, Math.Abs(sample));
            }

            return peakAmplitude;
        }

        private static int CalculateTailExtendedFrameCount(GameAudioProject project, int sampleRate, double secondsPerBeat)
        {
            int maxFrameCount = 0;
            bool hasSoloTrack = HasSoloTrack(project);

            if (project.Tracks == null)
            {
                return Math.Max(1, maxFrameCount);
            }

            foreach (GameAudioTrack track in project.Tracks)
            {
                if (!IsTrackAudible(track, hasSoloTrack))
                {
                    continue;
                }

                if (track?.Notes == null)
                {
                    continue;
                }

                foreach (GameAudioNote note in track.Notes)
                {
                    if (note == null)
                    {
                        continue;
                    }

                    GameAudioVoiceSettings voice = ResolveVoice(track, note);
                    GameAudioEffectSettings effect = voice.Effect ?? GameAudioProjectFactory.CreateDefaultEffect();
                    GameAudioDelaySettings delay = effect.Delay ?? GameAudioProjectFactory.CreateDefaultDelay();

                    int startFrame = Math.Max(0, SecondsToFrames(note.StartBeat * secondsPerBeat, sampleRate));
                    int bodyFrameCount = Math.Max(1, SecondsToFrames(Math.Max(GameAudioToolInfo.MinNoteDurationBeat, note.DurationBeat) * secondsPerBeat, sampleRate));
                    int releaseFrameCount = MillisecondsToFrames((voice.Adsr ?? GameAudioProjectFactory.CreateDefaultEnvelope()).ReleaseMs, sampleRate);
                    int delayFrameCount = delay.Enabled
                        ? Math.Max(1, MillisecondsToFrames(GameAudioValidationUtility.ClampInt(delay.TimeMs, 20, 1000), sampleRate))
                        : 0;

                    int noteFrameCount = bodyFrameCount + releaseFrameCount + (delayFrameCount * CalculateDelayRepeatCount(delay));
                    maxFrameCount = Math.Max(maxFrameCount, startFrame + noteFrameCount);
                }
            }

            return Math.Max(1, maxFrameCount);
        }

        private static bool HasSoloTrack(GameAudioProject project)
        {
            if (project.Tracks == null)
            {
                return false;
            }

            foreach (GameAudioTrack track in project.Tracks)
            {
                if (track != null && track.Solo)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsTrackAudible(GameAudioTrack track, bool hasSoloTrack)
        {
            if (track == null || track.Mute)
            {
                return false;
            }

            return !hasSoloTrack || track.Solo;
        }

        private static GameAudioVoiceSettings ResolveVoice(GameAudioTrack track, GameAudioNote note)
        {
            return note.VoiceOverride ?? track.DefaultVoice ?? GameAudioProjectFactory.CreateDefaultVoice();
        }

        private static double GetBeatsPerBar(GameAudioTimeSignature timeSignature)
        {
            if (timeSignature == null || timeSignature.Denominator <= 0)
            {
                return 4.0d;
            }

            return timeSignature.Numerator * (4.0d / timeSignature.Denominator);
        }

        private static int SecondsToFrames(double seconds, int sampleRate)
        {
            return (int)Math.Ceiling(Math.Max(0.0d, seconds) * sampleRate);
        }

        private static int MillisecondsToFrames(int milliseconds, int sampleRate)
        {
            return SecondsToFrames(Math.Max(0, milliseconds) / 1000.0d, sampleRate);
        }

        private static void CompressAttackAndDecay(int bodyFrameCount, ref int attackFrameCount, ref int decayFrameCount)
        {
            int total = attackFrameCount + decayFrameCount;
            if (total <= bodyFrameCount || total <= 0)
            {
                return;
            }

            double ratio = bodyFrameCount / (double)total;
            attackFrameCount = (int)Math.Round(attackFrameCount * ratio, MidpointRounding.AwayFromZero);
            attackFrameCount = Math.Min(attackFrameCount, bodyFrameCount);
            decayFrameCount = Math.Max(0, bodyFrameCount - attackFrameCount);
        }

        private static float GetEnvelope(int frameIndex, int bodyFrameCount, int attackFrameCount, int decayFrameCount, float sustainLevel, int releaseFrameCount)
        {
            if (frameIndex < bodyFrameCount)
            {
                if (attackFrameCount > 0 && frameIndex < attackFrameCount)
                {
                    return frameIndex / (float)attackFrameCount;
                }

                if (decayFrameCount > 0 && frameIndex < attackFrameCount + decayFrameCount)
                {
                    float decayProgress = (frameIndex - attackFrameCount) / (float)decayFrameCount;
                    return 1.0f - ((1.0f - sustainLevel) * decayProgress);
                }

                return sustainLevel;
            }

            if (releaseFrameCount <= 0)
            {
                return 0.0f;
            }

            int releaseFrameIndex = frameIndex - bodyFrameCount;
            if (releaseFrameIndex >= releaseFrameCount)
            {
                return 0.0f;
            }

            float releaseProgress = releaseFrameIndex / (float)releaseFrameCount;
            return sustainLevel * (1.0f - releaseProgress);
        }

        private static float GetFadeGain(int frameIndex, int totalFrameCount, int fadeInFrameCount, int fadeOutFrameCount)
        {
            float fadeIn = 1.0f;
            if (fadeInFrameCount > 0 && frameIndex < fadeInFrameCount)
            {
                fadeIn = frameIndex / (float)fadeInFrameCount;
            }

            float fadeOut = 1.0f;
            if (fadeOutFrameCount > 0 && frameIndex >= Math.Max(0, totalFrameCount - fadeOutFrameCount))
            {
                int fadeOutFrameIndex = frameIndex - Math.Max(0, totalFrameCount - fadeOutFrameCount);
                fadeOut = 1.0f - (fadeOutFrameIndex / (float)fadeOutFrameCount);
            }

            return Math.Max(0.0f, fadeIn * fadeOut);
        }

        private static float SampleWaveform(GameAudioWaveformType waveform, double phase, float pulseWidth)
        {
            double phase01 = phase - Math.Floor(phase);

            switch (waveform)
            {
                case GameAudioWaveformType.Sine:
                    return (float)Math.Sin(phase01 * Math.PI * 2.0d);
                case GameAudioWaveformType.Triangle:
                    return (float)(1.0d - (4.0d * Math.Abs(phase01 - 0.5d)));
                case GameAudioWaveformType.Saw:
                    return (float)((phase01 * 2.0d) - 1.0d);
                case GameAudioWaveformType.Pulse:
                    return phase01 < pulseWidth ? 1.0f : -1.0f;
                case GameAudioWaveformType.Square:
                default:
                    return phase01 < 0.5d ? 1.0f : -1.0f;
            }
        }

        private static void WriteSample(float[] noteBuffer, int frameIndex, int channelCount, float sample, float notePan)
        {
            int offset = frameIndex * channelCount;
            if (channelCount == 1)
            {
                noteBuffer[offset] += sample;
                return;
            }

            GetNotePanGains(notePan, out float leftGain, out float rightGain);
            noteBuffer[offset] += sample * leftGain;
            noteBuffer[offset + 1] += sample * rightGain;
        }

        private static void ApplyDelay(float[] noteBuffer, int totalFrameCount, int channelCount, int delayFrameCount, float feedback, float mix)
        {
            if (mix <= 0.0f)
            {
                return;
            }

            float[] scheduled = new float[noteBuffer.Length];
            for (int frameIndex = 0; frameIndex < totalFrameCount; frameIndex++)
            {
                int baseOffset = frameIndex * channelCount;
                for (int channelIndex = 0; channelIndex < channelCount; channelIndex++)
                {
                    int offset = baseOffset + channelIndex;
                    float dry = noteBuffer[offset];
                    float echoed = scheduled[offset];
                    noteBuffer[offset] = dry + echoed;

                    int delayedFrame = frameIndex + delayFrameCount;
                    if (delayedFrame >= totalFrameCount)
                    {
                        continue;
                    }

                    int delayedOffset = (delayedFrame * channelCount) + channelIndex;
                    scheduled[delayedOffset] += (dry * mix) + (echoed * feedback);
                }
            }
        }

        private static int CalculateDelayRepeatCount(GameAudioDelaySettings delay)
        {
            if (delay == null || !delay.Enabled)
            {
                return 0;
            }

            float mix = GameAudioValidationUtility.ClampFloat(delay.Mix, 0.0f, 1.0f);
            float feedback = GameAudioValidationUtility.ClampFloat(delay.Feedback, 0.0f, 0.70f);
            if (mix <= 0.0f)
            {
                return 0;
            }

            int repeats = 1;
            float tailGain = mix;
            while (repeats < MaxDelayRepeats && tailGain * feedback > DelayTailThreshold)
            {
                tailGain *= feedback;
                repeats++;
            }

            return repeats;
        }

        private static void GetNotePanGains(float pan, out float leftGain, out float rightGain)
        {
            double left = Math.Sqrt(0.5d * (1.0d - pan));
            double right = Math.Sqrt(0.5d * (1.0d + pan));
            leftGain = (float)left;
            rightGain = (float)right;
        }

        private static void GetTrackPanGains(float pan, out float leftGain, out float rightGain)
        {
            leftGain = pan > 0.0f ? 1.0f - pan : 1.0f;
            rightGain = pan < 0.0f ? 1.0f + pan : 1.0f;
        }

        private static float DbToLinear(float db)
        {
            return (float)Math.Pow(10.0d, db / 20.0d);
        }

        private static double MidiNoteToFrequency(float midiNote)
        {
            return 440.0d * Math.Pow(2.0d, (midiNote - 69.0d) / 12.0d);
        }

        private static int ComputeNoiseSeed(string projectId, string trackId, string noteId)
        {
            unchecked
            {
                uint hash = 2166136261;
                AppendHash(projectId, ref hash);
                AppendHash(trackId, ref hash);
                AppendHash(noteId, ref hash);
                return (int)hash;
            }
        }

        private static void AppendHash(string value, ref uint hash)
        {
            string safeValue = string.IsNullOrWhiteSpace(value) ? "<null>" : value;
            for (int index = 0; index < safeValue.Length; index++)
            {
                hash ^= safeValue[index];
                hash *= 16777619;
            }
        }
    }
}
