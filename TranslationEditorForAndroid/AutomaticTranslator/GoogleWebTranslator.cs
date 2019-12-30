using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace Com.MeraBills.AutomaticTranslator
{
    public class GoogleWebTranslator : ITranslator
    {
        public Task<string> Translate(string fromLanguage, string toLanguage, string fromStrings)
        {
            throw new NotImplementedException();
        }

        public async Task<IDictionary<string, string>> Translate(string fromLanguage, string toLanguage, IDictionary<string, string> fromStrings)
        {

        }
    }
}
