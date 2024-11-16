using AwakenServer.Signature.Options;
using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AwakenServer.Signature;

public class AwakenServerSignatureModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<SignatureServerOptions>(context.Services.GetConfiguration().GetSection("SignatureServer"));
    }
}