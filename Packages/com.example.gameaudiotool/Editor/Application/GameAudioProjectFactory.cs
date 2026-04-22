using System;
using GameAudioTool.Editor.Domain;
using GameAudioTool.Editor.Utilities;

namespace GameAudioTool.Editor.Application
{
    public static class GameAudioProjectFactory
    {
        public static GameAudioProject CreateDefaultProject()
        {
            var project = new GameAudioProject
            {
                Id = CreateId("proj"),
                Name = "New Audio Project",
                Bpm = GameAudioToolInfo.DefaultBpm,
                TimeSignature = new GameAudioTimeSignature { Numerator = 4, Denominator = 4 },
                TotalBars = GameAudioToolInfo.DefaultTotalBars,
                SampleRate = GameAudioToolInfo.DefaultSampleRate,
                ChannelMode = GameAudioChannelMode.Stereo,
                MasterGainDb = 0.0f,
                LoopPlayback = false
            };

            project.Tracks.Add(CreateDefaultTrack(1));
            return project;
        }

        public static GameAudioTrack CreateDefaultTrack(int index)
        {
            return new GameAudioTrack
            {
                Id = CreateId("track"),
                Name = $"Track {index:00}",
                Mute = false,
                Solo = false,
                VolumeDb = 0.0f,
                Pan = 0.0f,
                DefaultVoice = CreateDefaultVoice()
            };
        }

        public static GameAudioVoiceSettings CreateDefaultVoice()
        {
            return new GameAudioVoiceSettings
            {
                Waveform = GameAudioWaveformType.Square,
                PulseWidth = 0.5f,
                NoiseEnabled = false,
                NoiseType = GameAudioNoiseType.White,
                NoiseMix = 0.0f,
                Adsr = CreateDefaultEnvelope(),
                Effect = CreateDefaultEffect()
            };
        }

        public static GameAudioEnvelopeSettings CreateDefaultEnvelope()
        {
            return new GameAudioEnvelopeSettings
            {
                AttackMs = 5,
                DecayMs = 80,
                Sustain = 0.7f,
                ReleaseMs = 120
            };
        }

        public static GameAudioEffectSettings CreateDefaultEffect()
        {
            return new GameAudioEffectSettings
            {
                VolumeDb = 0.0f,
                Pan = 0.0f,
                PitchSemitone = 0.0f,
                FadeInMs = 0,
                FadeOutMs = 0,
                Delay = CreateDefaultDelay()
            };
        }

        public static GameAudioDelaySettings CreateDefaultDelay()
        {
            return new GameAudioDelaySettings
            {
                Enabled = false,
                TimeMs = 180,
                Feedback = 0.25f,
                Mix = 0.2f
            };
        }

        private static string CreateId(string prefix)
        {
            return $"{prefix}-{Guid.NewGuid():N}";
        }
    }
}
