using System;
using System.Collections;
using System.Collections.Generic;

namespace Ashutosh.AnalyticsSdk.Internal.Validation
{
    internal static class EventValidator
    {
        // Keep these as constants for now (you can move to config later if you want).
        private const int MaxEventNameLength = 64;
        private const int MaxKeyLength = 64;
        private const int MaxProperties = 50;
        private const int MaxStringLength = 256;
        private const int MaxDepth = 3;

        internal static bool TrySanitize(
            string eventName,
            IReadOnlyDictionary<string, object> properties,
            out string sanitizedEventName,
            out Dictionary<string, object> sanitizedProperties,
            out string error)
        {
            sanitizedEventName = null;
            sanitizedProperties = null;
            error = null;

            if (string.IsNullOrWhiteSpace(eventName))
            {
                error = "Event name is null/empty.";
                return false;
            }

            sanitizedEventName = eventName.Trim();
            if (sanitizedEventName.Length > MaxEventNameLength)
                sanitizedEventName = sanitizedEventName.Substring(0, MaxEventNameLength);

            // Properties are optional.
            if (properties == null || properties.Count == 0)
            {
                sanitizedProperties = new Dictionary<string, object>(0);
                return true;
            }

            sanitizedProperties = new Dictionary<string, object>(Math.Min(properties.Count, MaxProperties));

            int added = 0;
            foreach (var kvp in properties)
            {
                if (added >= MaxProperties) break;

                var key = kvp.Key;
                if (string.IsNullOrWhiteSpace(key)) continue;

                key = key.Trim();
                if (key.Length > MaxKeyLength)
                    key = key.Substring(0, MaxKeyLength);

                // Avoid duplicates after truncation.
                if (sanitizedProperties.ContainsKey(key)) continue;

                if (TrySanitizeValue(kvp.Value, depth: 0, out var sanitizedValue))
                {
                    sanitizedProperties[key] = sanitizedValue;
                    added++;
                }
                // else: drop invalid values quietly
            }

            return true;
        }

        private static bool TrySanitizeValue(object value, int depth, out object sanitized)
        {
            sanitized = null;

            if (value == null) return false;
            if (depth > MaxDepth) return false;

            switch (value)
            {
                case string s:
                    sanitized = (s.Length > MaxStringLength) ? s.Substring(0, MaxStringLength) : s;
                    return true;

                case bool b:
                    sanitized = b;
                    return true;

                case int i:
                    sanitized = (long)i; // normalize ints -> long
                    return true;

                case long l:
                    sanitized = l;
                    return true;

                case float f:
                    sanitized = (double)f; // normalize floats -> double
                    return true;

                case double d:
                    sanitized = d;
                    return true;

                case decimal m:
                    sanitized = (double)m; // simple normalization; acceptable for analytics
                    return true;

                case DateTimeOffset dto:
                    sanitized = dto.ToUniversalTime().ToString("o"); // ISO 8601
                    return true;

                case DateTime dt:
                    sanitized = new DateTimeOffset(dt.ToUniversalTime()).ToString("o");
                    return true;
            }

            // Dictionaries (nested objects)
            if (value is IReadOnlyDictionary<string, object> roDict)
            {
                return TrySanitizeDict(roDict, depth + 1, out sanitized);
            }

            if (value is IDictionary dict)
            {
                return TrySanitizeNonGenericDict(dict, depth + 1, out sanitized);
            }

            // Lists / arrays
            if (value is IReadOnlyList<object> roList)
            {
                return TrySanitizeList(roList, depth + 1, out sanitized);
            }

            if (value is IEnumerable enumerable && !(value is string))
            {
                return TrySanitizeEnumerable(enumerable, depth + 1, out sanitized);
            }

            // Not supported (UnityEngine.Object, Vector3, custom classes, etc.)
            return false;
        }

        private static bool TrySanitizeDict(
            IReadOnlyDictionary<string, object> input,
            int depth,
            out object sanitized)
        {
            var result = new Dictionary<string, object>();

            foreach (var kvp in input)
            {
                var key = kvp.Key;
                if (string.IsNullOrWhiteSpace(key)) continue;

                key = key.Trim();
                if (key.Length > MaxKeyLength) key = key.Substring(0, MaxKeyLength);
                if (result.ContainsKey(key)) continue;

                if (TrySanitizeValue(kvp.Value, depth, out var valueSanitized))
                {
                    result[key] = valueSanitized;
                }
            }

            sanitized = result;
            return true;
        }

        private static bool TrySanitizeNonGenericDict(
            IDictionary input,
            int depth,
            out object sanitized)
        {
            var result = new Dictionary<string, object>();

            foreach (DictionaryEntry entry in input)
            {
                if (!(entry.Key is string key)) continue;
                if (string.IsNullOrWhiteSpace(key)) continue;

                key = key.Trim();
                if (key.Length > MaxKeyLength) key = key.Substring(0, MaxKeyLength);
                if (result.ContainsKey(key)) continue;

                if (TrySanitizeValue(entry.Value, depth, out var valueSanitized))
                {
                    result[key] = valueSanitized;
                }
            }

            sanitized = result;
            return true;
        }

        private static bool TrySanitizeList(
            IReadOnlyList<object> input,
            int depth,
            out object sanitized)
        {
            var result = new List<object>(input.Count);
            foreach (var item in input)
            {
                if (TrySanitizeValue(item, depth, out var itemSanitized))
                {
                    result.Add(itemSanitized);
                }
            }

            sanitized = result;
            return true;
        }

        private static bool TrySanitizeEnumerable(
            IEnumerable input,
            int depth,
            out object sanitized)
        {
            var result = new List<object>();
            foreach (var item in input)
            {
                if (TrySanitizeValue(item, depth, out var itemSanitized))
                {
                    result.Add(itemSanitized);
                }
            }

            sanitized = result;
            return true;
        }
    }
}
