using System.Collections.Generic;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.Asset;

public class UserPortfolioDto
{
    public double TotalPositionsInUSD { get; set; }
    public double TotalFeeInUSD { get; set; }
    public List<TradePairPortfolioDto> TradePairDistributions { get; set; }
    public List<TokenPortfolioInfoDto> TokenDistributions { get; set; }
}

public class TradePairPortfolioDto
{
    public TradePairWithTokenDto TradePair { get; set; }
    public double PositionInUsd { get; set; }
    public double PositionPercent { get; set; }
    public double FeeInUsd { get; set; }
    public double FeePercent { get; set; }
}

public class TokenPortfolioInfoDto
{
    public TokenDto Token { get; set; }
    public double PositionInUsd { get; set; }
    public double PositionPercent { get; set; }
    public double FeeInUsd { get; set; }
    public double FeePercent { get; set; }
}