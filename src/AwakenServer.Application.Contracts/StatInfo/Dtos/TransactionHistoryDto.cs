using AwakenServer.Trade.Dtos;

namespace AwakenServer.StatInfo.Dtos;

public class TransactionHistoryDto
{
    public long Timestamp { get; set; }
    public TradePairWithTokenDto TradePair { get; set; }
    public int TransactionType { get; set; }
    public int TradeType { get; set; }
    public double ValueInUsd { get; set; }
    public double Token0Amount { get; set; }
    public double Token1Amount { get; set; }
    public string TransactionId { get; set; }
}