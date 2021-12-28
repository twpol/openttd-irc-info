using System;

namespace OpenTTD_IRC_Info.OpenTTD
{
    static class Date
    {
        public static DateTimeOffset GetDate(uint date)
        {
            return new DateTimeOffset(1, 1, 1, 0, 0, 0, TimeSpan.Zero) + TimeSpan.FromDays(date - 366);
        }
    }
}
