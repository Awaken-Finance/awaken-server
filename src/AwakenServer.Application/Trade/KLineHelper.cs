namespace AwakenServer.Trade
{
    public class KLineHelper
    {
        public static long GetKLineTimestamp(int period, long timestamp)
        {
            long periodTimestamp;
            if (period == 3600 * 24 * 7)
            {
                var offset = 4 * 3600 * 24 * 1000;
                var offsetTime = timestamp - offset;
                periodTimestamp = offsetTime - offsetTime % (period * 1000) + offset;
            }
            else
            {
                periodTimestamp = timestamp - timestamp % (period * 1000);
            }

            return periodTimestamp;
        }
    }
}