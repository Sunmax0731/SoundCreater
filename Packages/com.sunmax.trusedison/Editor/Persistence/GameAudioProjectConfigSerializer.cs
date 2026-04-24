using System;
using System.IO;
using System.Text;
using TorusEdison.Editor.Config;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Utilities;
using UnityEngine;

namespace TorusEdison.Editor.Persistence
{
    public sealed class GameAudioProjectConfigSerializer
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public GameAudioProjectConfig LoadOrDefault(string path = null)
        {
            string resolvedPath = path ?? GameAudioConfigPaths.GetProjectConfigPath();
            if (!File.Exists(resolvedPath))
            {
                return new GameAudioProjectConfig();
            }

            try
            {
                return Deserialize(File.ReadAllText(resolvedPath, Utf8WithoutBom));
            }
            catch (Exception exception)
            {
                GameAudioDiagnosticLogger.Warning("Config", $"Project config fallback was applied for {resolvedPath}. {exception.Message}");
                return new GameAudioProjectConfig();
            }
        }

        public void Save(GameAudioProjectConfig config, string path = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            string resolvedPath = path ?? GameAudioConfigPaths.GetProjectConfigPath();
            string directoryPath = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(resolvedPath, Serialize(config), Utf8WithoutBom);
        }

        public string Serialize(GameAudioProjectConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var dto = new GameAudioProjectConfigDto
            {
                exportDirectory = config.ExportDirectory ?? string.Empty,
                autoRefreshAfterExport = config.AutoRefreshAfterExport,
                preferredSampleRate = config.PreferredSampleRate ?? 0,
                preferredChannelMode = config.PreferredChannelMode?.ToString() ?? string.Empty
            };

            return JsonUtility.ToJson(dto, true) + "\n";
        }

        public GameAudioProjectConfig Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new GameAudioProjectConfig();
            }

            GameAudioProjectConfigDto dto;
            try
            {
                dto = new GameAudioProjectConfigDto
                {
                    autoRefreshAfterExport = true
                };
                JsonUtility.FromJsonOverwrite(json, dto);
            }
            catch (Exception exception)
            {
                GameAudioDiagnosticLogger.Warning("Config", $"Project config JSON could not be parsed. {exception.Message}");
                return new GameAudioProjectConfig();
            }

            if (dto == null)
            {
                return new GameAudioProjectConfig();
            }

            GameAudioChannelMode? preferredChannelMode = null;
            if (Enum.TryParse(dto.preferredChannelMode, true, out GameAudioChannelMode parsedChannelMode))
            {
                preferredChannelMode = parsedChannelMode;
            }

            int? preferredSampleRate = null;
            if (GameAudioValidationUtility.IsSupportedSampleRate(dto.preferredSampleRate))
            {
                preferredSampleRate = dto.preferredSampleRate;
            }

            return new GameAudioProjectConfig
            {
                ExportDirectory = dto.exportDirectory ?? string.Empty,
                AutoRefreshAfterExport = dto.autoRefreshAfterExport,
                PreferredSampleRate = preferredSampleRate,
                PreferredChannelMode = preferredChannelMode
            };
        }
    }
}
