using System;
using System.Collections.Generic;

namespace AwakenServer.Comparers;

public class DateTimeDescendingComparer : IComparer<DateTime>
{
    public int Compare(DateTime x, DateTime y)
    {
        return y.CompareTo(x);
    }
}

public class StringDateTimeDescendingComparer : IComparer<Tuple<string, DateTime>>
{
    public int Compare(Tuple<string, DateTime> x, Tuple<string, DateTime> y)
    {
        return y.Item2.CompareTo(x.Item2);
    }
}