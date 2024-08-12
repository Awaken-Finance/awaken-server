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
using Newtonsoft.Json;
using Orleans;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AwakenServer.Trade;

[Collection(ClusterCollection.Name)]
public class LimitOrderAppServiceTests : TradeTestBase
{
    private readonly ILimitOrderAppService _LimitOrderAppService;
    private readonly ILocalEventBus _eventBus;
    private readonly MockGraphQLProvider _graphQlProvider;
    private readonly DateTime _order1CommitTime;
    
    public LimitOrderAppServiceTests()
    {
        _LimitOrderAppService = GetRequiredService<ILimitOrderAppService>();
        _eventBus = GetRequiredService<ILocalEventBus>();
        _graphQlProvider = GetRequiredService<MockGraphQLProvider>();
        _order1CommitTime = DateTime.UtcNow;
        
        _graphQlProvider.AddLimitOrder(new LimitOrderDto()
        {
            ChainId = ChainId,
            OrderId = 1,
            Maker = "0x123",
            SymbolIn = TokenEthSymbol,
            SymbolOut = TokenUsdtSymbol,
            TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28a",
            TransactionFee = 10000,
            AmountIn = NumberFormatter.WithDecimals(1, 8),
            AmountOut = 0,
            AmountInFilled = NumberFormatter.WithDecimals(1, 7),
            AmountOutFilled = 0,
            Deadline = DateTimeHelper.ToUnixTimeMilliseconds(_order1CommitTime.AddDays(1)),
            CommitTime = DateTimeHelper.ToUnixTimeMilliseconds(_order1CommitTime),
            LastUpdateTime = DateTimeHelper.ToUnixTimeMilliseconds(_order1CommitTime.AddHours(1)),
            LimitOrderStatus = LimitOrderStatus.PartiallyFilling,
            FillRecords = new List<FillRecord>
            {
                new FillRecord()
                {
                    AmountInFilled = NumberFormatter.WithDecimals(1, 7),
                    AmountOutFilled = 0,
                    Status = LimitOrderStatus.PartiallyFilling,
                    TakerAddress = "0x456",
                    TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28b",
                    TransactionTime = DateTimeHelper.ToUnixTimeMilliseconds(_order1CommitTime.AddHours(1)),
                    TransactionFee = 10000
                }
            }
        });
    }

    [Fact]
    public async Task GetListTest()
    {
        var result = await _LimitOrderAppService.GetListAsync(new GetLimitOrdersInput()
        {
            MakerAddress = "0x123"
        });

        result.Items.Count.ShouldBe(1);
        result.Items[0].MakerAddress.ShouldBe("0x123");
        result.Items[0].AmountIn.ShouldBe("1");
        result.Items[0].AmountInFilled.ShouldBe("0.1");
        result.Items[0].AmountInUSD.ShouldBe("1");
        result.Items[0].AmountInFilledUSD.ShouldBe("0.1");
        result.Items[0].TotalFee.ShouldBe("0");
        result.Items[0].NetworkFee.ShouldBe("0.0002");
        result.Items[0].TradePair.Token0.Symbol.ShouldBe(TokenEthSymbol);
        result.Items[0].TradePair.Token0.Decimals.ShouldBe(8);
        result.Items[0].TradePair.Token1.Symbol.ShouldBe(TokenUsdtSymbol);
        result.Items[0].TradePair.Token1.Decimals.ShouldBe(6);
        var resStr = JsonConvert.SerializeObject(result);
        
    }

    [Fact]
    public async Task GetDetailTest()
    {
        var result = await _LimitOrderAppService.GetListAsync(new GetLimitOrderDetailsInput()
        {
            OrderId = 1
        });
        
        result.Items.Count.ShouldBe(1);
        result.Items[0].AmountInFilled.ShouldBe("0.1");
        result.Items[0].AmountInFilledUSD.ShouldBe("0.1");
        result.Items[0].TransactionTime.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(_order1CommitTime.AddHours(1)));
        result.Items[0].TransactionHash.ShouldBe("0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28b");
        result.Items[0].NetworkFee.ShouldBe("0.0001");
        result.Items[0].TotalFee.ShouldBe("0");
        var resStr = JsonConvert.SerializeObject(result);
        
    }
}