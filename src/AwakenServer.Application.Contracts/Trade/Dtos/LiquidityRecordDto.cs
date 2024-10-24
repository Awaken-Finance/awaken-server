using System.Collections.Generic;
using Orleans;

namespace AwakenServer.Trade.Dtos;

/**
 * graphQL query result
 */
public class LiquidityRecordPageResult
{
    public long TotalCount { get; set; }
    public List<LiquidityRecordDto> Data { get; set; }
}

[GenerateSerializer]
public class LiquidityRecordDto
{
    [Id(0)]
    public string ChainId { get; set; }
    [Id(1)]
    public string Pair { get; set; }
    [Id(2)]
    public string To { get; set; }
    [Id(3)]
    public string Address { get; set; }
    [Id(4)]
    public long Token0Amount { get; set; }
    [Id(5)]
    public long Token1Amount { get; set; }
    [Id(6)]
    public string Token0 { get; set; }
    [Id(7)]
    public string Token1 { get; set; }
    [Id(8)]
    public long LpTokenAmount { get; set; }
    [Id(9)]
    public long Timestamp { get; set; }
    [Id(10)]
    public string TransactionHash { get; set; }
    [Id(11)]
    public string Channel { get; set; }
    [Id(12)]
    public string Sender { get; set; }
    [Id(13)]
    public LiquidityType Type { get; set; }
    [Id(14)]
    public long BlockHeight { get; set; }
    [Id(15)]
    public bool IsRevert { get; set; }
}

public class LiquidityRecordResultDto
{
    public List<LiquidityRecordDto> GetLiquidityRecords { get; set; }
    public LiquidityRecordPageResult LiquidityRecord { get; set; }
}