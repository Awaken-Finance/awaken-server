using System;
using AElf.AElfNode.EventHandler.TestBase;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler;
using AwakenServer.Trade;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using NSubstitute;
using Volo.Abp.Modularity;

namespace AwakenServer.Dividend
{
    [DependsOn(
        typeof(AwakenServerApplicationTestModule),
        typeof(AwakenServerContractEventHandlerCoreModule),
        typeof(AElfEventHandlerTestBaseModule)
    )]
    public class DividendTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var mockTokenPriceProvider = Substitute.For<ITokenPriceProvider>();
            mockTokenPriceProvider.GetTokenUSDPriceAsync(Arg.Any<string>(), Arg.Any<string>()).Returns(1);
            context.Services.RemoveAll<ITokenPriceProvider>();
            context.Services.AddSingleton(mockTokenPriceProvider);
            //context.Services.RemoveAll<IBlockchainClientProvider>();
            context.Services.AddSingleton<IBlockchainClientProvider, MockAElfClientProvider>();
        }
    }
}