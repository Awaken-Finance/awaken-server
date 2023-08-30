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
        public async Task NewCollateralFactor_Without_Confirmed_Should_Not_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            var newCollateralFactorMantissa = "1234";
            await NewCollateralFactorAsync(CProjectToken.Address, CProjectToken.CollateralFactorMantissa,
                newCollateralFactorMantissa, ContractEventStatus.Unconfirmed);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);
            cToken.CollateralFactorMantissa.ShouldBe(CProjectToken.CollateralFactorMantissa);
        }


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
            string newCollateralFactorMantissa,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var newCollateralFactorProcessor = GetRequiredService<IEventHandlerTestProcessor<NewCollateralFactor>>();
            await newCollateralFactorProcessor.HandleEventAsync(new NewCollateralFactor
            {
                CToken = cToken,
                OldCollateralFactorMantissa = BigInteger.Parse(oldCloseFactorMantissa),
                NewCollateralFactorMantissa = BigInteger.Parse(newCollateralFactorMantissa)
            }, GetDefaultEventContext(CompController.ControllerAddress, confirmStatus: confirmStatus));
        }
    }
}