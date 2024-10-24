namespace AwakenServer.StatInfo;

public class StatInfoHelper
{
    public static long GetSnapshotTimestamp(int period, long timestamp)
    {
        long periodTimestamp;
        // Monday
        if (period == 3600 * 24 * 7)
        {
            var offset = 4 * 3600 * 24 * 1000;
            var offsetTime = timestamp - offset;
            periodTimestamp = offsetTime - offsetTime % (period * 1000) + offset;
        }
        // 04:00, 10:00, 16:00, 22:00
        else if (period == 3600 * 6)
        {
            var offset = 4 * 3600 * 1000;
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