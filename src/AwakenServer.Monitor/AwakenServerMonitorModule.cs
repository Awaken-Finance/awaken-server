using Microsoft.Extensions.DependencyInjection;
using Volo.Abp.Modularity;

namespace AwakenServer.Monitor;

public class AwakenServerMonitorModule : AbpModule
{
    public override void ConfigureServices(ServiceConfigurationContext context)
    {
        Configure<IndicatorOptions>(context.Services.GetConfiguration().GetSection("Indicator"));
    }
}