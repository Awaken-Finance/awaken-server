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
    public double Token0Amount { get; set; }
    public double Token1Amount { get; set; }
    public double Token0Percent { get; set; }
    public double Token1Percent { get; set; }
    public double LpTokenAmount { get; set; }
    public LiquidityPoolValueInfo Position { get; set; }
    public LiquidityPoolValueInfo Fee { get; set; }
    public LiquidityPoolValueInfo cumulativeAddition { get; set; }
    public EstimatedAprType EstimatedAPRType { get; set; }
    public double EstimatedAPR { get; set; }
    public double DynamicAPR { get; set; }
    public double ImpermanentLossInUSD { get; set; }
}

public class PositionTradePairDto
{
    public string ChainId { get; set; }
    public string Address { get; set; }
    public double FeeRate { get; set; }
    public bool IsTokenReversed { get; set; }
    public TokenDto Token0 { get; set; }
    public TokenDto Token1 { get; set; }
    public double Price { get; set; }
    public double Volume24h { get; set; }
    public double TVL { get; set; }
}

public class LiquidityPoolValueInfo
{
    public double ValueInUsd { get; set; }
    public double Token0ValueInUsd { get; set; }
    public double Token1ValueInUsd { get; set; }
}

