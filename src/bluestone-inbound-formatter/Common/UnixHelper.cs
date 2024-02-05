using System;

namespace bluestone_inbound_provider.Common
{
    internal static class UnixHelper
    {
        public static string UnixTimeStampToDateTime(long unixTimestampMillis)
        {
            DateTime epoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            DateTime dateTime = epoch.AddMilliseconds(unixTimestampMillis);

            string formattedDateTime = dateTime.ToString("yyyy-MM-ddTHH:mm:ss.ffffffzzz");
            return formattedDateTime;
        }

    }
}
