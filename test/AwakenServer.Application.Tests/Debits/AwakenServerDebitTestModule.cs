using AElf.AElfNode.EventHandler.TestBase;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler;
using AwakenServer.Debits.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp.Modularity;

namespace AwakenServer.Debits
{
    [DependsOn(
        typeof(AwakenServerApplicationTestModule),
        typeof(AwakenServerContractEventHandlerCoreModule),
        typeof(AElfEthereumEventHandlerTestBaseModule),
        typeof(AElfEventHandlerTestBaseModule)
    )]
    public class AwakenServerDebitTestModule : AbpModule
    {
        public override void ConfigureServices(ServiceConfigurationContext context)
        {
            var services = context.Services;
            MockPriceAppService(services);
        }
        
        private void MockPriceAppService(IServiceCollection services)
        {
            services.RemoveAll<IDebitAppPriceService>();
            services.AddTransient(typeof(IDebitAppPriceService), typeof(MockDebitAppPriceService));

        }
    }
}