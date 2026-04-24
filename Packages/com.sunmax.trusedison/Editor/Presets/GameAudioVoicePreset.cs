using System;
using TorusEdison.Editor.Domain;

namespace TorusEdison.Editor.Presets
{
    public sealed class GameAudioVoicePreset
    {
        public GameAudioVoicePreset(string id, string category, string displayName, string description, GameAudioVoiceSettings voice, string[] tags = null)
        {
            Id = id ?? string.Empty;
            Category = category ?? string.Empty;
            DisplayName = displayName ?? string.Empty;
            Description = description ?? string.Empty;
            Voice = voice ?? new GameAudioVoiceSettings();
            Tags = tags ?? Array.Empty<string>();
        }

        public string Id { get; }

        public string Category { get; }

        public string DisplayName { get; }

        public string Description { get; }

        public GameAudioVoiceSettings Voice { get; }

        public string[] Tags { get; }
    }
}
