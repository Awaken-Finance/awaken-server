using AwakenServer.Trade;
using AwakenServer.Trade.Index;

namespace AwakenServer.StatInfo;

public class TransactionStatInfo
{
    public long Timestamp;
    public TradePairWithToken TradePair;
    public TransactionType TransactionType;
    public double ValueInUsd;
    public string Token0Amount;
    public string Token1Amount;
    public TradeSide Side { get; set; }
    public string TransactionHash { get; set; }
}