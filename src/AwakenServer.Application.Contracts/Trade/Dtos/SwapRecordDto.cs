using System.Collections.Generic;
using Orleans;

namespace AwakenServer.Trade.Dtos;

public class SwapRecordPageResultDto
{
    public long TotalCount { get; set; }
    public List<SwapRecordDto> Data { get; set; }
}

[GenerateSerializer]
public class SwapRecordDto
{
    [Id(0)]
    public string ChainId { get; set; }
    [Id(1)]
    public string PairAddress { get; set; }
    [Id(2)]
    public string Sender { get; set; }
    [Id(3)]
    public string TransactionHash { get; set; }
    [Id(4)]
    public long Timestamp { get; set; }
    [Id(5)]
    public long AmountOut { get; set; }
    [Id(6)]
    public long AmountIn { get; set; }
    [Id(7)]
    public string SymbolOut { get; set; }
    [Id(8)]
    public string SymbolIn { get; set; }
    [Id(9)]
    public long TotalFee { get; set; }
    [Id(10)]
    public string Channel { get; set; }
    [Id(11)]
    public long BlockHeight { get; set; }
    [Id(12)]
    public List<SwapRecord> SwapRecords { get; set; }
    [Id(13)]
    public string MethodName { get; set; }
    [Id(14)]
    public string InputArgs { get; set; }
    [Id(15)]
    public bool IsLimitOrder { get; set; }
    [Id(16)]
    public long LabsFee { get; set; }
    [Id(17)]
    public string LabsFeeSymbol { get; set; }
}

[GenerateSerializer]
public class SwapRecord
{
    [Id(0)]
    public string PairAddress { get; set; }
    [Id(1)]
    public long AmountOut { get; set; }
    [Id(2)]
    public long AmountIn { get; set; }
    [Id(3)]
    public long TotalFee { get; set; }
    [Id(4)]
    public string SymbolOut { get; set; }
    [Id(5)]
    public string SymbolIn { get; set; }
    [Id(6)]
    public string Channel { get; set; }
    [Id(7)]
    public bool IsLimitOrder { get; set; }
}

public class SwapRecordResultDto
{
    public List<SwapRecordDto> GetSwapRecords { get; set; }
}