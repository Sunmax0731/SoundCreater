using System;
using System.IO;
using System.Text;
using GameAudioTool.Editor.Config;
using GameAudioTool.Editor.Domain;
using GameAudioTool.Editor.Utilities;
using UnityEngine;

namespace GameAudioTool.Editor.Persistence
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

            return Deserialize(File.ReadAllText(resolvedPath, Utf8WithoutBom));
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

            var dto = JsonUtility.FromJson<GameAudioProjectConfigDto>(json);
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
