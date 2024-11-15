using AwakenServer.Signature.Provider;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;
using Volo.Abp.Modularity;

namespace AwakenServer;

[DependsOn(
    typeof(AwakenServerApplicationContractsModule)
)]
public class AwakenServerSignatureTestModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        context.Services.AddLogging(configure => configure.AddConsole());
        var mockSignatureProvider = new Mock<ISignatureProvider>();
        mockSignatureProvider.Setup(o => o.SignTxMsg(It.IsAny<string>(), It.IsAny<string>()))
            .ReturnsAsync("123");
        context.Services.AddSingleton<ISignatureProvider>(mockSignatureProvider.Object);
    }
}