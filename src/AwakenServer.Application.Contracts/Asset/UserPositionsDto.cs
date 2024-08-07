using System.Collections.Generic;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.Asset;

public class UserPositionsDto
{
    public string Address { get; set; }
    public List<TradePairPositionDto> TradePairPositions { get; set; }
}

public enum EstimatedAprType
{
    Week,
    Month,
    All
}

public class TradePairPositionDto
{
    public PositionTradePairDto TradePairInfo { get; set; }
    public string LpTokenAmount { get; set; }
    public string LpTokenPercent { get; set; }
    public LiquidityPoolValueInfo Position { get; set; }
    public LiquidityPoolValueInfo Fee { get; set; }
    public LiquidityPoolValueInfo CumulativeAddition { get; set; }
    public List<EstimatedAPR> EstimatedAPR { get; set; }
    public string DynamicAPR { get; set; }
    public string ImpermanentLossInUSD { get; set; }
}

public class EstimatedAPR
{
    public EstimatedAprType Type { get; set; }
    public string Percent { get; set; }
}

public class PositionTradePairDto
{
    public string ChainId { get; set; }
    public string Id { get; set; }
    public string Address { get; set; }
    public double FeeRate { get; set; }
    public bool IsTokenReversed { get; set; }
    public TokenDto Token0 { get; set; }
    public TokenDto Token1 { get; set; }
    public string Price { get; set; }
    public string Volume24h { get; set; }
    public string Volume24hInUsd { get; set; }
    public string TVL { get; set; }
}

public class LiquidityPoolValueInfo
{
    public string ValueInUsd { get; set; }
    public string Token0Amount { get; set; }
    public string Token0AmountInUsd { get; set; }
    public string Token0Percent { get; set; }
    public string Token1Amount { get; set; }
    public string Token1AmountInUsd { get; set; }
    public string Token1Percent { get; set; }
}

