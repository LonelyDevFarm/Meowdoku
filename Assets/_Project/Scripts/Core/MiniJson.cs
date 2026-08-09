using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;

namespace Meowdoku.Core
{
    // Runtime JSON parser used because Godot's JSON.parse_string returns dynamic
    // dictionaries while Unity's JsonUtility cannot deserialize numeric keys or jagged arrays.
    internal static class MiniJson
    {
        public static object Deserialize(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            using (var parser = new Parser(json)) return parser.ParseValue();
        }

        public static string Serialize(object value)
        {
            var builder = new StringBuilder(256);
            SerializeValue(value, builder);
            return builder.ToString();
        }

        private static void SerializeValue(object value, StringBuilder builder)
        {
            if (value == null)
            {
                builder.Append("null");
                return;
            }

            if (value is string text)
            {
                SerializeString(text, builder);
                return;
            }

            if (value is bool boolean)
            {
                builder.Append(boolean ? "true" : "false");
                return;
            }

            if (value is IDictionary<string, object> genericDictionary)
            {
                SerializeDictionary(genericDictionary, builder);
                return;
            }

            if (value is System.Collections.IDictionary dictionary)
            {
                SerializeDictionary(dictionary, builder);
                return;
            }

            if (value is System.Collections.IEnumerable enumerable)
            {
                SerializeArray(enumerable, builder);
                return;
            }

            if (IsNumeric(value))
            {
                builder.Append(Convert.ToString(value, CultureInfo.InvariantCulture));
                return;
            }

            throw new InvalidOperationException(
                $"MiniJson cannot serialize values of type {value.GetType().FullName}.");
        }

        private static void SerializeDictionary(
            IDictionary<string, object> dictionary,
            StringBuilder builder)
        {
            builder.Append('{');
            bool first = true;
            foreach (KeyValuePair<string, object> pair in dictionary)
            {
                if (!first) builder.Append(',');
                first = false;
                SerializeString(pair.Key, builder);
                builder.Append(':');
                SerializeValue(pair.Value, builder);
            }
            builder.Append('}');
        }

        private static void SerializeDictionary(
            System.Collections.IDictionary dictionary,
            StringBuilder builder)
        {
            builder.Append('{');
            bool first = true;
            foreach (System.Collections.DictionaryEntry pair in dictionary)
            {
                if (!(pair.Key is string key))
                {
                    throw new InvalidOperationException("MiniJson dictionary keys must be strings.");
                }

                if (!first) builder.Append(',');
                first = false;
                SerializeString(key, builder);
                builder.Append(':');
                SerializeValue(pair.Value, builder);
            }
            builder.Append('}');
        }

        private static void SerializeArray(
            System.Collections.IEnumerable values,
            StringBuilder builder)
        {
            builder.Append('[');
            bool first = true;
            foreach (object value in values)
            {
                if (!first) builder.Append(',');
                first = false;
                SerializeValue(value, builder);
            }
            builder.Append(']');
        }

        private static void SerializeString(string value, StringBuilder builder)
        {
            builder.Append('"');
            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                switch (character)
                {
                    case '"': builder.Append("\\\""); break;
                    case '\\': builder.Append("\\\\"); break;
                    case '\b': builder.Append("\\b"); break;
                    case '\f': builder.Append("\\f"); break;
                    case '\n': builder.Append("\\n"); break;
                    case '\r': builder.Append("\\r"); break;
                    case '\t': builder.Append("\\t"); break;
                    default:
                        if (character < 32 || character > 126)
                        {
                            builder.Append("\\u");
                            builder.Append(((int)character).ToString("x4"));
                        }
                        else
                        {
                            builder.Append(character);
                        }
                        break;
                }
            }
            builder.Append('"');
        }

        private static bool IsNumeric(object value)
        {
            return value is byte || value is sbyte || value is short || value is ushort ||
                   value is int || value is uint || value is long || value is ulong ||
                   value is float || value is double || value is decimal;
        }

        private sealed class Parser : IDisposable
        {
            private readonly StringReader _reader;

            public Parser(string json) { _reader = new StringReader(json); }
            public void Dispose() { _reader.Dispose(); }

            public object ParseValue()
            {
                EatWhitespace();
                int next = _reader.Peek();
                if (next == -1) return null;
                switch ((char)next)
                {
                    case '{': return ParseObject();
                    case '[': return ParseArray();
                    case '"': return ParseString();
                    case 't': ConsumeLiteral("true"); return true;
                    case 'f': ConsumeLiteral("false"); return false;
                    case 'n': ConsumeLiteral("null"); return null;
                    default: return ParseNumber();
                }
            }

            private Dictionary<string, object> ParseObject()
            {
                var result = new Dictionary<string, object>();
                _reader.Read();
                while (true)
                {
                    EatWhitespace();
                    if (_reader.Peek() == '}') { _reader.Read(); return result; }
                    string key = ParseString();
                    EatWhitespace();
                    Expect(':');
                    result[key] = ParseValue();
                    EatWhitespace();
                    int separator = _reader.Read();
                    if (separator == '}') return result;
                    if (separator != ',') throw Error("Expected ',' or '}'.");
                }
            }

            private List<object> ParseArray()
            {
                var result = new List<object>();
                _reader.Read();
                while (true)
                {
                    EatWhitespace();
                    if (_reader.Peek() == ']') { _reader.Read(); return result; }
                    result.Add(ParseValue());
                    EatWhitespace();
                    int separator = _reader.Read();
                    if (separator == ']') return result;
                    if (separator != ',') throw Error("Expected ',' or ']'.");
                }
            }

            private string ParseString()
            {
                Expect('"');
                var value = new StringBuilder();
                while (true)
                {
                    int next = _reader.Read();
                    if (next == -1) throw Error("Unterminated string.");
                    char c = (char)next;
                    if (c == '"') return value.ToString();
                    if (c != '\\') { value.Append(c); continue; }

                    int escaped = _reader.Read();
                    if (escaped == -1) throw Error("Unterminated escape sequence.");
                    switch ((char)escaped)
                    {
                        case '"': value.Append('"'); break;
                        case '\\': value.Append('\\'); break;
                        case '/': value.Append('/'); break;
                        case 'b': value.Append('\b'); break;
                        case 'f': value.Append('\f'); break;
                        case 'n': value.Append('\n'); break;
                        case 'r': value.Append('\r'); break;
                        case 't': value.Append('\t'); break;
                        case 'u': value.Append(ParseUnicode()); break;
                        default: throw Error("Invalid escape sequence.");
                    }
                }
            }

            private char ParseUnicode()
            {
                var digits = new char[4];
                for (int i = 0; i < digits.Length; i++)
                {
                    int next = _reader.Read();
                    if (next == -1) throw Error("Incomplete unicode escape.");
                    digits[i] = (char)next;
                }
                return (char)int.Parse(new string(digits), NumberStyles.HexNumber, CultureInfo.InvariantCulture);
            }

            private object ParseNumber()
            {
                var number = new StringBuilder();
                while (_reader.Peek() != -1 && "-+0123456789.eE".IndexOf((char)_reader.Peek()) >= 0)
                {
                    number.Append((char)_reader.Read());
                }
                string text = number.ToString();
                if (text.Length == 0) throw Error("Expected a JSON value.");
                if (text.IndexOfAny(new[] { '.', 'e', 'E' }) >= 0)
                {
                    return double.Parse(text, CultureInfo.InvariantCulture);
                }
                return long.Parse(text, CultureInfo.InvariantCulture);
            }

            private void ConsumeLiteral(string literal)
            {
                foreach (char expected in literal)
                {
                    if (_reader.Read() != expected) throw Error($"Expected '{literal}'.");
                }
            }

            private void Expect(char expected)
            {
                if (_reader.Read() != expected) throw Error($"Expected '{expected}'.");
            }

            private void EatWhitespace()
            {
                while (_reader.Peek() != -1 && char.IsWhiteSpace((char)_reader.Peek())) _reader.Read();
            }

            private static FormatException Error(string message) { return new FormatException(message); }
        }
    }
}
