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
        public async Task NewCollateralFactor_With_Confirmed_Should_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            var newCollateralFactorMantissa = "1234";
            await NewCollateralFactorAsync(CProjectToken.Address, CProjectToken.CollateralFactorMantissa,
                newCollateralFactorMantissa);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.CollateralFactorMantissa.ShouldBe(newCollateralFactorMantissa);
        }


        private async Task NewCollateralFactorAsync(string cToken, string oldCloseFactorMantissa,
            string newCollateralFactorMantissa)
        {
            var newCollateralFactorProcessor = GetRequiredService<IEventHandlerTestProcessor<CollateralFactorChanged>>();
            await newCollateralFactorProcessor.HandleEventAsync(new CollateralFactorChanged
            {
                AToken = Address.FromBase58(cToken),
                OldCollateralFactor = long.Parse(oldCloseFactorMantissa),
                NewCollateralFactor = long.Parse(newCollateralFactorMantissa)
            }, GetDefaultEventContext(CompController.ControllerAddress));
        }
    }
}