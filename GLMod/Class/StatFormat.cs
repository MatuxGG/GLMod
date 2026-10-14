using System;
using System.Globalization;

namespace GLMod.Class
{
    /// <summary>
    /// Pure formatting of the stats shown in the menu.
    /// </summary>
    public static class StatFormat
    {
        /// <summary>
        /// "54 s", "3 min 05 s", "12 min", "2 h 05"
        /// </summary>
        public static string Duration(double seconds)
        {
            long total = (long)Math.Round(Math.Max(0, seconds));
            if (total < 60)
            {
                return total + " s";
            }
            if (total < 600)
            {
                return (total / 60) + " min " + (total % 60).ToString("00", CultureInfo.InvariantCulture) + " s";
            }
            if (total < 3600)
            {
                return (total / 60) + " min";
            }
            return (total / 3600) + " h " + (total % 3600 / 60).ToString("00", CultureInfo.InvariantCulture);
        }

        /// <summary>
        /// Duration, or null when there is no value
        /// </summary>
        public static string Duration(double? seconds) => seconds.HasValue ? Duration(seconds.Value) : null;

        /// <summary>
        /// "3/4", or null when either side is missing
        /// </summary>
        public static string Ratio(int? value, int? total)
        {
            return value.HasValue && total.HasValue ? value.Value + "/" + total.Value : null;
        }

        /// <summary>
        /// "Yes" / "No", or null
        /// </summary>
        public static string YesNo(bool? value) => value.HasValue ? (value.Value ? "Yes" : "No") : null;

        /// <summary>
        /// Integer as is, decimal with at most 2 digits (invariant), or null
        /// </summary>
        public static string Number(double? value)
        {
            return value.HasValue ? Math.Round(value.Value, 2).ToString("0.##", CultureInfo.InvariantCulture) : null;
        }

        /// <summary>
        /// Integer, or null
        /// </summary>
        public static string Number(int? value) => value.HasValue ? value.Value.ToString(CultureInfo.InvariantCulture) : null;

        /// <summary>
        /// Unix seconds in the local time zone and culture (short date and time)
        /// </summary>
        public static string LocalDateTime(long unixSeconds, CultureInfo culture = null)
        {
            if (unixSeconds <= 0)
            {
                return "-";
            }
            return DateTimeOffset.FromUnixTimeSeconds(unixSeconds).ToLocalTime().ToString("g", culture ?? CultureInfo.CurrentCulture);
        }

        /// <summary>
        /// Vanilla roles are recorded as "Vanilla&lt;RoleTypes&gt;": "VanillaImpostor" -> "Impostor"
        /// </summary>
        public static string Role(string role, string team)
        {
            const string vanillaPrefix = "Vanilla";
            string value = role ?? "";
            if (value.StartsWith(vanillaPrefix, StringComparison.Ordinal) && value.Length > vanillaPrefix.Length)
            {
                value = value.Substring(vanillaPrefix.Length);
            }
            return string.IsNullOrEmpty(value) ? team ?? "" : value;
        }
    }
}
