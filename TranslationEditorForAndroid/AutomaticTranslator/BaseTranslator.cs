using System;
using System.Collections.Generic;
using System.Text;

namespace Com.MeraBills.AutomaticTranslator
{
    internal abstract class BaseTranslator
    {
        internal abstract void Translate(string fromLanguage, string toLanguage, IDictionary<string, string> fromStrings);
    }
}
