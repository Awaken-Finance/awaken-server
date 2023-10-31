using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Asset;
using AwakenServer.ContractEventHandler.Application;
using AwakenServer.Grains.Grain.ApplicationHandler;
using AwakenServer.Monitor;
using AwakenServer.Monitor.Logger;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using Orleans;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Provider;

public class GraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    private readonly GraphQLOptions _graphQLOptions;
    private readonly GraphQLHttpClient _graphQLClient;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger<GraphQLProvider> _logger;
    private readonly ITokenAppService _tokenAppService;
    private readonly IIndicatorLogger _indicatorLogger;

    public GraphQLProvider(ILogger<GraphQLProvider> logger, IClusterClient clusterClient,
        ITokenAppService tokenAppService,
        IIndicatorLogger indicatorLogger,
        IOptions<GraphQLOptions> graphQLOptions)
    {
        _logger = logger;
        _clusterClient = clusterClient;
        _graphQLOptions = graphQLOptions.Value;
        _graphQLClient = new GraphQLHttpClient(_graphQLOptions.Configuration, new NewtonsoftJsonSerializer());
        _tokenAppService = tokenAppService;
        _indicatorLogger = indicatorLogger;
    }

    public async Task<TradePairInfoDtoPageResultDto> GetTradePairInfoListAsync(GetTradePairsInfoInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        var graphQlResponse = await _graphQLClient.SendQueryAsync<TradePairInfoDtoPageResultDto>(new GraphQLRequest
        {
            Query =
                @"query($id:String = null ,$chainId:String = null,$address:String = null,$token0Symbol:String = null,$token1Symbol:String = null,$tokenSymbol:String = null,$feeRate:Float!){
            getTradePairInfoList(getTradePairInfoDto: {id:$id,chainId:$chainId,address:$address,token0Symbol:$token0Symbol,token1Symbol:$token1Symbol,tokenSymbol:$tokenSymbol,feeRate:$feeRate,skipCount:0,maxResultCount:1000}){
            totalCount,
            data {
                id,
                address,
                chainId,
                token0Symbol,
                token1Symbol,
                feeRate,
                isTokenReversed   
            }}}",
            Variables = new
            {
                id = input.Id,
                chainId = input.ChainId,
                address = input.Address,
                token0Symbol = input.Token0Symbol,
                token1Symbol = input.Token1Symbol,
                tokenSymbol = input.TokenSymbol,
                feeRate = input.FeeRate == 0 ? input.FeeRate : 0,
            }
        });
        stopwatch.Stop();
        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        var target = "GetTradePairInfoListAsync";
        _indicatorLogger.LogInformation(MonitorTag.GraphQL, target, duration);
        
        if (graphQlResponse.Errors != null)
        {
            ErrorLog(graphQlResponse.Errors);
            return new TradePairInfoDtoPageResultDto
            {
                GetTradePairInfoList = new TradePairInfoGplResultDto
                {
                    TotalCount = 0,
                    Data = new List<TradePairInfoDto>()
                },
            };
        }
        
         _logger.LogInformation("graphQlResponse:"+graphQlResponse.Data.GetTradePairInfoList.TotalCount);
        
        if (graphQlResponse.Data.GetTradePairInfoList.TotalCount == 0)
        {
            return new TradePairInfoDtoPageResultDto
            {
                GetTradePairInfoList = new TradePairInfoGplResultDto
                {
                    TotalCount = 0,
                    Data = new List<TradePairInfoDto>()
                },
            };
        }
        _logger.LogInformation("total count is {totalCount},data count:{dataCount}", graphQlResponse.Data.GetTradePairInfoList.TotalCount,graphQlResponse.Data.GetTradePairInfoList.Data.Count);

        graphQlResponse.Data.GetTradePairInfoList.Data.ForEach(pair =>
        {
            var token0 = _tokenAppService.GetBySymbolCache(pair.Token0Symbol);
            var token1 = _tokenAppService.GetBySymbolCache(pair.Token1Symbol);
            pair.Token0Id = token0?.Id ?? Guid.Empty;
            pair.Token1Id = token1?.Id ?? Guid.Empty;
        });

        return new TradePairInfoDtoPageResultDto
        {
            GetTradePairInfoList = new TradePairInfoGplResultDto
            {
                TotalCount = graphQlResponse.Data.GetTradePairInfoList.TotalCount,
                Data = graphQlResponse.Data.GetTradePairInfoList.Data
            },
        };
    }

    public async Task<List<LiquidityRecordDto>> GetLiquidRecordsAsync(string chainId, long startBlockHeight,
        long endBlockHeight)
    {
        /*if (startBlockHeight > endBlockHeight)
        {
            _logger.LogInformation("EndBlockHeight should be higher than StartBlockHeight");
            return new List<LiquidityRecordDto>();
        }*/
        var stopwatch = Stopwatch.StartNew();
        var graphQlResponse = await _graphQLClient.SendQueryAsync<LiquidityRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!){
            getLiquidityRecords(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
            {
                chainId,
                pair,
                to,
                address,
                token0Amount,
                token1Amount,
                token0,
                token1,
                lpTokenAmount,
                transactionHash,
                channel,
                sender,
                type,
                timestamp,
                blockHeight,
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        });
        stopwatch.Stop();
        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        var target = "GetLiquidRecordsAsync";
        _indicatorLogger.LogInformation(MonitorTag.GraphQL, target, duration);
        
        if (graphQlResponse.Data.GetLiquidityRecords.IsNullOrEmpty())
        {
            return new List<LiquidityRecordDto>();
        }
        return graphQlResponse.Data.GetLiquidityRecords;
    }

    public async Task<List<SwapRecordDto>> GetSwapRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        // if (startBlockHeight > endBlockHeight)
        // {
        //     _logger.LogInformation("EndBlockHeight should be higher than StartBlockHeight");
        //     return new List<SwapRecordDto>();
        // }
        var stopwatch = Stopwatch.StartNew();
        var graphQlResponse = await _graphQLClient.SendQueryAsync<SwapRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!){
            getSwapRecords(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
            {
                chainId,
                pairAddress,
                sender,
                transactionHash,
                timestamp,
                amountOut,
                amountIn,
                totalFee,
                symbolOut,
                symbolIn,
                channel,
                blockHeight
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        });
        stopwatch.Stop();
        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        var target = "GetSwapRecordsAsync";
        _indicatorLogger.LogInformation(MonitorTag.GraphQL, target, duration);
        
        if (graphQlResponse.Data.GetSwapRecords.IsNullOrEmpty())
        {
            return new List<SwapRecordDto>();
        }
        return graphQlResponse.Data.GetSwapRecords;
    }

    public async Task<List<SyncRecordDto>> GetSyncRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight)
    {
        /*if (startBlockHeight > endBlockHeight)
        {
            _logger.LogInformation("EndBlockHeight should be higher than StartBlockHeight");
            return new List<SyncRecordDto>();
        }*/
        var stopwatch = Stopwatch.StartNew();
        var graphQlResponse = await _graphQLClient.SendQueryAsync<SyncRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!){
            getSyncRecords(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight})
            {
                chainId,
                pairAddress,
                symbolA,
                symbolB,
                reserveA,
                reserveB,
                timestamp,
                blockHeight
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight
            }
        });
        stopwatch.Stop();
        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        var target = "GetSyncRecordsAsync";
        _indicatorLogger.LogInformation(MonitorTag.GraphQL, target, duration);
        
        if (graphQlResponse.Data.GetSyncRecords.IsNullOrEmpty())
        {
            return new List<SyncRecordDto>();
        }
        return graphQlResponse.Data.GetSyncRecords;
    }

    public async Task<LiquidityRecordPageResult> QueryLiquidityRecordAsync(GetLiquidityRecordIndexInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        var graphQlResponse = await _graphQLClient.SendQueryAsync<LiquidityRecordResultDto>(new GraphQLRequest
        {
            Query = 
                @"query($chainId:String!,$address:String,$pair:String = null,$type:LiquidityType = null,$tokenSymbol:String = null,$transactionHash:String = null,$token0:String = null,$token1:String = null,$timestampMin:Long!,$timestampMax:Long!,$skipCount:Int!,$maxResultCount:Int!,$sorting:String = null){
            liquidityRecord(dto: {chainId:$chainId,address:$address,pair:$pair,type:$type,tokenSymbol:$tokenSymbol,transactionHash:$transactionHash,token0:$token0,token1:$token1,timestampMin:$timestampMin,timestampMax:$timestampMax,skipCount:$skipCount,maxResultCount:$maxResultCount,sorting:$sorting}){
                totalCount,
                data{
                    chainId,
                    pair,
                    to,
                    address,
                    token0Amount,
                    token1Amount,
                    token0,
                    token1,
                    lpTokenAmount,
                    transactionHash,
                    channel,
                    sender,
                    type,
                    timestamp,
                }
            }
        }",
            Variables = new
            {
                chainId = input.ChainId,
                address = input.Address,
                pair = input.Pair,
                type = input.Type,
                tokenSymbol = string.IsNullOrEmpty(input.TokenSymbol) ? input.TokenSymbol : input.TokenSymbol.ToUpper(),
                transactionHash = input.TransactionHash,
                token0 = input.Token0,
                token1 = input.Token1,
                timestampMin = input.TimestampMin,
                timestampMax = input.TimestampMax,
                skipCount = input.SkipCount,
                maxResultCount = input.MaxResultCount,
                sorting = input.Sorting
            }
                
        });
        stopwatch.Stop();
        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        var target = "QueryLiquidityRecordAsync";
        _indicatorLogger.LogInformation(MonitorTag.GraphQL, target, duration);
        
        return graphQlResponse.Data.LiquidityRecord;
    }
    
    public async Task<UserLiquidityPageResultDto> QueryUserLiquidityAsync(GetUserLiquidityInput input)
    {
        var stopwatch = Stopwatch.StartNew();
        var graphQlResponse = await _graphQLClient.SendQueryAsync<UserLiquidityResultDto>(new GraphQLRequest
        {
            Query = 
                @"query($chainId:String,$address:String,$skipCount:Int!,$maxResultCount:Int!,$sorting:String = null){
            userLiquidity(dto: {chainId:$chainId,address:$address,skipCount:$skipCount,maxResultCount:$maxResultCount,sorting:$sorting}){
                totalCount,
                data{
                    chainId,
                    pair,
                    address,
                    lpTokenAmount,
                    timestamp,
                }
            }
        }",
            Variables = new
            {
                chainId = input.ChainId,
                address = input.Address,
                skipCount = input.SkipCount,
                maxResultCount = input.MaxResultCount,
                sorting = input.Sorting
            }
                
        });
        stopwatch.Stop();
        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        var target = "QueryUserLiquidityAsync";
        _indicatorLogger.LogInformation(MonitorTag.GraphQL, target, duration);
        
        return graphQlResponse.Data.UserLiquidity;
    }

    public async Task<List<UserTokenDto>> GetUserTokensAsync(string chainId, string address)
    {
        var stopwatch = Stopwatch.StartNew();
        var graphQLResponse = await _graphQLClient.SendQueryAsync<UserTokenResultDto>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$address:String) {
                    getUserTokens(dto: {chainId:$chainId,address:$address}){
                        chainId,
                        address,
                        symbol,
                        balance,    
                    }}",
            Variables = new
            {
                chainId,
                address
            }
        });
        stopwatch.Stop();
        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        var target = "GetUserTokensAsync";
        _indicatorLogger.LogInformation(MonitorTag.GraphQL, target, duration);
        
        return graphQLResponse.Data.GetUserTokens;
    }

    public async Task<long> GetIndexBlockHeightAsync(string chainId)
    {
        var stopwatch = Stopwatch.StartNew();
        var graphQLResponse = await _graphQLClient.SendQueryAsync<ConfirmedBlockHeightRecord>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$filterType:BlockFilterType!) {
                    syncState(dto: {chainId:$chainId,filterType:$filterType}){
                        confirmedBlockHeight}
                    }",
            Variables = new
            {
                chainId,
                filterType = BlockFilterType.LOG_EVENT
            }
        });
        stopwatch.Stop();
        var duration = Convert.ToInt32(stopwatch.Elapsed.TotalMilliseconds);
        var target = "GetIndexBlockHeightAsync";
        _indicatorLogger.LogInformation(MonitorTag.GraphQL, target, duration);
        
        return graphQLResponse.Data.SyncState.ConfirmedBlockHeight;
    }

    public async Task<long> GetLastEndHeightAsync(string chainId, string type)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(type + chainId);
            return await grain.GetStateAsync();
        }
        catch (Exception e)
        {
            _logger.LogError(e, "GetIndexBlockHeight on chain {id} error", chainId);
            return AppServiceConstant.LongError;
        }
    }

    public async Task SetLastEndHeightAsync(string chainId, string type, long height)
    {
        try
        {
            var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(type +
                                                                              chainId);
            await grain.SetStateAsync(height);
        }
        catch (Exception e)
        {
            _logger.LogError(e, "SetIndexBlockHeight on chain {id} error", chainId);
        }
    }

    private void ErrorLog(GraphQLError[] errors)
    {
        errors.ToList().ForEach(error =>
        {
            _logger.LogError("GraphQL error: {message}", error.Message);
        });
    }
}