using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Ashutosh.AnalyticsSdk.Internal.Serialization
{
    internal sealed class DeterministicJsonWriter
    {
        private readonly StringBuilder _sb = new StringBuilder(1024);

        public override string ToString() => _sb.ToString();

        public void WriteValue(object value)
        {
            if (value == null) { _sb.Append("null"); return; }

            switch (value)
            {
                case string s: WriteString(s); return;
                case bool b: _sb.Append(b ? "true" : "false"); return;
                case long l: _sb.Append(l.ToString(CultureInfo.InvariantCulture)); return;
                case int i: _sb.Append(((long)i).ToString(CultureInfo.InvariantCulture)); return;
                case double d: _sb.Append(d.ToString("R", CultureInfo.InvariantCulture)); return;
                case float f: _sb.Append(((double)f).ToString("R", CultureInfo.InvariantCulture)); return;

                case Dictionary<string, object> dict:
                    WriteObject(dict);
                    return;

                case IReadOnlyDictionary<string, object> roDict:
                    WriteObject(roDict);
                    return;

                case List<object> list:
                    WriteArray(list);
                    return;

                case IReadOnlyList<object> roList:
                    WriteArray(roList);
                    return;

                default:
                    // Unknown types shouldn't happen after validation; be strict.
                    _sb.Append("null");
                    return;
            }
        }

        public void WriteObject(IReadOnlyDictionary<string, object> obj)
        {
            _sb.Append('{');

            // Deterministic ordering
            var keys = new List<string>(obj.Count);
            foreach (var k in obj.Keys) keys.Add(k);
            keys.Sort(StringComparer.Ordinal);

            bool first = true;
            foreach (var key in keys)
            {
                if (!first) _sb.Append(',');
                first = false;

                WriteString(key);
                _sb.Append(':');
                WriteValue(obj[key]);
            }

            _sb.Append('}');
        }

        public void WriteObject(Dictionary<string, object> obj) => WriteObject((IReadOnlyDictionary<string, object>)obj);

        public void WriteArray(IReadOnlyList<object> arr)
        {
            _sb.Append('[');
            for (int i = 0; i < arr.Count; i++)
            {
                if (i > 0) _sb.Append(',');
                WriteValue(arr[i]);
            }
            _sb.Append(']');
        }

        public void WriteArray(List<object> arr) => WriteArray((IReadOnlyList<object>)arr);

        private void WriteString(string s)
        {
            _sb.Append('"');
            for (int i = 0; i < s.Length; i++)
            {
                char c = s[i];
                switch (c)
                {
                    case '"': _sb.Append("\\\""); break;
                    case '\\': _sb.Append("\\\\"); break;
                    case '\b': _sb.Append("\\b"); break;
                    case '\f': _sb.Append("\\f"); break;
                    case '\n': _sb.Append("\\n"); break;
                    case '\r': _sb.Append("\\r"); break;
                    case '\t': _sb.Append("\\t"); break;
                    default:
                        if (c < 32)
                        {
                            _sb.Append("\\u");
                            _sb.Append(((int)c).ToString("x4"));
                        }
                        else
                        {
                            _sb.Append(c);
                        }
                        break;
                }
            }
            _sb.Append('"');
        }
    }
}