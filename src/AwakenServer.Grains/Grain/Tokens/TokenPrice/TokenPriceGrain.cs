using System;
using System.Threading.Tasks;
using AwakenServer.Grains.State.Tokens;
using AwakenServer.Options;
using AwakenServer.Price;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;

namespace AwakenServer.Grains.Grain.Tokens.TokenPrice;

public class TokenPriceGrain : Grain<CurrentTokenPriceState>, ITokenPriceGrain
{
    private readonly TokenPriceExpirationTimeOptions _tokenPriceExpirationTimeOptions;
    private readonly ITokenPriceProvider _tokenPriceProvider;
    private readonly ILogger<TokenPriceGrain> _logger;

    public TokenPriceGrain(ITokenPriceProvider tokenPriceProvider,
        IOptionsSnapshot<TokenPriceExpirationTimeOptions> tokenPriceExpirationTimeOptions,
        ILogger<TokenPriceGrain> logger)
    {
        _tokenPriceProvider = tokenPriceProvider;
        _logger = logger;
        _tokenPriceExpirationTimeOptions = tokenPriceExpirationTimeOptions.Value;
    }

    public override async Task OnActivateAsync()
    {
        await ReadStateAsync();
        await base.OnActivateAsync();
    }

    public override async Task OnDeactivateAsync()
    {
        await WriteStateAsync();
        await base.OnDeactivateAsync();
    }

    public async Task<GrainResultDto<TokenPriceGrainDto>> GetCurrentPriceAsync(string symbol)
    {
        var result = new GrainResultDto<TokenPriceGrainDto>();
        if (State.PriceUpdateTime.AddSeconds(_tokenPriceExpirationTimeOptions.Time) > DateTime.UtcNow)
        {
            result.Success = true;
            result.Data = new TokenPriceGrainDto
            {
                Symbol = State.Symbol,
                PriceInUsd = State.PriceInUsd
            };
            return result;
        }

        decimal price = 0;
        try
        {
            price = await _tokenPriceProvider.GetPriceAsync(symbol);
            if (price == 0 && State.PriceInUsd > 0)
            {
                _logger.LogInformation($"get price from token price provider failed. use old price. symbol: {State.Symbol}, price: {State.PriceInUsd}, price time: {State.PriceUpdateTime}");
                result.Success = true;
                result.Data = new TokenPriceGrainDto
                {
                    Symbol = State.Symbol,
                    PriceInUsd = State.PriceInUsd
                };
                return result;
            }
            State.Id = this.GetPrimaryKeyString();
            State.Symbol = symbol;
            State.PriceInUsd = price;
            State.PriceUpdateTime = DateTime.UtcNow;
            await WriteStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "Get symbol price error: {symbol}", symbol);
        }

        result.Success = true;
        result.Data = new TokenPriceGrainDto
        {
            Symbol = symbol,
            PriceInUsd = price
        };

        _logger.LogInformation($"get price from token price provider. symbol: {State.Symbol}, price: {State.PriceInUsd}, price time: {State.PriceUpdateTime}");
        return result;
    }
}