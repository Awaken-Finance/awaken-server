using AwakenServer.Localization;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers
{
    /* Inherit your controllers from this class.
     */
    public abstract class AwakenServerController : AbpController
    {
        protected AwakenServerController()
        {
            LocalizationResource = typeof(AwakenServerResource);
        }
    }
}