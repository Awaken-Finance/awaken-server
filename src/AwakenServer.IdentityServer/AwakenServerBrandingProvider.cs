using Volo.Abp.Ui.Branding;
using Volo.Abp.DependencyInjection;

namespace AwakenServer
{
    [Dependency(ReplaceServices = true)]
    public class AwakenServerBrandingProvider : DefaultBrandingProvider
    {
        public override string AppName => "AwakenServer";
    }
}
