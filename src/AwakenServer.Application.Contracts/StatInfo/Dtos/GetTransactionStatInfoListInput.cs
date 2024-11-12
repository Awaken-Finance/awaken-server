using AwakenServer.Trade;

namespace AwakenServer.StatInfo.Dtos;

public class GetTransactionStatInfoListInput
{
    public int TransactionType { get; set; } //swap/add/remove
    public string Symbol { get; set; }
    public string PairAddress { get; set; }
}