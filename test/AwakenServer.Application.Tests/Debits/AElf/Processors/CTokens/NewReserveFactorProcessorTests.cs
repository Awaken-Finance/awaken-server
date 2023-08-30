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
        public async Task NewReserveFactor_With_Confirmed_Should_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.ReserveFactorMantissa.ShouldBe(DebitTestData.ReserveFactorMantissa);

            var oldReserveFactorMantissa = DebitTestData.ReserveFactorMantissa;
            var newReserveFactorMantissa = "4321";
            await NewReserveFactorAsync(CProjectToken.Address, oldReserveFactorMantissa, newReserveFactorMantissa);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.ReserveFactorMantissa.ShouldBe(newReserveFactorMantissa);
        }
        
        private async Task NewReserveFactorAsync(string cTokenVirtualAddress, string oldReserveFactorMantissa,
            string newReserveFactorMantissa)
        {
            var newReserveFactor = GetRequiredService<IEventHandlerTestProcessor<ReserveFactorChanged>>();
            await newReserveFactor.HandleEventAsync(new ReserveFactorChanged
            {
                AToken = Address.FromBase58(cTokenVirtualAddress),
                OldReserveFactor = long.Parse(oldReserveFactorMantissa),
                NewReserveFactor = long.Parse(newReserveFactorMantissa)
            }, GetDefaultEventContext(DebitTestData.CTokenContractAddressStr));
        }
    }
}