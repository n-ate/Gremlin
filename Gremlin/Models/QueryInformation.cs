using n_ate.Essentials.Serialization;
using ExRam.Gremlinq.Core;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace n_ate.Gremlin.Models
{
    /// <summary>
    /// Provides information about how the query is finally constructed. Properties are lazy loaded to guarantee minimal overhead.
    /// </summary>
    public class QueryInformation
    {
        private const int TAB_SPACES = 2;
        private static Regex _getGremlinHasSteps = new Regex("has\\((?<key>[a-z_]+),(?<value>[a-z_]+|[a-z]+\\([a-z_,]+\\))\\)");
        private ReadOnlyDictionary<string, object[]>? _arguments = null;
        private QueryDebug? _basicInfo = null;
        private ReadOnlyDictionary<string, object>? _bindings = null;
        private string? _formattedScript = null;
        private string? _normalizedScript = null;
        private string _raw;
        private ReadOnlyDictionary<string, string>? _stringBindings = null;

        internal QueryInformation(IGremlinQueryBase query)
        {
            _raw = query.Debug();
        }

        public ReadOnlyDictionary<string, object[]>? Arguments
        {
            get
            {
                if (_arguments is null && ParameterizedScript is not null)
                {
                    var result = new Dictionary<string, List<object>>();
                    var matches = _getGremlinHasSteps.Matches(ParameterizedScript);
                    foreach (Match match in matches)
                    {
                        var key = match.Groups["key"].Value;
                        var value = match.Groups["value"].Value;
                        var k = ResolveBindingKeys(key).ToString()!;
                        var v = ResolveBindingKeys(value);
                        if (result.ContainsKey(k)) result[k].Add(v);
                        else result[k] = new List<object> { v };
                    }
                    _arguments = new ReadOnlyDictionary<string, object[]>(result.ToDictionary(kv => kv.Key, kv => kv.Value.ToArray()));
                }
                return _arguments;
            }
        }

        public ReadOnlyDictionary<string, object> Bindings
        {
            get
            {
                if (_bindings is null)
                {
                    _bindings = new ReadOnlyDictionary<string, object>(
                     BasicInfo?.Bindings?.ToDictionary(kv =>
                         kv.Key,
                         kv =>
                         {
                             if (kv.Value.GetType() == typeof(double) && kv.Value.ToString()!.Contains("E+")) return Convert.ToUInt64(kv.Value);
                             else return kv.Value;
                         }
                     ) ?? new Dictionary<string, object>()
                 );
                }
                return _bindings;
            }
        }

        public string? FormattedScript
        {
            get
            {
                if (_formattedScript is null) _formattedScript = FriendlyFormatScript(NormalizedScript, TAB_SPACES);
                return _formattedScript;
            }
        }

        public string? Id => BasicInfo?.Id;

        public string? NormalizedScript
        {
            get
            {
                if (_normalizedScript is null) _normalizedScript = ResolveBindingKeyAsStrings(BasicInfo?.Script);
                return _normalizedScript;
            }
        }

        public string? ParameterizedScript => BasicInfo?.Script;

        public ReadOnlyDictionary<string, string> StringBindings
        {
            get
            {
                if (_stringBindings is null)
                {
                    _stringBindings = new ReadOnlyDictionary<string, string>(
                     Bindings.ToDictionary(kv =>
                         kv.Key,
                         kv =>
                         {
                             if (kv.Value is string || kv.Value.GetType() == typeof(char)) return $@"""{kv.Value}""";
                             else return kv.Value.ToString()!;
                         }
                     )
                 );
                }
                return _stringBindings;
            }
        }

        private QueryDebug? BasicInfo
        {
            get
            {
                if (_basicInfo is null)
                {
                    _basicInfo = JsonSerializer.Deserialize<QueryInformation.QueryDebug>(_raw, new JsonSerializerOptions() { Converters = { new StringDictionaryConverter<object>() } });
                }
                return _basicInfo;
            }
        }

        private string? FriendlyFormatScript(string? script, int tabSpaces)
        {
            if (script == null) return null;
            var lineStarts = new Stack<(int Index, int Tabs)>();
            var tabs = 1;
            var parentheses = 0;
            for (var i = GetNextIndex(script, 0); i > -1; i = GetNextIndex(script, i))
            {
                if (script[i] == '(') //is open (
                {
                    parentheses++;
                    if (script[i + 1] == '_' && script[i + 2] == '_') //followed by: __
                    {
                        tabs++;
                        lineStarts.Push((i + 1, tabs));
                    }
                }
                else //is close )
                {
                    parentheses--;
                    if (script[i - 1] != '(') //preceded by: NOT (    ,because we don't want to break on empty (), such as .v(), .e(), etc
                    {
                        if (i + 1 < script.Length && script[i + 1] == ',') //followed by: ,
                        {
                            lineStarts.Push((i + 2, tabs));
                        }
                        else if (tabs - 1 >= parentheses) //only create new-line if tabs equals parentheses
                        {
                            if (tabs - 1 > parentheses)
                            {
                                tabs--;
                                if (lineStarts.TryPop(out var previousStart)) //because each lineStart adds tabs after itself, the tabs will be wrong when decrementing.
                                {
                                    lineStarts.Push((previousStart.Index, tabs));
                                }
                            }
                            lineStarts.Push((i + 1, tabs));
                        }
                    }
                }
            }
            while (lineStarts.Count > 0)
            {
                var line = lineStarts.Pop();
                script = script.Insert(line.Index, "\n".PadRight(line.Tabs * tabSpaces + 1));
            }
            script = script.Insert(0, "".PadRight(tabSpaces - 1));
            return script;
        }

        private int GetNextIndex(string script, int index)
        {
            var start = index + 1;
            if (start >= script.Length) return -1;
            var a = script.IndexOf('(', start);
            var b = script.IndexOf(')', start);
            return a < 0 && b < 0 ? -1
                                 : a < 0 ? b
                                 : b < 0 ? a
                                 : Math.Min(a, b);
        }

        private string? ResolveBindingKeyAsStrings(string? script)
        {
            var result = script ?? string.Empty;
            foreach (var binding in StringBindings)
            {
                result = result.Replace($"{binding.Key},", $"{binding.Value},").Replace($"{binding.Key})", $"{binding.Value})");
            }
            return result;
        }

        private object ResolveBindingKeys(string script)
        {
            foreach (var binding in Bindings)
            {
                if (binding.Key == script) return binding.Value; //exact match; return raw binding value
                else script = script.Replace($"{binding.Key},", $"{binding.Value},").Replace($"{binding.Key})", $"{binding.Value})");
            }
            return script;
        }

        internal class QueryDebug
        {
            public Dictionary<string, object>? Bindings { get; set; }
            public string? Id { get; set; }
            public string? Script { get; set; }
        }
    }
}