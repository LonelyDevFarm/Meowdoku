using System.Collections.Generic;
using System.Text;

namespace Meowdoku.Core.Localization
{
    /// <summary>
    /// Small RFC-4180 reader used for the source Godot translation table.
    /// It supports escaped quotes and quoted newlines without retaining every
    /// locale column after the selected dictionary has been built.
    /// </summary>
    internal static class LocalizationCsvReader
    {
        public static IEnumerable<string[]> ReadRows(string source)
        {
            if (string.IsNullOrEmpty(source)) yield break;

            var row = new List<string>(80);
            var field = new StringBuilder(128);
            bool quoted = false;

            for (int index = 0; index < source.Length; index++)
            {
                char value = source[index];
                if (value == '"')
                {
                    if (quoted && index + 1 < source.Length &&
                        source[index + 1] == '"')
                    {
                        field.Append('"');
                        index++;
                    }
                    else if (quoted || field.Length == 0)
                    {
                        quoted = !quoted;
                    }
                    else
                    {
                        field.Append(value);
                    }
                    continue;
                }

                if (!quoted && value == ',')
                {
                    row.Add(field.ToString());
                    field.Clear();
                    continue;
                }

                if (!quoted && (value == '\r' || value == '\n'))
                {
                    if (value == '\r' && index + 1 < source.Length &&
                        source[index + 1] == '\n')
                        index++;
                    row.Add(field.ToString());
                    field.Clear();
                    yield return row.ToArray();
                    row.Clear();
                    continue;
                }

                field.Append(value);
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                yield return row.ToArray();
            }
        }
    }
}
