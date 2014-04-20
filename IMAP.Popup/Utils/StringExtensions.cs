using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace IMAP.Popup
{
    public static class StringExtensions
    {
        private readonly static ConcurrentDictionary<string, Regex> _regexCache;

        static StringExtensions()
        {
            _regexCache = new ConcurrentDictionary<string, Regex>();
        }

        public static bool RegexContains(this string s,string regexPattern)
        {
            var regex = _regexCache.GetOrAdd(regexPattern, pattern => new Regex(pattern, RegexOptions.Compiled));
            return regex.IsMatch(s);
        }
    }
}
