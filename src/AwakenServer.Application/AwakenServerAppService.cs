using AwakenServer.Localization;
using Volo.Abp.Application.Services;

namespace AwakenServer
{
    /* Inherit your application services from this class.
     */
    public abstract class AwakenServerAppService : ApplicationService
    {
        protected AwakenServerAppService()
        {
            LocalizationResource = typeof(AwakenServerResource);
        }
    }
}
