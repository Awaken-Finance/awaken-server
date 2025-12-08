using System;
using Orleans.TestingHost;

namespace AwakenServer.Grains.Tests;

public class AwakenServerGrainTestBase : AwakenServerTestBase<AwakenServerGrainTestModule>
{
    protected readonly TestCluster Cluster;

    protected string ChainId { get; }
    protected string ChainName { get; }
    protected Guid TradePairEthUsdtId { get; }
    
    protected string TradePairEthUsdtAddress { get; set; }

    protected Guid TokenUsdtId { get; }
    protected string TokenUsdtSymbol { get; }
    protected Guid TokenEthId { get; }
    protected string TokenEthSymbol { get; }
    
    public AwakenServerGrainTestBase()
    {
        Cluster = GetRequiredService<ClusterFixture>().Cluster;
        
        var environmentProvider = GetRequiredService<TestEnvironmentProvider>();
        ChainId = environmentProvider.EthChainId;
        ChainName = environmentProvider.EthChainName;
        TradePairEthUsdtId = environmentProvider.TradePairEthUsdtId;
        TradePairEthUsdtAddress = environmentProvider.TradePairEthUsdtAddress;
        TokenUsdtId = environmentProvider.TokenUsdtId;
        TokenUsdtSymbol = environmentProvider.TokenUsdtSymbol;
        TokenEthId = environmentProvider.TokenEthId;
        TokenEthSymbol = environmentProvider.TokenEthSymbol;
    }
}