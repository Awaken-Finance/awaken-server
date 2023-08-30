using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.TestBase;
using Awaken.Contracts.DividendPoolContract;
using Shouldly;
using Xunit;

namespace AwakenServer.Dividend
{
    public partial class DividendTests
    {
        [Fact(Skip = "no need")]
        public async Task HarvestProcessor_Should_Modify_UserToken_And_Add_Record()
        {
            var pid = 0;
            var pool = new AddPool
            {
                Token = DividendTestConstants.ElfTokenSymbol,
                AllocPoint = 1,
                LastRewardBlock = 100,
                Pid = pid
            };
            await AddPoolAsync(pool);

            var user = DividendTestConstants.Ge;
            var harvestAmount = "10000";
            var dividendToken = DividendTestConstants.ProjectTokenSymbol;
            var harvest = new Harvest
            {
                Token = dividendToken,
                To = user,
                Pid = pid,
                Amount = harvestAmount
            };
            await HarvestAsync(harvest);

            var userToken = (await _esUserTokenRepository.GetListAsync()).Item2.First();
            userToken.DividendToken.Id.ShouldNotBe(Guid.Empty);
            userToken.DividendToken.Symbol.ShouldBe(dividendToken);
            userToken.PoolBaseInfo.Pid.ShouldBe(pid);
            userToken.PoolBaseInfo.Id.ShouldNotBe(Guid.Empty);
            userToken.AccumulativeDividend.ShouldBe(harvestAmount);

            var userRecordInfo = (await _esUserRecordRepository.GetListAsync()).Item2.First();
            userRecordInfo.Amount.ShouldBe(harvestAmount);
            userRecordInfo.User.ShouldBe(user.ToBase58());
            userRecordInfo.BehaviorType.ShouldBe(BehaviorType.Harvest);
            userRecordInfo.TransactionHash.ShouldNotBe(string.Empty);
            userRecordInfo.DividendToken.Symbol.ShouldBe(dividendToken);
        }

        [Fact(Skip = "no need")]
        public async Task HarvestProcessor_Should_Get_Right_Accumulative_Dividend()
        {
            var pid = 0;
            var pool = new AddPool
            {
                Token = DividendTestConstants.ElfTokenSymbol,
                AllocPoint = 1,
                LastRewardBlock = 100,
                Pid = pid
            };
            await AddPoolAsync(pool);

            var user = DividendTestConstants.Qi;
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
            await HarvestAsync(harvest);

            var totalHarvest = (harvestAmount * 2).ToString();
            var userToken = (await _esUserTokenRepository.GetListAsync()).Item2.First();
            userToken.AccumulativeDividend.ShouldBe(totalHarvest);
        }

        private async Task HarvestAsync(Harvest harvest, EventContext eventContext = null)
        {
            eventContext ??= GetEventContext(AElfChainId, eventAddress: DividendTestConstants.DividendTokenAddress);
            var processor = GetRequiredService<IEventHandlerTestProcessor<Harvest>>();
            await processor.HandleEventAsync(harvest, eventContext);
        }
    }
}