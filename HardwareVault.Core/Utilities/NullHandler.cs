using System;

namespace HardwareVault.Core.Utilities
{
    public static class NullHandler
    {
        /// <summary>
        /// Converts null or empty strings to "Unknown"
        /// </summary>
        public static string HandleNull(string? value, string defaultValue = "Unknown")
        {
            return string.IsNullOrWhiteSpace(value) ? defaultValue : value.Trim();
        }

        /// <summary>
        /// Converts null strings to "Unknown" but preserves empty strings
        /// </summary>
        public static string HandleNullOnly(string? value, string defaultValue = "Unknown")
        {
            return value ?? defaultValue;
        }

        /// <summary>
        /// Handles nullable integers
        /// </summary>
        public static int HandleNull(int? value, int defaultValue = 0)
        {
            return value ?? defaultValue;
        }

        /// <summary>
        /// Handles nullable doubles
        /// </summary>
        public static double HandleNull(double? value, double defaultValue = 0.0)
        {
            return value ?? defaultValue;
        }

        /// <summary>
        /// Handles nullable DateTime
        /// </summary>
        public static string HandleNull(DateTime? value, string defaultValue = "Unknown")
        {
            return value?.ToString("yyyy-MM-dd HH:mm:ss") ?? defaultValue;
        }

        /// <summary>
        /// Filters out common placeholder values and converts them to "Unknown"
        /// </summary>
        public static string FilterPlaceholders(string? value, string defaultValue = "Unknown")
        {
            if (string.IsNullOrWhiteSpace(value)) 
                return defaultValue;

            var cleaned = value.Trim();
            var placeholders = new[] {
                "default string", "default", "unknown", "not available", "n/a", "na", "none",
                "to be filled by o.e.m.", "system manufacturer", "system product name",
                "system version", "type1productconfigid", "sku", "system sku", "x.x",
                "0000000000000000", "00000000"
            };

            return placeholders.Contains(cleaned.ToLowerInvariant()) ? defaultValue : cleaned;
        }

        /// <summary>
        /// Handles lists - converts null to empty list
        /// </summary>
        public static List<T> HandleNull<T>(List<T>? list)
        {
            return list ?? new List<T>();
        }
    }
}