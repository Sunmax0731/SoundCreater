using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;
using UnityEngine;

namespace TorusEdison.Editor.Persistence
{
    public sealed class GameAudioProjectSerializer
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public string Serialize(GameAudioProject project)
        {
            if (project == null)
            {
                throw new ArgumentNullException(nameof(project));
            }

            var dto = new GameAudioProjectFileDto
            {
                formatVersion = "1.0.0",
                toolVersion = GameAudioToolInfo.ToolVersion,
                project = ToDto(project)
            };

            return JsonUtility.ToJson(dto, true) + "\n";
        }

        public void SaveToFile(string path, GameAudioProject project)
        {
            string resolvedPath = GameAudioProjectFileUtility.NormalizeSavePath(path);

            string directoryPath = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(resolvedPath, Serialize(project), Utf8WithoutBom);
            GameAudioDiagnosticLogger.Verbose("ProjectSerializer", $"Serialized project file to {resolvedPath}.");
        }

        public GameAudioProjectLoadResult LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A source path is required.", nameof(path));
            }

            GameAudioProjectFileUtility.EnsureSessionFileExtension(path);

            if (!File.Exists(path))
            {
                throw new GameAudioPersistenceException($"Project file was not found: {path}");
            }

            string json = File.ReadAllText(path, Utf8WithoutBom);
            GameAudioProjectLoadResult result = Deserialize(json);
            GameAudioDiagnosticLogger.Verbose("ProjectSerializer", $"Loaded project file {path} with {result.Warnings.Count} warning(s).");
            return result;
        }

        public GameAudioProjectLoadResult Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new GameAudioPersistenceException("Project JSON is empty.");
            }

            GameAudioProjectSchemaValidator.Validate(json);

            GameAudioProjectFileDto rootDto;
            try
            {
                rootDto = JsonUtility.FromJson<GameAudioProjectFileDto>(json);
            }
            catch (Exception exception)
            {
                throw new GameAudioPersistenceException("Failed to parse project JSON.", exception);
            }

            if (rootDto == null)
            {
                throw new GameAudioPersistenceException("Project JSON did not produce a document.");
            }

            ValidateFormatVersion(rootDto.formatVersion);

            if (rootDto.project == null)
            {
                throw new GameAudioPersistenceException("Project JSON does not contain a project object.");
            }

            var warnings = new List<string>();
            GameAudioProject project = FromDto(rootDto.project, warnings);
            return new GameAudioProjectLoadResult(project, warnings);
        }

        private static void ValidateFormatVersion(string formatVersion)
        {
            if (!GameAudioValidationUtility.TryGetFormatMajor(formatVersion, out int majorVersion))
            {
                throw new GameAudioPersistenceException("formatVersion is missing or invalid.");
            }

            if (majorVersion.ToString() != GameAudioToolInfo.SupportedFormatMajor)
            {
                throw new GameAudioPersistenceException($"Unsupported formatVersion: {formatVersion}");
            }
        }

        private static GameAudioProjectDto ToDto(GameAudioProject project)
        {
            if (project.Tracks != null && project.Tracks.Count > GameAudioToolInfo.MaxTrackCount)
            {
                throw new GameAudioPersistenceException($"Track count exceeds the supported maximum of {GameAudioToolInfo.MaxTrackCount}.");
            }

            return new GameAudioProjectDto
            {
                id = string.IsNullOrWhiteSpace(project.Id) ? Guid.NewGuid().ToString("N") : project.Id,
                name = string.IsNullOrWhiteSpace(project.Name) ? "New Audio Project" : project.Name,
                bpm = Math.Max(1, project.Bpm),
                timeSignature = new GameAudioTimeSignatureDto
                {
                    numerator = project.TimeSignature?.Numerator ?? 4,
                    denominator = project.TimeSignature?.Denominator ?? 4
                },
                totalBars = GameAudioValidationUtility.ClampInt(project.TotalBars, 1, GameAudioToolInfo.MaxTotalBars),
                sampleRate = NormalizeSerializedSampleRate(project),
                channelMode = project.ChannelMode.ToString(),
                masterGainDb = GameAudioValidationUtility.ClampFloat(project.MasterGainDb, -24.0f, 6.0f),
                loopPlayback = project.LoopPlayback,
                exportSettings = ToDto(project.ExportSettings),
                importedAudioConversion = project.ImportedAudioConversion == null ? null : ToDto(project.ImportedAudioConversion),
                tracks = (project.Tracks ?? new List<GameAudioTrack>())
                    .Select(ToDto)
                    .ToArray()
            };
        }

        private static GameAudioExportSettingsDto ToDto(GameAudioExportSettings settings)
        {
            GameAudioExportSettings actualSettings = settings ?? new GameAudioExportSettings();
            GameAudioExportDurationMode durationMode = Enum.IsDefined(typeof(GameAudioExportDurationMode), actualSettings.DurationMode)
                ? actualSettings.DurationMode
                : GameAudioExportDurationMode.ProjectBars;
            float durationSeconds = GameAudioValidationUtility.ClampFloat(
                actualSettings.DurationSeconds <= 0.0f ? GameAudioToolInfo.DefaultExportDurationSeconds : actualSettings.DurationSeconds,
                GameAudioToolInfo.MinExportDurationSeconds,
                GameAudioToolInfo.MaxExportDurationSeconds);

            return new GameAudioExportSettingsDto
            {
                durationMode = durationMode.ToString(),
                durationSeconds = durationSeconds,
                includeTail = actualSettings.IncludeTail
            };
        }

        private static GameAudioImportedAudioConversionDto ToDto(GameAudioImportedAudioConversion conversion)
        {
            return new GameAudioImportedAudioConversionDto
            {
                sourceClipName = conversion.SourceClipName ?? string.Empty,
                sourceAssetPath = conversion.SourceAssetPath ?? string.Empty,
                sourceSampleRate = Math.Max(0, conversion.SourceSampleRate),
                sourceChannelCount = Math.Max(0, conversion.SourceChannelCount),
                sourceDurationSeconds = Math.Max(0.0f, conversion.SourceDurationSeconds),
                targetSampleRate = Math.Max(0, conversion.TargetSampleRate),
                targetChannelMode = string.IsNullOrWhiteSpace(conversion.TargetChannelMode) ? "Preserve" : conversion.TargetChannelMode,
                outputChannelCount = Math.Max(0, conversion.OutputChannelCount),
                outputWaveFileName = conversion.OutputWaveFileName ?? string.Empty
            };
        }

        private static GameAudioTrackDto ToDto(GameAudioTrack track)
        {
            GameAudioVoiceSettings defaultVoice = track.DefaultVoice ?? GameAudioProjectFactory.CreateDefaultVoice();
            return new GameAudioTrackDto
            {
                id = string.IsNullOrWhiteSpace(track.Id) ? Guid.NewGuid().ToString("N") : track.Id,
                name = string.IsNullOrWhiteSpace(track.Name) ? "Track" : track.Name,
                mute = track.Mute,
                solo = track.Solo,
                volumeDb = GameAudioValidationUtility.ClampFloat(track.VolumeDb, -48.0f, 6.0f),
                pan = GameAudioValidationUtility.ClampFloat(track.Pan, -1.0f, 1.0f),
                defaultVoice = ToVoiceDto(defaultVoice),
                notes = (track.Notes ?? new List<GameAudioNote>())
                    .OrderBy(note => note.StartBeat)
                    .ThenBy(note => note.Id, StringComparer.Ordinal)
                    .Select(ToDto)
                    .ToArray()
            };
        }

        private static GameAudioNoteDto ToDto(GameAudioNote note)
        {
            return new GameAudioNoteDto
            {
                id = string.IsNullOrWhiteSpace(note.Id) ? Guid.NewGuid().ToString("N") : note.Id,
                startBeat = Math.Max(0.0f, note.StartBeat),
                durationBeat = Math.Max(GameAudioToolInfo.MinNoteDurationBeat, note.DurationBeat),
                midiNote = GameAudioValidationUtility.ClampInt(note.MidiNote, 0, 127),
                velocity = GameAudioValidationUtility.ClampFloat(note.Velocity, 0.0f, 1.0f),
                voiceOverride = note.VoiceOverride == null ? null : ToVoiceDto(note.VoiceOverride)
            };
        }

        internal static GameAudioVoiceDto ToVoiceDto(GameAudioVoiceSettings voice)
        {
            GameAudioEnvelopeSettings adsr = voice.Adsr ?? GameAudioProjectFactory.CreateDefaultEnvelope();
            GameAudioEffectSettings effect = voice.Effect ?? GameAudioProjectFactory.CreateDefaultEffect();
            GameAudioDelaySettings delay = effect.Delay ?? GameAudioProjectFactory.CreateDefaultDelay();

            return new GameAudioVoiceDto
            {
                waveform = voice.Waveform.ToString(),
                pulseWidth = GameAudioValidationUtility.ClampFloat(voice.PulseWidth, 0.10f, 0.90f),
                noiseEnabled = voice.NoiseEnabled,
                noiseType = voice.NoiseType.ToString(),
                noiseMix = GameAudioValidationUtility.ClampFloat(voice.NoiseMix, 0.0f, 1.0f),
                adsr = new GameAudioEnvelopeDto
                {
                    attackMs = GameAudioValidationUtility.ClampInt(adsr.AttackMs, 0, 5000),
                    decayMs = GameAudioValidationUtility.ClampInt(adsr.DecayMs, 0, 5000),
                    sustain = GameAudioValidationUtility.ClampFloat(adsr.Sustain, 0.0f, 1.0f),
                    releaseMs = GameAudioValidationUtility.ClampInt(adsr.ReleaseMs, 0, 5000)
                },
                effect = new GameAudioEffectDto
                {
                    volumeDb = GameAudioValidationUtility.ClampFloat(effect.VolumeDb, -48.0f, 6.0f),
                    pan = GameAudioValidationUtility.ClampFloat(effect.Pan, -1.0f, 1.0f),
                    pitchSemitone = GameAudioValidationUtility.ClampFloat(effect.PitchSemitone, -24.0f, 24.0f),
                    stereoDetuneSemitone = GameAudioValidationUtility.ClampFloat(effect.StereoDetuneSemitone, 0.0f, 12.0f),
                    stereoDelayMs = GameAudioValidationUtility.ClampInt(effect.StereoDelayMs, 0, 1000),
                    fadeInMs = GameAudioValidationUtility.ClampInt(effect.FadeInMs, 0, 3000),
                    fadeOutMs = GameAudioValidationUtility.ClampInt(effect.FadeOutMs, 0, 3000),
                    delay = new GameAudioDelayDto
                    {
                        enabled = delay.Enabled,
                        timeMs = GameAudioValidationUtility.ClampInt(delay.TimeMs, 20, 1000),
                        feedback = GameAudioValidationUtility.ClampFloat(delay.Feedback, 0.0f, 0.70f),
                        mix = GameAudioValidationUtility.ClampFloat(delay.Mix, 0.0f, 1.0f)
                    }
                }
            };
        }

        private static GameAudioProject FromDto(GameAudioProjectDto dto, List<string> warnings)
        {
            GameAudioTrackDto[] trackDtos = dto.tracks ?? Array.Empty<GameAudioTrackDto>();
            if (trackDtos.Length > GameAudioToolInfo.MaxTrackCount)
            {
                throw new GameAudioPersistenceException($"Track count exceeds the supported maximum of {GameAudioToolInfo.MaxTrackCount}.");
            }

            GameAudioImportedAudioConversion importedAudioConversion = ReadImportedAudioConversion(dto.importedAudioConversion, warnings);
            var project = new GameAudioProject
            {
                Id = ReadId(dto.id, "proj", warnings, "project"),
                Name = ReadRequiredText(dto.name, "New Audio Project", warnings, "project.name"),
                Bpm = ReadPositive(dto.bpm, GameAudioToolInfo.DefaultBpm, warnings, "project.bpm"),
                TimeSignature = ReadTimeSignature(dto.timeSignature, warnings),
                TotalBars = ReadTotalBars(dto.totalBars, warnings),
                SampleRate = ReadSampleRate(dto.sampleRate, warnings, "project.sampleRate", importedAudioConversion != null),
                ChannelMode = ReadChannelMode(dto.channelMode, GameAudioChannelMode.Stereo, warnings, "project.channelMode"),
                MasterGainDb = ReadClamped(dto.masterGainDb, -24.0f, 6.0f, 0.0f, warnings, "project.masterGainDb"),
                LoopPlayback = dto.loopPlayback,
                ExportSettings = ReadExportSettings(dto.exportSettings, warnings),
                ImportedAudioConversion = importedAudioConversion
            };

            for (int index = 0; index < trackDtos.Length; index++)
            {
                GameAudioTrackDto trackDto = trackDtos[index];
                project.Tracks.Add(FromDto(trackDto, index + 1, warnings));
            }

            return project;
        }

        private static GameAudioExportSettings ReadExportSettings(GameAudioExportSettingsDto dto, List<string> warnings)
        {
            if (dto == null)
            {
                return new GameAudioExportSettings();
            }

            GameAudioExportDurationMode durationMode = ReadEnum(
                dto.durationMode,
                GameAudioExportDurationMode.ProjectBars,
                warnings,
                "project.exportSettings.durationMode");
            float durationSeconds = ReadClamped(
                dto.durationSeconds,
                GameAudioToolInfo.MinExportDurationSeconds,
                GameAudioToolInfo.MaxExportDurationSeconds,
                GameAudioToolInfo.DefaultExportDurationSeconds,
                warnings,
                "project.exportSettings.durationSeconds");

            return new GameAudioExportSettings
            {
                DurationMode = durationMode,
                DurationSeconds = durationSeconds,
                IncludeTail = dto.includeTail
            };
        }

        private static GameAudioImportedAudioConversion ReadImportedAudioConversion(GameAudioImportedAudioConversionDto dto, List<string> warnings)
        {
            if (!HasImportedAudioConversionPayload(dto))
            {
                return null;
            }

            return new GameAudioImportedAudioConversion
            {
                SourceClipName = ReadRequiredText(dto.sourceClipName, string.Empty, warnings, "project.importedAudioConversion.sourceClipName"),
                SourceAssetPath = ReadRequiredText(dto.sourceAssetPath, string.Empty, warnings, "project.importedAudioConversion.sourceAssetPath"),
                SourceSampleRate = ReadPositive(dto.sourceSampleRate, 0, warnings, "project.importedAudioConversion.sourceSampleRate"),
                SourceChannelCount = ReadPositive(dto.sourceChannelCount, 0, warnings, "project.importedAudioConversion.sourceChannelCount"),
                SourceDurationSeconds = ReadClamped(dto.sourceDurationSeconds, 0.0f, 36000.0f, 0.0f, warnings, "project.importedAudioConversion.sourceDurationSeconds"),
                TargetSampleRate = ReadPositive(dto.targetSampleRate, 0, warnings, "project.importedAudioConversion.targetSampleRate"),
                TargetChannelMode = ReadRequiredText(dto.targetChannelMode, "Preserve", warnings, "project.importedAudioConversion.targetChannelMode"),
                OutputChannelCount = ReadPositive(dto.outputChannelCount, 0, warnings, "project.importedAudioConversion.outputChannelCount"),
                OutputWaveFileName = ReadRequiredText(dto.outputWaveFileName, string.Empty, warnings, "project.importedAudioConversion.outputWaveFileName")
            };
        }

        private static GameAudioTrack FromDto(GameAudioTrackDto dto, int trackIndex, List<string> warnings)
        {
            GameAudioTrack track = GameAudioProjectFactory.CreateDefaultTrack(trackIndex);
            track.Id = ReadId(dto?.id, "track", warnings, $"track[{trackIndex}].id");
            track.Name = ReadRequiredText(dto?.name, $"Track {trackIndex:00}", warnings, $"track[{trackIndex}].name");
            track.Mute = dto != null && dto.mute;
            track.Solo = dto != null && dto.solo;
            track.VolumeDb = dto == null
                ? 0.0f
                : ReadClamped(dto.volumeDb, -48.0f, 6.0f, 0.0f, warnings, $"track[{trackIndex}].volumeDb");
            track.Pan = dto == null
                ? 0.0f
                : ReadClamped(dto.pan, -1.0f, 1.0f, 0.0f, warnings, $"track[{trackIndex}].pan");
            track.DefaultVoice = !HasVoicePayload(dto?.defaultVoice)
                ? WithWarning(GameAudioProjectFactory.CreateDefaultVoice(), warnings, $"track[{trackIndex}].defaultVoice missing; default voice was applied.")
                : FromVoiceDto(dto.defaultVoice, warnings, $"track[{trackIndex}].defaultVoice");

            GameAudioNoteDto[] noteDtos = dto?.notes ?? Array.Empty<GameAudioNoteDto>();
            track.Notes = noteDtos
                .Select((noteDto, noteIndex) => FromDto(noteDto, warnings, $"track[{trackIndex}].notes[{noteIndex}]"))
                .OrderBy(note => note.StartBeat)
                .ThenBy(note => note.Id, StringComparer.Ordinal)
                .ToList();

            return track;
        }

        private static GameAudioNote FromDto(GameAudioNoteDto dto, List<string> warnings, string path)
        {
            var note = new GameAudioNote
            {
                Id = ReadId(dto?.id, "note", warnings, $"{path}.id"),
                StartBeat = dto == null
                    ? 0.0f
                    : ReadClamped(dto.startBeat, 0.0f, 100000.0f, 0.0f, warnings, $"{path}.startBeat"),
                DurationBeat = dto == null
                    ? 1.0f
                    : ReadClamped(dto.durationBeat, GameAudioToolInfo.MinNoteDurationBeat, 100000.0f, GameAudioToolInfo.MinNoteDurationBeat, warnings, $"{path}.durationBeat"),
                MidiNote = dto == null
                    ? 60
                    : ReadClamped(dto.midiNote, 0, 127, 60, warnings, $"{path}.midiNote"),
                Velocity = dto == null
                    ? 0.8f
                    : ReadClamped(dto.velocity, 0.0f, 1.0f, 0.8f, warnings, $"{path}.velocity"),
                VoiceOverride = HasVoicePayload(dto?.voiceOverride) ? FromVoiceDto(dto.voiceOverride, warnings, $"{path}.voiceOverride") : null
            };

            return note;
        }

        internal static GameAudioVoiceSettings FromVoiceDto(GameAudioVoiceDto dto, List<string> warnings, string path)
        {
            GameAudioVoiceSettings voice = GameAudioProjectFactory.CreateDefaultVoice();

            voice.Waveform = ReadWaveform(dto?.waveform, warnings, $"{path}.waveform");
            voice.PulseWidth = dto == null
                ? 0.5f
                : ReadClamped(dto.pulseWidth, 0.10f, 0.90f, 0.5f, warnings, $"{path}.pulseWidth");
            voice.NoiseEnabled = dto != null && dto.noiseEnabled;
            voice.NoiseType = ReadNoiseType(dto?.noiseType, warnings, $"{path}.noiseType");
            voice.NoiseMix = dto == null
                ? 0.0f
                : ReadClamped(dto.noiseMix, 0.0f, 1.0f, 0.0f, warnings, $"{path}.noiseMix");
            voice.Adsr = FromDto(dto?.adsr, warnings, $"{path}.adsr");
            voice.Effect = FromDto(dto?.effect, warnings, $"{path}.effect");
            return voice;
        }

        private static GameAudioEnvelopeSettings FromDto(GameAudioEnvelopeDto dto, List<string> warnings, string path)
        {
            return new GameAudioEnvelopeSettings
            {
                AttackMs = dto == null ? 5 : ReadClamped(dto.attackMs, 0, 5000, 5, warnings, $"{path}.attackMs"),
                DecayMs = dto == null ? 80 : ReadClamped(dto.decayMs, 0, 5000, 80, warnings, $"{path}.decayMs"),
                Sustain = dto == null ? 0.7f : ReadClamped(dto.sustain, 0.0f, 1.0f, 0.7f, warnings, $"{path}.sustain"),
                ReleaseMs = dto == null ? 120 : ReadClamped(dto.releaseMs, 0, 5000, 120, warnings, $"{path}.releaseMs")
            };
        }

        private static GameAudioEffectSettings FromDto(GameAudioEffectDto dto, List<string> warnings, string path)
        {
            GameAudioDelayDto delayDto = HasDelayPayload(dto?.delay) ? dto.delay : null;
            return new GameAudioEffectSettings
            {
                VolumeDb = dto == null ? 0.0f : ReadClamped(dto.volumeDb, -48.0f, 6.0f, 0.0f, warnings, $"{path}.volumeDb"),
                Pan = dto == null ? 0.0f : ReadClamped(dto.pan, -1.0f, 1.0f, 0.0f, warnings, $"{path}.pan"),
                PitchSemitone = dto == null ? 0.0f : ReadClamped(dto.pitchSemitone, -24.0f, 24.0f, 0.0f, warnings, $"{path}.pitchSemitone"),
                StereoDetuneSemitone = dto == null ? 0.0f : ReadClamped(dto.stereoDetuneSemitone, 0.0f, 12.0f, 0.0f, warnings, $"{path}.stereoDetuneSemitone"),
                StereoDelayMs = dto == null ? 0 : ReadClamped(dto.stereoDelayMs, 0, 1000, 0, warnings, $"{path}.stereoDelayMs"),
                FadeInMs = dto == null ? 0 : ReadClamped(dto.fadeInMs, 0, 3000, 0, warnings, $"{path}.fadeInMs"),
                FadeOutMs = dto == null ? 0 : ReadClamped(dto.fadeOutMs, 0, 3000, 0, warnings, $"{path}.fadeOutMs"),
                Delay = FromDto(delayDto, warnings, $"{path}.delay")
            };
        }

        private static GameAudioDelaySettings FromDto(GameAudioDelayDto dto, List<string> warnings, string path)
        {
            if (dto == null)
            {
                warnings.Add($"{path} missing; default delay was applied.");
            }

            return new GameAudioDelaySettings
            {
                Enabled = dto != null && dto.enabled,
                TimeMs = dto == null ? 180 : ReadClamped(dto.timeMs, 20, 1000, 180, warnings, $"{path}.timeMs"),
                Feedback = dto == null ? 0.25f : ReadClamped(dto.feedback, 0.0f, 0.70f, 0.25f, warnings, $"{path}.feedback"),
                Mix = dto == null ? 0.2f : ReadClamped(dto.mix, 0.0f, 1.0f, 0.2f, warnings, $"{path}.mix")
            };
        }

        private static GameAudioTimeSignature ReadTimeSignature(GameAudioTimeSignatureDto dto, List<string> warnings)
        {
            if (dto == null)
            {
                warnings.Add("project.timeSignature missing; defaulted to 4/4.");
                return new GameAudioTimeSignature { Numerator = 4, Denominator = 4 };
            }

            bool isSupported = (dto.numerator == 4 && dto.denominator == 4)
                || (dto.numerator == 3 && dto.denominator == 4)
                || (dto.numerator == 6 && dto.denominator == 8);

            if (!isSupported)
            {
                warnings.Add($"project.timeSignature ({dto.numerator}/{dto.denominator}) is unsupported; defaulted to 4/4.");
                return new GameAudioTimeSignature { Numerator = 4, Denominator = 4 };
            }

            return new GameAudioTimeSignature
            {
                Numerator = dto.numerator,
                Denominator = dto.denominator
            };
        }

        private static GameAudioWaveformType ReadWaveform(string value, List<string> warnings, string path)
        {
            if (GameAudioEnumUtility.TryParseDefined(value, out GameAudioWaveformType waveform))
            {
                return waveform;
            }

            warnings.Add($"{path} was unknown; defaulted to Square.");
            return GameAudioWaveformType.Square;
        }

        private static GameAudioNoiseType ReadNoiseType(string value, List<string> warnings, string path)
        {
            if (GameAudioEnumUtility.TryParseDefined(value, out GameAudioNoiseType noiseType))
            {
                return noiseType;
            }

            warnings.Add($"{path} was unknown; defaulted to White.");
            return GameAudioNoiseType.White;
        }

        private static GameAudioChannelMode ReadChannelMode(string value, GameAudioChannelMode fallback, List<string> warnings, string path)
        {
            if (GameAudioEnumUtility.TryParseDefined(value, out GameAudioChannelMode channelMode))
            {
                return channelMode;
            }

            warnings.Add($"{path} was unknown; defaulted to {fallback}.");
            return fallback;
        }

        private static TEnum ReadEnum<TEnum>(string value, TEnum fallback, List<string> warnings, string path)
            where TEnum : struct
        {
            if (GameAudioEnumUtility.TryParseDefined(value, out TEnum parsed))
            {
                return parsed;
            }

            warnings.Add($"{path} was unknown; defaulted to {fallback}.");
            return fallback;
        }

        private static int ReadTotalBars(int totalBars, List<string> warnings)
        {
            if (totalBars <= 0)
            {
                warnings.Add($"project.totalBars must be positive; defaulted to {GameAudioToolInfo.DefaultTotalBars}.");
                return GameAudioToolInfo.DefaultTotalBars;
            }

            int clampedValue = GameAudioValidationUtility.ClampInt(totalBars, 1, GameAudioToolInfo.MaxTotalBars);
            if (clampedValue != totalBars)
            {
                warnings.Add($"project.totalBars was out of range and was clamped to {clampedValue}.");
            }

            return clampedValue;
        }

        private static int ReadSampleRate(int sampleRate, List<string> warnings, string path, bool allowImportedConversionRate)
        {
            if (GameAudioValidationUtility.IsSupportedSampleRate(sampleRate)
                || (allowImportedConversionRate && sampleRate > 0))
            {
                return sampleRate;
            }

            warnings.Add($"{path} was unsupported; defaulted to {GameAudioToolInfo.DefaultSampleRate}.");
            return GameAudioToolInfo.DefaultSampleRate;
        }

        private static int ReadPositive(int value, int fallback, List<string> warnings, string path)
        {
            if (value > 0)
            {
                return value;
            }

            warnings.Add($"{path} must be positive; defaulted to {fallback}.");
            return fallback;
        }

        private static string ReadRequiredText(string value, string fallback, List<string> warnings, string path)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            warnings.Add($"{path} was empty; defaulted to \"{fallback}\".");
            return fallback;
        }

        private static string ReadId(string value, string prefix, List<string> warnings, string path)
        {
            if (!string.IsNullOrWhiteSpace(value))
            {
                return value;
            }

            string generatedValue = $"{prefix}-{Guid.NewGuid():N}";
            warnings.Add($"{path} was missing; generated {generatedValue}.");
            return generatedValue;
        }

        private static int ReadClamped(int value, int min, int max, int fallback, List<string> warnings, string path)
        {
            if (value == 0 && fallback != 0 && min > 0)
            {
                warnings.Add($"{path} was missing or zero; defaulted to {fallback}.");
                return fallback;
            }

            int clampedValue = GameAudioValidationUtility.ClampInt(value, min, max);
            if (clampedValue != value)
            {
                warnings.Add($"{path} was out of range and was clamped to {clampedValue}.");
            }

            return clampedValue;
        }

        private static float ReadClamped(float value, float min, float max, float fallback, List<string> warnings, string path)
        {
            if (Mathf.Approximately(value, 0.0f) && !Mathf.Approximately(fallback, 0.0f) && min > 0.0f)
            {
                warnings.Add($"{path} was missing or zero; defaulted to {fallback}.");
                return fallback;
            }

            float clampedValue = GameAudioValidationUtility.ClampFloat(value, min, max);
            if (!Mathf.Approximately(clampedValue, value))
            {
                warnings.Add($"{path} was out of range and was clamped to {clampedValue}.");
            }

            return clampedValue;
        }

        private static T WithWarning<T>(T value, List<string> warnings, string message)
        {
            warnings.Add(message);
            return value;
        }

        private static bool ShouldPreserveImportedConversionSampleRate(GameAudioProject project)
        {
            return project.ImportedAudioConversion != null && project.SampleRate > 0;
        }

        private static int NormalizeSerializedSampleRate(GameAudioProject project)
        {
            if (ShouldPreserveImportedConversionSampleRate(project))
            {
                return project.SampleRate;
            }

            return GameAudioValidationUtility.IsSupportedSampleRate(project.SampleRate)
                ? project.SampleRate
                : GameAudioToolInfo.DefaultSampleRate;
        }

        private static bool HasImportedAudioConversionPayload(GameAudioImportedAudioConversionDto dto)
        {
            return dto != null
                && (!string.IsNullOrWhiteSpace(dto.sourceClipName)
                    || !string.IsNullOrWhiteSpace(dto.sourceAssetPath)
                    || dto.sourceSampleRate > 0
                    || dto.sourceChannelCount > 0
                    || dto.sourceDurationSeconds > 0.0f
                    || dto.targetSampleRate > 0
                    || !string.IsNullOrWhiteSpace(dto.targetChannelMode)
                    || dto.outputChannelCount > 0
                    || !string.IsNullOrWhiteSpace(dto.outputWaveFileName));
        }

        private static bool HasVoicePayload(GameAudioVoiceDto dto)
        {
            return dto != null
                && (!string.IsNullOrWhiteSpace(dto.waveform)
                    || !Mathf.Approximately(dto.pulseWidth, 0.0f)
                    || dto.noiseEnabled
                    || !string.IsNullOrWhiteSpace(dto.noiseType)
                    || !Mathf.Approximately(dto.noiseMix, 0.0f)
                    || HasEnvelopePayload(dto.adsr)
                    || HasEffectPayload(dto.effect));
        }

        private static bool HasEnvelopePayload(GameAudioEnvelopeDto dto)
        {
            return dto != null
                && (dto.attackMs != 0
                    || dto.decayMs != 0
                    || !Mathf.Approximately(dto.sustain, 0.0f)
                    || dto.releaseMs != 0);
        }

        private static bool HasEffectPayload(GameAudioEffectDto dto)
        {
            return dto != null
                && (!Mathf.Approximately(dto.volumeDb, 0.0f)
                    || !Mathf.Approximately(dto.pan, 0.0f)
                    || !Mathf.Approximately(dto.pitchSemitone, 0.0f)
                    || !Mathf.Approximately(dto.stereoDetuneSemitone, 0.0f)
                    || dto.stereoDelayMs != 0
                    || dto.fadeInMs != 0
                    || dto.fadeOutMs != 0
                    || HasDelayPayload(dto.delay));
        }

        private static bool HasDelayPayload(GameAudioDelayDto dto)
        {
            return dto != null
                && (dto.enabled
                    || dto.timeMs != 0
                    || !Mathf.Approximately(dto.feedback, 0.0f)
                    || !Mathf.Approximately(dto.mix, 0.0f));
        }
    }
}
