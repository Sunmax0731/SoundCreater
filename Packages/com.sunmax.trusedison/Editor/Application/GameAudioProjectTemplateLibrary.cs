using System;
using System.Collections.Generic;
using System.Linq;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Presets;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Application
{
    public static class GameAudioProjectTemplateLibrary
    {
        public const string TemplateKind = "torusEdison.projectTemplate";
        public const string TemplateFormatVersion = "1.0.0";
        public const string TemplateFileExtension = ".gats-template.json";
        public const string UserTemplateDirectory = "%LocalAppData%/GameAudioTool/project-templates";

        private static readonly IReadOnlyList<GameAudioProjectTemplate> BuiltInTemplatesInternal = new[]
        {
            CreateOneShotTemplate(
                "ui-click",
                "UI",
                "UI Click",
                "Short one-shot project for menu focus and small buttons.",
                "UI Click",
                "ui-click",
                120,
                2,
                72,
                0.20f),
            CreateOneShotTemplate(
                "coin-pickup",
                "Pickup",
                "Coin Pickup",
                "Bright pickup or reward tone with a compact tail.",
                "Coin Pickup",
                "coin-pickup",
                132,
                2,
                76,
                0.25f),
            CreateOneShotTemplate(
                "explosion",
                "Impact",
                "Explosion",
                "Noise-forward impact starter for bursts and hits.",
                "Explosion",
                "noise-hit",
                96,
                2,
                48,
                0.50f),
            CreateOneShotTemplate(
                "laser-shot",
                "Action",
                "Laser Shot",
                "Sharp action shot starter with a short delay tail.",
                "Laser Shot",
                "laser-shot",
                140,
                2,
                79,
                0.30f),
            new GameAudioProjectTemplate(
                "simple-loop",
                "Loop",
                "Simple Loop",
                "Four-bar loop starter with repeated lead notes.",
                CreateLoopProject)
        };

        public static IReadOnlyList<GameAudioProjectTemplate> BuiltInTemplates => BuiltInTemplatesInternal;

        public static bool TryGetTemplate(string id, out GameAudioProjectTemplate template)
        {
            template = BuiltInTemplatesInternal.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal));
            return template != null;
        }

        public static string FormatLabel(string id)
        {
            return TryGetTemplate(id, out GameAudioProjectTemplate template)
                ? $"{template.Category} / {template.DisplayName}"
                : id ?? string.Empty;
        }

        private static GameAudioProjectTemplate CreateOneShotTemplate(
            string id,
            string category,
            string displayName,
            string description,
            string projectName,
            string voicePresetId,
            int bpm,
            int totalBars,
            int midiNote,
            float durationBeat)
        {
            return new GameAudioProjectTemplate(
                id,
                category,
                displayName,
                description,
                (sampleRate, channelMode) => CreateOneShotProject(projectName, voicePresetId, bpm, totalBars, midiNote, durationBeat, sampleRate, channelMode));
        }

        private static GameAudioProject CreateOneShotProject(
            string projectName,
            string voicePresetId,
            int bpm,
            int totalBars,
            int midiNote,
            float durationBeat,
            int sampleRate,
            GameAudioChannelMode channelMode)
        {
            GameAudioProject project = CreateBaseProject(projectName, bpm, totalBars, sampleRate, channelMode);
            project.ExportSettings = new GameAudioExportSettings
            {
                DurationMode = GameAudioExportDurationMode.AutoTrim,
                DurationSeconds = GameAudioToolInfo.DefaultExportDurationSeconds,
                IncludeTail = true
            };

            GameAudioTrack track = project.Tracks[0];
            track.Name = projectName;
            if (GameAudioVoicePresetLibrary.TryGetPreset(voicePresetId, out GameAudioVoicePreset preset))
            {
                track.DefaultVoice = GameAudioVoicePresetLibrary.CloneVoice(preset.Voice);
            }

            track.Notes.Add(new GameAudioNote
            {
                Id = $"note-{CreateIdSafe(projectName)}-001",
                StartBeat = 0.0f,
                DurationBeat = Math.Max(GameAudioToolInfo.MinNoteDurationBeat, durationBeat),
                MidiNote = GameAudioValidationUtility.ClampInt(midiNote, 0, 127),
                Velocity = 0.9f
            });

            return project;
        }

        private static GameAudioProject CreateLoopProject(int sampleRate, GameAudioChannelMode channelMode)
        {
            GameAudioProject project = CreateBaseProject("Simple Loop", 120, 4, sampleRate, channelMode);
            project.LoopPlayback = true;
            project.ExportSettings = new GameAudioExportSettings
            {
                DurationMode = GameAudioExportDurationMode.ProjectBars,
                DurationSeconds = GameAudioToolInfo.DefaultExportDurationSeconds,
                IncludeTail = false
            };

            GameAudioTrack track = project.Tracks[0];
            track.Name = "Loop Lead";
            if (GameAudioVoicePresetLibrary.TryGetPreset("ui-confirm", out GameAudioVoicePreset preset))
            {
                track.DefaultVoice = GameAudioVoicePresetLibrary.CloneVoice(preset.Voice);
            }

            int[] notes = { 60, 64, 67, 72 };
            for (int index = 0; index < notes.Length; index++)
            {
                track.Notes.Add(new GameAudioNote
                {
                    Id = $"note-simple-loop-{index + 1:00}",
                    StartBeat = index,
                    DurationBeat = 0.75f,
                    MidiNote = notes[index],
                    Velocity = 0.75f
                });
            }

            return project;
        }

        private static GameAudioProject CreateBaseProject(string name, int bpm, int totalBars, int sampleRate, GameAudioChannelMode channelMode)
        {
            GameAudioProject project = GameAudioProjectFactory.CreateDefaultProject(sampleRate, channelMode);
            project.Name = name;
            project.Bpm = GameAudioValidationUtility.ClampInt(bpm, 40, 240);
            project.TotalBars = GameAudioValidationUtility.ClampInt(totalBars, 1, GameAudioToolInfo.MaxTotalBars);
            return project;
        }

        private static string CreateIdSafe(string value)
        {
            string sanitized = GameAudioValidationUtility.SanitizeExportFileName(value).Replace(' ', '-').ToLowerInvariant();
            return string.IsNullOrWhiteSpace(sanitized) ? "template" : sanitized;
        }
    }
}
