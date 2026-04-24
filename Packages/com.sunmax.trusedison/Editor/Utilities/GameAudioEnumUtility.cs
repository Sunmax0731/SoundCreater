using System;

namespace TorusEdison.Editor.Utilities
{
    internal static class GameAudioEnumUtility
    {
        public static bool TryParseDefined<TEnum>(string value, out TEnum result)
            where TEnum : struct
        {
            if (Enum.TryParse(value, true, out result) && Enum.IsDefined(typeof(TEnum), result))
            {
                return true;
            }

            result = default;
            return false;
        }
    }
}
