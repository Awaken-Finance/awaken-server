using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Price;
using AwakenServer.Provider;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Services;

namespace AwakenServer.Asset;

[RemoteService(false)]
public class AssetAppService : ApplicationService, IAssetAppService
{
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ITokenAppService _tokenAppService;
    private readonly IPriceAppService _priceAppService;
    private readonly AssetShowOptions _assetShowOptions;
    private readonly IAElfClientProvider _aelfClientProvider;

    public AssetAppService(IGraphQLProvider graphQlProvider,
        ITokenAppService tokenAppService,
        IPriceAppService priceAppService,
        IOptionsSnapshot<AssetShowOptions> optionsSnapshot,
        IAElfClientProvider aelfClientProvider)
    {
        _graphQlProvider = graphQlProvider;
        _tokenAppService = tokenAppService;
        _priceAppService = priceAppService;
        _assetShowOptions = optionsSnapshot.Value;
        _aelfClientProvider = aelfClientProvider;
    }

    public async Task<UserAssetInfoDto> GetUserAssetInfoAsync(GetUserAssetInfoDto input)
    {
        var tokenList = await _graphQlProvider.GetUserTokensAsync(input.ChainId, input.Address);
        var showList = new List<UserTokenInfo>();
        var hiddenList = new List<UserTokenInfo>();
        var symbolList = tokenList.Select(i => i.Symbol).ToList();
        var symbolPriceMap = (await _priceAppService.GetTokenPriceListAsync(symbolList)).Items.ToDictionary(i => i.Symbol, i => i.PriceInUsd);
        foreach (var userTokenDto in tokenList)
        {
            var userTokenInfo = ObjectMapper.Map<UserTokenDto, UserTokenInfo>(userTokenDto);
            if (_assetShowOptions.ShowList.Contains(userTokenInfo.Symbol))
            {
                showList.Add(userTokenInfo);
            }
            else
            {
                hiddenList.Add(userTokenInfo);
            }

            var tokenDto = await _tokenAppService.GetAsync(new GetTokenInput
            {
                Symbol = userTokenInfo.Symbol
            });
            if (tokenDto == null)
            {
                var tokenInfo = await _aelfClientProvider.GetTokenInfoAsync(input.ChainId, null, userTokenInfo.Symbol);
                if (tokenInfo == null || tokenInfo.Decimals == 0)
                {
                    continue;
                }

                await _tokenAppService.CreateAsync(new TokenCreateDto
                {
                    Symbol = userTokenInfo.Symbol,
                    Address = tokenInfo.Address,
                    Decimals = tokenInfo.Decimals,
                    ChainId = input.ChainId
                });
                tokenDto = new TokenDto
                {
                    Decimals = tokenInfo.Decimals
                };
            }
            userTokenInfo.Amount = userTokenInfo.Balance.ToDecimalsString(tokenDto.Decimals);
            userTokenInfo.PriceInUsd =
                ((long) (userTokenInfo.Balance * symbolPriceMap.GetValueOrDefault(userTokenDto.Symbol)))
                .ToDecimalsString(tokenDto.Decimals);
        }

        if (showList.Count + hiddenList.Count <= 6)
        {
            showList = showList.Concat(hiddenList).ToList();
            hiddenList.Clear();
        }

        showList = showList.OrderByDescending(o => Double.Parse(o.PriceInUsd)).ToList();
        hiddenList = hiddenList.OrderByDescending(o => Double.Parse(o.PriceInUsd)).ToList();

        return new UserAssetInfoDto()
        {
            ShowList = showList,
            HiddenList = hiddenList
        };
    }

    public async Task<TransactionFeeDto> GetTransactionFeeAsync()
    {
        return new TransactionFeeDto
        {
            TransactionFee = _assetShowOptions.TransactionFee
        };
    }
}