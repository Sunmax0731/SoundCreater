using System;
using System.Linq;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Audio;
using TorusEdison.Editor.Domain;
using NUnit.Framework;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioProjectRendererTests
    {
        [Test]
        public void Render_IsDeterministicForTheSameProjectData()
        {
            GameAudioProject project = CreateNoiseProject();
            var renderer = new GameAudioProjectRenderer();

            GameAudioRenderResult first = renderer.Render(project);
            GameAudioRenderResult second = renderer.Render(project);

            Assert.That(second.SampleRate, Is.EqualTo(first.SampleRate));
            Assert.That(second.ChannelCount, Is.EqualTo(first.ChannelCount));
            Assert.That(second.FrameCount, Is.EqualTo(first.FrameCount));
            Assert.That(second.Samples, Is.EqualTo(first.Samples));
        }

        [Test]
        public void Render_SupportsAllRequiredWaveforms()
        {
            var renderer = new GameAudioProjectRenderer();

            foreach (GameAudioWaveformType waveform in Enum.GetValues(typeof(GameAudioWaveformType)))
            {
                GameAudioProject project = CreateSingleWaveformProject(waveform);
                GameAudioRenderResult result = renderer.Render(project, new GameAudioRenderSettings { ExtendTail = false });

                Assert.That(result.FrameCount, Is.GreaterThan(0), waveform.ToString());
                Assert.That(result.Samples.Any(sample => Math.Abs(sample) > 0.0001f), Is.True, waveform.ToString());
            }
        }

        [Test]
        public void Render_ExtendsTailForReleaseAndDelayAndMixesMultipleTracks()
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Name = "TailAndMix";
            project.TotalBars = 1;
            project.SampleRate = 48000;
            project.ChannelMode = GameAudioChannelMode.Stereo;
            project.Tracks[0].Id = "track-tail";
            project.Tracks[0].Name = "Tail";
            project.Tracks[0].Pan = -0.2f;
            project.Tracks[0].DefaultVoice.Effect.Delay.Enabled = true;
            project.Tracks[0].DefaultVoice.Effect.Delay.TimeMs = 180;
            project.Tracks[0].DefaultVoice.Effect.Delay.Feedback = 0.4f;
            project.Tracks[0].DefaultVoice.Effect.Delay.Mix = 0.3f;
            project.Tracks[0].DefaultVoice.Adsr.ReleaseMs = 240;
            project.Tracks[0].Notes.Add(new GameAudioNote
            {
                Id = "tail-note",
                StartBeat = 3.5f,
                DurationBeat = 0.5f,
                MidiNote = 60,
                Velocity = 0.8f
            });

            project.Tracks.Add(new GameAudioTrack
            {
                Id = "track-chord",
                Name = "Chord",
                DefaultVoice = new GameAudioVoiceSettings
                {
                    Waveform = GameAudioWaveformType.Triangle,
                    Adsr = new GameAudioEnvelopeSettings
                    {
                        AttackMs = 5,
                        DecayMs = 80,
                        Sustain = 0.7f,
                        ReleaseMs = 120
                    },
                    Effect = new GameAudioEffectSettings
                    {
                        Delay = new GameAudioDelaySettings(),
                        Pan = 0.25f
                    }
                },
                Notes =
                {
                    new GameAudioNote { Id = "c1", StartBeat = 0.0f, DurationBeat = 1.0f, MidiNote = 48, Velocity = 0.7f },
                    new GameAudioNote { Id = "c2", StartBeat = 0.0f, DurationBeat = 1.0f, MidiNote = 52, Velocity = 0.7f },
                    new GameAudioNote { Id = "c3", StartBeat = 0.0f, DurationBeat = 1.0f, MidiNote = 55, Velocity = 0.7f }
                }
            });

            var renderer = new GameAudioProjectRenderer();
            GameAudioRenderResult result = renderer.Render(project);

            Assert.That(result.ChannelCount, Is.EqualTo(2));
            Assert.That(result.FrameCount, Is.GreaterThan(result.ProjectFrameCount));
            Assert.That(result.PeakAmplitude, Is.GreaterThan(0.0f));
            Assert.That(HasNonZeroSampleAfter(result, result.ProjectFrameCount), Is.True);
        }

        [Test]
        public void Render_UsesExplicitExportSecondsShorterThanProjectBars()
        {
            GameAudioProject project = CreateShortExportProject();
            project.ExportSettings = new GameAudioExportSettings
            {
                DurationMode = GameAudioExportDurationMode.Seconds,
                DurationSeconds = 0.25f,
                IncludeTail = false
            };

            GameAudioRenderResult result = new GameAudioProjectRenderer().Render(project);

            Assert.That(result.DurationMode, Is.EqualTo(GameAudioExportDurationMode.Seconds));
            Assert.That(result.IncludeTail, Is.False);
            Assert.That(result.TargetFrameCount, Is.EqualTo(12000));
            Assert.That(result.FrameCount, Is.EqualTo(result.TargetFrameCount));
            Assert.That(result.ProjectFrameCount, Is.GreaterThan(result.FrameCount));
            Assert.That(result.Samples.Any(sample => Math.Abs(sample) > 0.0001f), Is.True);
        }

        [Test]
        public void Render_IncludeTailExtendsReleasePastExplicitTarget()
        {
            GameAudioProject project = CreateShortExportProject();
            project.ExportSettings = new GameAudioExportSettings
            {
                DurationMode = GameAudioExportDurationMode.Seconds,
                DurationSeconds = 0.125f,
                IncludeTail = true
            };
            project.Tracks[0].DefaultVoice.Adsr.ReleaseMs = 300;
            project.Tracks[0].Notes[0].DurationBeat = 0.125f;

            GameAudioRenderResult result = new GameAudioProjectRenderer().Render(project);

            Assert.That(result.TargetFrameCount, Is.EqualTo(6000));
            Assert.That(result.FrameCount, Is.GreaterThan(result.TargetFrameCount));
            Assert.That(HasNonZeroSampleAfter(result, result.TargetFrameCount), Is.True);
        }

        [Test]
        public void Render_AutoTrimEndsAtLastNoteBodyBeforeProjectBars()
        {
            GameAudioProject project = CreateShortExportProject();
            project.ExportSettings = new GameAudioExportSettings
            {
                DurationMode = GameAudioExportDurationMode.AutoTrim,
                IncludeTail = false
            };
            project.Tracks[0].Notes[0].DurationBeat = 0.5f;

            GameAudioRenderResult result = new GameAudioProjectRenderer().Render(project);

            Assert.That(result.DurationMode, Is.EqualTo(GameAudioExportDurationMode.AutoTrim));
            Assert.That(result.TargetFrameCount, Is.EqualTo(12000));
            Assert.That(result.FrameCount, Is.EqualTo(result.TargetFrameCount));
            Assert.That(result.ProjectFrameCount, Is.GreaterThan(result.FrameCount));
        }

        [Test]
        public void Render_StereoSpreadProducesDifferentLeftAndRightSamples()
        {
            GameAudioProject project = CreateStereoSpreadProject(GameAudioChannelMode.Stereo);

            GameAudioRenderResult result = new GameAudioProjectRenderer().Render(project);

            Assert.That(result.ChannelCount, Is.EqualTo(2));
            Assert.That(HasDifferentLeftAndRightSamples(result), Is.True);
        }

        [Test]
        public void Render_MonoOutputIgnoresStereoSpreadTailExtension()
        {
            GameAudioProject project = CreateStereoSpreadProject(GameAudioChannelMode.Mono);
            project.ExportSettings = new GameAudioExportSettings
            {
                DurationMode = GameAudioExportDurationMode.Seconds,
                DurationSeconds = 0.25f,
                IncludeTail = true
            };

            GameAudioRenderResult result = new GameAudioProjectRenderer().Render(project);

            Assert.That(result.ChannelCount, Is.EqualTo(1));
            Assert.That(result.FrameCount, Is.EqualTo(result.TargetFrameCount));
        }

        [Test]
        public void Render_ClampsUnsafeValuesAndSupportsMonoOutput()
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Name = "Unsafe";
            project.SampleRate = 32000;
            project.ChannelMode = GameAudioChannelMode.Mono;
            project.MasterGainDb = 18.0f;
            project.Tracks[0].VolumeDb = 24.0f;
            project.Tracks[0].Pan = 1.0f;
            project.Tracks[0].DefaultVoice.Waveform = GameAudioWaveformType.Pulse;
            project.Tracks[0].DefaultVoice.PulseWidth = 2.0f;
            project.Tracks[0].DefaultVoice.NoiseEnabled = true;
            project.Tracks[0].DefaultVoice.NoiseMix = 5.0f;
            project.Tracks[0].DefaultVoice.Adsr.AttackMs = 4000;
            project.Tracks[0].DefaultVoice.Adsr.DecayMs = 4000;
            project.Tracks[0].DefaultVoice.Adsr.Sustain = 2.0f;
            project.Tracks[0].DefaultVoice.Effect.VolumeDb = 24.0f;
            project.Tracks[0].DefaultVoice.Effect.Pan = -1.0f;
            project.Tracks[0].DefaultVoice.Effect.PitchSemitone = 99.0f;
            project.Tracks[0].DefaultVoice.Effect.FadeInMs = 4000;
            project.Tracks[0].DefaultVoice.Effect.FadeOutMs = 4000;
            project.Tracks[0].DefaultVoice.Effect.Delay.Enabled = true;
            project.Tracks[0].DefaultVoice.Effect.Delay.TimeMs = 1;
            project.Tracks[0].DefaultVoice.Effect.Delay.Feedback = 3.0f;
            project.Tracks[0].DefaultVoice.Effect.Delay.Mix = 3.0f;
            project.Tracks[0].Notes.Clear();
            project.Tracks[0].Notes.Add(new GameAudioNote
            {
                Id = "unsafe-note",
                StartBeat = 0.0f,
                DurationBeat = 0.25f,
                MidiNote = 200,
                Velocity = 5.0f
            });

            var renderer = new GameAudioProjectRenderer();
            GameAudioRenderResult result = renderer.Render(project);

            Assert.That(result.SampleRate, Is.EqualTo(48000));
            Assert.That(result.ChannelCount, Is.EqualTo(1));
            Assert.That(result.Samples.All(sample => !float.IsNaN(sample) && !float.IsInfinity(sample)), Is.True);
            Assert.That(result.Samples.All(sample => sample >= -1.0f && sample <= 1.0f), Is.True);
        }

        private static GameAudioProject CreateNoiseProject()
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Name = "Deterministic";
            project.Tracks[0].Id = "track-noise";
            project.Tracks[0].DefaultVoice.Waveform = GameAudioWaveformType.Square;
            project.Tracks[0].DefaultVoice.NoiseEnabled = true;
            project.Tracks[0].DefaultVoice.NoiseMix = 0.5f;
            project.Tracks[0].Notes.Add(new GameAudioNote
            {
                Id = "noise-note",
                StartBeat = 0.0f,
                DurationBeat = 0.5f,
                MidiNote = 72,
                Velocity = 0.8f
            });

            return project;
        }

        private static GameAudioProject CreateShortExportProject()
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Name = "ShortExport";
            project.Bpm = 120;
            project.TotalBars = 4;
            project.SampleRate = 48000;
            project.Tracks[0].DefaultVoice.Adsr.AttackMs = 0;
            project.Tracks[0].DefaultVoice.Adsr.DecayMs = 0;
            project.Tracks[0].DefaultVoice.Adsr.Sustain = 1.0f;
            project.Tracks[0].DefaultVoice.Adsr.ReleaseMs = 0;
            project.Tracks[0].DefaultVoice.Effect.Delay.Enabled = false;
            project.Tracks[0].Notes.Clear();
            project.Tracks[0].Notes.Add(new GameAudioNote
            {
                Id = "short-note",
                StartBeat = 0.0f,
                DurationBeat = 1.0f,
                MidiNote = 69,
                Velocity = 1.0f
            });

            return project;
        }

        private static GameAudioProject CreateStereoSpreadProject(GameAudioChannelMode channelMode)
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Name = "StereoSpread";
            project.Bpm = 120;
            project.TotalBars = 1;
            project.SampleRate = 48000;
            project.ChannelMode = channelMode;
            project.Tracks[0].DefaultVoice.Waveform = GameAudioWaveformType.Sine;
            project.Tracks[0].DefaultVoice.Adsr.AttackMs = 0;
            project.Tracks[0].DefaultVoice.Adsr.DecayMs = 0;
            project.Tracks[0].DefaultVoice.Adsr.Sustain = 1.0f;
            project.Tracks[0].DefaultVoice.Adsr.ReleaseMs = 0;
            project.Tracks[0].DefaultVoice.Effect.StereoDetuneSemitone = 4.0f;
            project.Tracks[0].DefaultVoice.Effect.StereoDelayMs = 40;
            project.Tracks[0].DefaultVoice.Effect.Delay.Enabled = false;
            project.Tracks[0].Notes.Clear();
            project.Tracks[0].Notes.Add(new GameAudioNote
            {
                Id = "spread-note",
                StartBeat = 0.0f,
                DurationBeat = 0.5f,
                MidiNote = 69,
                Velocity = 1.0f
            });

            return project;
        }

        private static GameAudioProject CreateSingleWaveformProject(GameAudioWaveformType waveform)
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Name = waveform.ToString();
            project.Tracks[0].DefaultVoice.Waveform = waveform;
            project.Tracks[0].DefaultVoice.NoiseEnabled = false;
            project.Tracks[0].Notes.Clear();
            project.Tracks[0].Notes.Add(new GameAudioNote
            {
                Id = $"note-{waveform}",
                StartBeat = 0.0f,
                DurationBeat = 0.25f,
                MidiNote = 69,
                Velocity = 1.0f
            });

            return project;
        }

        private static bool HasNonZeroSampleAfter(GameAudioRenderResult result, int frameIndex)
        {
            int start = Math.Max(0, frameIndex * result.ChannelCount);
            for (int index = start; index < result.Samples.Length; index++)
            {
                if (Math.Abs(result.Samples[index]) > 0.0001f)
                {
                    return true;
                }
            }

            return false;
        }

        private static bool HasDifferentLeftAndRightSamples(GameAudioRenderResult result)
        {
            if (result.ChannelCount != 2)
            {
                return false;
            }

            for (int frameIndex = 0; frameIndex < result.FrameCount; frameIndex++)
            {
                int offset = frameIndex * 2;
                if (Math.Abs(result.Samples[offset] - result.Samples[offset + 1]) > 0.0001f)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
