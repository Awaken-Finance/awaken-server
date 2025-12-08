using System;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Volo.Abp;
using Volo.Abp.EventBus.Distributed;
using Volo.Abp.Modularity;
using Volo.Abp.Threading;

namespace AwakenServer.Chains;

[DependsOn(
    typeof(AwakenServerApplicationTestModule)
)]
public class ChainTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddSingleton<TestEnvironmentProvider>();
        context.Services.AddSingleton<IDistributedEventBus, LocalDistributedEventBus>();
        context.Services.AddSingleton<IBlockchainClientProviderFactory, MockDefaultBlockchainClientProviderFactory>();
        context.Services.RemoveAll<IBlockchainClientProvider>();
        context.Services.AddSingleton<IBlockchainClientProvider, MockAElfClientProvider>(); 
        context.Services.AddSingleton<IBlockchainClientProvider, MockDefaultClientProvider>();
        context.Services.AddSingleton<IBlockchainClientProvider, MockEthereumClientProvider>();
        
        context.Services.Configure<ChainsInitOptions>(o =>
        {
            o.Chains = new List<ChainDto>
            {
                new ChainDto
                {
                    Id = "AELF",
                    Name = "AELF",
                    BlocksPerDay = 5760,
                    LatestBlockHeight = 0,
                    AElfChainId = 1
                },
                new ChainDto
                {
                    Id = "tDVV",
                    Name = "tDVV",
                    BlocksPerDay = 5760,
                    LatestBlockHeight = 0,
                    AElfChainId = 1
                }
            };
        });
    }

    public override void OnApplicationInitialization(ApplicationInitializationContext context)
    {
        var chainTestHelper = context.ServiceProvider.GetRequiredService<ChainTestHelper>();
        var environmentProvider = context.ServiceProvider.GetRequiredService<TestEnvironmentProvider>();
        
        var chainEth = AsyncHelper.RunSync(async ()=> await chainTestHelper.CreateAsync(new ChainCreateDto
        {
            Id = "Ethereum",
            Name = "Ethereum",
            AElfChainId = 1,
        }));
        environmentProvider.EthChainId = chainEth.Id;
        environmentProvider.EthChainName = chainEth.Name;
        
        var chainElf = AsyncHelper.RunSync(async ()=> await chainTestHelper.CreateAsync(new ChainCreateDto
        {
            Id = "AElfMock",
            Name = "AElfMock",
            AElfChainId = 2
        }));
        environmentProvider.AElfChainId = chainElf.Id;
        environmentProvider.AElfChainName = chainElf.Name;
    }
}