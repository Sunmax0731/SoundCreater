using System.Collections.Generic;

namespace TorusEdison.Editor.Domain
{
    public sealed class GameAudioProject
    {
        public string Id { get; set; } = string.Empty;

        public string Name { get; set; } = "New Audio Project";

        public int Bpm { get; set; } = 120;

        public GameAudioTimeSignature TimeSignature { get; set; } = new GameAudioTimeSignature();

        public int TotalBars { get; set; } = 8;

        public int SampleRate { get; set; } = 48000;

        public GameAudioChannelMode ChannelMode { get; set; } = GameAudioChannelMode.Stereo;

        public float MasterGainDb { get; set; }

        public bool LoopPlayback { get; set; }

        public GameAudioImportedAudioConversion ImportedAudioConversion { get; set; }

        public List<GameAudioTrack> Tracks { get; set; } = new List<GameAudioTrack>();
    }
}
