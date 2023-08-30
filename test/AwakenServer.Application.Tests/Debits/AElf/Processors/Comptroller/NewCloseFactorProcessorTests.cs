using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.TestBase;
using Awaken.Contracts.Controller;
using Shouldly;
using Xunit;

namespace AwakenServer.Debits.AElf.Tests
{

    public partial class DebitAElfAppServiceTests
    {
        [Fact(Skip = "no need")]
        public async Task NewCloseFactor_Should_Modify_CompController()
        {
            var newCloseFactorMantissa = "12345";
            await NewCloseFactorAsync(newCloseFactorMantissa);
            var (_, compControllers) = await _esCompRepository.GetListAsync();
            var compController = compControllers.First();
            compController.CloseFactorMantissa.ShouldBe(newCloseFactorMantissa);
            compController.DividendToken.Id.ShouldNotBe(Guid.Empty);
        }
        
        private async Task NewCloseFactorAsync(string newCloseFactorMantissa)
        {
            var newCloseFactorProcessor = GetRequiredService<IEventHandlerTestProcessor<CloseFactorChanged>>();
            await newCloseFactorProcessor.HandleEventAsync(new CloseFactorChanged
            {
                OldCloseFactor = 0,
                NewCloseFactor = long.Parse(newCloseFactorMantissa)
            }, GetDefaultEventContext(CompController.ControllerAddress));
        }
    }
}