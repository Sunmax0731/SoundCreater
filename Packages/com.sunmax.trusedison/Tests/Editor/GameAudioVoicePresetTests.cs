using System.Linq;
using NUnit.Framework;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Presets;

namespace TorusEdison.Editor.Tests
{
    public sealed class GameAudioVoicePresetTests
    {
        [Test]
        public void BuiltInPresets_HaveStableIdsAndVoices()
        {
            var presets = GameAudioVoicePresetLibrary.BuiltInPresets;

            Assert.That(presets.Count, Is.GreaterThanOrEqualTo(5));
            Assert.That(presets.Select(preset => preset.Id).Distinct().Count(), Is.EqualTo(presets.Count));
            Assert.That(presets.All(preset => preset.Voice != null), Is.True);
            Assert.That(GameAudioVoicePresetLibrary.PresetKind, Is.EqualTo("torusEdison.voicePreset"));
            Assert.That(GameAudioVoicePresetLibrary.PresetFileExtension, Is.EqualTo(".gats-preset.json"));
        }

        [Test]
        public void ApplyPresetToNoteVoiceOverride_IsUndoable()
        {
            GameAudioVoicePreset preset = GameAudioVoicePresetLibrary.BuiltInPresets.First(item => item.Id == "laser-shot");
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Tracks[0].Notes.Add(new GameAudioNote
            {
                Id = "note-a",
                StartBeat = 0.0f,
                DurationBeat = 1.0f,
                MidiNote = 60,
                Velocity = 0.8f
            });

            var session = new GameAudioEditorSession(project);
            session.Execute(GameAudioProjectCommandFactory.ChangeNotes(
                session.CurrentProject,
                new[] { "note-a" },
                "Apply Voice Preset",
                note => note.VoiceOverride = GameAudioVoicePresetLibrary.CloneVoice(preset.Voice)));

            Assert.That(session.CurrentProject.Tracks[0].Notes[0].VoiceOverride, Is.Not.Null);
            Assert.That(session.CurrentProject.Tracks[0].Notes[0].VoiceOverride.Waveform, Is.EqualTo(preset.Voice.Waveform));
            Assert.That(session.CurrentProject.Tracks[0].Notes[0].VoiceOverride.Effect.Delay.Enabled, Is.EqualTo(preset.Voice.Effect.Delay.Enabled));

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].Notes[0].VoiceOverride, Is.Null);

            Assert.That(session.Redo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].Notes[0].VoiceOverride.Waveform, Is.EqualTo(preset.Voice.Waveform));
        }

        [Test]
        public void ApplyPresetToTrackDefaultVoice_IsUndoable()
        {
            GameAudioVoicePreset preset = GameAudioVoicePresetLibrary.BuiltInPresets.First(item => item.Id == "noise-hit");
            var session = new GameAudioEditorSession(GameAudioProjectFactory.CreateDefaultProject());
            string trackId = session.CurrentProject.Tracks[0].Id;
            GameAudioWaveformType originalWaveform = session.CurrentProject.Tracks[0].DefaultVoice.Waveform;

            session.Execute(GameAudioProjectCommandFactory.ChangeTracks(
                session.CurrentProject,
                new[] { trackId },
                "Apply Voice Preset",
                track => track.DefaultVoice = GameAudioVoicePresetLibrary.CloneVoice(preset.Voice)));

            Assert.That(session.CurrentProject.Tracks[0].DefaultVoice.Waveform, Is.EqualTo(preset.Voice.Waveform));
            Assert.That(session.CurrentProject.Tracks[0].DefaultVoice.NoiseEnabled, Is.True);

            Assert.That(session.Undo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].DefaultVoice.Waveform, Is.EqualTo(originalWaveform));

            Assert.That(session.Redo(), Is.True);
            Assert.That(session.CurrentProject.Tracks[0].DefaultVoice.NoiseMix, Is.EqualTo(preset.Voice.NoiseMix));
        }
    }
}
