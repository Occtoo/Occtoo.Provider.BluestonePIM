using System.Collections.Generic;
using System.Linq;

namespace bluestone_inbound_formatter.Common
{
    public static class DictionaryHelper
    {
        public static Dictionary<string, string> ExtractToDictionary(string defaultContext, string additionalContexts)
        {
            var defaultKeyPairs = defaultContext
                .Split(',')
                .Select(pair => pair.Split(':'))
                .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim());

            var keyValuePairs = additionalContexts
                .Split(',')
                .Select(pair => pair.Split(':'))
                .ToDictionary(pair => pair[0].Trim(), pair => pair[1].Trim());

            foreach (var kvp in defaultKeyPairs)
            {
                if (!keyValuePairs.ContainsKey(kvp.Key))
                {
                    keyValuePairs.Add(kvp.Key, kvp.Value);
                }
            }
            return keyValuePairs;
        }

    }
}
