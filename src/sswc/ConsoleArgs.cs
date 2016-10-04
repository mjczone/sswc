using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Text.RegularExpressions;

namespace Ssw.Cli
{
    // Adapted from: MadProps.AppArgs
    internal static class ConsoleArgs
    {
        private static readonly Regex Pattern = new Regex("[/-](?'key'[^\\s=:]+)"
            + "([=:]("
                + "((?'open'\").+(?'value-open'\"))"
                + "|"
                + "(?'value'.+)"
            + "))?", RegexOptions.IgnoreCase | RegexOptions.Compiled);

        private static readonly Regex UriPattern = new Regex(@"[\\?&](?'key'[^&=]+)(=(?'value'[^&]+))?", RegexOptions.Compiled);
        private static readonly Regex QueryStringPattern = new Regex(@"(^|&)(?'key'[^&=]+)(=(?'value'[^&]+))?", RegexOptions.Compiled);

        private static IEnumerable<ArgProperty> PropertiesOf<T>()
        {
            return from p in typeof(T).GetProperties(BindingFlags.Instance | BindingFlags.Public | BindingFlags.SetProperty)
                   let d = p.Attribute<DescriptionAttribute>()
                   let alias = p.Attribute<DisplayAttribute>()
                   let def = p.Attribute<DefaultValueAttribute>()
                   select new ArgProperty
                   {
                       Property = p,
                       Name = string.IsNullOrWhiteSpace(alias?.GetShortName()) ? p.Name.ToLower() : alias.GetShortName(),
                       Type = p.PropertyType,
                       Order = alias == null ? 0 : alias.GetOrder().GetValueOrDefault(0),
                       Required = p.Attribute<RequiredAttribute>() != null,
                       RequiresValue = !(p.PropertyType == typeof(bool) || p.PropertyType == typeof(bool?)),
                       Description = ((!string.IsNullOrWhiteSpace(d?.Description) ? d.Description : (alias != null ? alias.GetDescription() : string.Empty)) +
                                      (def?.Value == null ? string.Empty : $" [default: {def.Value}]")).Trim()
                   };
        }

        /// <summary>
        /// Parses the arguments in <paramref name="args"/> and creates an instance of <typeparamref name="T"/> with the
        /// corresponding properties populated.
        /// </summary>
        /// <typeparam name="T">The custom type to be populated from <paramref name="args"/>.</typeparam>
        /// <param name="args">Command-line arguments, usually in the form of "/name=value".</param>
        /// <returns>A new instance of <typeparamref name="T"/>.</returns>
        public static T As<T>(this string[] args) where T : class, new()
        {
            var arguments = (from a in args
                             let match = Pattern.Match(a)
                             where match.Success
                             select new
                             {
                                 Key = match.Groups["key"].Value.ToLower(),
                                 match.Groups["value"].Value
                             }
                ).ToDictionary(a => a.Key, a => a.Value);

            return arguments.As<T>();
        }

        /// <summary>
        /// Parses the arguments in the supplied string and creates an instance of <typeparamref name="T"/> with the
        /// corresponding properties populated.
        /// The string should be in the format "key1=value1&amp;key2=value2&amp;key3=value3".
        /// </summary>
        /// <typeparam name="T">The custom type to be populated from <paramref name="queryString"/>.</typeparam>
        /// <param name="queryString">Command-line arguments, usually in the form of "/name=value".</param>
        /// <returns>A new instance of <typeparamref name="T"/>.</returns>
        public static T As<T>(this string queryString) where T : new()
        {
            var arguments = (from match in QueryStringPattern.Matches(queryString).Cast<Match>()
                             where match.Success
                             select new
                             {
                                 Key = match.Groups["key"].Value.ToLower(),
                                 match.Groups["value"].Value
                             }
                ).ToDictionary(a => a.Key, a => a.Value);

            return arguments.As<T>();
        }

        /// <summary>
        /// Parses the URI parameters in <paramref name="uri"/> and creates an instance of <typeparamref name="T"/> with the
        /// corresponding properties populated.
        /// </summary>
        /// <typeparam name="T">The custom type to be populated from <paramref name="uri"/>.</typeparam>
        /// <param name="uri">A URI, usually a ClickOnce activation URI.</param>
        /// <returns>A new instance of <typeparamref name="T"/>.</returns>
        public static T As<T>(this Uri uri) where T : new()
        {
            var arguments = (from match in UriPattern.Matches(uri.ToString()).Cast<Match>()
                             where match.Success
                             select new
                             {
                                 Key = match.Groups["key"].Value.ToLower(),
                                 match.Groups["value"].Value
                             }
                ).ToDictionary(a => a.Key, a => a.Value);

            return arguments.As<T>();
        }

        /// <summary>
        /// Parses the name/value pairs in <paramref name="arguments"/> and creates an instance of <typeparamref name="T"/> with the
        /// corresponding properties populated.
        /// </summary>
        /// <typeparam name="T">The custom type to be populated from <paramref name="arguments"/>.</typeparam>
        /// <param name="arguments">The key/value pairs to be parsed.</param>
        /// <returns>A new instance of <typeparamref name="T"/>.</returns>
        public static T As<T>(this Dictionary<string, string> arguments) where T : new()
        {
            T result = new T();

            var props = PropertiesOf<T>().ToList();

            foreach (var arg in arguments)
            {
                var matches = props.Where(p => p.Name.StartsWith(arg.Key, StringComparison.OrdinalIgnoreCase)).ToList();
                if (matches.Count == 0)
                {
                    //Ignore unknown arguments
                    //throw new ArgumentException("Unknown argument '" + arg.Key + "'");
                    continue;
                }
                else if (matches.Count > 1)
                {
                    throw new ArgumentException("Ambiguous argument '" + arg.Key + "'");
                }

                var prop = matches[0];

                if (!String.IsNullOrWhiteSpace(arg.Value))
                {
                    if (prop.Type.IsArray)
                    {
                        string v = arg.Value;

                        if (v.StartsWith("{") && v.EndsWith("}"))
                        {
                            v = v.Substring(1, arg.Value.Length - 2);
                        }

                        var values = v.Split(',').ToArray();
                        var array = Array.CreateInstance(prop.Type.GetElementType(), values.Length);
                        for (int i = 0; i < values.Length; i++)
                        {
                            var converter = TypeDescriptor.GetConverter(prop.Type.GetElementType());
                            array.SetValue(converter.ConvertFrom(values[i]), i);
                        }
                        
                        prop.Property.SetValue(result, array, null);
                    }
                    else
                    {
                        var converter = TypeDescriptor.GetConverter(prop.Type);
                        prop.Property.SetValue(result, converter.ConvertFromString(arg.Value), null);
                    }
                }
                else if (prop.Type == typeof(bool))
                {
                    prop.Property.SetValue(result, true, null);
                }
                else
                {
                    throw new ArgumentException("No value supplied for argument '" + arg.Key + "'");
                }
            }

            foreach (var p in props.Where(p => p.Required))
            {
                if (!arguments.Keys.Any(a => p.Name.StartsWith(a)))
                {
                    throw new ArgumentException("Argument missing: '" + p.Name + "'");
                }
            }

            return result;
        }

        /// <summary>
        /// Returns a string describing the arguments necessary to populate an instance of <typeparamref name="T"/>.
        /// </summary>
        /// <typeparam name="T">A class representing the potential application arguments.</typeparam>
        /// <returns>A string describing the arguments necessary to populate an instance of <typeparamref name="T"/></returns>
        public static string HelpFor<T>(bool includeUsageExamples = false)
        {
            var props = PropertiesOf<T>().Where(p => !p.Name.StartsWith("_")).OrderBy(p => p.RequiresValue).ThenBy(p => p.Name).ToList();

            var sb = new StringBuilder();

            //sb.Append(Path.GetFileNameWithoutExtension(Environment.GetCommandLineArgs()[0]));
            sb.Append(Path.GetFileName(new Uri(Assembly.GetExecutingAssembly().CodeBase).LocalPath).ToLower());
            foreach (var p in props.Where(p => p.Required))
            {
                sb.Append(" /" + p.Name + (p.RequiresValue ? "=value" : ""));
            }

            foreach (var p in props.Where(p => !p.Required))
            {
                sb.Append(" [/" + p.Name + (p.RequiresValue ? "=value" : "") + "]");
            }

            sb.AppendLine();
            sb.AppendLine();

            if (includeUsageExamples)
            {
                var usage = typeof(T).Attribute<DescriptionAttribute>()?.Description;
                if (!string.IsNullOrWhiteSpace(usage))
                {
                    sb.Append(usage.Trim());
                    sb.AppendLine();
                    sb.AppendLine();
                }
            }

            var hasRequiredArguments = false;
            var output = props.Where(p => p.Required).OrderBy(p => p.Order).ToDictionary(
                k => "/" + k.Name.Trim(),
                v => (v.Description ?? "").Trim());
            if (output.Count > 0)
            {
                hasRequiredArguments = true;
                sb.AppendLine("Required arguments:");
                sb.AppendLine();
                output.To2ColumnsOfText(s => sb.Append(s), 0, 0, 2, 1, 1, 2);
            }

            output = props.Where(p => !p.Required).OrderBy(p => p.Order).ToDictionary(
                k => "/" + k.Name.Trim(),
                v => (v.Description ?? "").Trim());
            if (output.Count > 0)
            {
                if (hasRequiredArguments) sb.AppendLine();
                sb.AppendLine("Optional arguments:");
                sb.AppendLine();
                output.To2ColumnsOfText(s => sb.Append(s), 0, 0, 2, 1, 1, 2);
            }

            return sb.ToString();
        }

        private class ArgProperty
        {
            public PropertyInfo Property { get; set; }
            public string Name { get; set; }
            public bool Required { get; set; }
            public bool RequiresValue { get; set; }
            public Type Type { get; set; }
            public string Description { get; set; }
            public int Order { get; set; }
        }
    }

    public static class PropertyInfoExtensions
    {
        public static T Attribute<T>(this PropertyInfo p)
        {
            return p.GetCustomAttributes(typeof(T), true).Cast<T>().FirstOrDefault();
        }

        public static T Attribute<T>(this Type t)
        {
            return t.GetCustomAttributes(typeof(T), true).Cast<T>().FirstOrDefault();
        }

        public static T Attribute<T>(this Assembly ass) where T : Attribute
        {
            return ass.GetCustomAttributes(typeof(T), false).Cast<T>().FirstOrDefault();
        }

        public static void To2ColumnsOfText(this Dictionary<string, string> target, Action<string> output, int col1Size = 0, int col2Size = 0, int col1LeftMargin = 0, int col1RightMargin = 0, int col2LeftMargin = 0, int col2RightMargin = 0)
        {
            var cWidth = 80;
            if (col1Size == 0 || col2Size == 0)
            {
                try
                {
                    cWidth = Console.WindowWidth;
                }
                catch
                {
                    // ignored
                }
            }

            if (col1Size == 0 && col2Size == 0)
                col1Size = target.Keys.Max(k => k.Length + col1LeftMargin + col1RightMargin + 1);
            if (col2Size == 0 && col1Size > cWidth)
                col1Size = cWidth / 2;

            if (col1Size == 0 && col2Size > 0)
                col1Size = cWidth - col2Size;
            if (col2Size == 0 && col1Size > 0)
                col2Size = cWidth - col1Size;

            foreach (var kvp in target)
            {
                var col1Lines = WordWrapToLines(kvp.Key, col1Size, col1LeftMargin, col1RightMargin);
                var col2Lines = WordWrapToLines(kvp.Value, col2Size, col2LeftMargin, col2RightMargin);

                var len = Math.Max(col1Lines.Count, col2Lines.Count);
                while (col1Lines.Count < col2Lines.Count)
                    col1Lines.Add("");
                while (col2Lines.Count < col1Lines.Count)
                    col2Lines.Add("");

                for (var i = 0; i < len; i++)
                {
                    output(col1Lines[i].PadRight(col1Size));
                    output(col2Lines[i]);
                    output(Environment.NewLine);
                }
            }
        }

        public static void WordWrapToFunc(this string text, Action<string> output, int maxWidth, int leftMargin, int rightMargin)
        {
            while (text.Contains("  "))
                text = text.Replace("  ", " ");

            var width = maxWidth - leftMargin - rightMargin;

            var words = text.Split(' ');
            var currentLine = new StringBuilder();
            for (var w = 0; w < words.Length; w++)
            {
                var word = words[w];
                if ((currentLine.Length + word.Length) < width)
                {
                    currentLine.Append(w == 0 ? word : " " + word);
                }
                else
                {
                    var line = currentLine.ToString().Trim();
                    output(line.PadLeft(line.Length + leftMargin));
                    currentLine.Clear();
                    currentLine.Append(word);
                }
            }
            if (currentLine.Length > 0)
            {
                var line = currentLine.ToString().Trim();
                output(line.PadLeft(line.Length + leftMargin));
            }
        }

        public static void WordWrapToWriter(this string text, TextWriter output, int maxWidth, int leftMargin, int rightMargin)
        {
            WordWrapToFunc(text, output.WriteLine, maxWidth, leftMargin, rightMargin);
        }

        public static List<string> WordWrapToLines(this string text, int maxWidth, int leftMargin, int rightMargin)
        {
            var lines = new List<string>();
            WordWrapToFunc(text, s => lines.Add(s), maxWidth, leftMargin, rightMargin);
            return lines;
        }

        public static string WordWrapToString(this string text, int maxWidth, int leftMargin, int rightMargin)
        {
            var sb = new StringBuilder();
            WordWrapToFunc(text, s => sb.AppendLine(s), maxWidth, leftMargin, rightMargin);
            return sb.ToString().TrimEnd();
        }
    }
}