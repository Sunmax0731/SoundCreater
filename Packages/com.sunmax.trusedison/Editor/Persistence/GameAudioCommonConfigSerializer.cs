using System;
using System.IO;
using System.Text;
using TorusEdison.Editor.Config;
using TorusEdison.Editor.Domain;
using TorusEdison.Editor.Localization;
using TorusEdison.Editor.Utilities;
using UnityEngine;

namespace TorusEdison.Editor.Persistence
{
    public sealed class GameAudioCommonConfigSerializer
    {
        private static readonly UTF8Encoding Utf8WithoutBom = new UTF8Encoding(false);

        public GameAudioCommonConfig LoadOrDefault(string path = null)
        {
            string resolvedPath = path ?? GameAudioConfigPaths.GetCommonConfigPath();
            if (!File.Exists(resolvedPath))
            {
                return new GameAudioCommonConfig();
            }

            try
            {
                return Deserialize(File.ReadAllText(resolvedPath, Utf8WithoutBom));
            }
            catch (Exception)
            {
                return new GameAudioCommonConfig();
            }
        }

        public void Save(GameAudioCommonConfig config, string path = null)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            string resolvedPath = path ?? GameAudioConfigPaths.GetCommonConfigPath();
            string directoryPath = Path.GetDirectoryName(resolvedPath);
            if (!string.IsNullOrWhiteSpace(directoryPath))
            {
                Directory.CreateDirectory(directoryPath);
            }

            File.WriteAllText(resolvedPath, Serialize(config), Utf8WithoutBom);
        }

        public string Serialize(GameAudioCommonConfig config)
        {
            if (config == null)
            {
                throw new ArgumentNullException(nameof(config));
            }

            var dto = new GameAudioCommonConfigDto
            {
                defaultSampleRate = GameAudioValidationUtility.IsSupportedSampleRate(config.DefaultSampleRate)
                    ? config.DefaultSampleRate
                    : GameAudioToolInfo.DefaultSampleRate,
                defaultChannelMode = config.DefaultChannelMode.ToString(),
                defaultExportDirectory = string.IsNullOrWhiteSpace(config.DefaultExportDirectory)
                    ? "Exports/Audio"
                    : config.DefaultExportDirectory,
                showStartupGuide = config.ShowStartupGuide,
                rememberLastProject = config.RememberLastProject,
                defaultGridDivision = string.IsNullOrWhiteSpace(config.DefaultGridDivision)
                    ? "1/16"
                    : config.DefaultGridDivision,
                undoHistoryLimit = GameAudioValidationUtility.ClampInt(config.UndoHistoryLimit, 1, 1000),
                displayLanguage = config.DisplayLanguage.ToString()
            };

            return JsonUtility.ToJson(dto, true) + "\n";
        }

        public GameAudioCommonConfig Deserialize(string json)
        {
            if (string.IsNullOrWhiteSpace(json))
            {
                return new GameAudioCommonConfig();
            }

            GameAudioCommonConfigDto dto;
            try
            {
                dto = JsonUtility.FromJson<GameAudioCommonConfigDto>(json);
            }
            catch (Exception)
            {
                return new GameAudioCommonConfig();
            }

            if (dto == null)
            {
                return new GameAudioCommonConfig();
            }

            return new GameAudioCommonConfig
            {
                DefaultSampleRate = GameAudioValidationUtility.IsSupportedSampleRate(dto.defaultSampleRate)
                    ? dto.defaultSampleRate
                    : GameAudioToolInfo.DefaultSampleRate,
                DefaultChannelMode = Enum.TryParse(dto.defaultChannelMode, true, out GameAudioChannelMode channelMode)
                    ? channelMode
                    : GameAudioChannelMode.Stereo,
                DefaultExportDirectory = string.IsNullOrWhiteSpace(dto.defaultExportDirectory)
                    ? "Exports/Audio"
                    : dto.defaultExportDirectory,
                ShowStartupGuide = dto.showStartupGuide,
                RememberLastProject = dto.rememberLastProject,
                DefaultGridDivision = string.IsNullOrWhiteSpace(dto.defaultGridDivision)
                    ? "1/16"
                    : dto.defaultGridDivision,
                UndoHistoryLimit = GameAudioValidationUtility.ClampInt(dto.undoHistoryLimit <= 0 ? 100 : dto.undoHistoryLimit, 1, 1000),
                DisplayLanguage = Enum.TryParse(dto.displayLanguage, true, out GameAudioLanguageMode displayLanguage)
                    ? displayLanguage
                    : GameAudioLanguageMode.Auto
            };
        }
    }
}
