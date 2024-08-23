using System.Collections.Generic;

namespace AwakenServer.Trade.Dtos;

public class SwapRecordPageResultDto
{
    public long TotalCount { get; set; }
    public List<SwapRecordDto> Data { get; set; }
}

public class SwapRecordDto
{
    public string ChainId { get; set; }
    public string PairAddress { get; set; }
    public string Sender { get; set; }
    public string TransactionHash { get; set; }
    public long Timestamp { get; set; }
    public long AmountOut { get; set; }
    public long AmountIn { get; set; }
    public string SymbolOut { get; set; }
    public string SymbolIn { get; set; }
    public long TotalFee { get; set; }
    public string Channel { get; set; }
    public long BlockHeight { get; set; }
    public List<SwapRecord> SwapRecords { get; set; }
    public string MethodName { get; set; }
    public string InputArgs { get; set; }
    public bool IsLimitOrder { get; set; }
}

public class SwapRecord
{
    public string PairAddress { get; set; }
    public long AmountOut { get; set; }
    public long AmountIn { get; set; }
    public long TotalFee { get; set; }
    public string SymbolOut { get; set; }
    public string SymbolIn { get; set; }
    public string Channel { get; set; }
    public bool IsLimitOrder { get; set; }
}

public class SwapRecordResultDto
{
    public List<SwapRecordDto> GetSwapRecords { get; set; }
}