using NUnit.Framework;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Persistence;
using TorusEdison.Editor.Utilities;
using System.IO;

namespace TorusEdison.Editor.Tests
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
        public void SerializeAndDeserialize_RoundTripsImportedAudioConversionMetadata()
        {
            var serializer = new GameAudioProjectSerializer();
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject();
            project.Name = "ImportedConversion";
            project.ImportedAudioConversion = new GameAudioImportedAudioConversion
            {
                SourceClipName = "voice-source",
                SourceAssetPath = "Assets/Audio/voice-source.wav",
                SourceSampleRate = 44100,
                SourceChannelCount = 2,
                SourceDurationSeconds = 3.5f,
                TargetSampleRate = 11025,
                TargetChannelMode = "Mono",
                OutputChannelCount = 1,
                OutputWaveFileName = "voice-source_8bit.wav"
            };

            string json = serializer.Serialize(project);
            GameAudioProjectLoadResult result = serializer.Deserialize(json);

            Assert.That(result.Project.ImportedAudioConversion, Is.Not.Null);
            Assert.That(result.Project.ImportedAudioConversion.SourceClipName, Is.EqualTo("voice-source"));
            Assert.That(result.Project.ImportedAudioConversion.SourceAssetPath, Is.EqualTo("Assets/Audio/voice-source.wav"));
            Assert.That(result.Project.ImportedAudioConversion.TargetSampleRate, Is.EqualTo(11025));
            Assert.That(result.Project.ImportedAudioConversion.TargetChannelMode, Is.EqualTo("Mono"));
            Assert.That(result.Project.ImportedAudioConversion.OutputWaveFileName, Is.EqualTo("voice-source_8bit.wav"));
        }

        [Test]
        public void Deserialize_RejectsUnsupportedFormatVersion()
        {
            const string json = @"{
  ""formatVersion"": ""2.0.0"",
  ""toolVersion"": ""0.2.0"",
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
  ""toolVersion"": ""0.2.0"",
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
        public void Deserialize_AllowsSameMajorCompatibilityJsonWithFallbackWarnings()
        {
            const string json = @"{
  ""formatVersion"": ""1.0.0"",
  ""project"": {
    ""name"": ""Compat"",
    ""futureProjectField"": ""ignored"",
    ""tracks"": [
      {
        ""id"": ""track-001"",
        ""name"": ""Lead"",
        ""notes"": [
          {
            ""id"": ""note-001"",
            ""startBeat"": 0.0,
            ""durationBeat"": 1.0,
            ""midiNote"": 60,
            ""velocity"": 0.8,
            ""voiceOverride"": {
              ""waveform"": ""Triangle"",
              ""effect"": {}
            }
          }
        ]
      }
    ]
  }
}";

            var serializer = new GameAudioProjectSerializer();
            GameAudioProjectLoadResult result = serializer.Deserialize(json);

            Assert.That(result.Project.Name, Is.EqualTo("Compat"));
            Assert.That(result.Project.SampleRate, Is.EqualTo(GameAudioToolInfo.DefaultSampleRate));
            Assert.That(result.Project.ChannelMode, Is.EqualTo(GameAudioChannelMode.Stereo));
            Assert.That(result.Project.MasterGainDb, Is.EqualTo(0.0f));
            Assert.That(result.Project.LoopPlayback, Is.False);
            Assert.That(result.Project.Tracks[0].DefaultVoice, Is.Not.Null);
            Assert.That(result.Project.Tracks[0].Notes[0].VoiceOverride.Waveform, Is.EqualTo(GameAudioWaveformType.Triangle));
            Assert.That(result.Project.Tracks[0].Notes[0].VoiceOverride.Effect.Delay.TimeMs, Is.EqualTo(180));
            Assert.That(result.Warnings, Has.Some.EqualTo("track[1].defaultVoice missing; default voice was applied."));
            Assert.That(result.Warnings, Has.Some.EqualTo("track[1].notes[0].voiceOverride.effect.delay missing; default delay was applied."));
        }

        [Test]
        public void Deserialize_RejectsPresentOptionalFieldWithWrongType()
        {
            const string json = @"{
  ""formatVersion"": ""1.0.0"",
  ""project"": {
    ""name"": ""Compat"",
    ""masterGainDb"": ""loud"",
    ""tracks"": []
  }
}";

            var serializer = new GameAudioProjectSerializer();
            GameAudioPersistenceException exception = Assert.Throws<GameAudioPersistenceException>(() => serializer.Deserialize(json));

            StringAssert.Contains("$.project.masterGainDb must be a number.", exception.Message);
        }

        [Test]
        public void Deserialize_ClampsOutOfRangeValues_AndSortsNotes()
        {
            const string json = @"{
  ""formatVersion"": ""1.0.0"",
  ""toolVersion"": ""0.2.0"",
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
        public void Deserialize_RejectsUndefinedNumericEnumStrings_AndWarns()
        {
            var serializer = new GameAudioProjectSerializer();
            string json = serializer.Serialize(GameAudioProjectFactory.CreateDefaultProject())
                .Replace(@"""channelMode"": ""Stereo""", @"""channelMode"": ""999""")
                .Replace(@"""waveform"": ""Square""", @"""waveform"": ""999""")
                .Replace(@"""noiseType"": ""White""", @"""noiseType"": ""999""");

            GameAudioProjectLoadResult result = serializer.Deserialize(json);

            Assert.That(result.Project.ChannelMode, Is.EqualTo(GameAudioChannelMode.Stereo));
            Assert.That(result.Project.Tracks[0].DefaultVoice.Waveform, Is.EqualTo(GameAudioWaveformType.Square));
            Assert.That(result.Project.Tracks[0].DefaultVoice.NoiseType, Is.EqualTo(GameAudioNoiseType.White));
            Assert.That(result.Warnings, Has.Some.EqualTo("project.channelMode was unknown; defaulted to Stereo."));
            Assert.That(result.Warnings, Has.Some.EqualTo("track[1].defaultVoice.waveform was unknown; defaulted to Square."));
            Assert.That(result.Warnings, Has.Some.EqualTo("track[1].defaultVoice.noiseType was unknown; defaulted to White."));
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
  ""toolVersion"": ""0.2.0"",
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
        public void Deserialize_ClampsTotalBarsAboveMaximum()
        {
            const string json = @"{
  ""formatVersion"": ""1.0.0"",
  ""toolVersion"": ""0.2.0"",
  ""project"": {
    ""id"": ""proj-001"",
    ""name"": ""LongForm"",
    ""bpm"": 120,
    ""timeSignature"": { ""numerator"": 4, ""denominator"": 4 },
    ""totalBars"": 256,
    ""sampleRate"": 48000,
    ""channelMode"": ""Stereo"",
    ""masterGainDb"": 0.0,
    ""loopPlayback"": false,
    ""tracks"": []
  }
}";

            var serializer = new GameAudioProjectSerializer();
            GameAudioProjectLoadResult result = serializer.Deserialize(json);

            Assert.That(result.Project.TotalBars, Is.EqualTo(GameAudioToolInfo.MaxTotalBars));
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
