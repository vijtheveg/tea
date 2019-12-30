using System;

namespace Com.MeraBills.AutomaticTranslator
{
    internal sealed class TranslationRequest
    {
        internal TranslationRequest(string fromLanguage, string sourceStringId, uint sourceStringPartNumber, string sourceString)
        {
            this.FromLanguage = fromLanguage;
            this.SourceStringId = sourceStringId;
            this.SourceStringPartNumber = sourceStringPartNumber;
        }

        internal string FromLanguage { get; private set; }

        internal string SourceStringId { get; private set; }

        internal uint SourceStringPartNumber { get; private set; }

        internal string TranslatedString { get; set; }

        internal Exception TranslationError { get; set; }
    }
}