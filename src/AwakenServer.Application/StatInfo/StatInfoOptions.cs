using System.Collections.Generic;

namespace AwakenServer.StatInfo;

public class StatInfoOptions
{
    public List<int> Periods { get; set; }
    public Dictionary<string, long> TypePeriodMapping { get; set; }
}