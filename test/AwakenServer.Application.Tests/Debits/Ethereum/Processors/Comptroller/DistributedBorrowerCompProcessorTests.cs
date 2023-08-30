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
        public async Task DistributedBorrower_Without_Confirmed_Should_Not_Modify_Comp()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming);
            var (_, users) = await _esUserInfoRepository.GetListAsync();
            var user = users.Single(x => x.User == DebitTestData.Ming);
            user.AccumulativeBorrowComp.ShouldBe(DebitTestData.ZeroBalance);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeBorrowComp.ShouldBe(DebitTestData.ZeroBalance);

            await DistributedBorrowerCompAsync(CProjectToken.Address, DebitTestData.Ming, "10000", "1",
                ContractEventStatus.Unconfirmed);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeBorrowComp.ShouldBe(DebitTestData.ZeroBalance);
            (_, users) = await _esUserInfoRepository.GetListAsync();
            user = users.Single(x => x.User == DebitTestData.Ming);
            user.AccumulativeBorrowComp.ShouldBe(DebitTestData.ZeroBalance);
        }

        [Fact(Skip = "no need")]
        public async Task DistributedBorrower_Should_Modify_Comp()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Ming);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeBorrowComp.ShouldBe(DebitTestData.ZeroBalance);

            var compDelta = "1111";
            await DistributedBorrowerCompAsync(CProjectToken.Address, DebitTestData.Ming, compDelta, "1");
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.AccumulativeBorrowComp.ShouldBe(compDelta);
            var (_, users) = await _esUserInfoRepository.GetListAsync();
            var user = users.Single(x => x.User == DebitTestData.Ming);
            user.AccumulativeBorrowComp.ShouldBe(compDelta);
        }

        private async Task DistributedBorrowerCompAsync(string cTokenAddress, string borrower, string compDelta,
            string compIndex,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var compBorrowUpdateProcessor = GetRequiredService<IEventHandlerTestProcessor<DistributedBorrowerComp>>();
            await compBorrowUpdateProcessor.HandleEventAsync(new DistributedBorrowerComp
            {
                CToken = cTokenAddress,
                Borrower = borrower,
                CompDelta = BigInteger.Parse(compDelta),
                CompBorrowIndex = BigInteger.Parse(compIndex)
            }, GetDefaultEventContext(CompController.ControllerAddress, confirmStatus: confirmStatus));
        }
    }
}