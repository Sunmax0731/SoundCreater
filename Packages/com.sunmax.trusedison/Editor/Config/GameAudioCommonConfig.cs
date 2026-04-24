using System;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Localization;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Config
{
    public sealed class GameAudioCommonConfig
    {
        public int DefaultSampleRate { get; set; } = 48000;

        public GameAudioChannelMode DefaultChannelMode { get; set; } = GameAudioChannelMode.Stereo;

        public string DefaultExportDirectory { get; set; } = "Exports/Audio";

        public bool ShowStartupGuide { get; set; } = true;

        public bool RememberLastProject { get; set; } = true;

        public string LastProjectPath { get; set; } = string.Empty;

        public string DefaultGridDivision { get; set; } = "1/16";

        public string VoicePresetSearchQuery { get; set; } = string.Empty;

        public string VoicePresetCategoryFilter { get; set; } = string.Empty;

        public string[] RecentVoicePresetKeys { get; set; } = Array.Empty<string>();

        public int UndoHistoryLimit { get; set; } = 100;

        public GameAudioLanguageMode DisplayLanguage { get; set; } = GameAudioLanguageMode.Auto;

        public bool EnableDiagnosticLogging { get; set; } = false;

        public GameAudioDiagnosticLogLevel DiagnosticLogLevel { get; set; } = GameAudioDiagnosticLogLevel.Info;
    }
}
