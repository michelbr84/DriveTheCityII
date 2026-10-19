using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace DriveTheCityII.Core.Persistence
{
    public enum JsonKind
    {
        Null,
        Bool,
        Number,
        String,
        Array,
        Object,
    }

    /// <summary>
    /// Minimal, dependency-free JSON value (SPEC-SAVE serialization). Objects keep insertion order so output is
    /// deterministic; numbers are doubles written with invariant culture and round-trip precision ("R").
    /// </summary>
    public sealed class JsonValue
    {
        private readonly List<KeyValuePair<string, JsonValue>> m_Object;
        private readonly List<JsonValue> m_Array;

        public JsonKind Kind { get; }
        public bool BoolValue { get; }
        public double NumberValue { get; }
        public string StringValue { get; }

        public static readonly JsonValue Null = new JsonValue(JsonKind.Null);

        private JsonValue(JsonKind kind)
        {
            Kind = kind;
            if (kind == JsonKind.Object)
            {
                m_Object = new List<KeyValuePair<string, JsonValue>>();
            }
            else if (kind == JsonKind.Array)
            {
                m_Array = new List<JsonValue>();
            }
        }

        private JsonValue(bool b) : this(JsonKind.Bool) => BoolValue = b;
        private JsonValue(double d) : this(JsonKind.Number) => NumberValue = d;
        private JsonValue(string s) : this(JsonKind.String) => StringValue = s ?? string.Empty;

        public static JsonValue Object() => new JsonValue(JsonKind.Object);
        public static JsonValue Array() => new JsonValue(JsonKind.Array);
        public static JsonValue From(bool b) => new JsonValue(b);
        public static JsonValue From(double d) => new JsonValue(d);
        public static JsonValue From(long l) => new JsonValue((double)l);
        public static JsonValue From(int i) => new JsonValue((double)i);
        public static JsonValue From(string s) => s == null ? Null : new JsonValue(s);

        public int Count => Kind == JsonKind.Array ? m_Array.Count : Kind == JsonKind.Object ? m_Object.Count : 0;

        public IEnumerable<KeyValuePair<string, JsonValue>> Properties => m_Object ?? (IEnumerable<KeyValuePair<string, JsonValue>>)System.Array.Empty<KeyValuePair<string, JsonValue>>();

        public IEnumerable<JsonValue> Items => m_Array ?? (IEnumerable<JsonValue>)System.Array.Empty<JsonValue>();

        public JsonValue this[int index] => Kind == JsonKind.Array && index >= 0 && index < m_Array.Count ? m_Array[index] : Null;

        public JsonValue this[string key]
        {
            get
            {
                if (Kind != JsonKind.Object)
                {
                    return Null;
                }
                for (int i = 0; i < m_Object.Count; i++)
                {
                    if (m_Object[i].Key == key)
                    {
                        return m_Object[i].Value;
                    }
                }
                return Null;
            }
        }

        public bool Has(string key)
        {
            if (Kind != JsonKind.Object)
            {
                return false;
            }
            for (int i = 0; i < m_Object.Count; i++)
            {
                if (m_Object[i].Key == key)
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>Sets (or replaces) a property on an object. Returns this for chaining.</summary>
        public JsonValue Set(string key, JsonValue value)
        {
            if (Kind != JsonKind.Object)
            {
                throw new InvalidOperationException("Not a JSON object");
            }
            value ??= Null;
            for (int i = 0; i < m_Object.Count; i++)
            {
                if (m_Object[i].Key == key)
                {
                    m_Object[i] = new KeyValuePair<string, JsonValue>(key, value);
                    return this;
                }
            }
            m_Object.Add(new KeyValuePair<string, JsonValue>(key, value));
            return this;
        }

        public JsonValue Set(string key, string value) => Set(key, From(value));
        public JsonValue Set(string key, double value) => Set(key, From(value));
        public JsonValue Set(string key, long value) => Set(key, From(value));
        public JsonValue Set(string key, int value) => Set(key, From(value));
        public JsonValue Set(string key, bool value) => Set(key, From(value));

        public JsonValue Add(JsonValue value)
        {
            if (Kind != JsonKind.Array)
            {
                throw new InvalidOperationException("Not a JSON array");
            }
            m_Array.Add(value ?? Null);
            return this;
        }

        public string AsString(string fallback = null) => Kind == JsonKind.String ? StringValue : fallback;
        public double AsDouble(double fallback = 0) => Kind == JsonKind.Number ? NumberValue : fallback;
        public long AsLong(long fallback = 0) => Kind == JsonKind.Number ? (long)Math.Round(NumberValue) : fallback;
        public int AsInt(int fallback = 0) => Kind == JsonKind.Number ? (int)Math.Round(NumberValue) : fallback;
        public bool AsBool(bool fallback = false) => Kind == JsonKind.Bool ? BoolValue : fallback;

        // ------------------------------------------------------------------ writing

        public override string ToString() => ToJson(false);

        public string ToJson(bool indented)
        {
            var sb = new StringBuilder(256);
            Write(sb, indented, 0);
            return sb.ToString();
        }

        private void Write(StringBuilder sb, bool indented, int depth)
        {
            switch (Kind)
            {
                case JsonKind.Null:
                    sb.Append("null");
                    break;
                case JsonKind.Bool:
                    sb.Append(BoolValue ? "true" : "false");
                    break;
                case JsonKind.Number:
                    if (double.IsNaN(NumberValue) || double.IsInfinity(NumberValue))
                    {
                        sb.Append("null");
                    }
                    else if (NumberValue == Math.Floor(NumberValue) && Math.Abs(NumberValue) < 9e15)
                    {
                        sb.Append(((long)NumberValue).ToString(CultureInfo.InvariantCulture));
                    }
                    else
                    {
                        sb.Append(NumberValue.ToString("R", CultureInfo.InvariantCulture));
                    }
                    break;
                case JsonKind.String:
                    WriteString(sb, StringValue);
                    break;
                case JsonKind.Array:
                    sb.Append('[');
                    for (int i = 0; i < m_Array.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }
                        NewLine(sb, indented, depth + 1);
                        m_Array[i].Write(sb, indented, depth + 1);
                    }
                    if (m_Array.Count > 0)
                    {
                        NewLine(sb, indented, depth);
                    }
                    sb.Append(']');
                    break;
                case JsonKind.Object:
                    sb.Append('{');
                    for (int i = 0; i < m_Object.Count; i++)
                    {
                        if (i > 0)
                        {
                            sb.Append(',');
                        }
                        NewLine(sb, indented, depth + 1);
                        WriteString(sb, m_Object[i].Key);
                        sb.Append(indented ? ": " : ":");
                        m_Object[i].Value.Write(sb, indented, depth + 1);
                    }
                    if (m_Object.Count > 0)
                    {
                        NewLine(sb, indented, depth);
                    }
                    sb.Append('}');
                    break;
            }
        }

        private static void NewLine(StringBuilder sb, bool indented, int depth)
        {
            if (!indented)
            {
                return;
            }
            sb.Append('\n');
            sb.Append(' ', depth * 2);
        }

        private static void WriteString(StringBuilder sb, string s)
        {
            sb.Append('"');
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    case '\b': sb.Append("\\b"); break;
                    case '\f': sb.Append("\\f"); break;
                    default:
                        if (c < 0x20)
                        {
                            sb.Append("\\u").Append(((int)c).ToString("x4", CultureInfo.InvariantCulture));
                        }
                        else
                        {
                            sb.Append(c);
                        }
                        break;
                }
            }
            sb.Append('"');
        }

        // ------------------------------------------------------------------ parsing

        /// <summary>Parses JSON text. Throws <see cref="FormatException"/> on invalid input (never returns partial data).</summary>
        public static JsonValue Parse(string text)
        {
            if (text == null)
            {
                throw new FormatException("null JSON text");
            }
            int i = 0;
            JsonValue v = ParseValue(text, ref i, 0);
            SkipWs(text, ref i);
            if (i != text.Length)
            {
                throw new FormatException($"Trailing characters at {i}");
            }
            return v;
        }

        public static bool TryParse(string text, out JsonValue value, out string error)
        {
            try
            {
                value = Parse(text);
                error = null;
                return true;
            }
            catch (FormatException ex)
            {
                value = null;
                error = ex.Message;
                return false;
            }
        }

        private static void SkipWs(string s, ref int i)
        {
            while (i < s.Length && (s[i] == ' ' || s[i] == '\t' || s[i] == '\n' || s[i] == '\r'))
            {
                i++;
            }
        }

        private static JsonValue ParseValue(string s, ref int i, int depth)
        {
            if (depth > 64)
            {
                throw new FormatException("JSON nesting too deep");
            }
            SkipWs(s, ref i);
            if (i >= s.Length)
            {
                throw new FormatException("Unexpected end of JSON");
            }
            char c = s[i];
            switch (c)
            {
                case '{':
                {
                    i++;
                    JsonValue o = Object();
                    SkipWs(s, ref i);
                    if (i < s.Length && s[i] == '}')
                    {
                        i++;
                        return o;
                    }
                    while (true)
                    {
                        SkipWs(s, ref i);
                        if (i >= s.Length || s[i] != '"')
                        {
                            throw new FormatException($"Expected property name at {i}");
                        }
                        string key = ParseString(s, ref i);
                        SkipWs(s, ref i);
                        if (i >= s.Length || s[i] != ':')
                        {
                            throw new FormatException($"Expected ':' at {i}");
                        }
                        i++;
                        o.Set(key, ParseValue(s, ref i, depth + 1));
                        SkipWs(s, ref i);
                        if (i < s.Length && s[i] == ',')
                        {
                            i++;
                            continue;
                        }
                        if (i < s.Length && s[i] == '}')
                        {
                            i++;
                            return o;
                        }
                        throw new FormatException($"Expected ',' or '}}' at {i}");
                    }
                }
                case '[':
                {
                    i++;
                    JsonValue a = Array();
                    SkipWs(s, ref i);
                    if (i < s.Length && s[i] == ']')
                    {
                        i++;
                        return a;
                    }
                    while (true)
                    {
                        a.Add(ParseValue(s, ref i, depth + 1));
                        SkipWs(s, ref i);
                        if (i < s.Length && s[i] == ',')
                        {
                            i++;
                            continue;
                        }
                        if (i < s.Length && s[i] == ']')
                        {
                            i++;
                            return a;
                        }
                        throw new FormatException($"Expected ',' or ']' at {i}");
                    }
                }
                case '"':
                    return From(ParseString(s, ref i));
                case 't':
                    Expect(s, ref i, "true");
                    return From(true);
                case 'f':
                    Expect(s, ref i, "false");
                    return From(false);
                case 'n':
                    Expect(s, ref i, "null");
                    return Null;
                default:
                    if (c == '-' || (c >= '0' && c <= '9'))
                    {
                        int start = i;
                        i++;
                        while (i < s.Length && "0123456789+-.eE".IndexOf(s[i]) >= 0)
                        {
                            i++;
                        }
                        string num = s.Substring(start, i - start);
                        if (!double.TryParse(num, NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                        {
                            throw new FormatException($"Invalid number '{num}'");
                        }
                        return From(d);
                    }
                    throw new FormatException($"Unexpected character '{c}' at {i}");
            }
        }

        private static void Expect(string s, ref int i, string word)
        {
            if (string.CompareOrdinal(s, i, word, 0, word.Length) != 0)
            {
                throw new FormatException($"Expected '{word}' at {i}");
            }
            i += word.Length;
        }

        private static string ParseString(string s, ref int i)
        {
            i++; // opening quote
            var sb = new StringBuilder();
            while (i < s.Length)
            {
                char c = s[i++];
                if (c == '"')
                {
                    return sb.ToString();
                }
                if (c != '\\')
                {
                    sb.Append(c);
                    continue;
                }
                if (i >= s.Length)
                {
                    break;
                }
                char e = s[i++];
                switch (e)
                {
                    case '"': sb.Append('"'); break;
                    case '\\': sb.Append('\\'); break;
                    case '/': sb.Append('/'); break;
                    case 'b': sb.Append('\b'); break;
                    case 'f': sb.Append('\f'); break;
                    case 'n': sb.Append('\n'); break;
                    case 'r': sb.Append('\r'); break;
                    case 't': sb.Append('\t'); break;
                    case 'u':
                        if (i + 4 > s.Length)
                        {
                            throw new FormatException("Bad unicode escape");
                        }
                        sb.Append((char)int.Parse(s.Substring(i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                        i += 4;
                        break;
                    default:
                        throw new FormatException($"Bad escape '\\{e}'");
                }
            }
            throw new FormatException("Unterminated string");
        }
    }
}
