using System;
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
        public async Task AddTokenProcessor_Should_Add_Token()
        {
            var tokenSymbol = DividendTestConstants.ElfTokenSymbol;
            var token = new AddToken
            {
               TokenSymbol = tokenSymbol,
               Index = 0
            };
            await AddTokenAsync(token);
            var tokens = (await _esDividendTokenRepository.GetListAsync()).Item2;
            tokens.Count.ShouldBe(1);
            var targetToken = tokens[0];
            targetToken.Dividend.Id.ShouldBe(DividendId);
            targetToken.Token.Id.ShouldNotBe(Guid.Empty);
            targetToken.AmountPerBlock.ShouldBe("0");
        }
        
        private async Task AddTokenAsync(AddToken token, EventContext eventContext = null)
        {
            eventContext ??= GetEventContext(AElfChainId, eventAddress: DividendTestConstants.DividendTokenAddress);
            var processor = GetRequiredService<IEventHandlerTestProcessor<AddToken>>();
            await processor.HandleEventAsync(token, eventContext);
        }
    }
}