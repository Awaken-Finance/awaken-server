using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AElf.Client.MultiToken;
using AwakenServer.Chains;
using AwakenServer.Commons;
using AwakenServer.Grains.Grain.Asset;
using AwakenServer.Price;
using AwakenServer.Provider;
using AwakenServer.Tokens;
using AwakenServer.Trade;
using Google.Protobuf.WellKnownTypes;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
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
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<AssetAppService> _logger;

    public AssetAppService(IGraphQLProvider graphQlProvider,
        ITokenAppService tokenAppService,
        IPriceAppService priceAppService,
        IOptionsSnapshot<AssetShowOptions> optionsSnapshot,
        IAElfClientProvider aelfClientProvider,
        IOptionsSnapshot<AssetWhenNoTransactionOptions> showSymbolsWhenNoTransactionOptions,
        IDistributedCache<UserAssetInfoDto> userAssetInfoDtoCache, IClusterClient clusterClient,
        ILogger<AssetAppService> logger)
    {
        _graphQlProvider = graphQlProvider;
        _tokenAppService = tokenAppService;
        _priceAppService = priceAppService;
        _assetShowOptions = optionsSnapshot.Value;
        _aelfClientProvider = aelfClientProvider;
        _clusterClient = clusterClient;
        _assetWhenNoTransactionOptions = showSymbolsWhenNoTransactionOptions.Value;
        _userAssetInfoDtoCache = userAssetInfoDtoCache;
        _logger = logger;
    }

    public async Task<UserAssetInfoDto> GetUserAssetInfoAsync(GetUserAssetInfoDto input)
    {
        var tokenList = await _graphQlProvider.GetUserTokensAsync(input.ChainId, input.Address);
        if (tokenList == null || tokenList.Count == 0)
        {
            return await GetAssetFromCacheOrAElfAsync(input.ChainId, input.Address);
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

        showList = showList.Where(o => o.PriceInUsd != null).Where(o => Double.Parse(o.PriceInUsd) > 0)
            .OrderByDescending(o => Double.Parse(o.PriceInUsd)).ToList();
        hiddenList = hiddenList.Where(o => o.PriceInUsd != null).Where(o => Double.Parse(o.PriceInUsd) > 0)
            .OrderByDescending(o => Double.Parse(o.PriceInUsd)).ToList();

        return new UserAssetInfoDto()
        {
            ShowList = showList,
            HiddenList = hiddenList
        };
    }

    private async Task<UserAssetInfoDto> GetAssetFromCacheOrAElfAsync(string chainId, string address)
    {
        var symbolPriceMap =
            (await _priceAppService.GetTokenPriceListAsync(_assetWhenNoTransactionOptions.Symbols)).Items
            .ToDictionary(
                i => i.Symbol,
                i => i.PriceInUsd);

        var userAsset = await _userAssetInfoDtoCache.GetAsync($"{userAssetInfoDtoPrefix}:{chainId}:{address}");
        if (userAsset != null)
        {
            foreach (var userTokenInfo in userAsset.ShowList)
            {
                var decimals = await GetTokenDecimalAsync(userTokenInfo);
                userTokenInfo.PriceInUsd =
                    ((long)(userTokenInfo.Balance * symbolPriceMap.GetValueOrDefault(userTokenInfo.Symbol)))
                    .ToDecimalsString(decimals);
            }

            foreach (var userTokenInfo in userAsset.HiddenList)
            {
                var decimals = await GetTokenDecimalAsync(userTokenInfo);
                userTokenInfo.PriceInUsd =
                    ((long)(userTokenInfo.Balance * symbolPriceMap.GetValueOrDefault(userTokenInfo.Symbol)))
                    .ToDecimalsString(decimals);
            }

            await _userAssetInfoDtoCache.SetAsync($"{userAssetInfoDtoPrefix}:{chainId}:{address}", userAsset,
                new DistributedCacheEntryOptions
                {
                    AbsoluteExpiration =
                        DateTimeOffset.UtcNow.AddSeconds(_assetWhenNoTransactionOptions.ExpireDurationMinutes)
                });

            return userAsset;
        }


        var showList = new List<UserTokenInfo>();
        var hiddenList = new List<UserTokenInfo>();

        foreach (var symbol in _assetWhenNoTransactionOptions.Symbols)
        {
            var balanceOutput = await _aelfClientProvider.GetBalanceAsync(chainId, address,
                _assetWhenNoTransactionOptions.ContractAddressOfGetBalance[chainId], symbol);
            if (balanceOutput != null)
            {
                var userTokenInfo = new UserTokenInfo()
                {
                    Balance = balanceOutput.Balance,
                    Symbol = balanceOutput.Symbol,
                    ChainId = chainId,
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

                var decimals = await GetTokenDecimalAsync(userTokenInfo);
                userTokenInfo.Amount = userTokenInfo.Balance.ToDecimalsString(decimals);
                userTokenInfo.PriceInUsd =
                    ((long)(userTokenInfo.Balance * symbolPriceMap.GetValueOrDefault(userTokenInfo.Symbol)))
                    .ToDecimalsString(decimals);
            }
        }

        showList = showList.Where(o => o.PriceInUsd != null).Where(o => Double.Parse(o.PriceInUsd) > 0)
            .OrderByDescending(o => Double.Parse(o.PriceInUsd))
            .ToList();
        hiddenList = hiddenList.Where(o => o.PriceInUsd != null).Where(o => Double.Parse(o.PriceInUsd) > 0)
            .OrderByDescending(o => Double.Parse(o.PriceInUsd))
            .ToList();

        var result = new UserAssetInfoDto()
        {
            ShowList = showList,
            HiddenList = hiddenList
        };

        await _userAssetInfoDtoCache.SetAsync($"{userAssetInfoDtoPrefix}:{chainId}:{address}", result,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.UtcNow.AddMinutes(_assetWhenNoTransactionOptions.ExpireDurationMinutes)
            });
        return result;
    }

    public async Task<int> GetTokenDecimalAsync(UserTokenInfo userTokenInfo)
    {
        var tokenDto = await _tokenAppService.GetAsync(new GetTokenInput
        {
            Symbol = userTokenInfo.Symbol
        });

        if (tokenDto != null)
        {
            return tokenDto.Decimals;
        }


        var tokenInfo =
            await _aelfClientProvider.GetTokenInfoAsync(userTokenInfo.ChainId, null, userTokenInfo.Symbol);
        if (tokenInfo == null || tokenInfo.Decimals == 0)
        {
            return 0;
        }

        await _tokenAppService.CreateAsync(new TokenCreateDto
        {
            Symbol = userTokenInfo.Symbol,
            Address = tokenInfo.Address,
            Decimals = tokenInfo.Decimals,
            ChainId = userTokenInfo.ChainId
        });


        return tokenInfo.Decimals;
    }

    public async Task<TransactionFeeDto> GetTransactionFeeAsync()
    {
        return new TransactionFeeDto
        {
            TransactionFee = _assetShowOptions.TransactionFee
        };
    }

    public async Task<CommonResponseDto<Empty>> SetDefaultTokenAsync(SetDefaultTokenDto input)
    {
        try
        {
            if (!_assetShowOptions.ShowList.Exists(o => o == input.TokenSymbol))
            {
                throw new ArgumentException("no support symbol", input.TokenSymbol);
            }

            var defaultTokenGrain = _clusterClient.GetGrain<IDefaultTokenGrain>(input.Address);

            await defaultTokenGrain.SetTokenAsync(input.TokenSymbol);
            return new CommonResponseDto<Empty>();
        }
        catch (Exception e)
        {
            return new CommonResponseDto<Empty>().Error(e);
        }
    }


    public async Task<DefaultTokenDto> GetDefaultTokenAsync(GetDefaultTokenDto input)
    {
        var defaultTokenGrain = _clusterClient.GetGrain<IDefaultTokenGrain>(input.Address);

        var result = defaultTokenGrain.GetAsync();

        var defaultTokenDto = new DefaultTokenDto();
        defaultTokenDto.TokenSymbol = result.Result.Data.TokenSymbol ?? _assetShowOptions.DefaultSymbol;
        defaultTokenDto.Address = input.Address;
        return defaultTokenDto;
    }
}