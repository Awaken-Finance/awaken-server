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
        public async Task NewRewardProcessor_Should_Modify_Dividend_Token()
        {
            var dividendTokenSymbol = DividendTestConstants.UsdtTokenSymbol;
            var token = new AddToken
            {
                TokenSymbol = dividendTokenSymbol,
                Index = 0
            };
            await AddTokenAsync(token);

            var totalAmount = 100000;
            var startBlock = 101;
            var endBlock = 200;
            var rewardPerBlocks = totalAmount / (endBlock - startBlock + 1);
            var newReward = new NewReward
            {
                Token = dividendTokenSymbol,
                PerBlocks = rewardPerBlocks,
                StartBlock = startBlock,
                EndBlock = endBlock
            };
            await NewRewardAsync(newReward);
            var tokens = (await _esDividendTokenRepository.GetListAsync()).Item2;
            tokens.Count.ShouldBe(1);
            var targetToken = tokens[0];
            targetToken.StartBlock.ShouldBe(startBlock);
            targetToken.EndBlock.ShouldBe(endBlock);
            targetToken.AmountPerBlock.ShouldBe(rewardPerBlocks.ToString());
        }
        
        private async Task NewRewardAsync(NewReward newReward, EventContext eventContext = null)
        {
            eventContext ??= GetEventContext(AElfChainId, eventAddress: DividendTestConstants.DividendTokenAddress);
            var processor = GetRequiredService<IEventHandlerTestProcessor<NewReward>>();
            await processor.HandleEventAsync(newReward, eventContext);
        }
    }
}