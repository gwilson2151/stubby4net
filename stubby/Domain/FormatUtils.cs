using System.Collections.Specialized;

namespace stubby.Domain
{
    internal static class FormatUtils
    {
        internal static string FormatNameValueCollection(NameValueCollection collection, string initial, string format, char[] trimChars)
        {
            var str = initial;
            foreach (var key in collection.AllKeys)
            {
                str += string.Format(format, key, collection.Get(key));
            }
            str = str.TrimEnd(trimChars);

            return str;
        }

        internal static string FormatHeaders(NameValueCollection headers)
        {
            return FormatNameValueCollection(headers, string.Empty, "\"{0}\":\"{1}\" ", new[] { ' ' });
        }

        internal static string FormatQueryString(NameValueCollection query)
        {
            return FormatNameValueCollection(query, "?", "{0}={1}&", new[] { '&', '?' });
        }
    }
}