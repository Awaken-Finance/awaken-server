using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Types;
using Awaken.Contracts.Controller;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.AElf.Tests
{
    public partial class DebitAElfAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task MarketListed_Should_Contain_CToken()
        {
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cTokens.Count(x => x.IsList).ShouldBe(0);
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.IsList.ShouldBeTrue();
        }
        
        private async Task MarketListAsync(string cTokenAddress, string contractAddress)
        {
            var marketListedProcessor = GetRequiredService<IEventHandlerTestProcessor<MarketListed>>();
            await marketListedProcessor.HandleEventAsync(new MarketListed
            {
                AToken = Address.FromBase58(cTokenAddress)
            }, GetDefaultEventContext(contractAddress));
        }

        private EventContext GetDefaultEventContext(string contractAddress,
            string txHash = null, long blockTime = 0, string status = "Mined")
        {
            var timestamp = blockTime > 0 ? DateTimeHelper.FromUnixTimeMilliseconds(blockTime) : DateTime.UtcNow;
            return new EventContext
            {
                Status = status,
                ChainId = DefaultChain.AElfChainId,
                EventAddress = contractAddress,
                TransactionId = txHash,
                BlockTime = timestamp
            };
        }
    }
}