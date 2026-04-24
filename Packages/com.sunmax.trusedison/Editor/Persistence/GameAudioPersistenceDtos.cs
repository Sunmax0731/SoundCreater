using System;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Persistence
{
    [Serializable]
    internal sealed class GameAudioProjectFileDto
    {
        public string formatVersion;
        public string toolVersion;
        public GameAudioProjectDto project;
    }

    [Serializable]
    internal sealed class GameAudioVoicePresetFileDto
    {
        public string kind;
        public string presetFormatVersion;
        public string toolVersion;
        public GameAudioVoicePresetDto preset;
    }

    [Serializable]
    internal sealed class GameAudioVoicePresetDto
    {
        public string id;
        public string category;
        public string[] tags;
        public string displayName;
        public string description;
        public GameAudioVoiceDto voice;
    }

    [Serializable]
    internal sealed class GameAudioProjectDto
    {
        public string id;
        public string name;
        public int bpm;
        public GameAudioTimeSignatureDto timeSignature;
        public int totalBars;
        public int sampleRate;
        public string channelMode;
        public float masterGainDb;
        public bool loopPlayback;
        public GameAudioExportSettingsDto exportSettings;
        public GameAudioImportedAudioConversionDto importedAudioConversion;
        public GameAudioTrackDto[] tracks;
    }

    [Serializable]
    internal sealed class GameAudioExportSettingsDto
    {
        public string durationMode = "ProjectBars";
        public float durationSeconds = GameAudioToolInfo.DefaultExportDurationSeconds;
        public bool includeTail = true;
    }

    [Serializable]
    internal sealed class GameAudioImportedAudioConversionDto
    {
        public string sourceClipName;
        public string sourceAssetPath;
        public int sourceSampleRate;
        public int sourceChannelCount;
        public float sourceDurationSeconds;
        public int targetSampleRate;
        public string targetChannelMode;
        public int outputChannelCount;
        public string outputWaveFileName;
    }

    [Serializable]
    internal sealed class GameAudioTimeSignatureDto
    {
        public int numerator;
        public int denominator;
    }

    [Serializable]
    internal sealed class GameAudioTrackDto
    {
        public string id;
        public string name;
        public bool mute;
        public bool solo;
        public float volumeDb;
        public float pan;
        public GameAudioVoiceDto defaultVoice;
        public GameAudioNoteDto[] notes;
    }

    [Serializable]
    internal sealed class GameAudioVoiceDto
    {
        public string waveform;
        public float pulseWidth;
        public bool noiseEnabled;
        public string noiseType;
        public float noiseMix;
        public GameAudioEnvelopeDto adsr;
        public GameAudioEffectDto effect;
    }

    [Serializable]
    internal sealed class GameAudioEnvelopeDto
    {
        public int attackMs;
        public int decayMs;
        public float sustain;
        public int releaseMs;
    }

    [Serializable]
    internal sealed class GameAudioEffectDto
    {
        public float volumeDb;
        public float pan;
        public float pitchSemitone;
        public float stereoDetuneSemitone;
        public int stereoDelayMs;
        public int fadeInMs;
        public int fadeOutMs;
        public GameAudioDelayDto delay;
    }

    [Serializable]
    internal sealed class GameAudioDelayDto
    {
        public bool enabled;
        public int timeMs;
        public float feedback;
        public float mix;
    }

    [Serializable]
    internal sealed class GameAudioNoteDto
    {
        public string id;
        public float startBeat;
        public float durationBeat;
        public int midiNote;
        public float velocity;
        public GameAudioVoiceDto voiceOverride;
    }

    [Serializable]
    internal sealed class GameAudioCommonConfigDto
    {
        public int defaultSampleRate;
        public string defaultChannelMode;
        public string defaultExportDirectory;
        public bool showStartupGuide;
        public bool rememberLastProject;
        public string lastProjectPath;
        public string defaultGridDivision;
        public string voicePresetSearchQuery;
        public string voicePresetCategoryFilter;
        public string[] recentVoicePresetKeys;
        public int undoHistoryLimit;
        public string displayLanguage;
        public bool enableDiagnosticLogging;
        public string diagnosticLogLevel;
    }

    [Serializable]
    internal sealed class GameAudioProjectConfigDto
    {
        public string exportDirectory;
        public bool autoRefreshAfterExport;
        public int preferredSampleRate;
        public string preferredChannelMode;
    }
}
