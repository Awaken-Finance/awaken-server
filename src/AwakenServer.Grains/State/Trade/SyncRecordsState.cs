using System;
using System.Collections.Generic;

namespace AwakenServer.Grains.State.Trade;

public class SyncRecordsState
{
    public HashSet<String> SyncRecordSet { get; set; }
}