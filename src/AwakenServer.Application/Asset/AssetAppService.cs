using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.MultiToken;
using AwakenServer.Chains;
using AwakenServer.Price;
using AwakenServer.Provider;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Options;
using Volo.Abp;
using Volo.Abp.Application.Services;
using Volo.Abp.Caching;

namespace AwakenServer.Asset;

[RemoteService(false)]
public class AssetAppService : ApplicationService, IAssetAppService
{
    private readonly IGraphQLProvider _graphQlProvider;
    private readonly ITokenAppService _tokenAppService;
    private readonly IPriceAppService _priceAppService;
    private readonly AssetShowOptions _assetShowOptions;
    private readonly IAElfClientProvider _aelfClientProvider;
    private readonly AssetWhenNoTransactionOptions _assetWhenNoTransactionOptions;
    private readonly IDistributedCache<UserAssetInfoDto> _userAssetInfoDtoCache;
    private const string userAssetInfoDtoPrefix = "AwakenServer:Asset:";

    public AssetAppService(IGraphQLProvider graphQlProvider,
        ITokenAppService tokenAppService,
        IPriceAppService priceAppService,
        IOptionsSnapshot<AssetShowOptions> optionsSnapshot,
        IAElfClientProvider aelfClientProvider,
        IOptionsSnapshot<AssetWhenNoTransactionOptions> showSymbolsWhenNoTransactionOptions,
        IDistributedCache<UserAssetInfoDto> userAssetInfoDtoCache)
    {
        _graphQlProvider = graphQlProvider;
        _tokenAppService = tokenAppService;
        _priceAppService = priceAppService;
        _assetShowOptions = optionsSnapshot.Value;
        _aelfClientProvider = aelfClientProvider;
        _assetWhenNoTransactionOptions = showSymbolsWhenNoTransactionOptions.Value;
        _userAssetInfoDtoCache = userAssetInfoDtoCache;
    }

    public async Task<UserAssetInfoDto> GetUserAssetInfoAsync(GetUserAssetInfoDto input)
    {
        var tokenList = await _graphQlProvider.GetUserTokensAsync(input.ChainId, input.Address);
        tokenList = null;
        if (tokenList == null || tokenList.Count == 0)
        {
            return await GetAssetFromCacheOrAElf(input.ChainId, input.Address);
        }

        var showList = new List<UserTokenInfo>();
        var hiddenList = new List<UserTokenInfo>();
        var symbolList = tokenList.Select(i => i.Symbol).ToList();
        var symbolPriceMap =
            (await _priceAppService.GetTokenPriceListAsync(symbolList)).Items.ToDictionary(i => i.Symbol,
                i => i.PriceInUsd);
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
                ((long)(userTokenInfo.Balance * symbolPriceMap.GetValueOrDefault(userTokenDto.Symbol)))
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

    private async Task<UserAssetInfoDto> GetAssetFromCacheOrAElf(string chanId, string address)
    {
        var symbolPriceMap =
            (await _priceAppService.GetTokenPriceListAsync(_assetWhenNoTransactionOptions.Symbols)).Items
            .ToDictionary(
                i => i.Symbol,
                i => i.PriceInUsd);

        var userAsset = await _userAssetInfoDtoCache.GetAsync($"{userAssetInfoDtoPrefix}:{chanId}:{address}");
        if (userAsset != null)
        {
            foreach (var userTokenInfo in userAsset.ShowList)
            {
                var tokenDto = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    Symbol = userTokenInfo.Symbol
                });

                var decimals = tokenDto == null ? 0 : tokenDto.Decimals;
                userTokenInfo.PriceInUsd =
                    ((long)(userTokenInfo.Balance * symbolPriceMap.GetValueOrDefault(userTokenInfo.Symbol)))
                    .ToDecimalsString(decimals);
            }

            foreach (var userTokenInfo in userAsset.HiddenList)
            {
                var tokenDto = await _tokenAppService.GetAsync(new GetTokenInput
                {
                    Symbol = userTokenInfo.Symbol
                });

                var decimals = tokenDto == null ? 0 : tokenDto.Decimals;
                userTokenInfo.PriceInUsd =
                    ((long)(userTokenInfo.Balance * symbolPriceMap.GetValueOrDefault(userTokenInfo.Symbol)))
                    .ToDecimalsString(decimals);
            }
            await _userAssetInfoDtoCache.SetAsync($"{userAssetInfoDtoPrefix}:{chanId}:{address}", userAsset,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration =
                        DateTimeOffset.UtcNow.AddSeconds(1)
                });

            return userAsset;
        }


        var showList = new List<UserTokenInfo>();
        var hiddenList = new List<UserTokenInfo>();

        foreach (var symbol in _assetWhenNoTransactionOptions.Symbols)
        {
            var balanceOutput = await _aelfClientProvider.GetBalanceAsync(chanId, address,
                _assetWhenNoTransactionOptions.ContractAddressOfGetBalance, symbol);
            if (balanceOutput != null)
            {
                var userTokenInfo = new UserTokenInfo()
                {
                    Balance = balanceOutput.Balance,
                    Symbol = balanceOutput.Symbol,
                    ChainId = chanId,
                    Address = address
                };
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
                    var tokenInfo = await _aelfClientProvider.GetTokenInfoAsync(chanId, null, userTokenInfo.Symbol);
                    if (tokenInfo == null || tokenInfo.Decimals == 0)
                    {
                        continue;
                    }

                    await _tokenAppService.CreateAsync(new TokenCreateDto
                    {
                        Symbol = userTokenInfo.Symbol,
                        Address = tokenInfo.Address,
                        Decimals = tokenInfo.Decimals,
                        ChainId = userTokenInfo.ChainId
                    });
                    tokenDto = new TokenDto
                    {
                        Decimals = tokenInfo.Decimals
                    };
                }

                userTokenInfo.Amount = userTokenInfo.Balance.ToDecimalsString(tokenDto.Decimals);
                userTokenInfo.PriceInUsd =
                    ((long)(userTokenInfo.Balance * symbolPriceMap.GetValueOrDefault(userTokenInfo.Symbol)))
                    .ToDecimalsString(tokenDto.Decimals);
            }
        }

        showList = showList.OrderByDescending(o => Double.Parse(o.PriceInUsd)).ToList();
        hiddenList = hiddenList.OrderByDescending(o => Double.Parse(o.PriceInUsd)).ToList();

        var result = new UserAssetInfoDto()
        {
            ShowList = showList,
            HiddenList = hiddenList
        };
        await _userAssetInfoDtoCache.SetAsync($"{userAssetInfoDtoPrefix}:{chanId}:{address}", result,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.UtcNow.AddMinutes(_assetWhenNoTransactionOptions.ExpireDurationMinutes)
            });


        return result;
    }


    public async Task<TransactionFeeDto> GetTransactionFeeAsync()
    {
        return new TransactionFeeDto
        {
            TransactionFee = _assetShowOptions.TransactionFee
        };
    }
}