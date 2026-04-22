using NUnit.Framework;
using GameAudioTool.Editor.Application;
using GameAudioTool.Editor.Domain;
using GameAudioTool.Editor.Persistence;
using GameAudioTool.Editor.Utilities;
using System.IO;

namespace GameAudioTool.Editor.Tests
{
    public sealed class GameAudioProjectSerializerTests
    {
        [Test]
        public void SerializeAndDeserialize_RoundTripsCoreFields()
        {
            var serializer = new GameAudioProjectSerializer();
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Name = "RoundTrip";
            project.Bpm = 140;
            project.TotalBars = 12;
            project.ChannelMode = GameAudioChannelMode.Mono;
            project.Tracks[0].Notes.Add(new GameAudioNote
            {
                Id = "note-alpha",
                StartBeat = 2.0f,
                DurationBeat = 0.5f,
                MidiNote = 67,
                Velocity = 0.55f
            });

            string json = serializer.Serialize(project);
            GameAudioProjectLoadResult result = serializer.Deserialize(json);

            Assert.That(result.Project.Name, Is.EqualTo("RoundTrip"));
            Assert.That(result.Project.Bpm, Is.EqualTo(140));
            Assert.That(result.Project.TotalBars, Is.EqualTo(12));
            Assert.That(result.Project.ChannelMode, Is.EqualTo(GameAudioChannelMode.Mono));
            Assert.That(result.Project.Tracks.Count, Is.EqualTo(1));
            Assert.That(result.Project.Tracks[0].Notes.Count, Is.EqualTo(1));
            Assert.That(result.Project.Tracks[0].Notes[0].MidiNote, Is.EqualTo(67));
        }

        [Test]
        public void Deserialize_RejectsUnsupportedFormatVersion()
        {
            const string json = @"{
  ""formatVersion"": ""2.0.0"",
  ""toolVersion"": ""0.1.0"",
  ""project"": {
    ""id"": ""proj-001"",
    ""name"": ""Invalid"",
    ""bpm"": 120,
    ""timeSignature"": { ""numerator"": 4, ""denominator"": 4 },
    ""totalBars"": 4,
    ""sampleRate"": 48000,
    ""channelMode"": ""Stereo"",
    ""masterGainDb"": 0.0,
    ""loopPlayback"": false,
    ""tracks"": []
  }
}";

            var serializer = new GameAudioProjectSerializer();

            Assert.Throws<GameAudioPersistenceException>(() => serializer.Deserialize(json));
        }

        [Test]
        public void Deserialize_RejectsMissingRequiredField()
        {
            const string json = @"{
  ""formatVersion"": ""1.0.0"",
  ""toolVersion"": ""0.1.0"",
  ""project"": {
    ""id"": ""proj-001"",
    ""bpm"": 120,
    ""timeSignature"": { ""numerator"": 4, ""denominator"": 4 },
    ""totalBars"": 4,
    ""sampleRate"": 48000,
    ""channelMode"": ""Stereo"",
    ""masterGainDb"": 0.0,
    ""loopPlayback"": false,
    ""tracks"": []
  }
}";

            var serializer = new GameAudioProjectSerializer();
            GameAudioPersistenceException exception = Assert.Throws<GameAudioPersistenceException>(() => serializer.Deserialize(json));
            StringAssert.Contains("$.project.name is required.", exception.Message);
        }

        [Test]
        public void Deserialize_ClampsOutOfRangeValues_AndSortsNotes()
        {
            const string json = @"{
  ""formatVersion"": ""1.0.0"",
  ""toolVersion"": ""0.1.0"",
  ""project"": {
    ""id"": ""proj-001"",
    ""name"": ""Clamp"",
    ""bpm"": 0,
    ""timeSignature"": { ""numerator"": 5, ""denominator"": 4 },
    ""totalBars"": -2,
    ""sampleRate"": 32000,
    ""channelMode"": ""Unknown"",
    ""masterGainDb"": 40.0,
    ""loopPlayback"": false,
    ""tracks"": [
      {
        ""id"": ""track-001"",
        ""name"": """",
        ""mute"": false,
        ""solo"": false,
        ""volumeDb"": -99.0,
        ""pan"": 3.0,
        ""defaultVoice"": {
          ""waveform"": ""Bogus"",
          ""pulseWidth"": 2.0,
          ""noiseEnabled"": true,
          ""noiseType"": ""Invalid"",
          ""noiseMix"": 2.0,
          ""adsr"": { ""attackMs"": -1, ""decayMs"": 9000, ""sustain"": 2.0, ""releaseMs"": -5 },
          ""effect"": {
            ""volumeDb"": 9.0,
            ""pan"": -3.0,
            ""pitchSemitone"": 40.0,
            ""fadeInMs"": 4000,
            ""fadeOutMs"": -3,
            ""delay"": { ""enabled"": true, ""timeMs"": 2, ""feedback"": 5.0, ""mix"": -1.0 }
          }
        },
        ""notes"": [
          { ""id"": ""note-b"", ""startBeat"": 4.0, ""durationBeat"": 0.0, ""midiNote"": 300, ""velocity"": 3.0 },
          { ""id"": ""note-a"", ""startBeat"": 1.0, ""durationBeat"": 1.0, ""midiNote"": -2, ""velocity"": -1.0 }
        ]
      }
    ]
  }
}";

            var serializer = new GameAudioProjectSerializer();
            GameAudioProjectLoadResult result = serializer.Deserialize(json);

            Assert.That(result.Project.Bpm, Is.EqualTo(120));
            Assert.That(result.Project.TimeSignature.Numerator, Is.EqualTo(4));
            Assert.That(result.Project.TotalBars, Is.EqualTo(8));
            Assert.That(result.Project.SampleRate, Is.EqualTo(48000));
            Assert.That(result.Project.ChannelMode, Is.EqualTo(GameAudioChannelMode.Stereo));
            Assert.That(result.Project.MasterGainDb, Is.EqualTo(6.0f));
            Assert.That(result.Project.Tracks[0].VolumeDb, Is.EqualTo(-48.0f));
            Assert.That(result.Project.Tracks[0].Pan, Is.EqualTo(1.0f));
            Assert.That(result.Project.Tracks[0].Notes[0].Id, Is.EqualTo("note-a"));
            Assert.That(result.Project.Tracks[0].Notes[1].DurationBeat, Is.EqualTo(0.0625f));
            Assert.That(result.Warnings.Count, Is.GreaterThan(0));
        }

        [Test]
        public void Deserialize_RejectsTrackCountOverLimit()
        {
            string[] tracks = new string[33];
            for (int index = 0; index < tracks.Length; index++)
            {
                tracks[index] = @"{
          ""id"": ""track-" + index + @""",
          ""name"": ""Track"",
          ""mute"": false,
          ""solo"": false,
          ""volumeDb"": 0.0,
          ""pan"": 0.0,
          ""defaultVoice"": {
            ""waveform"": ""Square"",
            ""pulseWidth"": 0.5,
            ""noiseEnabled"": false,
            ""noiseType"": ""White"",
            ""noiseMix"": 0.0,
            ""adsr"": { ""attackMs"": 5, ""decayMs"": 80, ""sustain"": 0.7, ""releaseMs"": 120 },
            ""effect"": {
              ""volumeDb"": 0.0,
              ""pan"": 0.0,
              ""pitchSemitone"": 0.0,
              ""fadeInMs"": 0,
              ""fadeOutMs"": 0,
              ""delay"": { ""enabled"": false, ""timeMs"": 180, ""feedback"": 0.25, ""mix"": 0.2 }
            }
          },
          ""notes"": []
        }";
            }

            string json = @"{
  ""formatVersion"": ""1.0.0"",
  ""toolVersion"": ""0.1.0"",
  ""project"": {
    ""id"": ""proj-001"",
    ""name"": ""TooManyTracks"",
    ""bpm"": 120,
    ""timeSignature"": { ""numerator"": 4, ""denominator"": 4 },
    ""totalBars"": 4,
    ""sampleRate"": 48000,
    ""channelMode"": ""Stereo"",
    ""masterGainDb"": 0.0,
    ""loopPlayback"": false,
    ""tracks"": [" + string.Join(",", tracks) + @"]
  }
}";

            var serializer = new GameAudioProjectSerializer();
            Assert.Throws<GameAudioPersistenceException>(() => serializer.Deserialize(json));
        }

        [Test]
        public void SaveToFile_NormalizesSessionFileExtension()
        {
            var serializer = new GameAudioProjectSerializer();
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            string root = Path.Combine(Path.GetTempPath(), "GameAudioToolTests", Path.GetRandomFileName());
            string targetPath = Path.Combine(root, "session");
            string expectedPath = $"{targetPath}{GameAudioToolInfo.SessionFileExtension}";

            try
            {
                serializer.SaveToFile(targetPath, project);
                Assert.That(File.Exists(expectedPath), Is.True);
            }
            finally
            {
                if (Directory.Exists(root))
                {
                    Directory.Delete(root, true);
                }
            }
        }

        [Test]
        public void LoadFromFile_RejectsNonSessionFileExtension()
        {
            var serializer = new GameAudioProjectSerializer();
            string path = Path.Combine(Path.GetTempPath(), "project.json");

            GameAudioPersistenceException exception = Assert.Throws<GameAudioPersistenceException>(() => serializer.LoadFromFile(path));
            StringAssert.Contains(GameAudioToolInfo.SessionFileExtension, exception.Message);
        }
    }
}
