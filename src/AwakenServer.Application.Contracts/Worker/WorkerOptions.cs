using System.Collections.Generic;
using AwakenServer.Common;

namespace AwakenServer.Worker;

public class WorkerOptions
{
    public Dictionary<string, WorkerSetting> Workers { get; set; } = new Dictionary<string, WorkerSetting>();
    
    public WorkerSetting GetWorkerSettings(WorkerBusinessType businessType)
    {
        return Workers?.GetValueOrDefault(businessType.ToString()) ?? 
               new WorkerSetting();
    }
}

public class WorkerSetting
{ 
    public int TimePeriod { get; set; } = 3000;
    public bool OpenSwitch { get; set; } = false;
    public bool ResetBlockHeightFlag { get; set; } = false;
    public long ResetBlockHeight { get; set; } = 0;
    public long QueryStartBlockHeightOffset { get; set; } = -1;
    public int QueryOnceLimit { get; set; } = 10000;
    public bool IsSyncHistoryData { get; set; }
    public string DataVersion { get; set; }
}

public class TradeRecordRevertWorkerSettings : WorkerSetting
{
    public int BlockHeightLimit { get; set; } = 100;
    public int RetryLimit { get; set; } = 3;
    public int TransactionHashExpirationTime { get; set; } = 360;

    public int BatchFlushTimePeriod { get; set; } = 3;

    public int BatchFlushCount { get; set; } = 10;
    public int StartBlockHeightGap { get; set; } = 10;
}

public class DataCleanupWorkerSettings : WorkerSetting
{
    public List<string> Indexes { get; set; }
    public string DataVersion { get; set; }
    public bool ExecuteDeletion { get; set; }
}

public class StatInfoUpdateWorkerSettings : WorkerSetting
{
    public bool ExecuteRefreshTvl { get; set; }
}
