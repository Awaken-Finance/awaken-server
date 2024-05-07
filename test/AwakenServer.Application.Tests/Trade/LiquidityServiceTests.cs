using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.Grains;
using AwakenServer.Grains.Grain.Price.TradePair;
using AwakenServer.Grains.Grain.Trade;
using AwakenServer.Grains.Tests;
using AwakenServer.Provider;
using AwakenServer.Trade.Dtos;
using Orleans;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AwakenServer.Trade;

[Collection(ClusterCollection.Name)]
public class LiquidityServiceTests : TradeTestBase
{
    private readonly ILiquidityAppService _liquidityAppService;
    private readonly ILocalEventBus _eventBus;
    private readonly MockGraphQLProvider _graphQlProvider;
    private readonly ITradePairMarketDataProvider _tradePairMarketDataProvider;
    private readonly IClusterClient _clusterClient;
    private readonly ITradePairAppService _tradePairAppService;

    public LiquidityServiceTests()
    {
        _liquidityAppService = GetRequiredService<ILiquidityAppService>();
        _eventBus = GetRequiredService<ILocalEventBus>();
        _graphQlProvider = GetRequiredService<MockGraphQLProvider>();
        _tradePairMarketDataProvider = GetRequiredService<ITradePairMarketDataProvider>();
        _clusterClient = GetRequiredService<IClusterClient>();
        _tradePairAppService = GetRequiredService<ITradePairAppService>();
    }
    
    [Fact]
    public async Task CreateTest()
    {
        NewLiquidityRecordEvent recordEvent = null;
        _eventBus.Subscribe<NewLiquidityRecordEvent>(t =>
        {
            recordEvent = t;
            return Task.CompletedTask;
        });

        var inputMint = new LiquidityRecordDto()
        {
            ChainId = ChainName,
            Pair = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
            Address = "0x123456789",
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
            Token0Amount = 100,
            Token1Amount = 1000,
            LpTokenAmount = 50000,
            Type = LiquidityType.Mint,
            TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f",
            Channel = "TestChanel",
            Sender = "0x987654321",
            BlockHeight = 100
        };
        await _liquidityAppService.CreateAsync(0, inputMint);
        // var snapshotTime =
        //     _tradePairMarketDataProvider.GetSnapshotTime(DateTimeHelper.FromUnixTimeMilliseconds(inputMint.Timestamp));
        // var market =
        //     await _tradePairMarketDataProvider.GetTradePairMarketDataIndexAsync(ChainId, TradePairEthUsdtId,
        //         snapshotTime);
        // var grain = _clusterClient.GetGrain<ILiquiditySyncGrain>(
        //     GrainIdHelper.GenerateGrainId(inputMint.ChainId, inputMint.BlockHeight));
        // var existed = await grain.ExistTransactionHashAsync(inputMint.TransactionHash);
        // existed.ShouldBe(true);
        // market.TotalSupply.ShouldBe(inputMint.LpTokenAmount.ToDecimalsString(8));
        // recordEvent.ChainId.ShouldBe(ChainId);
        // recordEvent.TradePairId.ShouldBe(TradePairEthUsdtId);
        // recordEvent.Timestamp.ShouldBe(DateTimeHelper.FromUnixTimeMilliseconds(inputMint.Timestamp));
        // recordEvent.LpTokenAmount.ShouldBe(inputMint.LpTokenAmount.ToDecimalsString(8));
        //
        // await _liquidityAppService.CreateAsync(inputMint);
        // market = await _tradePairMarketDataProvider.GetTradePairMarketDataIndexAsync(ChainId, TradePairEthUsdtId,
        //     snapshotTime);
        // market.TotalSupply.ShouldBe(inputMint.LpTokenAmount.ToDecimalsString(8));
        //
        // inputMint.TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b281";
        // await _liquidityAppService.CreateAsync(inputMint);
        // existed = await grain.ExistTransactionHashAsync(inputMint.TransactionHash);
        // market = await _tradePairMarketDataProvider.GetTradePairMarketDataIndexAsync(ChainId, TradePairEthUsdtId,
        //     snapshotTime);
        // existed.ShouldBe(true);
        // market.TotalSupply.ShouldBe("0.001");
    }

    [Fact]
    public async Task CreateLiquidityTypeTest()
    {
        var liquidityRecord = new LiquidityRecordDto()
        {
            ChainId = ChainName,
            Pair = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
            Address = "0x123456789",
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
            Token0Amount = 100,
            Token1Amount = 1000,
            LpTokenAmount = 50000,
            Type = LiquidityType.Mint,
            TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f",
            Channel = "TestChanel",
            Sender = "0x987654321",
            BlockHeight = 100
        };
        await _liquidityAppService.CreateAsync(0,liquidityRecord);
        var tradePairDto = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
        tradePairDto.TotalSupply.ShouldBe(liquidityRecord.LpTokenAmount.ToDecimalsString(8));
        
        liquidityRecord.TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b281";
        liquidityRecord.Type = LiquidityType.Burn;
        liquidityRecord.LpTokenAmount = 5000;
        await _liquidityAppService.CreateAsync(0,liquidityRecord);
        tradePairDto = await _tradePairAppService.GetAsync(TradePairEthUsdtId);
        tradePairDto.TotalSupply.ShouldBe("0.00045");
    }

    [Fact]
    public async Task GetUserLiquidity()
    {
        var liquidityDto = new UserLiquidityDto()
        {
            ChainId = ChainName,
            Pair = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
            Address = "BBB",
            LpTokenAmount = 50000,
        };
        _graphQlProvider.AddUserLiquidity(liquidityDto);
        
        await _liquidityAppService.CreateAsync(0,new LiquidityRecordDto
        {
            ChainId = ChainName,
            Pair = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
            Address = "BBB",
            LpTokenAmount = 50000,
        });
        
        var records = await _liquidityAppService.GetUserLiquidityAsync(new GetUserLiquidityInput()
        {
            ChainId = ChainName,
            Address = "BBB",
            Sorting = "assetusd",
            MaxResultCount = 10
        });
        records.TotalCount.ShouldBe(1);
        records.Items.Count.ShouldBe(1);
        records.Items.First().LpTokenAmount.ShouldBe("0.0005");
        
        records = await _liquidityAppService.GetUserLiquidityFromGraphQLAsync(new GetUserLiquidityInput()
        {
            ChainId = ChainName,
            Address = "BBB",
            Sorting = "assetusd",
            MaxResultCount = 10
        });
        records.TotalCount.ShouldBe(1);
        records.Items.Count.ShouldBe(1);
        records.Items.First().LpTokenAmount.ShouldBe("0.0005");
        
        var record = await _liquidityAppService.GetUserAssetAsync(new GetUserAssertInput()
        {
            ChainId = ChainName,
            Address = "BBB"
        });
        record.AssetUSD.ShouldBe(0);
        
        
        record = await _liquidityAppService.GetUserAssetFromGraphQLAsync(new GetUserAssertInput()
        {
            ChainId = ChainName,
            Address = "BBB"
        });
        record.AssetUSD.ShouldBe(0);
    }

    [Fact]
    public async Task GetRecordsTest()
    {
        var recordDto1 = new LiquidityRecordDto()
        {
            ChainId = ChainName,
            Pair = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
            Address = "BBB",
            To = "CCC",
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
            Token0Amount = 100,
            Token1Amount = 1000,
            Token0 = "ETH",
            Token1 = "USDT",
            LpTokenAmount = 5000,
            Type = LiquidityType.Mint,
            TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f",
            Channel = "TestChanel",
            Sender = "0x987654321",
        };
        _graphQlProvider.AddRecord(recordDto1);
        
        var records = await _liquidityAppService.GetRecordsAsync(new GetLiquidityRecordsInput
        {
            ChainId = ChainName,
            TradePairId = TradePairEthUsdtId,
            Address = "BBB",
            MaxResultCount = 10,
            TimestampMax = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow) + 1000
        });
        records.TotalCount.ShouldBe(1);
        records.Items.Count.ShouldBe(1);
        records.Items.First().TransactionHash.ShouldBe(recordDto1.TransactionHash);
        records.Items.First().TransactionFee.ShouldBe(0.00000001);
        
        var recordDto2 = new LiquidityRecordDto()
        {
            ChainId = ChainName,
            Pair = "0xPool006a6FaC8c710e53c4B2c2F96477119dA361",
            Address = "BBB",
            To = "CCC",
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
            Token0Amount = 100,
            Token1Amount = 1000,
            Token0 = "ETH",
            Token1 = "USDT",
            LpTokenAmount = 50000,
            Type = LiquidityType.Mint,
            TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b280",
            Channel = "TestChanel",
            Sender = "0x987654321",
        };
        _graphQlProvider.AddRecord(recordDto2);
        var records2 = await _liquidityAppService.GetRecordsAsync(new GetLiquidityRecordsInput
        {
            ChainId = ChainName,
            TradePairId = TradePairEthUsdtId,
            Address = "BBB",
            MaxResultCount = 10,
            TimestampMax = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow) + 1000
        });
        records2.TotalCount.ShouldBe(2);
        records2.Items.Count.ShouldBe(2);
        records2.Items.First().TransactionHash.ShouldBe(recordDto2.TransactionHash);
        records2.Items[1].TransactionHash.ShouldBe(recordDto1.TransactionHash);
        
        var records3 = await _liquidityAppService.GetRecordsAsync(new GetLiquidityRecordsInput
        {
            ChainId = ChainName,
            TradePairId = TradePairEthUsdtId,
            Address = "CCC",
            MaxResultCount = 10,
            TimestampMax = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow) + 1000
        });
        records3.TotalCount.ShouldBe(0);
    }
    
    
    [Fact]
    public async Task RevertTest()
    {
        var recordDto1 = new LiquidityRecordDto()
        {
            ChainId = ChainName,
            Pair = TradePairEthUsdtAddress,
            Address = "BBB",
            To = "CCC",
            Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
            Token0Amount = 100,
            Token1Amount = 1000,
            Token0 = "ETH",
            Token1 = "USDT",
            LpTokenAmount = 50000,
            Type = LiquidityType.Mint,
            TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f",
            Channel = "TestChanel",
            Sender = "0x987654321",
        };
        _graphQlProvider.AddRecord(recordDto1);
        await _liquidityAppService.CreateAsync(recordDto1);

        var pairGrain = _clusterClient.GetGrain<ITradePairGrain>(GrainIdHelper.GenerateGrainId(TradePairEthUsdtId));
        var pairData = (await pairGrain.GetAsync()).Data;
        pairData.TotalSupply.ShouldBe("0.0005");

        var needDeletedTradeRecords = new List<string>
        {
            recordDto1.TransactionHash
        };
        
        await _liquidityAppService.DoRevertAsync(ChainId, needDeletedTradeRecords);
        pairData = (await pairGrain.GetAsync()).Data;
        pairData.TotalSupply.ShouldBe("0");

        await _liquidityAppService.RevertLiquidityAsync(ChainId);
    }
}