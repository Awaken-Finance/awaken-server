using System.Collections.Generic;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.Asset;

public class UserPortfolioDto
{
    public string TotalPositionsInUSD { get; set; }
    public string TotalFeeInUSD { get; set; }
    public List<TradePairPortfolioDto> TradePairDistributions { get; set; }
    public List<TokenPortfolioInfoDto> TokenDistributions { get; set; }
}

public class TradePairPortfolioDto
{
    public TradePairWithTokenDto TradePair { get; set; }
    public string PositionInUsd { get; set; }
    public string PositionPercent { get; set; }
    public string FeeInUsd { get; set; }
    public string FeePercent { get; set; }
}

public class TokenPortfolioInfoDto
{
    public TokenDto Token { get; set; }
    public string PositionInUsd { get; set; }
    public string PositionPercent { get; set; }
    public string FeeInUsd { get; set; }
    public string FeePercent { get; set; }
}