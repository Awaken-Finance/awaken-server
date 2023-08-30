using AwakenServer.Trade;

namespace AwakenServer.Grains.Grain.Price.TradeRecord;

public class TradeRecordGrainDto
{
    public Guid Id { get; set; }
    public string ChainId { get; set; }
    public Guid TradePairId { get; set; }
    public string Address { get; set; }
    public TradeSide Side { get; set; } = ((TradeSide[])Enum.GetValues(typeof(TradeSide)))[0];
    public string Token0Amount { get; set; }
    public string Token1Amount { get; set; }
    public DateTime Timestamp { get; set; }
    public string TransactionHash { get; set; }
    public string Channel { get; set; }
    public string Sender { get; set; }
    public double Price { get; set; }
}