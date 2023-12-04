using System.Collections.Generic;

namespace AwakenServer.Trade;

public class AssetWhenNoTransactionOptions
{
    public List<string> Symbols { get; set; }

    public int ExpireDurationMinutes { get; set; }

    public string ContractAddressOfGetBalance { get; set; }
}