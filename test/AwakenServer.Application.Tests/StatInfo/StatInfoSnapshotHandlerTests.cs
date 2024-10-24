using System;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.Grains.Tests;
using AwakenServer.StatInfo.Dtos;
using AwakenServer.StatInfo.Etos;
using AwakenServer.StatInfo.Index;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Microsoft.Extensions.Options;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;

namespace AwakenServer.StatInfo
{
    [Collection(ClusterCollection.Name)]
    public class StatInfoSnapshotHandlerTests : TradeTestBase
    {
        private readonly ILocalEventBus _eventBus;
        private readonly INESTRepository<StatInfoSnapshotIndex, Guid> _statInfoSnapshotIndexRepository;
        private readonly IStatInfoAppService _statInfoAppService;
        private readonly IStatInfoInternalAppService _statInfoInternalAppService;
        private readonly IOptionsSnapshot<StatInfoOptions> _statInfoOptions;
        
        public StatInfoSnapshotHandlerTests()
        {
            _eventBus = GetRequiredService<ILocalEventBus>();
            _statInfoSnapshotIndexRepository = GetRequiredService<INESTRepository<StatInfoSnapshotIndex, Guid>>();
            _statInfoAppService = GetRequiredService<IStatInfoAppService>();
            _statInfoInternalAppService = GetRequiredService<IStatInfoInternalAppService>();
            _statInfoOptions = GetRequiredService<IOptionsSnapshot<StatInfoOptions>>();
        }

        [Fact]
        public async Task GetSnapshotTimestampTestAsync()
        {
            var time = new DateTime(2019, 3, 2, 3, 50, 12);
            var snapshotTime = StatInfoHelper.GetSnapshotTimestamp(3600 * 6, DateTimeHelper.ToUnixTimeMilliseconds(time));
            var snapDate = DateTimeHelper.FromUnixTimeMilliseconds(snapshotTime);
            snapDate.Day.ShouldBe(1);
            snapDate.Hour.ShouldBe(22);
        }
        
        public static DateTime GetPreviousMonday(DateTime referenceDate)
        {
            int daysToMonday = (int)referenceDate.DayOfWeek - (int)DayOfWeek.Monday;
            if (daysToMonday < 0)
            {
                daysToMonday += 7;
            }
            return referenceDate.Date.AddDays(-daysToMonday);
        }

        [Fact]
        public async Task PeriodTest()
        {
            var baseTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 6, 5, 12);
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime);
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 10,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                VolumeInUsd = 1,
                Timestamp = timestamp
            });

            var result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress,
                BaseTimestamp = timestamp
            });
            result.Items.Count.ShouldBe(1);
            var hourSnapshotTime = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, baseTime.Hour, 0, 0);
            result.Items[0].Timestamp.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(hourSnapshotTime));
            result.Items[0].PriceInUsd.ShouldBe(10);

            result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Week,
                PairAddress = TradePairBtcEthAddress,
                BaseTimestamp = timestamp
            });
            result.TradePair.Token0.Symbol.ShouldBe("BTC");
            result.TradePair.Token1.Symbol.ShouldBe("ETH");
            result.Items.Count.ShouldBe(1);
            var sixHourSnapshotTime = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, 4, 0, 0);
            result.Items[0].Timestamp.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(sixHourSnapshotTime));
            result.Items[0].PriceInUsd.ShouldBe(10);
            
            result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Month,
                PairAddress = TradePairBtcEthAddress,
                BaseTimestamp = timestamp
            });
            result.TradePair.Token0.Symbol.ShouldBe("BTC");
            result.TradePair.Token1.Symbol.ShouldBe("ETH");
            result.Items.Count.ShouldBe(1);
            var daySnapshotTime = new DateTime(baseTime.Year, baseTime.Month, baseTime.Day, 0, 0, 0);
            result.Items[0].Timestamp.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(daySnapshotTime));
            result.Items[0].PriceInUsd.ShouldBe(10);
            
            
            result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Year,
                PairAddress = TradePairBtcEthAddress,
                BaseTimestamp = timestamp
            });
            result.TradePair.Token0.Symbol.ShouldBe("BTC");
            result.TradePair.Token1.Symbol.ShouldBe("ETH");
            result.Items.Count.ShouldBe(1);
            var lastMonday = GetPreviousMonday(baseTime);
            result.Items[0].Timestamp.ShouldBe(DateTimeHelper.ToUnixTimeMilliseconds(lastMonday));
            result.Items[0].PriceInUsd.ShouldBe(10);
        }
        

        [Fact]
        public async Task GetPoolPriceVolumeTest()
        {
            var baseTime = new DateTime(DateTime.UtcNow.Year, DateTime.UtcNow.Month, DateTime.UtcNow.Day, 6, 5, 12);
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime);
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 10,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime.AddHours(-2))
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                VolumeInUsd = 1,
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime.AddHours(-2))
            });
            
            var volResult = await _statInfoAppService.GetPoolVolumeHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress,
                BaseTimestamp = timestamp
            });
            volResult.Items.Count.ShouldBe(1);
            volResult.Items[0].VolumeInUsd.ShouldBe(1);
            
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 15,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                VolumeInUsd = 2,
                Timestamp = timestamp
            });
            
            var result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress,
                BaseTimestamp = timestamp
            });
            result.Items.Count.ShouldBe(2);
            result.Items[0].PriceInUsd.ShouldBe(10);
            result.Items[1].PriceInUsd.ShouldBe(15);
            
            volResult = await _statInfoAppService.GetPoolVolumeHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress,
                BaseTimestamp = timestamp
            });
            volResult.TotalVolumeInUsd.ShouldBe(3);
            volResult.Items.Count.ShouldBe(2);
            volResult.Items[0].VolumeInUsd.ShouldBe(1);
            volResult.Items[1].VolumeInUsd.ShouldBe(2);
            
            result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Week,
                PairAddress = TradePairBtcEthAddress,
                BaseTimestamp = timestamp
            });
            result.Items.Count.ShouldBe(1);
            result.Items[0].PriceInUsd.ShouldBe(15);
            
            volResult = await _statInfoAppService.GetPoolVolumeHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Week,
                PairAddress = TradePairBtcEthAddress,
                BaseTimestamp = timestamp
            });
            volResult.Items.Count.ShouldBe(1);
            volResult.Items[0].VolumeInUsd.ShouldBe(3);
        }
        
        [Fact]
        public async Task GetStatInfoTest()
        {
            var baseTime = DateTime.UtcNow;
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime);
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 0,
                Tvl = 11,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 0,
                VolumeInUsd = 22,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 1,
                Symbol = "BTC",
                PriceInUsd = 10,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 1,
                Symbol = "BTC",
                Tvl = 13,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 1,
                Symbol = "BTC",
                VolumeInUsd = 1,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 20,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Tvl = 6,
                Timestamp = timestamp
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                VolumeInUsd = 3,
                Timestamp = timestamp
            });
            
            // all
            var result = await _statInfoAppService.GetTvlHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day
            });
            result.Items.Count.ShouldBe(1);
            result.Items[0].Tvl.ShouldBe(11);
            
            var volResult = await _statInfoAppService.GetVolumeHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
            });
            volResult.TotalVolumeInUsd = 22;
            volResult.Items.Count.ShouldBe(1);
            volResult.Items[0].VolumeInUsd.ShouldBe(22);
            
            // token
            var tokenResult = await _statInfoAppService.GetTokenTvlHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                Symbol = "BTC"
            });
            tokenResult.Items.Count.ShouldBe(1);
            tokenResult.Items[0].Tvl.ShouldBe(13);
            
            var priceResult = await _statInfoAppService.GetTokenPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                Symbol = "BTC"
            });
            priceResult.Items.Count.ShouldBe(1);
            priceResult.Items[0].PriceInUsd.ShouldBe(10);
            
            var tokenVolResult = await _statInfoAppService.GetTokenVolumeHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                Symbol = "BTC"
            });
            tokenVolResult.Items.Count.ShouldBe(1);
            tokenVolResult.Items[0].VolumeInUsd.ShouldBe(1);
            
            // pool
            var poolResult = await _statInfoAppService.GetPoolTvlHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            poolResult.Items.Count.ShouldBe(1);
            poolResult.Items[0].Tvl.ShouldBe(6);
            
            var poolPriceResult = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            poolPriceResult.Items.Count.ShouldBe(1);
            poolPriceResult.Items[0].PriceInUsd.ShouldBe(20);
            
            var poolVolResult = await _statInfoAppService.GetPoolVolumeHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            poolVolResult.Items.Count.ShouldBe(1);
            poolVolResult.Items[0].VolumeInUsd.ShouldBe(3);
            
        }
        
        [Fact]
        public async Task DataVersionTest()
        {
            var baseTime = DateTime.UtcNow;
            var timestamp = DateTimeHelper.ToUnixTimeMilliseconds(baseTime);
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = "v2",
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                Price = 10,
                Timestamp = timestamp
            });

            var result = await _statInfoAppService.GetPoolPriceHistoryAsync(new GetStatHistoryInput()
            {
                ChainId = ChainId,
                PeriodType = (int)PeriodType.Day,
                PairAddress = TradePairBtcEthAddress
            });
            result.Items.Count.ShouldBe(0);
        }
        
        [Fact]
        public async Task CalculateApr7dTest()
        {
            var timestamp1 = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-8));
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                LpFeeInUsd = 10,
                Tvl = 100000,
                Timestamp = timestamp1
            });

            var result = await _statInfoAppService.CalculateApr7dAsync(TradePairBtcEthAddress);
            result.ShouldBe(0.0);
            
            var timestamp2 = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow.AddDays(-1));
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                LpFeeInUsd = 5,
                Tvl = 100000,
                Timestamp = timestamp2
            });
            await _eventBus.PublishAsync(new StatInfoSnapshotEto
            {
                Version = _statInfoOptions.Value.DataVersion,
                ChainId = ChainId,
                StatType = 2,
                PairAddress = TradePairBtcEthAddress,
                LpFeeInUsd = 3,
                Tvl = 100000,
                Timestamp = timestamp2
            });

            result = await _statInfoAppService.CalculateApr7dAsync(TradePairBtcEthAddress);
            result.ShouldBe(3.4680000000000004);
        }

        [Fact]
        public async Task StatInfoListTest()
        {
            await _statInfoInternalAppService.UpdateTokenFollowPairAsync(ChainName, _statInfoOptions.Value.DataVersion);
            var syncResult = await _statInfoInternalAppService.CreateLiquidityRecordAsync(new LiquidityRecordDto()
            {
                ChainId = ChainName,
                Pair = TradePairBtcUsdtAddress,
                Address = "0x123456789",
                Timestamp = 1000,
                Token0Amount = NumberFormatter.WithDecimals(10, TokenBtcDecimal),
                Token0 = "BTC",
                Token1Amount = NumberFormatter.WithDecimals(100, TokenUsdtDecimal),
                Token1 = "USDT",
                LpTokenAmount = 50000,
                Type = LiquidityType.Mint,
                TransactionHash = "0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f",
                Channel = "TestChanel",
                Sender = "0x123456789",
                To = "0x123456789",
                BlockHeight = 100
            }, _statInfoOptions.Value.DataVersion);
            syncResult.ShouldBeTrue();
            
            syncResult = await _statInfoInternalAppService.CreateSwapRecordAsync(new SwapRecordDto()
            {
                ChainId = ChainName,
                PairAddress = TradePairBtcUsdtAddress,
                Sender = "0x123456789",
                TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d37",
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                AmountOut = NumberFormatter.WithDecimals(100, TokenUsdtDecimal),
                AmountIn = NumberFormatter.WithDecimals(50, TokenBtcDecimal),
                SymbolOut = "USDT",
                SymbolIn = "BTC",
                Channel = "test",
                BlockHeight = 99
            }, _statInfoOptions.Value.DataVersion);
            syncResult.ShouldBeTrue();
            
            syncResult = await _statInfoInternalAppService.CreateSyncRecordAsync(new SyncRecordDto()
            {
                ChainId = ChainId,
                PairAddress = TradePairBtcUsdtAddress,
                SymbolA = "BTC",
                SymbolB = "USDT",
                ReserveA = NumberFormatter.WithDecimals(10, TokenBtcDecimal),
                ReserveB = NumberFormatter.WithDecimals(20, TokenUsdtDecimal),
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow)
            }, _statInfoOptions.Value.DataVersion);
            syncResult.ShouldBeTrue();

            // all
            var tokenList = await _statInfoAppService.GetTokenStatInfoListAsync(new GetTokenStatInfoListInput());
            tokenList.Items.Count.ShouldBe(3);
            
            // filter by symbol
            tokenList = await _statInfoAppService.GetTokenStatInfoListAsync(new GetTokenStatInfoListInput()
            {
                Symbol = "USDT"
            });
            tokenList.Items[0].Token.Symbol.ShouldBe("USDT");
            tokenList.Items[0].PriceInUsd.ShouldBe(1);
            tokenList.Items[0].Volume24hInUsd.ShouldBe(100);
            tokenList.Items[0].Tvl.ShouldBe(20);
            tokenList.Items[0].PairCount.ShouldBe(1);
            tokenList.Items[0].TransactionCount.ShouldBe(2);
            
            tokenList = await _statInfoAppService.GetTokenStatInfoListAsync(new GetTokenStatInfoListInput()
            {
                Symbol = "BTC"
            });
            tokenList.Items[0].Token.Symbol.ShouldBe("BTC");
            tokenList.Items[0].PriceInUsd.ShouldBe(2);
            tokenList.Items[0].Volume24hInUsd.ShouldBe(50);
            tokenList.Items[0].Tvl.ShouldBe(10);
            tokenList.Items[0].PairCount.ShouldBe(1);
            tokenList.Items[0].TransactionCount.ShouldBe(2);
            
            // all
            var poolList = await _statInfoAppService.GetPoolStatInfoListAsync(new GetPoolStatInfoListInput());
            poolList.Items.Count.ShouldBe(1);
            poolList.Items[0].TradePair.Address.ShouldBe(TradePairBtcUsdtAddress);
            poolList.Items[0].Tvl.ShouldBe(30);
            poolList.Items[0].TransactionCount.ShouldBe(2);
            poolList.Items[0].Volume24hInUsd.ShouldBe(50);
            poolList.Items[0].Volume7dInUsd.ShouldBe(50);
            poolList.Items[0].Apr7d.ShouldBe(0);
            poolList.Items[0].ValueLocked0.ShouldBe(10);
            poolList.Items[0].ValueLocked1.ShouldBe(20);
            
            // filter by pair address
            poolList = await _statInfoAppService.GetPoolStatInfoListAsync(new GetPoolStatInfoListInput()
            {
                PairAddress = TradePairBtcUsdtAddress
            });
            poolList.Items.Count.ShouldBe(1);
            poolList.Items[0].TradePair.Address.ShouldBe(TradePairBtcUsdtAddress);
            
            // filter by symbol
            poolList = await _statInfoAppService.GetPoolStatInfoListAsync(new GetPoolStatInfoListInput()
            {
                Symbol = "BTC"
            });
            poolList.Items.Count.ShouldBe(1);
            poolList.Items[0].TradePair.Address.ShouldBe(TradePairBtcUsdtAddress);
            
            // filter by type
            var txnList = await _statInfoAppService.GetTransactionStatInfoListAsync(new GetTransactionStatInfoListInput()
            {
                TransactionType = (int)TransactionType.Add
            });
            txnList.Items.Count.ShouldBe(1);
            txnList.Items[0].TransactionId.ShouldBe("0xdab24d0f0c28a3be6b59332ab0cb0b4cd54f10f3c1b12cfc81d72e934d74b28f");
            
            txnList = await _statInfoAppService.GetTransactionStatInfoListAsync(new GetTransactionStatInfoListInput()
            {
                TransactionType = (int)TransactionType.Trade
            });
            txnList.Items.Count.ShouldBe(1);
            txnList.Items[0].TransactionId.ShouldBe("6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d37");
        }
    }
}