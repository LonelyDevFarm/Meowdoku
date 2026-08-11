using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;

namespace Meowdoku.Core.UI
{
    public sealed class DialogPriorityRule
    {
        public DialogPriorityRule(
            string openScene,
            int priority,
            string key,
            bool canExceedLimit)
        {
            OpenScene = openScene ?? string.Empty;
            Priority = priority;
            Key = key ?? string.Empty;
            CanExceedLimit = canExceedLimit;
        }

        public string OpenScene { get; }
        public int Priority { get; }
        public string Key { get; }
        public bool CanExceedLimit { get; }
    }

    public sealed class AbSwitchPopupRule
    {
        public AbSwitchPopupRule(
            string rawTrigger,
            string rawParameters,
            IReadOnlyDictionary<string, object> trigger,
            IReadOnlyDictionary<string, object> parameters)
        {
            RawTrigger = rawTrigger ?? string.Empty;
            RawParameters = rawParameters ?? string.Empty;
            Trigger = trigger;
            Parameters = parameters;
        }

        public string RawTrigger { get; }
        public string RawParameters { get; }
        public IReadOnlyDictionary<string, object> Trigger { get; }
        public IReadOnlyDictionary<string, object> Parameters { get; }
    }

    public static class UIPopupConfig
    {
        public static IReadOnlyList<DialogPriorityRule> ParsePriorities(string json)
        {
            var result = new List<DialogPriorityRule>();
            if (!(MiniJson.Deserialize(json) is IList<object> entries)) return result;
            foreach (object raw in entries)
            {
                if (!(raw is IDictionary<string, object> entry)) continue;
                result.Add(new DialogPriorityRule(
                    ReadString(entry, "OpenScene"),
                    ReadInt(entry, "Priority"),
                    ReadString(entry, "Key"),
                    ReadInt(entry, "CanExceedLimit") != 0));
            }
            return result;
        }

        public static IReadOnlyList<AbSwitchPopupRule> ParseAbSwitchRules(string json)
        {
            var result = new List<AbSwitchPopupRule>();
            if (!(MiniJson.Deserialize(json) is IList<object> entries)) return result;
            foreach (object raw in entries)
            {
                if (!(raw is IDictionary<string, object> entry)) continue;
                string trigger = ReadString(entry, "Trigger");
                string parameters = ReadString(entry, "Param");
                result.Add(new AbSwitchPopupRule(
                    trigger,
                    parameters,
                    ParseTriggerDsl(trigger),
                    ParseParameterDsl(parameters)));
            }
            return result;
        }

        public static IReadOnlyDictionary<string, object> ParseTriggerDsl(string value)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            foreach (string part in SplitOutsideBraces(value))
            {
                int equals = part.IndexOf('=');
                if (equals < 0) continue;
                string key = part.Substring(0, equals).Trim();
                string raw = part.Substring(equals + 1).Trim();
                if (raw.Length >= 2 && raw[0] == '{' && raw[raw.Length - 1] == '}')
                {
                    var numbers = new List<int>();
                    string inner = raw.Substring(1, raw.Length - 2);
                    foreach (string item in inner.Split(','))
                    {
                        if (int.TryParse(item.Trim(), NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out int number))
                            numbers.Add(number);
                    }
                    result[key] = numbers;
                }
                else
                {
                    result[key] = raw;
                }
            }
            return result;
        }

        public static IReadOnlyDictionary<string, object> ParseParameterDsl(string value)
        {
            var result = new Dictionary<string, object>(StringComparer.Ordinal);
            int index = 0;
            value ??= string.Empty;
            while (index < value.Length)
            {
                int equals = value.IndexOf('=', index);
                if (equals < 0) break;
                string key = value.Substring(index, equals - index).Trim();
                int valueStart = equals + 1;
                if (valueStart < value.Length && value[valueStart] == '{')
                {
                    int close = value.IndexOf('}', valueStart);
                    if (close < 0) break;
                    var nested = new Dictionary<string, object>(StringComparer.Ordinal);
                    string inner = value.Substring(valueStart + 1,
                        close - valueStart - 1);
                    foreach (string pair in inner.Split(','))
                    {
                        string[] keyValue = pair.Split('=');
                        if (keyValue.Length != 2) continue;
                        if (int.TryParse(keyValue[1].Trim(), NumberStyles.Integer,
                                CultureInfo.InvariantCulture, out int number))
                            nested[keyValue[0].Trim()] = number;
                    }
                    result[key] = nested;
                    index = close + 1;
                    if (index < value.Length && value[index] == ',') index++;
                }
                else
                {
                    int comma = value.IndexOf(',', valueStart);
                    if (comma < 0) comma = value.Length;
                    result[key] = value.Substring(valueStart,
                        comma - valueStart).Trim();
                    index = comma + 1;
                }
            }
            return result;
        }

        public static void BuildQueueForScene(
            IEnumerable<DialogPriorityRule> rules,
            string scene,
            IReadOnlyDictionary<string, Func<IEnumerator>> handlers,
            UIPopupQueue queue)
        {
            if (rules == null || handlers == null || queue == null) return;
            foreach (DialogPriorityRule rule in rules)
            {
                if (!string.Equals(rule.OpenScene, scene,
                        StringComparison.Ordinal) ||
                    !handlers.TryGetValue(rule.Key, out Func<IEnumerator> handler))
                    continue;
                // CanExceedLimit is parsed for parity, but HomePage currently
                // does not consult it while building the source queue.
                queue.Enqueue(new UIPopupEntry(rule.Key, rule.Priority, handler));
            }
        }

        public static AbSwitchPopupRule FindSwitchRule(
            IReadOnlyList<AbSwitchPopupRule> rules,
            string key,
            int occurrence)
        {
            if (rules == null || string.IsNullOrEmpty(key) ||
                occurrence <= 0)
                return null;
            int matched = 0;
            for (int index = 0; index < rules.Count; index++)
            {
                AbSwitchPopupRule rule = rules[index];
                if (rule == null ||
                    !rule.Trigger.TryGetValue(
                        "trigger",
                        out object trigger) ||
                    !string.Equals(
                        Convert.ToString(trigger),
                        "abtest_switch",
                        StringComparison.Ordinal) ||
                    !rule.Trigger.TryGetValue("key", out object ruleKey) ||
                    !string.Equals(
                        Convert.ToString(ruleKey),
                        key,
                        StringComparison.Ordinal))
                    continue;
                matched++;
                if (matched == occurrence) return rule;
            }
            return null;
        }

        private static List<string> SplitOutsideBraces(string value)
        {
            var result = new List<string>();
            if (string.IsNullOrEmpty(value)) return result;
            int depth = 0;
            int start = 0;
            for (int index = 0; index < value.Length; index++)
            {
                char character = value[index];
                if (character == '{') depth++;
                else if (character == '}') depth--;
                else if (character == ',' && depth == 0)
                {
                    result.Add(value.Substring(start, index - start).Trim());
                    start = index + 1;
                }
            }
            if (start < value.Length)
                result.Add(value.Substring(start).Trim());
            return result;
        }

        private static string ReadString(
            IDictionary<string, object> dictionary,
            string key)
        {
            return dictionary.TryGetValue(key, out object value)
                ? Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty
                : string.Empty;
        }

        private static int ReadInt(
            IDictionary<string, object> dictionary,
            string key)
        {
            if (!dictionary.TryGetValue(key, out object value)) return 0;
            try
            {
                return Convert.ToInt32(value, CultureInfo.InvariantCulture);
            }
            catch (Exception)
            {
                return 0;
            }
        }
    }
}
