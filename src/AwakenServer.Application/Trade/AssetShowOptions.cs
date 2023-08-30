using System.Collections.Generic;

namespace AwakenServer.Trade;

public class AssetShowOptions
{
    public List<string> ShowList { get; set; }
    public long TransactionFee { get; set; }
}