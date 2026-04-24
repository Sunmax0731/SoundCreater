using System;
using System.Collections.Generic;
using System.Linq;
using TorusEdison.Editor.Application;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Presets
{
    public static class GameAudioVoicePresetLibrary
    {
        public const string PresetKind = "torusEdison.voicePreset";
        public const string PresetFormatVersion = "1.0.0";
        public const string PresetFileExtension = ".gats-preset.json";
        public const string UserPresetDirectory = "%LocalAppData%/GameAudioTool/voice-presets";

        private static readonly IReadOnlyList<GameAudioVoicePreset> BuiltInPresetsInternal = new[]
        {
            CreatePreset(
                "ui-click",
                "UI",
                "UI Click",
                "Short square click for menu focus and small buttons.",
                GameAudioWaveformType.Square,
                pulseWidth: 0.35f,
                noiseEnabled: false,
                noiseMix: 0.0f,
                attackMs: 0,
                decayMs: 35,
                sustain: 0.15f,
                releaseMs: 45,
                volumeDb: -4.0f,
                pan: 0.0f,
                pitchSemitone: 12.0f,
                fadeOutMs: 25,
                delayEnabled: false,
                tags: new[] { "short", "button", "menu", "square" }),
            CreatePreset(
                "ui-confirm",
                "UI",
                "UI Confirm",
                "Rounded triangle tone for confirm actions.",
                GameAudioWaveformType.Triangle,
                pulseWidth: 0.5f,
                noiseEnabled: false,
                noiseMix: 0.0f,
                attackMs: 3,
                decayMs: 90,
                sustain: 0.45f,
                releaseMs: 130,
                volumeDb: -3.0f,
                pan: 0.0f,
                pitchSemitone: 7.0f,
                fadeOutMs: 40,
                delayEnabled: true,
                delayTimeMs: 110,
                delayFeedback: 0.18f,
                delayMix: 0.12f,
                tags: new[] { "confirm", "positive", "triangle", "delay" }),
            CreatePreset(
                "ui-cancel",
                "UI",
                "UI Cancel",
                "Lower warning tone with a small noise edge.",
                GameAudioWaveformType.Saw,
                pulseWidth: 0.5f,
                noiseEnabled: true,
                noiseMix: 0.12f,
                attackMs: 0,
                decayMs: 70,
                sustain: 0.25f,
                releaseMs: 100,
                volumeDb: -5.0f,
                pan: 0.0f,
                pitchSemitone: -7.0f,
                fadeOutMs: 30,
                delayEnabled: false,
                tags: new[] { "cancel", "warning", "saw", "noise" }),
            CreatePreset(
                "coin-pickup",
                "Pickup",
                "Coin Pickup",
                "Bright pulse tone for pickups and rewards.",
                GameAudioWaveformType.Pulse,
                pulseWidth: 0.25f,
                noiseEnabled: false,
                noiseMix: 0.0f,
                attackMs: 0,
                decayMs: 65,
                sustain: 0.35f,
                releaseMs: 85,
                volumeDb: -2.0f,
                pan: 0.0f,
                pitchSemitone: 19.0f,
                fadeOutMs: 20,
                delayEnabled: true,
                delayTimeMs: 90,
                delayFeedback: 0.15f,
                delayMix: 0.10f,
                tags: new[] { "coin", "reward", "bright", "pulse", "delay" }),
            CreatePreset(
                "laser-shot",
                "Action",
                "Laser Shot",
                "Sharp saw lead with a short delay tail for shots.",
                GameAudioWaveformType.Saw,
                pulseWidth: 0.5f,
                noiseEnabled: false,
                noiseMix: 0.0f,
                attackMs: 0,
                decayMs: 110,
                sustain: 0.10f,
                releaseMs: 160,
                volumeDb: -3.0f,
                pan: 0.0f,
                pitchSemitone: 12.0f,
                fadeOutMs: 60,
                delayEnabled: true,
                delayTimeMs: 140,
                delayFeedback: 0.22f,
                delayMix: 0.18f,
                tags: new[] { "shot", "laser", "saw", "delay" }),
            CreatePreset(
                "noise-hit",
                "Impact",
                "Noise Hit",
                "Short noise-heavy impact for bursts and hits.",
                GameAudioWaveformType.Square,
                pulseWidth: 0.5f,
                noiseEnabled: true,
                noiseMix: 0.85f,
                attackMs: 0,
                decayMs: 45,
                sustain: 0.0f,
                releaseMs: 65,
                volumeDb: -4.0f,
                pan: 0.0f,
                pitchSemitone: -2.0f,
                fadeOutMs: 30,
                delayEnabled: false,
                tags: new[] { "hit", "impact", "burst", "noise" })
        };

        public static IReadOnlyList<GameAudioVoicePreset> BuiltInPresets => BuiltInPresetsInternal;

        public static GameAudioVoicePreset CreateUserPreset(string displayName, string description, GameAudioVoiceSettings voice, string[] tags = null)
        {
            string normalizedName = string.IsNullOrWhiteSpace(displayName)
                ? "Voice Preset"
                : displayName.Trim();
            return new GameAudioVoicePreset(
                CreatePresetId(normalizedName),
                "User",
                normalizedName,
                string.IsNullOrWhiteSpace(description) ? "Exported from Torus Edison." : description.Trim(),
                CloneVoice(voice),
                NormalizeTags(tags));
        }

        public static bool TryGetPreset(string id, out GameAudioVoicePreset preset)
        {
            preset = BuiltInPresetsInternal.FirstOrDefault(candidate => string.Equals(candidate.Id, id, StringComparison.Ordinal));
            return preset != null;
        }

        public static GameAudioVoiceSettings CloneVoice(GameAudioVoiceSettings voice)
        {
            if (voice == null)
            {
                return GameAudioProjectFactory.CreateDefaultVoice();
            }

            return new GameAudioVoiceSettings
            {
                Waveform = voice.Waveform,
                PulseWidth = voice.PulseWidth,
                NoiseEnabled = voice.NoiseEnabled,
                NoiseType = voice.NoiseType,
                NoiseMix = voice.NoiseMix,
                Adsr = new GameAudioEnvelopeSettings
                {
                    AttackMs = voice.Adsr?.AttackMs ?? 5,
                    DecayMs = voice.Adsr?.DecayMs ?? 80,
                    Sustain = voice.Adsr?.Sustain ?? 0.7f,
                    ReleaseMs = voice.Adsr?.ReleaseMs ?? 120
                },
                Effect = new GameAudioEffectSettings
                {
                    VolumeDb = voice.Effect?.VolumeDb ?? 0.0f,
                    Pan = voice.Effect?.Pan ?? 0.0f,
                    PitchSemitone = voice.Effect?.PitchSemitone ?? 0.0f,
                    StereoDetuneSemitone = voice.Effect?.StereoDetuneSemitone ?? 0.0f,
                    StereoDelayMs = voice.Effect?.StereoDelayMs ?? 0,
                    FadeInMs = voice.Effect?.FadeInMs ?? 0,
                    FadeOutMs = voice.Effect?.FadeOutMs ?? 0,
                    Delay = new GameAudioDelaySettings
                    {
                        Enabled = voice.Effect?.Delay?.Enabled ?? false,
                        TimeMs = voice.Effect?.Delay?.TimeMs ?? 180,
                        Feedback = voice.Effect?.Delay?.Feedback ?? 0.25f,
                        Mix = voice.Effect?.Delay?.Mix ?? 0.2f
                    }
                }
            };
        }

        public static void CopyTo(GameAudioVoiceSettings source, GameAudioVoiceSettings destination)
        {
            if (destination == null)
            {
                throw new ArgumentNullException(nameof(destination));
            }

            GameAudioVoiceSettings clone = CloneVoice(source);
            destination.Waveform = clone.Waveform;
            destination.PulseWidth = clone.PulseWidth;
            destination.NoiseEnabled = clone.NoiseEnabled;
            destination.NoiseType = clone.NoiseType;
            destination.NoiseMix = clone.NoiseMix;
            destination.Adsr = clone.Adsr;
            destination.Effect = clone.Effect;
        }

        public static string FormatLabel(string id)
        {
            return TryGetPreset(id, out GameAudioVoicePreset preset)
                ? $"{preset.Category} / {preset.DisplayName}"
                : id ?? string.Empty;
        }

        public static string CreatePresetId(string displayName)
        {
            string sanitized = GameAudioValidationUtility.SanitizeExportFileName(displayName).Trim();
            return string.IsNullOrWhiteSpace(sanitized)
                ? "voice-preset"
                : sanitized.Replace(' ', '-').ToLowerInvariant();
        }

        private static GameAudioVoicePreset CreatePreset(
            string id,
            string category,
            string displayName,
            string description,
            GameAudioWaveformType waveform,
            float pulseWidth,
            bool noiseEnabled,
            float noiseMix,
            int attackMs,
            int decayMs,
            float sustain,
            int releaseMs,
            float volumeDb,
            float pan,
            float pitchSemitone,
            int fadeOutMs,
            bool delayEnabled,
            int delayTimeMs = 180,
            float delayFeedback = 0.25f,
            float delayMix = 0.2f,
            string[] tags = null)
        {
            return new GameAudioVoicePreset(
                id,
                category,
                displayName,
                description,
                new GameAudioVoiceSettings
                {
                    Waveform = waveform,
                    PulseWidth = pulseWidth,
                    NoiseEnabled = noiseEnabled,
                    NoiseType = GameAudioNoiseType.White,
                    NoiseMix = noiseMix,
                    Adsr = new GameAudioEnvelopeSettings
                    {
                        AttackMs = attackMs,
                        DecayMs = decayMs,
                        Sustain = sustain,
                        ReleaseMs = releaseMs
                    },
                    Effect = new GameAudioEffectSettings
                    {
                        VolumeDb = volumeDb,
                        Pan = pan,
                        PitchSemitone = pitchSemitone,
                        StereoDetuneSemitone = 0.0f,
                        StereoDelayMs = 0,
                        FadeInMs = 0,
                        FadeOutMs = fadeOutMs,
                        Delay = new GameAudioDelaySettings
                        {
                            Enabled = delayEnabled,
                            TimeMs = delayTimeMs,
                            Feedback = delayFeedback,
                            Mix = delayMix
                        }
                    }
                },
                NormalizeTags(tags));
        }

        public static string[] NormalizeTags(IEnumerable<string> tags)
        {
            if (tags == null)
            {
                return Array.Empty<string>();
            }

            return tags
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Select(tag => tag.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(tag => tag, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }
    }
}
