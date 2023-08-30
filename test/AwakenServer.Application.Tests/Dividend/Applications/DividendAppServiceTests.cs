using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AElf.Types;
using AwakenServer.Chains;
using AwakenServer.Dividend.DividendAppDto;
using AwakenServer.Dividend.Entities.Es;
using Awaken.Contracts.DividendPoolContract;
using Shouldly;
using Volo.Abp.Caching;
using Xunit;

namespace AwakenServer.Dividend
{
    public partial class DividendTests : DividendTestBase
    {
        private readonly INESTReaderRepository<Entities.Dividend, Guid> _esDividendRepository;
        private readonly INESTReaderRepository<DividendPool, Guid> _esPoolRepository;
        private readonly INESTReaderRepository<DividendToken, Guid> _esDividendTokenRepository;
        private readonly INESTReaderRepository<DividendUserPool, Guid> _esUserPoolRepository;
        private readonly INESTReaderRepository<DividendUserRecord, Guid> _esUserRecordRepository;
        private readonly INESTReaderRepository<DividendUserToken, Guid> _esUserTokenRepository;
        private readonly INESTReaderRepository<DividendPoolToken, Guid> _esPoolTokenRepository;
        private readonly IDividendAppService _dividendAppService;

        public DividendTests()
        {
            _esDividendRepository = GetRequiredService<INESTReaderRepository<Entities.Dividend, Guid>>();
            _esPoolRepository = GetRequiredService<INESTReaderRepository<DividendPool, Guid>>();
            _esDividendTokenRepository = GetRequiredService<INESTReaderRepository<DividendToken, Guid>>();
            _esUserPoolRepository = GetRequiredService<INESTReaderRepository<DividendUserPool, Guid>>();
            _esUserRecordRepository = GetRequiredService<INESTReaderRepository<DividendUserRecord, Guid>>();
            _esUserTokenRepository = GetRequiredService<INESTReaderRepository<DividendUserToken, Guid>>();
            _esPoolTokenRepository = GetRequiredService<INESTReaderRepository<DividendPoolToken, Guid>>();
            _dividendAppService = GetRequiredService<IDividendAppService>();
        }

        [Fact(Skip = "no need")]
        public async Task GetDividend_Should_Get_Right_Data()
        {
            var pool = new AddPool
            {
                Token = DividendTestConstants.ElfTokenSymbol,
                AllocPoint = 1,
                LastRewardBlock = 100,
                Pid = 0
            };
            await AddPoolAsync(pool);

            var dividends = await _dividendAppService.GetDividendAsync(new GetDividendInput());
            dividends.Items.Count.ShouldBe(1);
            var dividend = dividends.Items[0];
            dividend.Id.ShouldBe(DividendId);
            dividend.TotalWeight.ShouldBe(1);

            dividends = await _dividendAppService.GetDividendAsync(new GetDividendInput
            {
                ChainId = ChainId
            });
            dividends.Items.Count.ShouldBe(1);

            dividends = await _dividendAppService.GetDividendAsync(new GetDividendInput
            {
                DividendId = DividendId
            });
            dividends.Items.Count.ShouldBe(1);
        }
        
        [Fact(Skip = "no need")]
        public async Task GetDividend_Should_Get_Right_Dividend_Tokens()
        {
            var pool = new AddPool
            {
                Token = DividendTestConstants.ElfTokenSymbol,
                AllocPoint = 1,
                LastRewardBlock = 100,
                Pid = 0
            };
            await AddPoolAsync(pool);
            var tokenSymbol = DividendTestConstants.ProjectTokenSymbol;
            var token = new AddToken
            {
                TokenSymbol = tokenSymbol,
                Index = 0
            };
            await AddTokenAsync(token);
            tokenSymbol = DividendTestConstants.ElfTokenSymbol;
            token = new AddToken
            {
                TokenSymbol = tokenSymbol,
                Index = 0
            };
            await AddTokenAsync(token);
            var dividends = await _dividendAppService.GetDividendAsync(new GetDividendInput
            {
                DividendId = DividendId
            });
            dividends.Items.Count.ShouldBe(1);
            var dividendTokens = dividends.Items[0].DividendTokens;
            dividendTokens.Count.ShouldBe(2);
        }

        // current height: 200
        [Theory(Skip = "no need")]
        [InlineData(2000, 101, 200, 990)]
        [InlineData(2000, 101, 300, 495)]
        public async Task GetDividendPools_Should_Get_Right_Data(long totalAmount, long from, long to, long ret)
        {
            var poolOne = new AddPool
            {
                Token = DividendTestConstants.ElfTokenSymbol,
                AllocPoint = 50,
                Pid = 0
            };
            await AddPoolAsync(poolOne);

            var poolTwo = new AddPool
            {
                Token = DividendTestConstants.ElfTokenSymbol,
                AllocPoint = 50,
                Pid = 1
            };
            await AddPoolAsync(poolTwo);

            var tokenSymbol = DividendTestConstants.ProjectTokenSymbol;
            var token = new AddToken
            {
                TokenSymbol = tokenSymbol,
                Index = 0
            };
            await AddTokenAsync(token);
            await AddNewRewardAsync(tokenSymbol, totalAmount, from, to);

            var pools = await _dividendAppService.GetDividendPoolsAsync(new GetDividendPoolsInput
            {
                DividendId = DividendId
            });

            var targetPool = pools.Items.Single(x => x.Pid == 0);
            targetPool.DividendTokenInfo.Count.ShouldBe(1);
            var dividendTokenInfo = targetPool.DividendTokenInfo.First();
            long.Parse(dividendTokenInfo.ToDistributedDivided).ShouldBe(ret);
        }

        [Fact(Skip = "no need")]
        public async Task GetUserDividend_Should_Get_Right_Data()
        {
            var pid = 0;
            var pool = new AddPool
            {
                Token = DividendTestConstants.ElfTokenSymbol,
                AllocPoint = 50,
                Pid = 0
            };
            await AddPoolAsync(pool);

            var user = DividendTestConstants.Qi;
            var depositAmount = 99999;
            var deposit = new Deposit
            {
                User = user,
                Pid = pid,
                Amount = depositAmount
            };
            await DepositAsync(deposit);

            var harvestAmount = 10000;
            var dividendToken = DividendTestConstants.ProjectTokenSymbol;
            var harvest = new Harvest
            {
                Token = dividendToken,
                To = user,
                Pid = pid,
                Amount = harvestAmount
            };
            await HarvestAsync(harvest);
            var userInfo = await _dividendAppService.GetUserDividendAsync(new GetUserDividendInput
            {
                User = user.ToBase58(),
                DividendId = DividendId
            });
            userInfo.UserPools.Count.ShouldBe(1);
            var poolInfo = userInfo.UserPools[0];
            poolInfo.DepositAmount.ShouldBe(depositAmount.ToString());
            poolInfo.PoolBaseInfo.Id.ShouldNotBe(Guid.Empty);
            userInfo.UserTokens.Count.ShouldBe(1);
            var userToken = userInfo.UserTokens[0];
            userToken.AccumulativeDividend.ShouldBe(harvestAmount.ToString());
            userToken.DividendToken.Id.ShouldNotBe(Guid.Empty);
        }

        [Fact(Skip = "no need")]
        public async Task GetDividendUserRecords_With_Skip_Count_Should_Get_Right_Records()
        {
            var user = DividendTestConstants.Xue;
            await CreateMultipleDataAsync(user, 0);
            await CreateMultipleDataAsync(user, 1);
            var recordsBefore = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58()
            });
            recordsBefore.Items.Count.ShouldBe(6);

            var recordsAfter = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58(),
                SkipCount = 2,
                Size = 2
            });
            recordsAfter.Items.Count.ShouldBe(2);
            recordsAfter.Items[0].Id.ShouldBe(recordsBefore.Items[2].Id);
            recordsAfter.Items[1].Id.ShouldBe(recordsBefore.Items[3].Id);
        }
        
        [Fact(Skip = "no need")]
        public async Task GetDividendUserRecords_With_PoolId_Should_Get_Right_Records()
        {
            var user = DividendTestConstants.Xue;
            var targetPid = 0;
            await CreateMultipleDataAsync(user, targetPid);
            await CreateMultipleDataAsync(user, 1);
            var pools = await _dividendAppService.GetDividendPoolsAsync(new GetDividendPoolsInput
            {
                DividendId = DividendId
            });
            var pool = pools.Items.Single(x => x.Pid == targetPid);
            var records = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58(),
                PoolId = pool.Id
            });
            records.TotalCount.ShouldBe(3);
        }
        
        [Fact(Skip = "no need")]
        public async Task GetDividendUserRecords_With_TokenId_Should_Get_Right_Records()
        {
            var user = DividendTestConstants.Xue;
            var dividendTokenOne = DividendTestConstants.ProjectTokenSymbol;
            var dividendTokenTwo = DividendTestConstants.UsdtTokenSymbol;
            await CreateMultipleDataAsync(user, 0, dividendTokenOne);
            await CreateMultipleDataAsync(user, 1, dividendTokenTwo);
            var records = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58(),
            });
            var tokenInfo = records.Items.First(t =>
                t.DividendToken != null && t.DividendToken.Symbol == dividendTokenOne);
            records = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58(),
                TokenId = tokenInfo.DividendToken.Id
            });
            records.TotalCount.ShouldBe(1);
            records.Items[0].DividendToken.Symbol.ShouldBe(dividendTokenOne);
        }

        [Fact(Skip = "no need")]
        public async Task GetDividendUserRecords_With_BehaviorType_Should_Get_Right_Records()
        {
            var user = DividendTestConstants.Xue;
            await CreateMultipleDataAsync(user);
            var records = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58(),
                BehaviorType = BehaviorType.Deposit
            });
            records.TotalCount.ShouldBe(1);
            
            records = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58(),
                BehaviorType = BehaviorType.Harvest
            });
            records.TotalCount.ShouldBe(1);
            
            records = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58(),
                BehaviorType = BehaviorType.Withdraw
            });
            records.TotalCount.ShouldBe(1);
        }

        [Fact(Skip = "no need")]
        public async Task GetDividendUserRecords_With_Timestamp_Should_Get_Right_Records()
        {
            var user = DividendTestConstants.Xue;
            await CreateMultipleDataAsync(user);
            var records = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58()
            });
            records.TotalCount.ShouldBe(3);

            var startDateTime = DateTime.UtcNow.AddHours(1);
            var startTimestamp = DateTimeHelper.ToUnixTimeMilliseconds(startDateTime);
            records = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58(),
                TimestampMin = startTimestamp
            });
            records.TotalCount.ShouldBe(1);

            var endDateTime = startDateTime;
            var endTimestamp = DateTimeHelper.ToUnixTimeMilliseconds(endDateTime);
            records = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58(),
                TimestampMax = endTimestamp
            });
            records.TotalCount.ShouldBe(2);
            
            startDateTime = DateTime.UtcNow.AddHours(-1);
            startTimestamp = DateTimeHelper.ToUnixTimeMilliseconds(startDateTime);
            endDateTime = DateTime.UtcNow.AddHours(1);
            endTimestamp = DateTimeHelper.ToUnixTimeMilliseconds(endDateTime);
            records = await _dividendAppService.GetDividendUserRecordsAsync(new GetDividendUserRecordsInput
            {
                DividendId = DividendId,
                User = user.ToBase58(),
                TimestampMin = startTimestamp,
                TimestampMax = endTimestamp
            });
            records.TotalCount.ShouldBe(1);
            records.Items.First().BehaviorType.ShouldBe(BehaviorType.Withdraw);
        }

        [Theory(Skip = "no need")]
        [InlineData(149, "10000.00000000", "9800.00000000")]
        [InlineData(148, "9800.00000000", "9800.00000000")]
        [InlineData(100, "5000.00000000", "0.00000000")]
        // should update all pools when newReward
        public async Task GetDividendPoolStatistic_Should_Get_Right_Statistic_Info(long currentHeight,
            string expectationAccumulative, string expectationCurrentDividend)
        {
            var user = DividendTestConstants.Xue;
            var pid = 0;
            var dividendToken = DividendTestConstants.ProjectTokenSymbol;
            var token = new AddToken
            {
                TokenSymbol = dividendToken,
                Index = 0
            };
            await AddTokenAsync(token);
            await CreateMultipleDataAsync(user, pid, dividendToken);
            var totalAmount = GetAmountWithDecimal(10000, DividendTestConstants.ProjectTokenDecimal);
            var from = 101L;
            var to = 150L;
            await AddNewRewardAsync(dividendToken, totalAmount, from, to);
            var reward = GetAmountWithDecimal(5000, DividendTestConstants.ProjectTokenDecimal);
            var newLastRewardBlock = 124;
            var updateInfo = new UpdatePool
            {
                Token = dividendToken,
                AccPerShare = 100,
                Pid = pid,
                Reward = reward,
                BlockHeigh = newLastRewardBlock
            };
            await UpdatePoolAsync(updateInfo);

            var chainAppService = GetRequiredService<IChainAppService>();
            await chainAppService.UpdateAsync(new ChainUpdateDto
            {
                Id = ChainId,
                LatestBlockHeight = currentHeight,
            });

            var dividendStatisticInfo = await _dividendAppService.GetDividendPoolStatisticAsync(
                new GetDividendPoolStatisticInput
                {
                    DividendId = DividendId,
                });
            dividendStatisticInfo.TotalAccumulativeValue.ShouldBe(expectationAccumulative);
            dividendStatisticInfo.TotalCurrentDividendValue.ShouldBe(expectationCurrentDividend);
        }

        [Fact(Skip = "no need")]
        public async Task GetUserStatistic_Should_Get_Right_Data()
        {
            var user = DividendTestConstants.Xue;
            var pid = 0;
            var dividendToken = DividendTestConstants.ProjectTokenSymbol;
            var token = new AddToken
            {
                TokenSymbol = dividendToken,
                Index = 0
            };
            await AddTokenAsync(token);
            await CreateMultipleDataAsync(user, pid, dividendToken);
            var userStatisticInfo = await _dividendAppService.GetUserStatisticAsync(new GetUserStatisticInput
            {
                User = user.ToBase58(),
                DividendId = DividendId
            });
            userStatisticInfo.TotalAccumulativeValue.ShouldBe("0.00010000");
            userStatisticInfo.TotalDepositValue.ShouldBe("0.00009999");
        }

        private long GetAmountWithDecimal(long amount, int decimals)
        {
            return (long)Math.Pow(10, decimals) * amount;
        }

        private async Task CreateMultipleDataAsync(Address user, int pid = 0, string dividendToken = null)
        {
            var pool = new AddPool
            {
                Token = DividendTestConstants.ElfTokenSymbol,
                AllocPoint = 50,
                Pid = pid
            };
            await AddPoolAsync(pool);

            var depositAmount = 99999;
            var deposit = new Deposit
            {
                User = user,
                Pid = pid,
                Amount = depositAmount
            };
            var depositDate = DateTime.UtcNow.AddDays(-1);
            var depositEventContext = GetEventContext(AElfChainId,
                eventAddress: DividendTestConstants.DividendTokenAddress,
                blockTime: depositDate);
            await DepositAsync(deposit, depositEventContext);

            var withdrawAmount = 90000;
            var withdraw = new Withdraw
            {
                User = user,
                Pid = pid,
                Amount = withdrawAmount
            };
            var withdrawDate = DateTime.UtcNow;
            var withdrawEventContext = GetEventContext(AElfChainId,
                eventAddress: DividendTestConstants.DividendTokenAddress,
                blockTime: withdrawDate);
            await WithdrawAsync(withdraw, withdrawEventContext);

            var harvestAmount = 10000;
            dividendToken ??= DividendTestConstants.ProjectTokenSymbol;
            var harvest = new Harvest
            {
                Token = dividendToken,
                To = user,
                Pid = pid,
                Amount = harvestAmount
            };
            var harvestDate = DateTime.UtcNow.AddDays(1);
            var harvestEventContext = GetEventContext(AElfChainId,
                eventAddress: DividendTestConstants.DividendTokenAddress,
                blockTime: harvestDate);
            await HarvestAsync(harvest, harvestEventContext);
        }


        private async Task AddNewRewardAsync(string symbol, long totalAmount, long from, long to)
        {
            var rewardPerBlocks = totalAmount / (to - from + 1);
            var newReward = new NewReward
            {
                Token = symbol,
                PerBlocks = rewardPerBlocks,
                StartBlock = from,
                EndBlock = to
            };
            await NewRewardAsync(newReward);
        }
    }
}