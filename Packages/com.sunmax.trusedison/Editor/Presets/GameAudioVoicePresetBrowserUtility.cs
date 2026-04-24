using System;
using System.Collections.Generic;
using System.Linq;

namespace TorusEdison.Editor.Presets
{
    internal sealed class GameAudioVoicePresetBrowserEntry
    {
        public GameAudioVoicePresetBrowserEntry(string key, GameAudioVoicePreset preset, string sourcePath, bool isBuiltIn)
        {
            Key = key ?? string.Empty;
            Preset = preset ?? throw new ArgumentNullException(nameof(preset));
            SourcePath = sourcePath ?? string.Empty;
            IsBuiltIn = isBuiltIn;
        }

        public string Key { get; }

        public GameAudioVoicePreset Preset { get; }

        public string SourcePath { get; }

        public bool IsBuiltIn { get; }
    }

    internal sealed class GameAudioVoicePresetBrowserError
    {
        public GameAudioVoicePresetBrowserError(string sourcePath, string message)
        {
            SourcePath = sourcePath ?? string.Empty;
            Message = message ?? string.Empty;
        }

        public string SourcePath { get; }

        public string Message { get; }
    }

    internal static class GameAudioVoicePresetBrowserUtility
    {
        public const string BuiltInKeyPrefix = "builtin:";
        public const string UserKeyPrefix = "user:";
        public const int MaxRecentPresetCount = 6;

        public static string CreateBuiltInKey(GameAudioVoicePreset preset)
        {
            return BuiltInKeyPrefix + (preset?.Id ?? string.Empty);
        }

        public static string CreateUserKey(string sourcePath)
        {
            return UserKeyPrefix + (sourcePath ?? string.Empty);
        }

        public static IReadOnlyList<GameAudioVoicePresetBrowserEntry> CreateBuiltInEntries(IEnumerable<GameAudioVoicePreset> presets)
        {
            return (presets ?? Array.Empty<GameAudioVoicePreset>())
                .Where(preset => preset != null)
                .Select(preset => new GameAudioVoicePresetBrowserEntry(CreateBuiltInKey(preset), preset, string.Empty, true))
                .ToArray();
        }

        public static IReadOnlyList<GameAudioVoicePresetBrowserEntry> FilterEntries(
            IEnumerable<GameAudioVoicePresetBrowserEntry> entries,
            string searchQuery,
            string categoryOrTagFilter)
        {
            string[] tokens = Tokenize(searchQuery);
            string normalizedFilter = NormalizeFilter(categoryOrTagFilter);

            return (entries ?? Array.Empty<GameAudioVoicePresetBrowserEntry>())
                .Where(entry => entry?.Preset != null)
                .Where(entry => MatchesFilter(entry.Preset, normalizedFilter))
                .Where(entry => MatchesSearch(entry.Preset, tokens))
                .OrderBy(entry => entry.Preset.Category, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Preset.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ThenBy(entry => entry.Key, StringComparer.Ordinal)
                .ToArray();
        }

        public static IReadOnlyList<string> GetCategoryAndTagFilters(IEnumerable<GameAudioVoicePresetBrowserEntry> entries)
        {
            return (entries ?? Array.Empty<GameAudioVoicePresetBrowserEntry>())
                .Where(entry => entry?.Preset != null)
                .SelectMany(entry => EnumerateCategoryAndTags(entry.Preset))
                .Where(value => !string.IsNullOrWhiteSpace(value))
                .Select(value => value.Trim())
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(value => value, StringComparer.OrdinalIgnoreCase)
                .ToArray();
        }

        public static string[] AddRecentPresetKey(IEnumerable<string> recentKeys, string selectedKey, IEnumerable<GameAudioVoicePresetBrowserEntry> entries)
        {
            if (string.IsNullOrWhiteSpace(selectedKey))
            {
                return NormalizeRecentPresetKeys(recentKeys, entries);
            }

            string[] normalizedExisting = NormalizeRecentPresetKeys(recentKeys, entries);
            HashSet<string> knownKeys = BuildKnownKeySet(entries);
            if (!knownKeys.Contains(selectedKey))
            {
                return normalizedExisting;
            }

            return new[] { selectedKey }
                .Concat(normalizedExisting.Where(key => !string.Equals(key, selectedKey, StringComparison.Ordinal)))
                .Take(MaxRecentPresetCount)
                .ToArray();
        }

        public static string[] NormalizeRecentPresetKeys(IEnumerable<string> recentKeys, IEnumerable<GameAudioVoicePresetBrowserEntry> entries)
        {
            HashSet<string> knownKeys = BuildKnownKeySet(entries);
            return (recentKeys ?? Array.Empty<string>())
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Where(key => knownKeys.Count == 0 || knownKeys.Contains(key))
                .Distinct(StringComparer.Ordinal)
                .Take(MaxRecentPresetCount)
                .ToArray();
        }

        public static IReadOnlyList<GameAudioVoicePresetBrowserEntry> ResolveRecentEntries(
            IEnumerable<GameAudioVoicePresetBrowserEntry> entries,
            IEnumerable<string> recentKeys)
        {
            Dictionary<string, GameAudioVoicePresetBrowserEntry> byKey = (entries ?? Array.Empty<GameAudioVoicePresetBrowserEntry>())
                .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.Key))
                .GroupBy(entry => entry.Key, StringComparer.Ordinal)
                .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);

            return (recentKeys ?? Array.Empty<string>())
                .Where(key => !string.IsNullOrWhiteSpace(key))
                .Select(key => key.Trim())
                .Where(key => byKey.ContainsKey(key))
                .Select(key => byKey[key])
                .ToArray();
        }

        public static string FormatEntryLabel(GameAudioVoicePresetBrowserEntry entry)
        {
            if (entry?.Preset == null)
            {
                return string.Empty;
            }

            return $"{entry.Preset.Category} / {entry.Preset.DisplayName}";
        }

        private static bool MatchesFilter(GameAudioVoicePreset preset, string normalizedFilter)
        {
            if (string.IsNullOrWhiteSpace(normalizedFilter))
            {
                return true;
            }

            return string.Equals(preset.Category, normalizedFilter, StringComparison.OrdinalIgnoreCase)
                || (preset.Tags ?? Array.Empty<string>()).Any(tag => string.Equals(tag, normalizedFilter, StringComparison.OrdinalIgnoreCase));
        }

        private static bool MatchesSearch(GameAudioVoicePreset preset, string[] tokens)
        {
            if (tokens == null || tokens.Length == 0)
            {
                return true;
            }

            string metadata = BuildSearchMetadata(preset);
            return tokens.All(token => metadata.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0);
        }

        private static string BuildSearchMetadata(GameAudioVoicePreset preset)
        {
            var parts = new List<string>
            {
                preset.Id,
                preset.Category,
                preset.DisplayName,
                preset.Description,
                preset.Voice?.Waveform.ToString(),
                preset.Voice?.NoiseType.ToString()
            };

            if (preset.Voice?.NoiseEnabled == true)
            {
                parts.Add("noise");
            }

            if (preset.Voice?.Effect?.Delay?.Enabled == true)
            {
                parts.Add("delay");
            }

            parts.AddRange(preset.Tags ?? Array.Empty<string>());
            return string.Join(" ", parts.Where(part => !string.IsNullOrWhiteSpace(part)));
        }

        private static IEnumerable<string> EnumerateCategoryAndTags(GameAudioVoicePreset preset)
        {
            if (!string.IsNullOrWhiteSpace(preset.Category))
            {
                yield return preset.Category;
            }

            foreach (string tag in preset.Tags ?? Array.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(tag))
                {
                    yield return tag;
                }
            }
        }

        private static string[] Tokenize(string searchQuery)
        {
            if (string.IsNullOrWhiteSpace(searchQuery))
            {
                return Array.Empty<string>();
            }

            return searchQuery
                .Split(new[] { ' ', '\t', '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(token => token.Trim())
                .Where(token => !string.IsNullOrWhiteSpace(token))
                .ToArray();
        }

        private static string NormalizeFilter(string categoryOrTagFilter)
        {
            return string.IsNullOrWhiteSpace(categoryOrTagFilter)
                ? string.Empty
                : categoryOrTagFilter.Trim();
        }

        private static HashSet<string> BuildKnownKeySet(IEnumerable<GameAudioVoicePresetBrowserEntry> entries)
        {
            return new HashSet<string>(
                (entries ?? Array.Empty<GameAudioVoicePresetBrowserEntry>())
                    .Where(entry => entry != null && !string.IsNullOrWhiteSpace(entry.Key))
                    .Select(entry => entry.Key),
                StringComparer.Ordinal);
        }
    }
}
