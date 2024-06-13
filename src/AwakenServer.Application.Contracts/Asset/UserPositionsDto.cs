using System.Collections.Generic;
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
    public PositionPoolDto TradePairInfo { get; set; }
    public double Token0Amount { get; set; }
    public double Token1Amount { get; set; }
    public double LpTokenAmount { get; set; }
    public PositionDto Position { get; set; }
    public PositionDto Fee { get; set; }
    public PositionDto cumulativeAddition { get; set; }
    public EstimatedAprType EstimatedAPRType { get; set; }
    public double EstimatedAPR { get; set; }
    public double DynamicAPR { get; set; }
    public double ImpermanentLossInUSD { get; set; }
}

public class PositionPoolDto
{
    public TradePairWithTokenDto TradePair { get; set; }
    public double Price { get; set; }
    public double Volume24h { get; set; }
    public double TVL { get; set; }
}

public class PositionDto
{
    public string ValueInUsd { get; set; }
    public string Token0ValueInUsd { get; set; }
    public string Token0ValuePercent { get; set; }
    public string Token1ValueInUsd { get; set; }
    public string Token1ValuePercent { get; set; }
}

