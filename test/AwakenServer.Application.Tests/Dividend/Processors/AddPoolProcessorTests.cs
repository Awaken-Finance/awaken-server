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
        public async Task AddPoolProcessor_Add_One_Pool_Should_Modify_Total_Weight()
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
            var dividends = (await _esDividendRepository.GetListAsync()).Item2;
            dividends.Count.ShouldBe(1);
            dividends[0].TotalWeight.ShouldBe(weight);
            var pools = (await _esPoolRepository.GetListAsync()).Item2;
            pools.Count.ShouldBe(1);
            var targetPool = pools.First();
            targetPool.Weight.ShouldBe(weight);
            targetPool.PoolToken.Id.ShouldNotBe(Guid.Empty);
            targetPool.PoolToken.Symbol.ShouldBe(token);
            targetPool.Dividend.Id.ShouldBe(DividendId);
        }

        private async Task AddPoolAsync(AddPool pool, EventContext eventContext = null)
        {
            eventContext ??= GetEventContext(AElfChainId, eventAddress: DividendTestConstants.DividendTokenAddress);
            var processor = GetRequiredService<IEventHandlerTestProcessor<AddPool>>();
            await processor.HandleEventAsync(pool, eventContext);
        }

        private EventContext GetEventContext(int chainId, long blockNumber = 0L,
            string status = "Mined",
            string txHash = null, string blockHash = null, DateTime blockTime = new (), string fromAddress = null,
            string toAddress = null,
            string eventAddress = null,
            string methodName = null, string returnValue = null)
        {
            return new EventContext
            {
                TransactionId = txHash,
                Status = status,
                ReturnValue = returnValue,
                ChainId = chainId,
                BlockNumber = blockNumber,
                MethodName = methodName,
                BlockTime = blockTime,
                FromAddress = fromAddress,
                ToAddress = toAddress,
                BlockHash = blockHash,
                EventAddress = eventAddress
            };
        }
    }
}