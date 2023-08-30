using System;

namespace AwakenServer
{
    public class DateTimeHelper
    {
        public static long ToUnixTimeMilliseconds(DateTime value)
        {
            var span = value - DateTime.UnixEpoch;
            return (long) span.TotalMilliseconds;
        }
        
        public static long ToUnixTimeSeconds(DateTime value)
        {
            var span = value - DateTime.UnixEpoch;
            return (long) span.TotalMilliseconds / 1000;
        }

        public static DateTime FromUnixTimeMilliseconds(long value)
        {
            return DateTime.UnixEpoch.AddMilliseconds(value);
        }
    }
}