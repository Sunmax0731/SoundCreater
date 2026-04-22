using System.Collections.Generic;

namespace GameAudioTool.Editor.Domain
{
    public sealed class GameAudioTrack
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = string.Empty;

        public bool Mute { get; set; }

        public bool Solo { get; set; }

        public float VolumeDb { get; set; }

        public float Pan { get; set; }

        public GameAudioVoiceSettings DefaultVoice { get; set; } = new GameAudioVoiceSettings();

        public List<GameAudioNote> Notes { get; set; } = new List<GameAudioNote>();
    }
}
