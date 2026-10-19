using System;
using System.Collections.Generic;
using DriveTheCityII.Core.Persistence;

namespace DriveTheCityII.Core.Career
{
    /// <summary>
    /// Strict reader over the JSON DOM. Every missing, mistyped, non-finite or out-of-range value is recorded as an
    /// error with its path, so the loader can quarantine the whole record instead of partially loading it (SPEC-SAVE).
    /// </summary>
    internal sealed class JsonRead
    {
        /// <summary>Sanity bound for money in cents (1e13 cents = 100 billion cr).</summary>
        public const long MaxCents = 10000000000000L;

        /// <summary>Sanity bound for coordinates, times and distances.</summary>
        public const double MaxMagnitude = 1e12;

        public readonly List<string> Errors = new List<string>();

        public bool Ok => Errors.Count == 0;

        public void Error(string path, string message)
        {
            if (Errors.Count < 200)
            {
                Errors.Add(path + ": " + message);
            }
        }

        public JsonValue Obj(JsonValue o, string key, string path, bool required = true)
        {
            JsonValue v = o[key];
            if (v.Kind == JsonKind.Object)
            {
                return v;
            }
            if (required || v.Kind != JsonKind.Null)
            {
                Error(path + "." + key, "expected object");
            }
            return null;
        }

        public JsonValue Arr(JsonValue o, string key, string path)
        {
            JsonValue v = o[key];
            if (v.Kind == JsonKind.Array)
            {
                return v;
            }
            Error(path + "." + key, "expected array");
            return JsonValue.Array();
        }

        public double Num(JsonValue o, string key, string path, double min = -MaxMagnitude, double max = MaxMagnitude)
        {
            JsonValue v = o[key];
            if (v.Kind != JsonKind.Number)
            {
                Error(path + "." + key, "expected number");
                return 0;
            }
            double d = v.NumberValue;
            if (!CareerMath.IsFinite(d) || d < min || d > max)
            {
                Error(path + "." + key, "number out of range");
                return 0;
            }
            return d;
        }

        public long Long(JsonValue o, string key, string path, long min = -MaxCents, long max = MaxCents)
        {
            JsonValue v = o[key];
            if (v.Kind != JsonKind.Number)
            {
                Error(path + "." + key, "expected integer");
                return 0;
            }
            double d = v.NumberValue;
            if (!CareerMath.IsFinite(d) || d != Math.Floor(d) || d < min || d > max)
            {
                Error(path + "." + key, "integer out of range");
                return 0;
            }
            return (long)d;
        }

        public int Int(JsonValue o, string key, string path, int min = int.MinValue, int max = int.MaxValue)
        {
            return (int)Long(o, key, path, min, max);
        }

        public bool Bool(JsonValue o, string key, string path)
        {
            JsonValue v = o[key];
            if (v.Kind != JsonKind.Bool)
            {
                Error(path + "." + key, "expected bool");
                return false;
            }
            return v.BoolValue;
        }

        /// <summary>Reads a string. Required strings must be non-empty; optional ones may be null.</summary>
        public string Str(JsonValue o, string key, string path, bool required = true)
        {
            JsonValue v = o[key];
            if (v.Kind == JsonKind.String)
            {
                if (required && v.StringValue.Length == 0)
                {
                    Error(path + "." + key, "empty string");
                }
                return v.StringValue;
            }
            if (v.Kind == JsonKind.Null && !required)
            {
                return null;
            }
            Error(path + "." + key, "expected string");
            return null;
        }

        public T Enum<T>(JsonValue o, string key, string path) where T : struct
        {
            string s = Str(o, key, path);
            if (s != null && System.Enum.TryParse(s, false, out T value) && System.Enum.IsDefined(typeof(T), value) && !IsNumeric(s))
            {
                return value;
            }
            if (s != null)
            {
                Error(path + "." + key, "unknown value '" + s + "'");
            }
            return default;
        }

        public MapPoint Point(JsonValue o, string key, string path)
        {
            JsonValue p = Obj(o, key, path);
            if (p == null)
            {
                return default;
            }
            string sub = path + "." + key;
            return new MapPoint(Num(p, "x", sub), Num(p, "y", sub), Num(p, "z", sub));
        }

        public static JsonValue WritePoint(MapPoint p)
        {
            return JsonValue.Object().Set("x", p.X).Set("y", p.Y).Set("z", p.Z);
        }

        public List<string> StrList(JsonValue o, string key, string path)
        {
            var list = new List<string>();
            JsonValue a = Arr(o, key, path);
            int i = 0;
            foreach (JsonValue item in a.Items)
            {
                if (item.Kind != JsonKind.String || item.StringValue.Length == 0)
                {
                    Error(path + "." + key + "[" + i + "]", "expected string");
                }
                else
                {
                    list.Add(item.StringValue);
                }
                i++;
            }
            return list;
        }

        public static JsonValue WriteStrList(IEnumerable<string> items)
        {
            JsonValue a = JsonValue.Array();
            foreach (string s in items)
            {
                a.Add(JsonValue.From(s));
            }
            return a;
        }

        private static bool IsNumeric(string s)
        {
            return s.Length > 0 && (char.IsDigit(s[0]) || s[0] == '-');
        }
    }
}
