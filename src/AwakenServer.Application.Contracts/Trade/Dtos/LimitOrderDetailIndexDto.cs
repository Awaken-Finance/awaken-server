namespace AwakenServer.Trade.Dtos;

public class LimitOrderFillRecordIndexDto
{
    public string TakerAddress { get; set; }
    public string AmountInFilled { get; set; }
    public string AmountOutFilled { get; set; }
    public string AmountInFilledUSD { get; set; }
    public string AmountOutFilledUSD { get; set; }
    public long TransactionTime { get; set; }
    public string NetworkFee { get; set; }
    public string TotalFee { get; set; }
    public string TransactionHash { get; set; }
    public LimitOrderStatus Status { get; set; }
}