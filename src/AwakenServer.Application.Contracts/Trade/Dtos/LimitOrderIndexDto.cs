namespace AwakenServer.Trade.Dtos;

public class LimitOrderIndexDto
{
    public TradePairWithTokenDto TradePair { get; set; }
    public string ChainId { get; set; }
    public long OrderId { get; set; }
    public string Maker { get; set; }
    public string SymbolIn { get; set; }
    public string SymbolOut { get; set; }
    public string TransactionHash { get; set; }
    public string AmountIn { get; set; }
    public string AmountOut { get; set; }
    public string AmountInFilled { get; set; }
    public string AmountOutFilled { get; set; }
    public string AmountInUSD { get; set; }
    public string AmountOutUSD { get; set; }
    public string AmountInFilledUSD { get; set; }
    public string AmountOutFilledUSD { get; set; }
    public long Deadline { get; set; }
    public long CommitTime { get; set; }
    public long FillTime { get; set; }
    public long CancelTime { get; set; }
    public long RemoveTime { get; set; }
    public long LastUpdateTime { get; set; }
    public LimitOrderStatus LimitOrderStatus { get; set; }
}