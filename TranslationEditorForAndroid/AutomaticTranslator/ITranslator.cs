using System.Collections.Generic;
using System.Threading.Tasks;

namespace Com.MeraBills.AutomaticTranslator
{
    public interface ITranslator
    {
        Task<string> Translate(string fromLanguage, string toLanguage, string fromString);

        Task<IDictionary<string, string>> Translate(string fromLanguage, string toLanguage, IDictionary<string, string> fromStrings);
    }
}
