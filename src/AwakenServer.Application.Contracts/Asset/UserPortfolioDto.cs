using System.Collections.Generic;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;

namespace AwakenServer.Asset;

public class UserPortfolioDto
{
    public string TotalPositionsInUSD { get; set; }
    public string TotalFeeInUSD { get; set; }
    public List<TradePairPortfolioDto> TradePairPositionDistributions { get; set; }
    public List<TradePairPortfolioDto> TradePairFeeDistributions { get; set; }
    public List<TokenPortfolioInfoDto> TokenPositionDistributions { get; set; }
    public List<TokenPortfolioInfoDto> TokenFeeDistributions { get; set; }
}

public class TradePairPortfolioDto
{
    public string Name { get; set; }
    public TradePairWithTokenDto TradePair { get; set; }
    public string ValueInUsd { get; set; }
    public string ValuePercent { get; set; }
}

public class TokenPortfolioInfoDto
{
    public string Name { get; set; }
    public TokenDto Token { get; set; }
    public string ValueInUsd { get; set; }
    public string ValuePercent { get; set; }
}