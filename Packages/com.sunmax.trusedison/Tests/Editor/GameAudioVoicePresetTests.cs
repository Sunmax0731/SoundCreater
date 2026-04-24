using System.Linq;
using NUnit.Framework;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Persistence;
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

        [Test]
        public void PresetFileSerializer_RoundTripsVoicePreset()
        {
            var serializer = new GameAudioVoicePresetFileSerializer();
            GameAudioVoiceSettings voice = GameAudioVoicePresetLibrary.CloneVoice(GameAudioVoicePresetLibrary.BuiltInPresets.First(item => item.Id == "laser-shot").Voice);
            voice.Effect.StereoDetuneSemitone = 2.0f;
            voice.Effect.StereoDelayMs = 35;
            GameAudioVoicePreset preset = GameAudioVoicePresetLibrary.CreateUserPreset("Wide Laser", "Shared preset", voice);

            string json = serializer.Serialize(preset);
            GameAudioVoicePresetLoadResult result = serializer.Deserialize(json);

            StringAssert.Contains(GameAudioVoicePresetLibrary.PresetKind, json);
            Assert.That(result.Preset.DisplayName, Is.EqualTo("Wide Laser"));
            Assert.That(result.Preset.Category, Is.EqualTo("User"));
            Assert.That(result.Preset.Voice.Waveform, Is.EqualTo(voice.Waveform));
            Assert.That(result.Preset.Voice.Effect.StereoDetuneSemitone, Is.EqualTo(2.0f));
            Assert.That(result.Preset.Voice.Effect.StereoDelayMs, Is.EqualTo(35));
        }

        [Test]
        public void PresetFileSerializer_RejectsUnsupportedPresetFormatVersion()
        {
            const string json = @"{
  ""kind"": ""torusEdison.voicePreset"",
  ""presetFormatVersion"": ""2.0.0"",
  ""toolVersion"": ""0.3.0"",
  ""preset"": {
    ""displayName"": ""Future"",
    ""voice"": {}
  }
}";

            var serializer = new GameAudioVoicePresetFileSerializer();

            Assert.Throws<GameAudioPersistenceException>(() => serializer.Deserialize(json));
        }

        [Test]
        public void PresetFileSerializer_RejectsWrongJsonTypes()
        {
            const string json = @"{
  ""kind"": ""torusEdison.voicePreset"",
  ""presetFormatVersion"": ""1.0.0"",
  ""toolVersion"": ""0.3.0"",
  ""preset"": {
    ""displayName"": 123,
    ""voice"": {}
  }
}";

            var serializer = new GameAudioVoicePresetFileSerializer();

            Assert.Throws<GameAudioPersistenceException>(() => serializer.Deserialize(json));
        }

        [Test]
        public void PresetFileSerializer_NormalizesPresetFilePath()
        {
            string normalized = GameAudioVoicePresetFileSerializer.NormalizePresetFilePath("D:/Audio/Wide Laser.json");
            string doubleExtensionNormalized = GameAudioVoicePresetFileSerializer.NormalizePresetFilePath("D:/Audio/Wide Laser.gats-preset.json.json");

            Assert.That(normalized.Replace('\\', '/'), Is.EqualTo("D:/Audio/Wide Laser.gats-preset.json"));
            Assert.That(doubleExtensionNormalized.Replace('\\', '/'), Is.EqualTo("D:/Audio/Wide Laser.gats-preset.json"));
        }
    }
}
