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
        public async Task SetPoolProcessor_Should_Modify_Dividend_And_Pool_Weight()
        {
            var beforeWeight = 1;
            var pid = 0;
            var pool = new AddPool
            {
                Token = DividendTestConstants.ElfTokenSymbol,
                AllocPoint = beforeWeight,
                LastRewardBlock = 100,
                Pid = pid
            };
            await AddPoolAsync(pool);
            var dividendBeforeSetting = (await _esDividendRepository.GetListAsync()).Item2.First();

            var afterWeight = 100;
            var setPool = new SetPool
            {
                Pid = pid,
                AllocationPoint = afterWeight
            };
            await SetPoolAsync(setPool);
            var dividendAfterSetting = (await _esDividendRepository.GetListAsync()).Item2.First();
            var poolSetting = (await _esPoolRepository.GetListAsync()).Item2.First();
            poolSetting.Weight.ShouldBe(afterWeight);
            (dividendAfterSetting.TotalWeight - dividendBeforeSetting.TotalWeight).ShouldBe(afterWeight - beforeWeight);
        }

        private async Task SetPoolAsync(SetPool setPool, EventContext eventContext = null)
        {
            eventContext ??= GetEventContext(AElfChainId, eventAddress: DividendTestConstants.DividendTokenAddress);
            var processor = GetRequiredService<IEventHandlerTestProcessor<SetPool>>();
            await processor.HandleEventAsync(setPool, eventContext);
        }
    }
}