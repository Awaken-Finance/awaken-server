using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using Awaken.Contracts.Hooks;
using AwakenServer.Activity.Dtos;
using AwakenServer.Activity.Index;
using AwakenServer.Grains.Tests;
using AwakenServer.Provider;
using AwakenServer.Trade;
using AwakenServer.Trade.Dtos;
using Google.Protobuf;
using Microsoft.AspNetCore.Routing.Matching;
using Nest;
using Shouldly;
using Volo.Abp.EventBus.Local;
using Xunit;
using Token = AwakenServer.Tokens.Token;

namespace AwakenServer.Activity
{
    [Collection(ClusterCollection.Name)]
    public class ActivityAppServiceDataTests : TradeTestBase
    {
        private readonly IActivityAppService _activityAppService;
        private INESTRepository<UserActivityInfoIndex, Guid> _userActivityInfoRepository;
        private INESTRepository<RankingListSnapshotIndex, Guid> _rankingListSnapshotRepository;
        public ActivityAppServiceDataTests()
        {
            _activityAppService = GetRequiredService<IActivityAppService>();
            _userActivityInfoRepository = GetRequiredService<INESTRepository<UserActivityInfoIndex, Guid>>();
            _rankingListSnapshotRepository = GetRequiredService<INESTRepository<RankingListSnapshotIndex, Guid>>();
        }

        [Fact]
        public async Task SwapRepeatScanTest()
        {
            var swapRecord = new SwapRecordDto
            {
                ChainId = ChainId,
                PairAddress = TradePairEthUsdtAddress,
                Sender = "TV2aRV4W5oSJzxrkBvj8XmJKkMCiEQnAvLmtM9BqLTN3beXm2",
                TransactionHash = "6622966a928185655d691565d6128835e7d1ccdf1dd3b5f277c5f2a5b2802d37",
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                AmountOut = NumberFormatter.WithDecimals(10, 8),
                AmountIn = NumberFormatter.WithDecimals(100, 6),
                SymbolIn = TokenUsdtSymbol,
                SymbolOut = TokenEthSymbol,
                Channel = "test",
                BlockHeight = 99,
                LabsFee = 150000,
                LabsFeeSymbol = TokenEthSymbol
            };
            var createActivitySwapResult = await _activityAppService.CreateSwapAsync(swapRecord);
            createActivitySwapResult.ShouldBe(true);
            
            createActivitySwapResult = await _activityAppService.CreateSwapAsync(swapRecord);
            createActivitySwapResult.ShouldBe(false);
        }
        
        [Fact]
        public async Task RandomTimeLpSnapshotTest()
        {
            var random = new Random();
            var snapshotTimes = new List<TimeSpan>();
            var lastExecutionTime = DateTime.UtcNow;
            for (int i = 0; i < 1000; i++)
            {
                var snapshotTime = RandomSnapshotHelper.GetLpSnapshotTime(lastExecutionTime);
                snapshotTimes.Add(snapshotTime.TimeOfDay);
                var nextExecutionTime = RandomSnapshotHelper.GetNextLpSnapshotExecutionTime(random, lastExecutionTime);
                lastExecutionTime = new DateTime(lastExecutionTime.Year, lastExecutionTime.Month, lastExecutionTime.Day,
                    nextExecutionTime.Hours, nextExecutionTime.Minutes, nextExecutionTime.Seconds);
            }
            snapshotTimes.Count.ShouldBe(1000);
            for (int i = 1; i < snapshotTimes.Count; i++)
            {
                var timeDifference = (snapshotTimes[i] - snapshotTimes[i - 1]).Hours;
                if (snapshotTimes[i - 1].Hours == 23 && snapshotTimes[i].Hours == 0)
                {
                    timeDifference = 1;
                }
                timeDifference.ShouldBe(1, $"Snapshot at index {i} and {i-1} should have a 1-hour difference, but got {timeDifference} hours.");
            }
        }
        
        [Fact]
        public async Task SwapTest()
        {
            var createActivitySwapResult = await _activityAppService.CreateSwapAsync(new SwapRecordDto
            {
                ChainId = ChainId,
                PairAddress = TradePairEthUsdtAddress,
                Sender = "0x10",
                TransactionHash = "0x1",
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                AmountOut = NumberFormatter.WithDecimals(10, 8),
                AmountIn = NumberFormatter.WithDecimals(100, 6),
                SymbolIn = TokenUsdtSymbol,
                SymbolOut = TokenEthSymbol,
                Channel = "test",
                BlockHeight = 99,
                LabsFee = 150000,
                LabsFeeSymbol = TokenEthSymbol
            });
            createActivitySwapResult.ShouldBe(true);
            
            createActivitySwapResult = await _activityAppService.CreateSwapAsync(new SwapRecordDto
            {
                ChainId = ChainId,
                PairAddress = TradePairEthUsdtAddress,
                Sender = "0x10",
                TransactionHash = "0x2",
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                AmountOut = NumberFormatter.WithDecimals(10, 8),
                AmountIn = NumberFormatter.WithDecimals(100, 6),
                SymbolIn = TokenUsdtSymbol,
                SymbolOut = TokenEthSymbol,
                Channel = "test",
                BlockHeight = 99,
                LabsFee = 150000,
                LabsFeeSymbol = TokenEthSymbol
            });
            createActivitySwapResult.ShouldBe(true);
            
            createActivitySwapResult = await _activityAppService.CreateSwapAsync(new SwapRecordDto
            {
                ChainId = ChainId,
                PairAddress = TradePairEthUsdtAddress,
                Sender = "0x11",
                TransactionHash = "0x3",
                Timestamp = DateTimeHelper.ToUnixTimeMilliseconds(DateTime.UtcNow),
                AmountOut = NumberFormatter.WithDecimals(100, 8),
                AmountIn = NumberFormatter.WithDecimals(100, 6),
                SymbolIn = TokenUsdtSymbol,
                SymbolOut = TokenEthSymbol,
                Channel = "test",
                BlockHeight = 99,
                LabsFee = 1500000,
                LabsFeeSymbol = TokenEthSymbol
            });
            createActivitySwapResult.ShouldBe(true);
            
            var userActivityInfo = await _userActivityInfoRepository.GetListAsync();
            userActivityInfo.Item2.Count.ShouldBe(2);
            userActivityInfo.Item2[0].ActivityId.ShouldBe(1);
            userActivityInfo.Item2[0].Address.ShouldBe("0x10");
            userActivityInfo.Item2[0].TotalPoint.ShouldBe(2);
            
            userActivityInfo.Item2[1].ActivityId.ShouldBe(1);
            userActivityInfo.Item2[1].Address.ShouldBe("0x11");
            userActivityInfo.Item2[1].TotalPoint.ShouldBe(10);
            
            
            var ranking = await _rankingListSnapshotRepository.GetListAsync();
            ranking.Item2.Count.ShouldBe(1);
            ranking.Item2[0].ActivityId.ShouldBe(1);
            ranking.Item2[0].NumOfJoin.ShouldBe(2);
            ranking.Item2[0].RankingList.Count.ShouldBe(2);
            ranking.Item2[0].RankingList[0].Address.ShouldBe("0x11");
            ranking.Item2[0].RankingList[0].TotalPoint.ShouldBe(10);
            ranking.Item2[0].RankingList[1].Address.ShouldBe("0x10");
            ranking.Item2[0].RankingList[1].TotalPoint.ShouldBe(2);
        }

        [Fact]
        public async Task GetRankingListTests()
        {
            await SwapTest();
            var myRankingDto = await _activityAppService.GetMyRankingAsync(new GetMyRankingInput()
            {
                ActivityId = 1,
                Address = "0x10"
            });
            myRankingDto.Ranking.ShouldBe(2);
            myRankingDto.TotalPoint.ShouldBe(2);

            var rankingList = await _activityAppService.GetRankingListAsync(new ActivityBaseDto()
            {
                ActivityId = 1
            });
            rankingList.Items.Count.ShouldBe(2);
            rankingList.Items[0].Ranking.ShouldBe(1);
            rankingList.Items[0].TotalPoint.ShouldBe(10);
            rankingList.Items[0].Address.ShouldBe("0x11");
            rankingList.Items[0].NewStatus.ShouldBe(1);
            rankingList.Items[0].RankingChange1H.ShouldBe(0);
            rankingList.Items[1].Ranking.ShouldBe(2);
            rankingList.Items[1].TotalPoint.ShouldBe(2);
            rankingList.Items[1].Address.ShouldBe("0x10");
            rankingList.Items[1].NewStatus.ShouldBe(1);
            rankingList.Items[1].RankingChange1H.ShouldBe(0);
        }
    }
}