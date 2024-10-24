using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using AElf.ExceptionHandler;
using AwakenServer.Asset;
using AwakenServer.Common;
using AwakenServer.ContractEventHandler.Application;
using AwakenServer.Dtos.GraphQL;
using AwakenServer.Grains.Grain.ApplicationHandler;
using AwakenServer.Tokens;
using AwakenServer.Trade.Dtos;
using GraphQL;
using GraphQL.Client.Http;
using GraphQL.Client.Serializer.Newtonsoft;
using Microsoft.Extensions.Options;
using Newtonsoft.Json;
using Orleans;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Provider;

public class GraphQLProvider : IGraphQLProvider, ISingletonDependency
{
    private readonly GraphQLOptions _graphQLOptions;
    private readonly GraphQLHttpClient _graphQLClient;
    private readonly IClusterClient _clusterClient;
    private readonly ILogger _logger;
    private readonly ITokenAppService _tokenAppService;
    
    
    public GraphQLProvider(IClusterClient clusterClient,
        ITokenAppService tokenAppService,
        IOptions<GraphQLOptions> graphQLOptions)
    {
        _logger = Log.ForContext<GraphQLProvider>();
        _clusterClient = clusterClient;
        _graphQLOptions = graphQLOptions.Value;
        _graphQLClient = new GraphQLHttpClient(_graphQLOptions.Configuration, new NewtonsoftJsonSerializer());
        _tokenAppService = tokenAppService;
    }

    public async Task<TradePairInfoDtoPageResultDto> GetTradePairInfoListAsync(GetTradePairsInfoInput input)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<TradePairInfoDtoPageResultDto>(new GraphQLRequest
        {
            Query =
                @"query($id:String = null ,$chainId:String = null,$address:String = null,$token0Symbol:String = null,
            $token1Symbol:String = null,$tokenSymbol:String = null,$feeRate:Float!,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxResultCount:Int!,$skipCount:Int!){
            tradePairInfoDtoList:getTradePairInfoList(getTradePairInfoDto: {id:$id,chainId:$chainId,address:$address,token0Symbol:$token0Symbol,
            token1Symbol:$token1Symbol,tokenSymbol:$tokenSymbol,feeRate:$feeRate,
            startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxResultCount:$maxResultCount,skipCount:$skipCount}){
            totalCount,
            data {
                id,
                address,
                chainId,
                token0Symbol,
                token1Symbol,
                feeRate,
                isTokenReversed,
                blockHeight,
                transactionHash
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
                startBlockHeight = input.StartBlockHeight,
                endBlockHeight = input.EndBlockHeight,
                maxResultCount = input.MaxResultCount,
                skipCount = input.SkipCount
            }
        });
        
        if (graphQlResponse.Errors != null)
        {
            ErrorLog(graphQlResponse.Errors);
            return new TradePairInfoDtoPageResultDto
            {
                TradePairInfoDtoList = new TradePairInfoGqlResultDto
                {
                    TotalCount = 0,
                    Data = new List<TradePairInfoDto>()
                },
            };
        }
        
        _logger.Information("graphQlResponse: {totalCount}", graphQlResponse.Data.TradePairInfoDtoList.TotalCount);
        
        if (graphQlResponse.Data.TradePairInfoDtoList.TotalCount == 0)
        {
            return new TradePairInfoDtoPageResultDto
            {
                TradePairInfoDtoList = new TradePairInfoGqlResultDto
                {
                    TotalCount = 0,
                    Data = new List<TradePairInfoDto>()
                },
            };
        }
        _logger.Information("total count is {totalCount},data count:{dataCount}", graphQlResponse.Data.TradePairInfoDtoList.TotalCount,graphQlResponse.Data.TradePairInfoDtoList.Data.Count);

        graphQlResponse.Data.TradePairInfoDtoList.Data.ForEach(pair =>
        {
            var token0 = _tokenAppService.GetBySymbolCache(pair.Token0Symbol);
            var token1 = _tokenAppService.GetBySymbolCache(pair.Token1Symbol);
            pair.Token0Id = token0?.Id ?? Guid.Empty;
            pair.Token1Id = token1?.Id ?? Guid.Empty;
        });

        return new TradePairInfoDtoPageResultDto
        {
            TradePairInfoDtoList = new TradePairInfoGqlResultDto
            {
                TotalCount = graphQlResponse.Data.TradePairInfoDtoList.TotalCount,
                Data = graphQlResponse.Data.TradePairInfoDtoList.Data
            },
        };
    }

    public async Task<List<LiquidityRecordDto>> GetLiquidRecordsAsync(string chainId, long startBlockHeight,
        long endBlockHeight, int skipCount, int maxResultCount)
    {
        /*if (startBlockHeight > endBlockHeight)
        {
            _logger.Information("EndBlockHeight should be higher than StartBlockHeight");
            return new List<LiquidityRecordDto>();
        }*/

        var graphQlResponse = await _graphQLClient.SendQueryAsync<LiquidityRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxResultCount:Int!,$skipCount:Int!){
            getLiquidityRecords(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxResultCount:$maxResultCount,skipCount:$skipCount})
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
                blockHeight
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight,
                maxResultCount,
                skipCount
            }
        });
        
        _logger.Information($"getLiquidityRecords " +
                               $"startBlockHeight: {startBlockHeight}, " +
                               $"endBlockHeight: {endBlockHeight}, " +
                               $"maxResultCount: {maxResultCount}, " +
                               $"skipCount: {skipCount}, " +
                               $"graphQlResponse data count: {graphQlResponse.Data.GetLiquidityRecords.Count}");
        
        if (graphQlResponse.Errors != null)
        {
            ErrorLog(graphQlResponse.Errors);
            return new List<LiquidityRecordDto>();
        }
        
        if (graphQlResponse.Data.GetLiquidityRecords.IsNullOrEmpty())
        {
            return new List<LiquidityRecordDto>();
        }
        return graphQlResponse.Data.GetLiquidityRecords;
    }

    public async Task<List<SwapRecordDto>> GetSwapRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight, int skipCount, int maxResultCount)
    {
        // if (startBlockHeight > endBlockHeight)
        // {
        //     _logger.Information("EndBlockHeight should be higher than StartBlockHeight");
        //     return new List<SwapRecordDto>();
        // }

        var graphQlResponse = await _graphQLClient.SendQueryAsync<SwapRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxResultCount:Int!,$skipCount:Int!){
            getSwapRecords(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxResultCount:$maxResultCount,skipCount:$skipCount})
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
                blockHeight,
                swapRecords {
                    pairAddress,
                    amountOut,
                    amountIn,
                    totalFee,
                    symbolOut,
                    symbolIn,
                    channel,
                    isLimitOrder
                },
                methodName,
                inputArgs,
                isLimitOrder,
                labsFee,
                labsFeeSymbol
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight,
                maxResultCount,
                skipCount
            }
        });
        if (graphQlResponse.Errors != null)
        {
            ErrorLog(graphQlResponse.Errors);
            return new List<SwapRecordDto>();
        }
        if (graphQlResponse.Data.GetSwapRecords.IsNullOrEmpty())
        {
            return new List<SwapRecordDto>();
        }
        return graphQlResponse.Data.GetSwapRecords;
    }


    public async Task<List<LimitOrderFillRecordDto>> GetLimitOrderFillRecordsAsync(string chainId, long startBlockHeight,
        long endBlockHeight, int skipCount, int maxResultCount)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<LimitOrderFillRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxResultCount:Int!,$skipCount:Int!){
            getLimitOrderFillRecords(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxResultCount:$maxResultCount,skipCount:$skipCount})
            {
                chainId
                orderId
                makerAddress
                symbolIn
                symbolOut
                takerAddress
                transactionHash
                amountInFilled
                amountOutFilled
                totalFee
                transactionTime
                transactionFee
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight,
                maxResultCount,
                skipCount
            }
        });
        if (graphQlResponse.Errors != null)
        {
            ErrorLog(graphQlResponse.Errors);
            return new List<LimitOrderFillRecordDto>();
        }
        if (graphQlResponse.Data.GetLimitOrderFillRecords.IsNullOrEmpty())
        {
            return new List<LimitOrderFillRecordDto>();
        }
        return graphQlResponse.Data.GetLimitOrderFillRecords;
    }

    public async Task<List<SyncRecordDto>> GetSyncRecordsAsync(string chainId, long startBlockHeight, long endBlockHeight, int skipCount, int maxResultCount)
    {


        var graphQlResponse = await _graphQLClient.SendQueryAsync<SyncRecordResultDto>(new GraphQLRequest
        {
            Query =
                @"query($chainId:String,$startBlockHeight:Long!,$endBlockHeight:Long!,$maxResultCount:Int!,$skipCount:Int!){
            getSyncRecords(dto: {chainId:$chainId,startBlockHeight:$startBlockHeight,endBlockHeight:$endBlockHeight,maxResultCount:$maxResultCount,skipCount:$skipCount})
            {
                chainId,
                pairAddress,
                symbolA,
                symbolB,
                reserveA,
                reserveB,
                timestamp,
                blockHeight,
                transactionHash
            }}",
            Variables = new
            {
                chainId,
                startBlockHeight,
                endBlockHeight,
                maxResultCount,
                skipCount
            }
        });
        if (graphQlResponse.Errors != null)
        {
            ErrorLog(graphQlResponse.Errors);
            return new List<SyncRecordDto>();
        }
        if (graphQlResponse.Data.GetSyncRecords.IsNullOrEmpty())
        {
            return new List<SyncRecordDto>();
        }
        return graphQlResponse.Data.GetSyncRecords;
    }

    public async Task<LiquidityRecordPageResult> QueryLiquidityRecordAsync(GetLiquidityRecordIndexInput input)
    {
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
        _logger.Information($"liquidityRecord from graphql: {graphQlResponse.Data.LiquidityRecord.TotalCount}");
        return graphQlResponse.Data.LiquidityRecord;
    }

    
    public async Task<UserLiquidityPageResultDto> QueryUserLiquidityAsync(GetUserLiquidityInput input)
    {
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
        return graphQlResponse.Data.UserLiquidity;
    }

    public async Task<LimitOrderPageResultDto> QueryLimitOrderAsync(GetLimitOrdersInput input)
    {
        var graphQlResponse = await _graphQLClient.SendQueryAsync<LimitOrderResultDto>(new GraphQLRequest
        {
            Query = 
                @"query($makerAddress:String,$limitOrderStatus:Int!,$tokenSymbol:String = null,$skipCount:Int!,$maxResultCount:Int!,$sorting:String = null){
            limitOrders(dto: {makerAddress:$makerAddress,limitOrderStatus:$limitOrderStatus,tokenSymbol:$tokenSymbol,skipCount:$skipCount,maxResultCount:$maxResultCount,sorting:$sorting}){
                    totalCount,
                    data{
                        chainId,
                        orderId,
                        maker,
                        symbolIn,
                        symbolOut,
                        transactionHash,
                        transactionFee,
                        amountIn,
                        amountOut,
                        amountInFilled,
                        amountOutFilled,
                        deadline,
                        commitTime,
                        fillTime,
                        cancelTime,
                        removeTime,
                        lastUpdateTime,
                        limitOrderStatus,
                        fillRecords{
                            takerAddress,
                            amountInFilled,
                            amountOutFilled,
                            totalFee,
                            transactionTime,
                            transactionHash,
                            transactionFee,
                            status,            
                        }
                    }
                }
            }",
            Variables = new
            {
                makerAddress = input.MakerAddress,
                limitOrderStatus = input.LimitOrderStatus,
                tokenSymbol = input.TokenSymbol,
                skipCount = input.SkipCount,
                maxResultCount = input.MaxResultCount,
                sorting = input.Sorting
            }
                
        });
        return graphQlResponse.Data.LimitOrders;
    }
    
    public async Task<LimitOrderPageResultDto> QueryLimitOrderAsync(GetLimitOrderDetailsInput input)
    {
        _logger.Information($"Query limitOrderDetails, OrderId: {input.OrderId}");
        var graphQlResponse = await _graphQLClient.SendQueryAsync<LimitOrderDetailResultDto>(new GraphQLRequest
        {
            Query = 
                @"query($orderId:Long!){
            limitOrderDetails(dto: {orderId:$orderId}){
                    totalCount,
                    data{
                        chainId,
                        orderId,
                        maker,
                        symbolIn,
                        symbolOut,
                        transactionHash,
                        transactionFee,
                        amountIn,
                        amountOut,
                        amountInFilled,
                        amountOutFilled,
                        deadline,
                        commitTime,
                        fillTime,
                        cancelTime,
                        removeTime,
                        lastUpdateTime,
                        limitOrderStatus,
                        fillRecords{
                            takerAddress,
                            amountInFilled,
                            amountOutFilled,
                            totalFee,
                            transactionTime,
                            transactionHash,
                            transactionFee,
                            status,            
                        }
                    }
                }
            }",
            Variables = new
            {
                orderId = input.OrderId
            }
        });
        
        if (graphQlResponse.Errors != null)
        {
            ErrorLog(graphQlResponse.Errors);
            return new LimitOrderPageResultDto();
        }
        
        if (graphQlResponse.Data == null || graphQlResponse.Data.LimitOrderDetails == null)
        {
            _logger.Error($"Query limitOrderDetails, result is null");
            return new LimitOrderPageResultDto();
        }
        
        return graphQlResponse.Data.LimitOrderDetails;
    }
    
    public async Task<List<UserTokenDto>> GetUserTokensAsync(string chainId, string address)
    {
        var graphQLResponse = await _graphQLClient.SendQueryAsync<UserTokenResultDto>(new GraphQLRequest
        {
            Query = @"
			    query($chainId:String,$address:String) {
                    getUserTokens(dto: {chainId:$chainId,address:$address}){
                        chainId,
                        address,
                        symbol,
                        balance,
                        imageUri
                    }}",
            Variables = new
            {
                chainId,
                address
            }
        });
        return graphQLResponse.Data.GetUserTokens;
    }
    
    [ExceptionHandler(typeof(Exception), Message = "GetLastEndHeight Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturnLongMinusOne))]
    public virtual async Task<long> GetLastEndHeightAsync(string chainId, WorkerBusinessType type)
    {
        var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(type.ToString() + chainId);
            return await grain.GetStateAsync();
    }

    [ExceptionHandler(typeof(Exception), Message = "SetLastEndHeight Error", TargetType = typeof(HandlerExceptionService), MethodName = nameof(HandlerExceptionService.HandleWithReturn))]
    public virtual async Task SetLastEndHeightAsync(string chainId, WorkerBusinessType type, long height)
    {
        var grain = _clusterClient.GetGrain<IContractServiceGraphQLGrain>(type.ToString() +
                                                                          chainId);
        await grain.SetStateAsync(height);
    }

    private void ErrorLog(GraphQLError[] errors)
    {
        errors.ToList().ForEach(error =>
        {
            _logger.Error("GraphQL error: {message}", error.Message);
        });
    }
}