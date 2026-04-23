using System;
using System.Collections.Generic;
using System.Linq;
using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Application
{
    internal static class GameAudioProjectCloner
    {
        public static GameAudioProject CloneProject(GameAudioProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            return new GameAudioProject
            {
                Id = project.Id ?? string.Empty,
                Name = project.Name ?? string.Empty,
                Bpm = project.Bpm,
                TimeSignature = CloneTimeSignature(project.TimeSignature),
                TotalBars = project.TotalBars,
                SampleRate = project.SampleRate,
                ChannelMode = project.ChannelMode,
                MasterGainDb = project.MasterGainDb,
                LoopPlayback = project.LoopPlayback,
                Tracks = project.Tracks == null
                    ? new List<GameAudioTrack>()
                    : project.Tracks.Select(CloneTrack).ToList()
            };
        }

        public static GameAudioTrack CloneTrack(GameAudioTrack track)
        {
            if (track == null)
            {
                throw new ArgumentNullException(nameof(track));
            }

            return new GameAudioTrack
            {
                Id = track.Id ?? string.Empty,
                Name = track.Name ?? string.Empty,
                Mute = track.Mute,
                Solo = track.Solo,
                VolumeDb = track.VolumeDb,
                Pan = track.Pan,
                DefaultVoice = CloneVoice(track.DefaultVoice),
                Notes = track.Notes == null
                    ? new List<GameAudioNote>()
                    : track.Notes.Select(CloneNote).ToList()
            };
        }

        public static GameAudioNote CloneNote(GameAudioNote note)
        {
            if (note == null)
            {
                throw new ArgumentNullException(nameof(note));
            }

            return new GameAudioNote
            {
                Id = note.Id ?? string.Empty,
                StartBeat = note.StartBeat,
                DurationBeat = note.DurationBeat,
                MidiNote = note.MidiNote,
                Velocity = note.Velocity,
                VoiceOverride = note.VoiceOverride == null ? null : CloneVoice(note.VoiceOverride)
            };
        }

        private static GameAudioTimeSignature CloneTimeSignature(GameAudioTimeSignature timeSignature)
        {
            return timeSignature == null
                ? new GameAudioTimeSignature()
                : new GameAudioTimeSignature
                {
                    Numerator = timeSignature.Numerator,
                    Denominator = timeSignature.Denominator
                };
        }

        private static GameAudioVoiceSettings CloneVoice(GameAudioVoiceSettings voice)
        {
            return voice == null
                ? GameAudioProjectFactory.CreateDefaultVoice()
                : new GameAudioVoiceSettings
                {
                    Waveform = voice.Waveform,
                    PulseWidth = voice.PulseWidth,
                    NoiseEnabled = voice.NoiseEnabled,
                    NoiseType = voice.NoiseType,
                    NoiseMix = voice.NoiseMix,
                    Adsr = CloneEnvelope(voice.Adsr),
                    Effect = CloneEffect(voice.Effect)
                };
        }

        private static GameAudioEnvelopeSettings CloneEnvelope(GameAudioEnvelopeSettings envelope)
        {
            return envelope == null
                ? GameAudioProjectFactory.CreateDefaultEnvelope()
                : new GameAudioEnvelopeSettings
                {
                    AttackMs = envelope.AttackMs,
                    DecayMs = envelope.DecayMs,
                    Sustain = envelope.Sustain,
                    ReleaseMs = envelope.ReleaseMs
                };
        }

        private static GameAudioEffectSettings CloneEffect(GameAudioEffectSettings effect)
        {
            return effect == null
                ? GameAudioProjectFactory.CreateDefaultEffect()
                : new GameAudioEffectSettings
                {
                    VolumeDb = effect.VolumeDb,
                    Pan = effect.Pan,
                    PitchSemitone = effect.PitchSemitone,
                    FadeInMs = effect.FadeInMs,
                    FadeOutMs = effect.FadeOutMs,
                    Delay = CloneDelay(effect.Delay)
                };
        }

        private static GameAudioDelaySettings CloneDelay(GameAudioDelaySettings delay)
        {
            return delay == null
                ? GameAudioProjectFactory.CreateDefaultDelay()
                : new GameAudioDelaySettings
                {
                    Enabled = delay.Enabled,
                    TimeMs = delay.TimeMs,
                    Feedback = delay.Feedback,
                    Mix = delay.Mix
                };
        }
    }
}
