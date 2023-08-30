using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.Types;
using Awaken.Contracts.AToken;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.AElf.Tests
{
    public partial class DebitAElfAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task ReservesAdded_With_Confirmed_Should_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            await MarketEnteredAsync(CProjectToken.Address, DebitTestData.Gui);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            ;
            cToken.TotalUnderlyingAssetReserveAmount.ShouldBe(DebitTestData.ZeroBalance);

            var benefactor = DebitTestData.Gui;
            var addAmount = "1000";
            var newTotal = "10000";
            await ReservesAddedAsync(CProjectToken.Address, benefactor, addAmount, newTotal);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken = cTokens.Single(x => x.Id == CProjectToken.Id);
            ;
            cToken.TotalUnderlyingAssetReserveAmount.ShouldBe(newTotal);
        }

        private async Task ReservesAddedAsync(string cTokenVirtualAddress, Address benefactor, string addAmount,
            string newTotalReserves)
        {
            var reservesAddedProcessor = GetRequiredService<IEventHandlerTestProcessor<ReservesAdded>>();
            await reservesAddedProcessor.HandleEventAsync(new ReservesAdded
            {
                AToken = Address.FromBase58(cTokenVirtualAddress),
                Sender = benefactor,
                AddAmount = long.Parse(addAmount),
                TotalReserves = long.Parse(newTotalReserves),
            }, GetDefaultEventContext(DebitTestData.CTokenContractAddressStr));
        }
    }
}