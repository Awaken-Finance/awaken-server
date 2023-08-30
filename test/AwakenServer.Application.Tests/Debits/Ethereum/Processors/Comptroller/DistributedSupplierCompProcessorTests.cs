using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.Comptroller;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.Ethereum.Tests
{
    public partial class DebitEthereumAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task DistributedSupplier_Without_Confirmed_Should_Not_Modify_Comp()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming);
            var (_, users) = await _esUserInfoRepository.GetListAsync();
            var user = users.Single(x => x.User == DebitTestData.Ming);
            user.AccumulativeSupplyComp.ShouldBe(DebitTestData.ZeroBalance);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeSupplyComp.ShouldBe(DebitTestData.ZeroBalance);

            await DistributedSupplierCompAsync(CProjectToken.Address, DebitTestData.Ming, "10000", "1",
                ContractEventStatus.Unconfirmed);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeSupplyComp.ShouldBe(DebitTestData.ZeroBalance);
            (_, users) = await _esUserInfoRepository.GetListAsync();
            user = users.Single(x => x.User == DebitTestData.Ming);
            user.AccumulativeSupplyComp.ShouldBe(DebitTestData.ZeroBalance);
        }

        [Fact(Skip = "no need")]
        public async Task DistributedSupplier_Should_Modify_Comp()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeSupplyComp.ShouldBe(DebitTestData.ZeroBalance);

            var compDelta = "1111";
            await DistributedSupplierCompAsync(CProjectToken.Address, DebitTestData.Ming, compDelta, "1");
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeSupplyComp.ShouldBe(compDelta);
            var (_, users) = await _esUserInfoRepository.GetListAsync();
            var user = users.Single(x => x.User == DebitTestData.Ming);
            user.AccumulativeSupplyComp.ShouldBe(compDelta);
        }

        private async Task DistributedSupplierCompAsync(string cTokenAddress, string supplier, string compDelta,
            string compIndex,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var compSupplyUpdateProcessor = GetRequiredService<IEventHandlerTestProcessor<DistributedSupplierComp>>();
            await compSupplyUpdateProcessor.HandleEventAsync(new DistributedSupplierComp
            {
                CToken = cTokenAddress,
                Supplier = supplier,
                CompDelta = BigInteger.Parse(compDelta),
                CompSupplyIndex = BigInteger.Parse(compIndex)
            }, GetDefaultEventContext(CompController.ControllerAddress, confirmStatus: confirmStatus));
        }
    }
}