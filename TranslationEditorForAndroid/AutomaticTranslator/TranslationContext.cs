using System.Collections.Generic;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Xml.Linq;

namespace Com.MeraBills.AutomaticTranslator
{
    internal sealed class TranslationContext
    {
        internal TranslationContext(IList<string> excludedStrings, string replacementStringPrefix)
        {
            _lockObject = new object();
            _excludedStrings = excludedStrings;

            _replacementStringPrefix = replacementStringPrefix;
            _map = new List<KeyValuePair<string, string>>();
            _map.Add(new KeyValuePair<string, string>("\\\"", "\"")); // \" with "
            _map.Add(new KeyValuePair<string, string>("\\'", "'")); // \' with '
            _map.Add(new KeyValuePair<string, string>("\\@", "@")); // \@ with @
            _map.Add(new KeyValuePair<string, string>("\\?", "?")); // \? with ?
            _map.Add(new KeyValuePair<string, string>("\\t", string.Format(TagTemplate, _replacementStringPrefix, "_tab")));
            _map.Add(new KeyValuePair<string, string>("<b>", string.Format(TagTemplate, _replacementStringPrefix, "_bs")));
            _map.Add(new KeyValuePair<string, string>("</b>", string.Format(TagTemplate, _replacementStringPrefix, "_be")));
            _map.Add(new KeyValuePair<string, string>("<i>", string.Format(TagTemplate, _replacementStringPrefix, "_is")));
            _map.Add(new KeyValuePair<string, string>("</i>", string.Format(TagTemplate, _replacementStringPrefix, "_ie")));
            _map.Add(new KeyValuePair<string, string>("<u>", string.Format(TagTemplate, _replacementStringPrefix, "_us")));
            _map.Add(new KeyValuePair<string, string>("</u>", string.Format(TagTemplate, _replacementStringPrefix, "_ue")));

            _xElement = new XElement("t");
            _urlRegex = new Regex(@"^(http:\/\/www\.|https:\/\/www\.|http:\/\/|https:\/\/)?[a-z0-9]+([\-\.]{1}[a-z0-9]+)*\.[a-z]{2,5}(:[0-9]{1,5})?(\/.*)?$",
                RegexOptions.IgnoreCase | RegexOptions.CultureInvariant | RegexOptions.Singleline | RegexOptions.Compiled);
        }

        internal PreparationDetails PrepareSingleLine(string sourceString)
        {
            var result = new PreparationDetails(sourceString, _replacementStringPrefix);
            if (string.IsNullOrWhiteSpace(sourceString))
                return result;

            // TODO: Check if the entire string refers to another string - if so, don't translate this string - do this in a dfifferent function

            string prepared;
            lock (_lockObject)
            {
                // IMPORTANT: The order of these transformations is signficant - do not change willy-nilly

                // Remove leading double-quotes
                prepared = sourceString.TrimStart(DoubleQuote);
                if (prepared.Length < sourceString.Length)
                {
                    // We trimmed a few leading double-quotes
                    result.EnclosedInDoubleQuotes = true;

                    // Remove trailing double-quotes as well
                    int lastBackslashIndex = prepared.LastIndexOf(Backslash);
                    if ((lastBackslashIndex >= 0) && (lastBackslashIndex < (prepared.Length - 2)) && (prepared[lastBackslashIndex + 1] == '"'))
                    {
                        // Make sure that we don't remove any escaped double-quotes
                        string firstPart = prepared.Substring(0, lastBackslashIndex + 2); // include backslash and the escaped double-quote
                        string secondPart = prepared.Substring(lastBackslashIndex + 2);
                        secondPart = secondPart.TrimEnd(DoubleQuote);
                        prepared = firstPart + secondPart;
                    }
                    else
                        prepared = prepared.TrimEnd(DoubleQuote);

                    if (string.IsNullOrWhiteSpace(sourceString))
                        return result;
                }

                // Replace Android string special characters and HTML formatting tags (<b></b>, <i></i> and <u></u>)
                foreach(KeyValuePair<string, string> pair in _map)
                    prepared = prepared.Replace(pair.Key, pair.Value, true, CultureInfo.InvariantCulture);

                // TODO: Replace format specifiers, if formatted is true

                // XML decode the string
                _xElement.SetValue(prepared);
                prepared = _xElement.LastNode.ToString();

                // Replace URLs
                List<string> replacements = new List<string>();
                uint replacedStringIndex = 0;
                MatchEvaluator matchEvaluator = match =>
                {
                    replacements.Add(match.Value);
                    string replacement = _replacementStringPrefix + string.Format(CultureInfo.InvariantCulture, "{0:D}", replacedStringIndex);
                    ++replacedStringIndex;
                    return replacement;
                };
                prepared = _urlRegex.Replace(prepared, matchEvaluator);

                result.Replacements = replacements;
                return result;
            }
        }

        internal const char DoubleQuote = '"';
        internal const char Backslash = '\\';
        private const string TagTemplate = " {0:s}_{1:s} ";

        private readonly object _lockObject;
        private readonly IList<string> _excludedStrings;
        private readonly string _replacementStringPrefix;
        private readonly List<KeyValuePair<string, string>> _map;
        private readonly Regex _urlRegex;
        private readonly XElement _xElement;
    }
}
