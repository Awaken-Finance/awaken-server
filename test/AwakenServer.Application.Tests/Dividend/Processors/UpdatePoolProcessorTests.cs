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
        public async Task UpdatePoolProcessor_Should_Modify_Pool_And_PoolToken()
        {
            var token = DividendTestConstants.ElfTokenSymbol;
            var weight = 1;
            var lastRewardBlock = 100;
            var pid = 0;
            var pool = new AddPool
            {
                Token = token,
                AllocPoint = weight,
                LastRewardBlock = lastRewardBlock,
                Pid = pid
            };
            await AddPoolAsync(pool);

            var reward = "100000";
            var newLastRewardBlock = 1000;
            var dividendToken = DividendTestConstants.ProjectTokenSymbol;
            var updateInfo = new UpdatePool
            {
                Token = dividendToken,
                AccPerShare = 100,
                Pid = pid,
                Reward = reward,
                BlockHeigh = newLastRewardBlock
            };
            await UpdatePoolAsync(updateInfo);

            var poolTokenInfo = (await _esPoolTokenRepository.GetListAsync()).Item2.First();
            poolTokenInfo.DividendToken.Symbol.ShouldBe(dividendToken);
            poolTokenInfo.AccumulativeDividend.ShouldBe(reward);
            poolTokenInfo.LastRewardBlock.ShouldBe(newLastRewardBlock);
        }

        [Fact(Skip = "no need")]
        public async Task UpdatePoolProcessor_Should_Get_Right_Accumulative_Token()
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

            var reward = 100000;
            var newLastRewardBlock = 1000;
            var dividendToken = DividendTestConstants.ProjectTokenSymbol;
            var updateInfo = new UpdatePool
            {
                Token = dividendToken,
                AccPerShare = 100,
                Pid = pid,
                Reward = reward,
                BlockHeigh = newLastRewardBlock
            };
            await UpdatePoolAsync(updateInfo);
            updateInfo.BlockHeigh = newLastRewardBlock * 2;
            await UpdatePoolAsync(updateInfo);
            var totalReward = (reward * 2).ToString();
            var poolTokenInfo = (await _esPoolTokenRepository.GetListAsync()).Item2.First();
            poolTokenInfo.AccumulativeDividend.ShouldBe(totalReward);
        }

        private async Task UpdatePoolAsync(UpdatePool updatePool, EventContext eventContext = null)
        {
            eventContext ??= GetEventContext(AElfChainId, eventAddress: DividendTestConstants.DividendTokenAddress);
            var processor = GetRequiredService<IEventHandlerTestProcessor<UpdatePool>>();
            await processor.HandleEventAsync(updatePool, eventContext);
        }
    }
}