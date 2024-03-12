using System;
using System.Collections.Concurrent;
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
using Microsoft.EntityFrameworkCore.Metadata.Internal;
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
        _logger.LogInformation("get user token list from indexer,symbol:{symbol}",
            tokenList.Select(s => s.Symbol).ToList());
        if (tokenList == null || tokenList.Count == 0)
        {
            return await GetAssetFromCacheOrAElfAsync(input.ChainId, input.Address);
        }

        var list = new List<UserTokenInfo>();

        var symbolList = tokenList.Select(i => i.Symbol).ToList();
        var symbolPriceMap =
            (await _priceAppService.GetTokenPriceListAsync(symbolList)).Items.ToDictionary(i => i.Symbol,
                i => i.PriceInUsd);
        foreach (var userTokenDto in tokenList)
        {
            var userTokenInfo = ObjectMapper.Map<UserTokenDto, UserTokenInfo>(userTokenDto);
            list.Add(userTokenInfo);

            await SetUserTokenInfoAsync(userTokenInfo, symbolPriceMap.GetValueOrDefault(userTokenDto.Symbol));
        }

        await AddNftTokenInfoAsync(list, input.ChainId, input.Address);


        return await FilterListAsync(list);
    }

    private async Task<UserAssetInfoDto> FilterListAsync(List<UserTokenInfo> list)
    {
        var showList = new List<UserTokenInfo>();
        var hiddenList = new List<UserTokenInfo>();

        foreach (var userTokenInfo in list)
        {
            if (_assetShowOptions.ShowList.Contains(userTokenInfo.Symbol))
            {
                showList.Add(userTokenInfo);
            }
            else
            {
                hiddenList.Add(userTokenInfo);
            }
        }

        showList = showList.Where(o => o.PriceInUsd != null).Where(o => o.Balance > 0)
            .OrderByDescending(o => Double.Parse(o.PriceInUsd)).ThenBy(o => o.Symbol).ToList();

        hiddenList = hiddenList.Where(o => o.PriceInUsd != null).Where(o => o.Balance > 0)
            .OrderByDescending(o => Double.Parse(o.PriceInUsd)).ThenBy(o => o.Symbol).ToList();

        if (showList.Count < _assetShowOptions.ShowListLength)
        {
            var fillLen = _assetShowOptions.ShowListLength - showList.Count;

            if (hiddenList.Count >= fillLen)
            {
                showList.AddRange(hiddenList.GetRange(0, fillLen));
                hiddenList = hiddenList.GetRange(fillLen, hiddenList.Count - fillLen);
            }
            else
            {
                showList.AddRange(hiddenList);
                hiddenList = new List<UserTokenInfo>();
            }

            showList = showList.Where(o => o.PriceInUsd != null).Where(o => o.Balance > 0)
                .OrderByDescending(o => Double.Parse(o.PriceInUsd)).ThenBy(o => o.Symbol).ToList();
        }

        if (showList.Count > _assetShowOptions.ShowListLength)
        {
            hiddenList.InsertRange(0, showList.GetRange(_assetShowOptions.ShowListLength,
                showList.Count - _assetShowOptions.ShowListLength));


            showList.RemoveRange(_assetShowOptions.ShowListLength, showList.Count - _assetShowOptions.ShowListLength);
        }

        return new UserAssetInfoDto()
        {
            ShowList = showList,
            HiddenList = hiddenList
        };
    }

    private async Task AddNftTokenInfoAsync(List<UserTokenInfo> list, string chainId, string address)
    {
        var userTokenInfosDic = list.ToDictionary(key => key.Symbol);

        var concurrentList = new ConcurrentBag<UserTokenInfo>();
        await Task.WhenAll(_assetShowOptions.NftList.Select(async nftSymbol =>
        {
            if (!userTokenInfosDic.ContainsKey(nftSymbol))
            {
                try
                {
                    var balanceOutput = await Task.Run(() => _aelfClientProvider.GetBalanceAsync(chainId, address,
                        _assetWhenNoTransactionOptions.ContractAddressOfGetBalance[chainId], nftSymbol));
                    _logger.LogInformation("get balance address:{address},token:{token},,balance:{balance}", address,
                        nftSymbol,
                        balanceOutput == null ? 0 : balanceOutput.Balance);
                    if (balanceOutput == null || balanceOutput.Balance == 0)
                    {
                        return;
                    }


                    var userTokenInfo = new UserTokenInfo()
                    {
                        Balance = balanceOutput.Balance,
                        Symbol = balanceOutput.Symbol,
                        ChainId = chainId,
                        Address = address,
                    };

                    await SetUserTokenInfoAsync(userTokenInfo, 0);
                    concurrentList.Add(userTokenInfo);
                }
                catch (Exception e)
                {
                    _logger.LogError(e, "get balance from node err,token:{token},chanId:{chainId},address:{address}",
                        nftSymbol, chainId, address);
                    throw new UserFriendlyException("Internal service error, place try again later.");
                }
            }
        }));

        list.AddRange(concurrentList);
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
            return userAsset;
        }


        var list = new List<UserTokenInfo>();
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
                list.Add(userTokenInfo);

                await SetUserTokenInfoAsync(userTokenInfo, symbolPriceMap.GetValueOrDefault(userTokenInfo.Symbol));
            }
        }


        await AddNftTokenInfoAsync(list, chainId, address);

        var result = await FilterListAsync(list);


        await _userAssetInfoDtoCache.SetAsync($"{userAssetInfoDtoPrefix}:{chainId}:{address}", result,
            new DistributedCacheEntryOptions
            {
                AbsoluteExpiration =
                    DateTimeOffset.UtcNow.AddMinutes(_assetWhenNoTransactionOptions.ExpireDurationMinutes)
            });
        return result;
    }


    public async Task SetUserTokenInfoAsync(UserTokenInfo userTokenInfo, decimal priceInUsd)
    {
        var tokenDecimal = await GetTokenDecimalAsync(userTokenInfo.Symbol, userTokenInfo.ChainId);


        userTokenInfo.Amount = userTokenInfo.Balance.ToDecimalsString(tokenDecimal);


        userTokenInfo.PriceInUsd =
            ((long)(userTokenInfo.Balance * priceInUsd))
            .ToDecimalsString(tokenDecimal);
    }

    public async Task<int> GetTokenDecimalAsync(string symbol, string chainId)
    {
        var tokenDto = await _tokenAppService.GetAsync(new GetTokenInput
        {
            Symbol = symbol
        });

        if (tokenDto != null)
        {
            return tokenDto.Decimals;
        }


        var tokenInfo =
            await _aelfClientProvider.GetTokenInfoAsync(chainId, null, symbol);
        if (tokenInfo == null)
        {
            _logger.LogInformation("GetTokenInfo is null:{token}", symbol);
            return 0;
        }


        await _tokenAppService.CreateAsync(new TokenCreateDto
        {
            Symbol = symbol,
            Address = tokenInfo.Address,
            Decimals = tokenInfo.Decimals,
            ChainId = chainId
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