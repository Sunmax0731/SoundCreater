using System;
using TorusEdison.Editor.Config;
using UnityEngine;

namespace TorusEdison.Editor.Utilities
{
    internal static class GameAudioDiagnosticLogger
    {
        private const string Prefix = "[Torus Edison]";

        private static bool _enabled;
        private static GameAudioDiagnosticLogLevel _minimumLevel = GameAudioDiagnosticLogLevel.Info;

        public static bool IsEnabled => _enabled;

        public static GameAudioDiagnosticLogLevel MinimumLevel => _minimumLevel;

        public static void Configure(GameAudioCommonConfig config)
        {
            Configure(
                config?.EnableDiagnosticLogging ?? false,
                config?.DiagnosticLogLevel ?? GameAudioDiagnosticLogLevel.Info);
        }

        public static void Configure(bool enabled, GameAudioDiagnosticLogLevel minimumLevel)
        {
            _enabled = enabled;
            _minimumLevel = minimumLevel;
        }

        public static void Error(string area, string message)
        {
            Log(GameAudioDiagnosticLogLevel.Error, area, message);
        }

        public static void Warning(string area, string message)
        {
            Log(GameAudioDiagnosticLogLevel.Warning, area, message);
        }

        public static void Info(string area, string message)
        {
            Log(GameAudioDiagnosticLogLevel.Info, area, message);
        }

        public static void Verbose(string area, string message)
        {
            Log(GameAudioDiagnosticLogLevel.Verbose, area, message);
        }

        public static void Exception(string area, Exception exception, string context = null)
        {
            if (exception == null)
            {
                return;
            }

            string message = string.IsNullOrWhiteSpace(context)
                ? exception.Message
                : $"{context}: {exception.Message}";
            Log(GameAudioDiagnosticLogLevel.Error, area, message, exception);
        }

        private static void Log(GameAudioDiagnosticLogLevel level, string area, string message, Exception exception = null)
        {
            if (!_enabled || level > _minimumLevel)
            {
                return;
            }

            string normalizedArea = string.IsNullOrWhiteSpace(area) ? "General" : area;
            string normalizedMessage = string.IsNullOrWhiteSpace(message) ? "(no message)" : message;
            string formatted = $"{Prefix}[{normalizedArea}][{level}] {normalizedMessage}";
            if (exception != null)
            {
                formatted = $"{formatted}\n{exception}";
            }

            switch (level)
            {
                case GameAudioDiagnosticLogLevel.Error:
                    Debug.LogError(formatted);
                    break;
                case GameAudioDiagnosticLogLevel.Warning:
                    Debug.LogWarning(formatted);
                    break;
                default:
                    Debug.Log(formatted);
                    break;
            }
        }
    }
}
