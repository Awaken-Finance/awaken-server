using System;
using System.Linq;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Debit.Ethereum.DTOs.CToken;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.Ethereum.Tests
{
    public partial class DebitEthereumAppServiceTests
    {
        
        [Fact(Skip = "no need")]
        public async Task NewReserveFactor_Without_Confirmed_Should_Not_Modify_CToken()
        {
            await MarketListAsync(CProjectToken.Address, CompController.ControllerAddress);
            var (_, cTokens) = await _esCTokenRepository.GetListAsync();
            var cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.ReserveFactorMantissa.ShouldBe(DebitTestData.ReserveFactorMantissa);

            var oldReserveFactorMantissa = DebitTestData.ReserveFactorMantissa;
            var newReserveFactorMantissa = "4321";
            await NewReserveFactorAsync(CProjectToken.Address, oldReserveFactorMantissa, newReserveFactorMantissa, ContractEventStatus.Unconfirmed);
            (_, cTokens) = await _esCTokenRepository.GetListAsync();
            cToken =cTokens.Single(x => x.Id == CProjectToken.Id);;
            cToken.ReserveFactorMantissa.ShouldBe(oldReserveFactorMantissa);
        }
        
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
        
        private async Task NewReserveFactorAsync(string cTokenAddress, string oldReserveFactorMantissa,
            string newReserveFactorMantissa,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var newReserveFactor = GetRequiredService<IEventHandlerTestProcessor<NewReserveFactor>>();
            await newReserveFactor.HandleEventAsync(new NewReserveFactor
            {
                OldReserveFactorMantissa = BigInteger.Parse(oldReserveFactorMantissa),
                NewReserveFactorMantissa = BigInteger.Parse(newReserveFactorMantissa)
            }, GetDefaultEventContext(cTokenAddress, confirmStatus: confirmStatus));
        }
    }
}