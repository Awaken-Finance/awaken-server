using System;
using System.Linq;
using System.Threading.Tasks;
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
        public async Task DistributedBorrower_Should_Modify_Comp()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeBorrowComp.ShouldBe(DebitTestData.ZeroBalance);

            var compDelta = "1111";
            await DistributedBorrowerCompAsync(CProjectToken.Address, DebitTestData.Ming.ToBase58(), compDelta, "1");
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeBorrowComp.ShouldBe(compDelta);
            var (_, users) = await _esUserInfoRepository.GetListAsync();
            var user = users.Single(x => x.User == DebitTestData.Ming.ToBase58());
            user.AccumulativeBorrowComp.ShouldBe(compDelta);
        }

        private async Task DistributedBorrowerCompAsync(string cTokenAddress, string borrower, string compDelta,
            string compIndex)
        {
            var compBorrowUpdateProcessor = GetRequiredService<IEventHandlerTestProcessor<DistributedBorrowerPlatformToken>>();
            await compBorrowUpdateProcessor.HandleEventAsync(new DistributedBorrowerPlatformToken
            {
                AToken = Address.FromBase58(cTokenAddress),
                Borrower = Address.FromBase58(borrower),
                PlatformTokenDelta = long.Parse(compDelta),
                PlatformTokenBorrowIndex = long.Parse(compIndex)
            }, GetDefaultEventContext(CompController.ControllerAddress));
        }
    }
}