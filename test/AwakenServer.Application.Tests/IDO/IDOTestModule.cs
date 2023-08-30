using AElf.AElfNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler;
using Volo.Abp.Modularity;

namespace AwakenServer.IDO
{
    [DependsOn(
        typeof(AwakenServerApplicationTestModule),
        typeof(AwakenServerContractEventHandlerCoreModule),
        typeof(AElfEventHandlerTestBaseModule)
    )]
    public class IDOTestModule: AbpModule
    {
        
    }
}