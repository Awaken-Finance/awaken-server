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
        public async Task DistributedSupplier_Should_Modify_Comp()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeSupplyComp.ShouldBe(DebitTestData.ZeroBalance);

            var compDelta = "1111";
            await DistributedSupplierCompAsync(CProjectToken.Address, DebitTestData.Ming.ToBase58(), compDelta, "1");
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeSupplyComp.ShouldBe(compDelta);
            var (_, users) = await _esUserInfoRepository.GetListAsync();
            var user = users.Single(x => x.User == DebitTestData.Ming.ToBase58());
            user.AccumulativeSupplyComp.ShouldBe(compDelta);
        }

        private async Task DistributedSupplierCompAsync(string cTokenAddress, string supplier, string compDelta,
            string compIndex)
        {
            var compSupplyUpdateProcessor = GetRequiredService<IEventHandlerTestProcessor<DistributedSupplierPlatformToken>>();
            await compSupplyUpdateProcessor.HandleEventAsync(new DistributedSupplierPlatformToken
            {
                AToken = Address.FromBase58(cTokenAddress),
                Supplier = Address.FromBase58(supplier),
                PlatformTokenDelta = long.Parse(compDelta),
                PlatformTokenSupplyIndex = long.Parse(compIndex)
            }, GetDefaultEventContext(CompController.ControllerAddress));
        }
    }
}