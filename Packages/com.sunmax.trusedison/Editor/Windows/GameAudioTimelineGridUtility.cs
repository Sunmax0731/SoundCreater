using System;
using System.Collections.Generic;
using TorusEdison.Editor.Utilities;

namespace TorusEdison.Editor.Windows
{
    internal static class GameAudioTimelineGridUtility
    {
        private static readonly string[] SupportedDivisionsInternal = { "1/4", "1/8", "1/16", "1/32", "1/64" };

        public static IReadOnlyList<string> SupportedDivisions => SupportedDivisionsInternal;

        public static string NormalizeDivision(string value)
        {
            foreach (string division in SupportedDivisionsInternal)
            {
                if (string.Equals(division, value, StringComparison.Ordinal))
                {
                    return division;
                }
            }

            return "1/16";
        }

        public static float GetBeatStep(string division)
        {
            return NormalizeDivision(division) switch
            {
                "1/4" => 1.0f,
                "1/8" => 0.5f,
                "1/16" => 0.25f,
                "1/32" => 0.125f,
                "1/64" => GameAudioToolInfo.MinNoteDurationBeat,
                _ => 0.25f
            };
        }

        public static float SnapBeat(float beat, string division)
        {
            float step = GetBeatStep(division);
            if (step <= 0.0f)
            {
                return Math.Max(0.0f, beat);
            }

            return Math.Max(0.0f, (float)Math.Round(beat / step, MidpointRounding.AwayFromZero) * step);
        }

        public static float SnapDuration(float durationBeat, string division)
        {
            float step = Math.Max(GameAudioToolInfo.MinNoteDurationBeat, GetBeatStep(division));
            if (step <= 0.0f)
            {
                return Math.Max(GameAudioToolInfo.MinNoteDurationBeat, durationBeat);
            }

            float snapped = (float)Math.Round(durationBeat / step, MidpointRounding.AwayFromZero) * step;
            return Math.Max(GameAudioToolInfo.MinNoteDurationBeat, snapped);
        }
    }
}
