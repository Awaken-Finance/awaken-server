using AwakenServer.Tokens;

namespace AwakenServer.StatInfo.Dtos;

public class TokenStatInfoDto
{
    public TokenDto Token { get; set; }
    public double Price { get; set; }
    public double PricePercentChange24h { get; set; }
    public double Volume24hInUsd { get; set; }
    public double Tvl { get; set; }
    public long PairCount { get; set; }
    public long TransactionCount { get; set; }
}