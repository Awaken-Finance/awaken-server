namespace AwakenServer.Trade.Dtos;

/**
 * graphQl Query param
 */
public class GetLiquidityRecordIndexInput : GetLiquidityRecordsInput
{
    public string Pair { get; set; }
    public string Token0 { get; set; }
    public string Token1 { get; set; }
}