using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Persistence;
using TorusEdison.Editor.Utilities;
using UnityEngine;

namespace TorusEdison.Editor.Presets
{
    public sealed class GameAudioVoicePresetLoadResult
    {
        public GameAudioVoicePresetLoadResult(GameAudioVoicePreset preset, IReadOnlyList<string> warnings)
        {
            Preset = preset ?? throw new ArgumentNullException(nameof(preset));
            Warnings = warnings ?? Array.Empty<string>();
        }

        public GameAudioVoicePreset Preset { get; }

        public IReadOnlyList<string> Warnings { get; }
    }

    public sealed class GameAudioVoicePresetFileSerializer
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public string Serialize(GameAudioVoicePreset preset)
        {
            if (preset == null)
            {
                throw new ArgumentNullException(nameof(preset));
            }

            var dto = new GameAudioVoicePresetFileDto
            {
                kind = GameAudioVoicePresetLibrary.PresetKind,
                presetFormatVersion = GameAudioVoicePresetLibrary.PresetFormatVersion,
                toolVersion = GameAudioToolInfo.ToolVersion,
                preset = new GameAudioVoicePresetDto
                {
                    id = string.IsNullOrWhiteSpace(preset.Id)
                        ? GameAudioVoicePresetLibrary.CreatePresetId(preset.DisplayName)
                        : preset.Id,
                    category = string.IsNullOrWhiteSpace(preset.Category) ? "User" : preset.Category,
                    displayName = string.IsNullOrWhiteSpace(preset.DisplayName) ? "Voice Preset" : preset.DisplayName,
                    description = preset.Description ?? string.Empty,
                    voice = GameAudioProjectSerializer.ToVoiceDto(preset.Voice ?? GameAudioProjectFactory.CreateDefaultVoice())
                }
            };

            return JsonUtility.ToJson(dto, true) + "\n";
        }

        public void SaveToFile(string path, GameAudioVoicePreset preset)
        {
            string resolvedPath = NormalizePresetFilePath(path);
            string directoryPath = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(resolvedPath, Serialize(preset), Utf8WithoutBom);
            GameAudioDiagnosticLogger.Verbose("Preset", $"Saved voice preset to {resolvedPath}.");
        }

        public GameAudioVoicePresetLoadResult LoadFromFile(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A preset source path is required.", nameof(path));
            }

            if (!File.Exists(path))
            {
                throw new GameAudioPersistenceException($"Preset file was not found: {path}");
            }

            string json = File.ReadAllText(path, Utf8WithoutBom);
            GameAudioVoicePresetLoadResult result = Deserialize(json);
            GameAudioDiagnosticLogger.Verbose("Preset", $"Loaded voice preset file {path} with {result.Warnings.Count} warning(s).");
            return result;
        }

        public GameAudioVoicePresetLoadResult Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                throw new GameAudioPersistenceException("Preset JSON is empty.");
            }

            ValidatePresetSchema(json);

            GameAudioVoicePresetFileDto rootDto;
            try
            {
                rootDto = JsonUtility.FromJson<GameAudioVoicePresetFileDto>(json);
            }
            catch (Exception exception)
            {
                throw new GameAudioPersistenceException("Failed to parse preset JSON.", exception);
            }

            if (rootDto == null)
            {
                throw new GameAudioPersistenceException("Preset JSON did not produce a document.");
            }

            ValidateKind(rootDto.kind);
            ValidatePresetFormatVersion(rootDto.presetFormatVersion);

            var warnings = new List<string>();
            ValidateToolVersion(rootDto.toolVersion, warnings);

            GameAudioVoicePresetDto presetDto = rootDto.preset;
            if (presetDto == null)
            {
                throw new GameAudioPersistenceException("Preset JSON does not contain a preset object.");
            }

            string displayName = string.IsNullOrWhiteSpace(presetDto.displayName)
                ? "Imported Voice Preset"
                : presetDto.displayName.Trim();
            string id = string.IsNullOrWhiteSpace(presetDto.id)
                ? GameAudioVoicePresetLibrary.CreatePresetId(displayName)
                : presetDto.id.Trim();
            string category = string.IsNullOrWhiteSpace(presetDto.category) ? "Imported" : presetDto.category.Trim();
            string description = presetDto.description ?? string.Empty;
            GameAudioVoiceSettings voice = GameAudioProjectSerializer.FromVoiceDto(presetDto.voice, warnings, "$.preset.voice");

            return new GameAudioVoicePresetLoadResult(
                new GameAudioVoicePreset(id, category, displayName, description, voice),
                warnings);
        }

        public static string NormalizePresetFilePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                throw new ArgumentException("A preset target path is required.", nameof(path));
            }

            string trimmed = path.Trim();
            if (trimmed.EndsWith(GameAudioVoicePresetLibrary.PresetFileExtension, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            string doubledJsonExtension = GameAudioVoicePresetLibrary.PresetFileExtension + ".json";
            if (trimmed.EndsWith(doubledJsonExtension, StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring(0, trimmed.Length - ".json".Length);
            }

            if (trimmed.EndsWith(".json", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed.Substring(0, trimmed.Length - ".json".Length) + GameAudioVoicePresetLibrary.PresetFileExtension;
            }

            return trimmed + GameAudioVoicePresetLibrary.PresetFileExtension;
        }

        private static void ValidateKind(string kind)
        {
            if (!string.Equals(kind, GameAudioVoicePresetLibrary.PresetKind, StringComparison.Ordinal))
            {
                throw new GameAudioPersistenceException($"Unsupported preset kind: {kind ?? "(missing)"}");
            }
        }

        private static void ValidatePresetFormatVersion(string presetFormatVersion)
        {
            if (!GameAudioValidationUtility.TryGetFormatMajor(presetFormatVersion, out int majorVersion))
            {
                throw new GameAudioPersistenceException("presetFormatVersion is missing or invalid.");
            }

            if (majorVersion != 1)
            {
                throw new GameAudioPersistenceException($"Unsupported presetFormatVersion: {presetFormatVersion}");
            }
        }

        private static void ValidateToolVersion(string toolVersion, List<string> warnings)
        {
            if (string.IsNullOrWhiteSpace(toolVersion))
            {
                warnings.Add("toolVersion missing; preset imported as compatible.");
                return;
            }

            if (!GameAudioValidationUtility.TryGetFormatMajor(toolVersion, out int sourceMajor)
                || !GameAudioValidationUtility.TryGetFormatMajor(GameAudioToolInfo.ToolVersion, out int currentMajor))
            {
                warnings.Add($"toolVersion ({toolVersion}) could not be compared; preset imported as compatible.");
                return;
            }

            if (sourceMajor > currentMajor)
            {
                warnings.Add($"toolVersion ({toolVersion}) is newer than this tool ({GameAudioToolInfo.ToolVersion}); preset imported with current compatibility rules.");
            }
        }

        private static void ValidatePresetSchema(string json)
        {
            GameAudioJsonNode root = GameAudioSimpleJsonParser.Parse(json);
            RequireObject(root, "$");
            RequireString(root, "$", "kind");
            RequireString(root, "$", "presetFormatVersion");
            OptionalString(root, "$", "toolVersion");

            GameAudioJsonNode preset = RequireObjectProperty(root, "$", "preset");
            OptionalString(preset, "$.preset", "id");
            OptionalString(preset, "$.preset", "category");
            OptionalString(preset, "$.preset", "displayName");
            OptionalString(preset, "$.preset", "description");
            ValidateVoice(RequireObjectProperty(preset, "$.preset", "voice"), "$.preset.voice");
        }

        private static void ValidateVoice(GameAudioJsonNode node, string path)
        {
            RequireObject(node, path);
            OptionalString(node, path, "waveform");
            OptionalNumber(node, path, "pulseWidth");
            OptionalBoolean(node, path, "noiseEnabled");
            OptionalString(node, path, "noiseType");
            OptionalNumber(node, path, "noiseMix");
            if (TryGetOptionalObject(node, path, "adsr", out GameAudioJsonNode adsr))
            {
                OptionalNumber(adsr, $"{path}.adsr", "attackMs");
                OptionalNumber(adsr, $"{path}.adsr", "decayMs");
                OptionalNumber(adsr, $"{path}.adsr", "sustain");
                OptionalNumber(adsr, $"{path}.adsr", "releaseMs");
            }

            if (TryGetOptionalObject(node, path, "effect", out GameAudioJsonNode effect))
            {
                OptionalNumber(effect, $"{path}.effect", "volumeDb");
                OptionalNumber(effect, $"{path}.effect", "pan");
                OptionalNumber(effect, $"{path}.effect", "pitchSemitone");
                OptionalNumber(effect, $"{path}.effect", "stereoDetuneSemitone");
                OptionalNumber(effect, $"{path}.effect", "stereoDelayMs");
                OptionalNumber(effect, $"{path}.effect", "fadeInMs");
                OptionalNumber(effect, $"{path}.effect", "fadeOutMs");
                if (TryGetOptionalObject(effect, $"{path}.effect", "delay", out GameAudioJsonNode delay))
                {
                    OptionalBoolean(delay, $"{path}.effect.delay", "enabled");
                    OptionalNumber(delay, $"{path}.effect.delay", "timeMs");
                    OptionalNumber(delay, $"{path}.effect.delay", "feedback");
                    OptionalNumber(delay, $"{path}.effect.delay", "mix");
                }
            }
        }

        private static void RequireObject(GameAudioJsonNode node, string path)
        {
            if (node == null || node.Kind != GameAudioJsonKind.Object)
            {
                throw new GameAudioPersistenceException($"{path} must be an object.");
            }
        }

        private static GameAudioJsonNode RequireObjectProperty(GameAudioJsonNode node, string path, string propertyName)
        {
            if (!TryGetProperty(node, propertyName, out GameAudioJsonNode propertyValue) || propertyValue.Kind == GameAudioJsonKind.Null)
            {
                throw new GameAudioPersistenceException($"{path}.{propertyName} is required.");
            }

            if (propertyValue.Kind != GameAudioJsonKind.Object)
            {
                throw new GameAudioPersistenceException($"{path}.{propertyName} must be an object.");
            }

            return propertyValue;
        }

        private static void RequireString(GameAudioJsonNode node, string path, string propertyName)
        {
            if (!TryGetProperty(node, propertyName, out GameAudioJsonNode propertyValue) || propertyValue.Kind == GameAudioJsonKind.Null)
            {
                throw new GameAudioPersistenceException($"{path}.{propertyName} is required.");
            }

            if (propertyValue.Kind != GameAudioJsonKind.String)
            {
                throw new GameAudioPersistenceException($"{path}.{propertyName} must be a string.");
            }
        }

        private static void OptionalString(GameAudioJsonNode node, string path, string propertyName)
        {
            OptionalKind(node, path, propertyName, GameAudioJsonKind.String);
        }

        private static void OptionalNumber(GameAudioJsonNode node, string path, string propertyName)
        {
            OptionalKind(node, path, propertyName, GameAudioJsonKind.Number);
        }

        private static void OptionalBoolean(GameAudioJsonNode node, string path, string propertyName)
        {
            OptionalKind(node, path, propertyName, GameAudioJsonKind.Boolean);
        }

        private static bool TryGetOptionalObject(GameAudioJsonNode node, string path, string propertyName, out GameAudioJsonNode propertyValue)
        {
            propertyValue = null;
            if (!TryGetProperty(node, propertyName, out GameAudioJsonNode candidate) || candidate.Kind == GameAudioJsonKind.Null)
            {
                return false;
            }

            if (candidate.Kind != GameAudioJsonKind.Object)
            {
                throw new GameAudioPersistenceException($"{path}.{propertyName} must be an object.");
            }

            propertyValue = candidate;
            return true;
        }

        private static void OptionalKind(GameAudioJsonNode node, string path, string propertyName, GameAudioJsonKind expectedKind)
        {
            if (!TryGetProperty(node, propertyName, out GameAudioJsonNode propertyValue) || propertyValue.Kind == GameAudioJsonKind.Null)
            {
                return;
            }

            if (propertyValue.Kind != expectedKind)
            {
                throw new GameAudioPersistenceException($"{path}.{propertyName} must be a {expectedKind.ToString().ToLowerInvariant()}.");
            }
        }

        private static bool TryGetProperty(GameAudioJsonNode node, string propertyName, out GameAudioJsonNode propertyValue)
        {
            propertyValue = null;
            return node != null
                && node.Kind == GameAudioJsonKind.Object
                && node.TryGetProperty(propertyName, out propertyValue);
        }
    }
}
