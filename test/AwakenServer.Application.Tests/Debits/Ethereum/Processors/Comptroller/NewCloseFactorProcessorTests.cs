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
        public async Task NewCloseFactor_Without_Confirmed_Should_Not_Modify_CompController()
        {
            var (_, compControllers) = await _esCompRepository.GetListAsync();
            var compController = compControllers.First();
            compController.CloseFactorMantissa.ShouldBe(DebitTestData.CloseFactorMantissa);

            var newCloseFactorMantissa = "12345";
            await NewCloseFactorAsync(newCloseFactorMantissa, ContractEventStatus.Unconfirmed);
            (_, compControllers) = await _esCompRepository.GetListAsync();
            compController = compControllers.First();
            compController.CloseFactorMantissa.ShouldBe(DebitTestData.CloseFactorMantissa);
        }
        
        [Fact(Skip = "no need")]
        public async Task NewCloseFactor_Should_Modify_CompController()
        {
            var (_, compControllers) = await _esCompRepository.GetListAsync();
            var compController = compControllers.First();
            compController.CloseFactorMantissa.ShouldBe(DebitTestData.CloseFactorMantissa);

            var newCloseFactorMantissa = "12345";
            await NewCloseFactorAsync(newCloseFactorMantissa);
            (_, compControllers) = await _esCompRepository.GetListAsync();
            compController = compControllers.First();
            compController.CloseFactorMantissa.ShouldBe(newCloseFactorMantissa);
        }
        
        private async Task NewCloseFactorAsync(string newCloseFactorMantissa,
            ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            var newCloseFactorProcessor = GetRequiredService<IEventHandlerTestProcessor<NewCloseFactor>>();
            await newCloseFactorProcessor.HandleEventAsync(new NewCloseFactor
            {
                OldCloseFactorMantissa = BigInteger.Zero,
                NewCloseFactorMantissa = BigInteger.Parse(newCloseFactorMantissa)
            }, GetDefaultEventContext(CompController.ControllerAddress, confirmStatus: confirmStatus));
        }
    }
}