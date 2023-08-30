using System;

namespace AwakenServer.Chains;

public class ChainTestBase : AwakenServerTestBase<ChainTestModule>
{
    protected string ChainId { get; }
    protected string ChainName { get; }
    
    protected ChainTestBase()
    {
        var environmentProvider = GetRequiredService<TestEnvironmentProvider>();
        ChainId = environmentProvider.AElfChainId;
        ChainName = environmentProvider.AElfChainName;
    }
}