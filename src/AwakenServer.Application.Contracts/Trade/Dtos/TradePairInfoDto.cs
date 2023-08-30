using System;
using System.Collections.Generic;

namespace AwakenServer.Trade.Dtos;

public class TradePairInfoDtoPageResultDto
{
    public TradePairInfoGplResultDto GetTradePairInfoList { get; set; }
}

public class TradePairInfoGplResultDto
{
    public long TotalCount { get; set; }
    public List<TradePairInfoDto> Data { get; set; }
}

public class TradePairInfoDto
{
    public string Id { get; set; }
    public string ChainId { get; set; }
    public string Address { get; set; }
    public string Token0Symbol { get; set; }
    public string Token1Symbol { get; set; }
    public Guid Token0Id { get; set; }
    public Guid Token1Id { get; set; }
    public double FeeRate { get; set; }
    public bool IsTokenReversed { get; set; }
}