using System.Collections.Generic;

namespace AwakenServer.Trade;

public class AssetShowOptions
{
    public List<string> ShowList { get; set; }

    public List<string> NftList { get; set; }
    public long TransactionFee { get; set; }

    public int ShowListLength { get; set; }

    public string DefaultSymbol { get; set; }
}