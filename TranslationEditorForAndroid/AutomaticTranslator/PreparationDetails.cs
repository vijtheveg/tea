using System.Collections.Generic;

namespace Com.MeraBills.AutomaticTranslator
{
    internal sealed class PreparationDetails
    {
        internal PreparationDetails(string sourceString, string replacementStringPrefix)
        {
            this.SourceString = sourceString;
            this.ReplacementStringPrefix = replacementStringPrefix;
        }

        internal bool EnclosedInDoubleQuotes { get; set; }

        internal IList<string> Replacements { get; set; }

        internal readonly string SourceString;
        internal readonly string ReplacementStringPrefix;
    }
}
