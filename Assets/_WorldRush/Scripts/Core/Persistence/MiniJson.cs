using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace WorldRush.Core
{
    /// <summary>
    /// Kleiner, sicherer JSON-Leser/-Schreiber ohne Unity-Abhängigkeit.
    /// Ergebnis: Dictionary&lt;string, object&gt;, List&lt;object&gt;, double, string, bool oder null.
    /// Begrenzt Länge und Verschachtelungstiefe, damit manipulierte Dateien keinen Schaden anrichten.
    /// </summary>
    public static class MiniJson
    {
        public const int MaxLength = 4 * 1024 * 1024;
        public const int MaxDepth = 32;

        public static bool TryParse(string json, out object value)
        {
            value = null;
            if (json == null || json.Length > MaxLength) return false;
            try
            {
                var p = new Parser(json);
                value = p.ParseValue(0);
                p.SkipWhitespace();
                return p.AtEnd;
            }
            catch (FormatException)
            {
                value = null;
                return false;
            }
        }

        public static string Serialize(object value)
        {
            var sb = new StringBuilder();
            Write(sb, value);
            return sb.ToString();
        }

        private static void Write(StringBuilder sb, object v)
        {
            switch (v)
            {
                case null: sb.Append("null"); break;
                case string s: WriteString(sb, s); break;
                case bool b: sb.Append(b ? "true" : "false"); break;
                case float f: sb.Append(Finite(f).ToString("R", CultureInfo.InvariantCulture)); break;
                case double d: sb.Append(Finite(d).ToString("R", CultureInfo.InvariantCulture)); break;
                case int i: sb.Append(i.ToString(CultureInfo.InvariantCulture)); break;
                case long l: sb.Append(l.ToString(CultureInfo.InvariantCulture)); break;
                case IDictionary<string, object> dict:
                    sb.Append('{');
                    bool first = true;
                    foreach (var kv in dict)
                    {
                        if (!first) sb.Append(',');
                        first = false;
                        WriteString(sb, kv.Key);
                        sb.Append(':');
                        Write(sb, kv.Value);
                    }
                    sb.Append('}');
                    break;
                case System.Collections.IEnumerable list:
                    sb.Append('[');
                    bool firstItem = true;
                    foreach (var item in list)
                    {
                        if (!firstItem) sb.Append(',');
                        firstItem = false;
                        Write(sb, item);
                    }
                    sb.Append(']');
                    break;
                default: throw new ArgumentException("Nicht serialisierbar: " + v.GetType());
            }
        }

        private static double Finite(double d) => double.IsNaN(d) || double.IsInfinity(d) ? 0d : d;

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
                    default:
                        if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4"));
                        else sb.Append(c);
                        break;
                }
            }
            sb.Append('"');
        }

        private sealed class Parser
        {
            private readonly string _s;
            private int _i;

            public Parser(string s) { _s = s; }
            public bool AtEnd => _i >= _s.Length;

            public void SkipWhitespace()
            {
                while (_i < _s.Length && char.IsWhiteSpace(_s[_i])) _i++;
            }

            public object ParseValue(int depth)
            {
                if (depth > MaxDepth) throw new FormatException("zu tief verschachtelt");
                SkipWhitespace();
                if (AtEnd) throw new FormatException("unerwartetes Ende");
                char c = _s[_i];
                switch (c)
                {
                    case '{': return ParseObject(depth);
                    case '[': return ParseArray(depth);
                    case '"': return ParseString();
                    case 't': Expect("true"); return true;
                    case 'f': Expect("false"); return false;
                    case 'n': Expect("null"); return null;
                    default:
                        if (c == '-' || char.IsDigit(c)) return ParseNumber();
                        throw new FormatException("unerwartetes Zeichen");
                }
            }

            private Dictionary<string, object> ParseObject(int depth)
            {
                var d = new Dictionary<string, object>();
                _i++;
                SkipWhitespace();
                if (Peek('}')) { _i++; return d; }
                while (true)
                {
                    SkipWhitespace();
                    if (!Peek('"')) throw new FormatException("Schlüssel erwartet");
                    string key = ParseString();
                    SkipWhitespace();
                    if (!Peek(':')) throw new FormatException(": erwartet");
                    _i++;
                    d[key] = ParseValue(depth + 1);
                    SkipWhitespace();
                    if (Peek(',')) { _i++; continue; }
                    if (Peek('}')) { _i++; return d; }
                    throw new FormatException(", oder } erwartet");
                }
            }

            private List<object> ParseArray(int depth)
            {
                var l = new List<object>();
                _i++;
                SkipWhitespace();
                if (Peek(']')) { _i++; return l; }
                while (true)
                {
                    l.Add(ParseValue(depth + 1));
                    SkipWhitespace();
                    if (Peek(',')) { _i++; continue; }
                    if (Peek(']')) { _i++; return l; }
                    throw new FormatException(", oder ] erwartet");
                }
            }

            private string ParseString()
            {
                var sb = new StringBuilder();
                _i++;
                while (!AtEnd)
                {
                    char c = _s[_i++];
                    if (c == '"') return sb.ToString();
                    if (c != '\\') { sb.Append(c); continue; }
                    if (AtEnd) break;
                    char e = _s[_i++];
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
                            if (_i + 4 > _s.Length) throw new FormatException("ungültiges \\u");
                            sb.Append((char)int.Parse(_s.Substring(_i, 4), NumberStyles.HexNumber, CultureInfo.InvariantCulture));
                            _i += 4;
                            break;
                        default: throw new FormatException("ungültiges Escape");
                    }
                }
                throw new FormatException("String nicht beendet");
            }

            private double ParseNumber()
            {
                int start = _i;
                while (!AtEnd && "+-0123456789.eE".IndexOf(_s[_i]) >= 0) _i++;
                if (!double.TryParse(_s.Substring(start, _i - start), NumberStyles.Float, CultureInfo.InvariantCulture, out double d))
                    throw new FormatException("ungültige Zahl");
                return d;
            }

            private void Expect(string word)
            {
                if (string.CompareOrdinal(_s, _i, word, 0, word.Length) != 0) throw new FormatException(word + " erwartet");
                _i += word.Length;
            }

            private bool Peek(char c) => !AtEnd && _s[_i] == c;
        }
    }
}
