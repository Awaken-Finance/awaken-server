using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.Indexing.Elasticsearch;
using AwakenServer.ContractEventHandler.Providers;
using AwakenServer.Farm;
using AwakenServer.Farms.Entities.Es;
using AwakenServer.Tokens;
using Shouldly;
using Volo.Abp.Validation;
using Xunit;

namespace AwakenServer.Farms.Ethereum.Tests
{
    public partial class FarmAppServiceTests : AwakenServerFarmApplicationTestBase
    {
        private readonly IFarmAppService _farmAppService;
        private readonly IFarmStatisticAppService _farmStatisticAppService;
        private readonly INESTReaderRepository<FarmPool, Guid> _esPoolRepository;
        private readonly INESTReaderRepository<Entities.Es.Farm, Guid> _esFarmRepository;
        private readonly INESTReaderRepository<FarmUserInfo, Guid> _esFarmUserInfoRepository;
        private readonly INESTReaderRepository<FarmRecord, Guid> _esRecordRepository;

        public FarmAppServiceTests()
        {
            _farmAppService = GetRequiredService<IFarmAppService>();
            _farmStatisticAppService = GetRequiredService<IFarmStatisticAppService>();
            _esPoolRepository = GetRequiredService<INESTReaderRepository<FarmPool, Guid>>();
            _esFarmRepository = GetRequiredService<INESTReaderRepository<Entities.Es.Farm, Guid>>();
            _esFarmUserInfoRepository = GetRequiredService<INESTReaderRepository<FarmUserInfo, Guid>>();
            _esRecordRepository = GetRequiredService<INESTReaderRepository<FarmRecord, Guid>>();
        }

        [Fact(Skip = "no need")]
        public async Task After_Set_Data_Cache_Should_Contain_It()
        {
            var tokenAppService = GetRequiredService<ITokenAppService>();
            var token = await tokenAppService.GetAsync(new GetTokenInput
            {
                Symbol = FarmTestData.ProjectTokenSymbol,
            });
            token = await tokenAppService.GetAsync(token.Id);
            token.ShouldNotBeNull();
            
            /*var newKey = "Test";
            cacheProvider.SetCachedData(newKey, token, token.Id);
            token = await cacheProvider.GetOrSetCachedDataAsync(newKey,
                x => x.Symbol == FarmTestData.ProjectTokenSymbol);
            token.ShouldNotBeNull();*/
        }

        [Fact(Skip = "no need")]
        public async Task GetFarmList_Farm_Should_Contain_Initial_Farm()
        {
            await AddPoolAsync(FarmTestData.MassiveFarmAddress, 0, PoolType.Massive,
                FarmTestData.SwapTokenOneContractAddress, 1230, 1001);
            await AddPoolAsync(FarmTestData.GeneralFarmAddress, 0, PoolType.Massive,
                FarmTestData.SwapTokenOneContractAddress, 1230, 1001);
            var listResultDto = await _farmAppService.GetFarmListAsync(new GetFarmInput());
            var farms = listResultDto.Items;
            farms.Count.ShouldBe(2);
            var massiveFarm = farms.SingleOrDefault(x => x.FarmAddress == FarmTestData.MassiveFarmAddress);
            massiveFarm.ShouldNotBeNull();
            massiveFarm.ChainId.ShouldNotBeNull();
            massiveFarm.FarmType.ShouldBe(FarmType.Massive);
            massiveFarm.Id.ShouldNotBe(Guid.Empty);
            var massiveFarmId = massiveFarm.Id;
            var chainId = massiveFarm.ChainId;
            listResultDto = await _farmAppService.GetFarmListAsync(new GetFarmInput
            {
                FarmId = massiveFarmId
            });
            farms = listResultDto.Items;
            farms.Count.ShouldBe(1);
            farms[0].FarmAddress.ShouldBe(FarmTestData.MassiveFarmAddress);

            listResultDto = await _farmAppService.GetFarmListAsync(new GetFarmInput
            {
                ChainId = chainId
            });
            farms = listResultDto.Items;
            farms.Count.ShouldBe(2);
        }

        [Fact(Skip = "no need")]
        public async Task GetFarmPoolList_Should_Contain_Pools()
        {
            var farmAddress = FarmTestData.GeneralFarmAddress;
            var pid = 0;
            var poolType = PoolType.Normal;
            var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);
            
            var farmAddress2 = FarmTestData.MassiveFarmAddress;
            var pid2 = 0;
            var poolType2 = PoolType.Massive;
            var tokenAddress2 = FarmTestData.SwapTokenTwoContractAddress;
            var lastRewardBlock2 = 1230;
            var weight2 = 1000;
            await AddPoolAsync(farmAddress2, pid2, poolType2, tokenAddress2, lastRewardBlock2, weight2);
            var poolListResultDto = await _farmAppService.GetFarmPoolListAsync(new GetFarmPoolInput());
            var pools = poolListResultDto.Items;
            pools.Count.ShouldBe(2);

            var choosePoolId = pools.First().Id;
            poolListResultDto = await _farmAppService.GetFarmPoolListAsync(new GetFarmPoolInput
            {
                PoolId = choosePoolId
            });
            pools = poolListResultDto.Items;
            pools.Count.ShouldBe(1);
            
            var farmListResultDto = await _farmAppService.GetFarmListAsync(new GetFarmInput());
            var farm = farmListResultDto.Items.First();
            var chooseChainId = farm.ChainId;
            var chooseFarmId = farm.Id;
            
            poolListResultDto = await _farmAppService.GetFarmPoolListAsync(new GetFarmPoolInput
            {
                ChainId = chooseChainId
            });
            pools = poolListResultDto.Items;
            pools.Count.ShouldBe(2);
            
            poolListResultDto = await _farmAppService.GetFarmPoolListAsync(new GetFarmPoolInput
            {
                FarmId = chooseFarmId
            });
            pools = poolListResultDto.Items;
            pools.Count.ShouldBe(1);

            var pool = pools[0];
            pool.Id.ShouldNotBe(Guid.Empty);
            pool.ChainId.ShouldNotBeNull();
            pool.FarmId.ShouldNotBe(Guid.Empty);
            pool.SwapToken.Id.ShouldNotBe(Guid.Empty);
        }

        [Fact(Skip = "no need")]
        public async Task GetFarmPoolList_With_Update_Reward_Should_Contain_Reward()
        {
            var user1 = FarmTestData.Wei;
            var user2 = FarmTestData.Xin;
            await CreateRecordData(user1, user2);
        }

        [Fact(Skip = "no need")]
        public async Task GetFarmPoolList_With_User_Should_Contain_Right_Pool()
        {
            var user1 = FarmTestData.Wei;
            var user2 = FarmTestData.Yue;
            var notEnteredUser = FarmTestData.Xin;
            await CreateRecordData(user1, user2);
            var poolList = (await _farmAppService.GetFarmPoolListAsync(new GetFarmPoolInput
            {
                User = notEnteredUser
            })).Items;
            poolList.Count.ShouldBe(0);
            
            poolList = (await _farmAppService.GetFarmPoolListAsync(new GetFarmPoolInput
            {
                User = user1
            })).Items;
            var usersInfoList = (await _farmAppService.GetFarmUserInfoListAsync(new GetFarmUserInfoInput
            {
                User = user1
            })).Items;
            poolList.Count.ShouldBe(usersInfoList.Count);
            foreach (var pool in poolList)
            {
                var userInfo = usersInfoList.SingleOrDefault(x => x.PoolInfo.Id == pool.Id);
                userInfo.ShouldNotBeNull();
            }
        }
        
        [Fact(Skip = "no need")]
        public async Task GetFarmUserInfoList_With_PoolInfo_Should_Contain_Pools()
        {
            var user1 = FarmTestData.Wei;
            var user2 = FarmTestData.Yue;
            await CreateRecordData(user1, user2);
            var userListResultDto = await _farmAppService.GetFarmUserInfoListAsync(new GetFarmUserInfoInput
            {
                IsWithDetailPool = true
            });
            var users = userListResultDto.Items;
            users.Count.ShouldBe(4);
            users[0].PoolDetailInfo.ShouldNotBeNull();
            string.IsNullOrEmpty(users[0].PoolDetailInfo.TotalDepositAmount).ShouldBeFalse();
        }

        [Fact(Skip = "no need")]
        public async Task GetFarmUserInfoList_Should_Contain_Users()
        {
            var user1 = FarmTestData.Wei;
            var user2 = FarmTestData.Yue;
            await CreateRecordData(user1, user2);
            var userListResultDto = await _farmAppService.GetFarmUserInfoListAsync(new GetFarmUserInfoInput());
            var users = userListResultDto.Items;
            users.Count.ShouldBe(4);
            
            userListResultDto = await _farmAppService.GetFarmUserInfoListAsync(new GetFarmUserInfoInput
            {
                User = user1
            });
            users = userListResultDto.Items;
            users.Count.ShouldBe(2);
            users[0].SwapToken.Id.ShouldNotBe(Guid.Empty);
            users[0].Token1.Id.ShouldNotBe(Guid.Empty);
            users[0].Token2.Id.ShouldNotBe(Guid.Empty);

            var poolId = users[0].PoolInfo.Id;
            var farmListResultDto = await _farmAppService.GetFarmListAsync(new GetFarmInput());
            var farms = farmListResultDto.Items;

            var farmId = farms[0].Id;
            var chainId = farms[0].ChainId;
            userListResultDto = await _farmAppService.GetFarmUserInfoListAsync(new GetFarmUserInfoInput
            {
                ChainId = chainId
            });
            users = userListResultDto.Items;
            users.Count.ShouldBe(4);
            
            userListResultDto = await _farmAppService.GetFarmUserInfoListAsync(new GetFarmUserInfoInput
            {
                ChainId = chainId,
                User = user1
            });
            users = userListResultDto.Items;
            users.Count.ShouldBe(2);
            
            userListResultDto = await _farmAppService.GetFarmUserInfoListAsync(new GetFarmUserInfoInput
            {
                FarmId = farmId
            });
            users = userListResultDto.Items;
            users.Count.ShouldBe(2);
            
            userListResultDto = await _farmAppService.GetFarmUserInfoListAsync(new GetFarmUserInfoInput
            {
                FarmId = farmId,
                User = user2
            });
            users = userListResultDto.Items;
            users.Count.ShouldBe(1);
            
            userListResultDto = await _farmAppService.GetFarmUserInfoListAsync(new GetFarmUserInfoInput
            {
                PoolId = poolId
            });
            users = userListResultDto.Items;
            users.Count.ShouldBe(2);
            
            userListResultDto = await _farmAppService.GetFarmUserInfoListAsync(new GetFarmUserInfoInput
            {
                PoolId = poolId,
                User = user1
            });
            users = userListResultDto.Items;
            users.Count.ShouldBe(1);
        }

        [Fact(Skip = "no need")]
        public async Task GetFarmRecordList_Without_User_Should_Throw_Exception()
        {
            var ex = await Assert.ThrowsAsync<AbpValidationException>(async () =>
            {
                await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput());
            });
            ex.Message.ShouldContain("Method arguments are not valid");
        }

        [Fact(Skip = "no need")]
        public async Task GetFarmRecordList_With_ChainId_FarmId_TokenId_Should_Contain_Valid_Records()
        {
            var user1 = FarmTestData.Xin;
            var user2 = FarmTestData.Yue;
            await CreateRecordData(user1, user2);
            var recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1
            });
            recordListResultDto.TotalCount.ShouldBe(2);
            var records = recordListResultDto.Items;
            var chainId = records[0].FarmInfo.ChainId;
            var farmId = records[0].FarmInfo.Id;
            var tokenId = records[0].TokenInfo.Id;
            recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1
            });
            recordListResultDto.TotalCount.ShouldBe(2);
            recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1,
                ChainId = chainId
            });
            recordListResultDto.TotalCount.ShouldBe(2);
            
            recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1,
                ChainId = chainId,
                FarmId = farmId
            });
            recordListResultDto.TotalCount.ShouldBe(1);
            
            recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1,
                ChainId = chainId,
                TokenId = tokenId
            });
            recordListResultDto.TotalCount.ShouldBe(1);

            var record = recordListResultDto.Items.First();
            record.Id.ShouldNotBe(Guid.Empty);
            record.FarmInfo.Id.ShouldNotBe(Guid.Empty);
            record.PoolInfo.Id.ShouldNotBe(Guid.Empty);
            record.TokenInfo.Id.ShouldNotBe(Guid.Empty);
        }
        
        [Fact(Skip = "no need")]
        public async Task GetFarmRecordList_With_Skip_Should_Contain_Valid_Records()
        {
            var user1 = FarmTestData.Xin;
            var user2 = FarmTestData.Yue;
            await CreateRecordData(user1, user2);
            var recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1,
                SkipCount = 1,
                Size = 1
            });
            recordListResultDto.TotalCount.ShouldBe(2);
            recordListResultDto.Items.Count.ShouldBe(1);
        }
        
        [Fact(Skip = "no need")]
        public async Task GetFarmRecordList_With_Order_Should_Contain_Valid_Records()
        {
            var user1 = FarmTestData.Xin;
            var user2 = FarmTestData.Yue;
            await CreateRecordData(user1, user2);
            var recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1,
                IsAscend = true
            });
            var records = recordListResultDto.Items;
            var orderedRecords = records.OrderBy(x => x.Timestamp).ToList();
            for (int i = 0; i < records.Count; i++)
            {
                Assert.Equal(records[i], orderedRecords[i]);
            }
            
            recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1,
                IsAscend = false
            });
            records = recordListResultDto.Items;
            orderedRecords = records.OrderByDescending(x => x.Timestamp).ToList();
            for (int i = 0; i < records.Count; i++)
            {
                Assert.Equal(records[i], orderedRecords[i]);
            }
        }

        [Fact(Skip = "no need")]
        public async Task GetFarmRecordList_With_StartTime_EndTime_Should_Contain_Valid_Records()
        {
            var user1 = FarmTestData.Xin;
            var user2 = FarmTestData.Yue;
            var timestamp = DateTime.Now;
            var span = 500;
            var startTime = DateTimeHelper.ToUnixTimeMilliseconds(timestamp);
            var endTime = DateTimeHelper.ToUnixTimeMilliseconds(timestamp.AddSeconds(span));
            await CreateRecordData(user1, user2, timestamp, span);
            var recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1,
                StartTime = startTime - 1000,
                EndTime = startTime
            });
            recordListResultDto.TotalCount.ShouldBe(1);
            recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1,
                StartTime = startTime - 1000,
                EndTime = endTime
            });
            recordListResultDto.TotalCount.ShouldBe(2);
        }

        [Fact(Skip = "no need")]
        public async Task GetFarmRecordList_With_Behavior_Should_Contain_Valid_Records()
        {
            var user1 = FarmTestData.Xin;
            var user2 = FarmTestData.Yue;
            await CreateRecordData(user1, user2);
            var recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1,
                BehaviorType = BehaviorType.Deposit
            });
            recordListResultDto.TotalCount.ShouldBe(2);
            
            recordListResultDto = await _farmAppService.GetFarmRecordListAsync(new GetFarmRecordInput
            {
                User = user1,
                BehaviorType = BehaviorType.Withdraw
            });
            recordListResultDto.TotalCount.ShouldBe(0);
        }

        private async Task CreateRecordData(string user1, string user2, DateTime date = new (), long span = 100)
        {
            var farmAddress = FarmTestData.GeneralFarmAddress;
            var pid = 0;
            var poolType = PoolType.Normal;
            var tokenAddress = FarmTestData.SwapTokenOneContractAddress;
            var lastRewardBlock = 1230;
            var weight = 0;
            await AddPoolAsync(farmAddress, pid, poolType, tokenAddress, lastRewardBlock, weight);
            
            var farmAddress2 = FarmTestData.MassiveFarmAddress;
            var pid2 = 0;
            var poolType2 = PoolType.Massive;
            var tokenAddress2 = FarmTestData.SwapTokenTwoContractAddress;
            var lastRewardBlock2 = 1230;
            var weight2 = 1000;
            await AddPoolAsync(farmAddress2, pid2, poolType2, tokenAddress2, lastRewardBlock2, weight2);
            
            var depositAmount1 = 999999900;
            var depositTxHash1 = "depositone";
            await DepositAsync(user1, farmAddress, pid, depositTxHash1, date, depositAmount1);
            
            var depositAmount2 = 100000;
            var depositTxHash2 = "depositTwo";
            var depositTimestamp2 = date.AddSeconds(span);
            await DepositAsync(user1, farmAddress2, pid2, depositTxHash2, depositTimestamp2, depositAmount2);
            
            await DepositAsync(user2, farmAddress, pid, depositTxHash1, depositTimestamp2.AddSeconds(span), depositAmount1);
            await DepositAsync(user2, farmAddress2, pid2, depositTxHash1, depositTimestamp2.AddSeconds(span * 2), depositAmount1);
        }
    }
}