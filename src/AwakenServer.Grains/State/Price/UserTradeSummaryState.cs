namespace AwakenServer.Grains.State.Price;

public class UserTradeSummaryState
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public Guid TradePairId { get; set; }
    public string Address { get; set; }
    public DateTime LatestTradeTime { get; set; }
}