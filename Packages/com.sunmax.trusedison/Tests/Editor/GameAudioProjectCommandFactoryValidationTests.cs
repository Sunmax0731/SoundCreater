using System.Linq;
using NUnit.Framework;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioProjectCommandFactoryValidationTests
    {
        [Test]
        public void ChangeNotes_ClampsInspectorEditableNoteValues()
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Tracks[0].Notes.Add(new GameAudioNote
            {
                Id = "note-a",
                StartBeat = 1.0f,
                DurationBeat = 1.0f,
                MidiNote = 60,
                Velocity = 0.8f
            });

            var session = new GameAudioEditorSession(project);
            session.Execute(GameAudioProjectCommandFactory.ChangeNotes(
                session.CurrentProject,
                new[] { "note-a" },
                "Edit Note",
                note =>
                {
                    note.StartBeat = -3.0f;
                    note.DurationBeat = 0.0f;
                    note.MidiNote = 999;
                    note.Velocity = -1.0f;
                    note.VoiceOverride = new GameAudioVoiceSettings
                    {
                        PulseWidth = 2.0f,
                        NoiseMix = 3.0f,
                        Adsr = new GameAudioEnvelopeSettings
                        {
                            AttackMs = -1,
                            DecayMs = 99999,
                            Sustain = 2.0f,
                            ReleaseMs = -5
                        },
                        Effect = new GameAudioEffectSettings
                        {
                            VolumeDb = 99.0f,
                            Pan = -5.0f,
                            PitchSemitone = 99.0f,
                            StereoDetuneSemitone = 99.0f,
                            StereoDelayMs = 9000,
                            FadeInMs = -1,
                            FadeOutMs = 99999,
                            Delay = new GameAudioDelaySettings
                            {
                                TimeMs = 1,
                                Feedback = 5.0f,
                                Mix = -1.0f
                            }
                        }
                    };
                }));

            GameAudioNote note = session.CurrentProject.Tracks[0].Notes.Single();
            Assert.That(note.StartBeat, Is.EqualTo(0.0f));
            Assert.That(note.DurationBeat, Is.EqualTo(GameAudioToolInfo.MinNoteDurationBeat));
            Assert.That(note.MidiNote, Is.EqualTo(127));
            Assert.That(note.Velocity, Is.EqualTo(0.0f));
            Assert.That(note.VoiceOverride, Is.Not.Null);
            Assert.That(note.VoiceOverride.PulseWidth, Is.EqualTo(0.9f));
            Assert.That(note.VoiceOverride.NoiseMix, Is.EqualTo(1.0f));
            Assert.That(note.VoiceOverride.Adsr.AttackMs, Is.EqualTo(0));
            Assert.That(note.VoiceOverride.Adsr.DecayMs, Is.EqualTo(5000));
            Assert.That(note.VoiceOverride.Adsr.Sustain, Is.EqualTo(1.0f));
            Assert.That(note.VoiceOverride.Adsr.ReleaseMs, Is.EqualTo(0));
            Assert.That(note.VoiceOverride.Effect.VolumeDb, Is.EqualTo(6.0f));
            Assert.That(note.VoiceOverride.Effect.Pan, Is.EqualTo(-1.0f));
            Assert.That(note.VoiceOverride.Effect.PitchSemitone, Is.EqualTo(24.0f));
            Assert.That(note.VoiceOverride.Effect.StereoDetuneSemitone, Is.EqualTo(12.0f));
            Assert.That(note.VoiceOverride.Effect.StereoDelayMs, Is.EqualTo(1000));
            Assert.That(note.VoiceOverride.Effect.FadeInMs, Is.EqualTo(0));
            Assert.That(note.VoiceOverride.Effect.FadeOutMs, Is.EqualTo(3000));
            Assert.That(note.VoiceOverride.Effect.Delay.TimeMs, Is.EqualTo(20));
            Assert.That(note.VoiceOverride.Effect.Delay.Feedback, Is.EqualTo(0.7f));
            Assert.That(note.VoiceOverride.Effect.Delay.Mix, Is.EqualTo(0.0f));
        }

        [Test]
        public void ChangeTracks_ClampsTrackInspectorValuesAndKeepsVoiceSettingsValid()
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            string trackId = project.Tracks[0].Id;
            var session = new GameAudioEditorSession(project);

            session.Execute(GameAudioProjectCommandFactory.ChangeTracks(
                session.CurrentProject,
                new[] { trackId },
                "Edit Track",
                track =>
                {
                    track.Mute = true;
                    track.Solo = true;
                    track.VolumeDb = 12.0f;
                    track.Pan = -4.0f;
                    track.DefaultVoice = new GameAudioVoiceSettings
                    {
                        PulseWidth = 0.01f,
                        NoiseMix = 2.0f,
                        Effect = new GameAudioEffectSettings
                        {
                            PitchSemitone = -99.0f
                        }
                    };
                }));

            GameAudioTrack track = session.CurrentProject.Tracks.Single();
            Assert.That(track.Mute, Is.True);
            Assert.That(track.Solo, Is.True);
            Assert.That(track.VolumeDb, Is.EqualTo(6.0f));
            Assert.That(track.Pan, Is.EqualTo(-1.0f));
            Assert.That(track.DefaultVoice, Is.Not.Null);
            Assert.That(track.DefaultVoice.PulseWidth, Is.EqualTo(0.1f));
            Assert.That(track.DefaultVoice.NoiseMix, Is.EqualTo(1.0f));
            Assert.That(track.DefaultVoice.Effect.PitchSemitone, Is.EqualTo(-24.0f));
        }

        [Test]
        public void ChangeProject_ClampsProjectInspectorValues()
        {
            var session = new GameAudioEditorSession(GameAudioProjectFactory.CreateDefaultProject());

            session.Execute(GameAudioProjectCommandFactory.ChangeProject(
                session.CurrentProject,
                "Edit Project",
                project =>
                {
                    project.Name = " ";
                    project.Bpm = 0;
                    project.TotalBars = 999;
                    project.SampleRate = 12345;
                    project.ChannelMode = (GameAudioChannelMode)999;
                    project.MasterGainDb = 24.0f;
                    project.TimeSignature = new GameAudioTimeSignature { Numerator = 5, Denominator = 4 };
                    project.LoopPlayback = true;
                    project.ExportSettings = new GameAudioExportSettings
                    {
                        DurationMode = (GameAudioExportDurationMode)999,
                        DurationSeconds = -1.0f,
                        IncludeTail = false
                    };
                }));

            GameAudioProject project = session.CurrentProject;
            Assert.That(project.Name, Is.EqualTo("New Audio Project"));
            Assert.That(project.Bpm, Is.EqualTo(1));
            Assert.That(project.TotalBars, Is.EqualTo(GameAudioToolInfo.MaxTotalBars));
            Assert.That(project.SampleRate, Is.EqualTo(GameAudioToolInfo.DefaultSampleRate));
            Assert.That(project.ChannelMode, Is.EqualTo(GameAudioChannelMode.Stereo));
            Assert.That(project.MasterGainDb, Is.EqualTo(6.0f));
            Assert.That(project.TimeSignature.Numerator, Is.EqualTo(4));
            Assert.That(project.TimeSignature.Denominator, Is.EqualTo(4));
            Assert.That(project.LoopPlayback, Is.True);
            Assert.That(project.ExportSettings.DurationMode, Is.EqualTo(GameAudioExportDurationMode.ProjectBars));
            Assert.That(project.ExportSettings.DurationSeconds, Is.EqualTo(GameAudioToolInfo.DefaultExportDurationSeconds));
            Assert.That(project.ExportSettings.IncludeTail, Is.False);
        }
    }
}
