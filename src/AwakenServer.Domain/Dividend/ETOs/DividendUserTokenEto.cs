using AwakenServer.Dividend.Entities.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.Dividend.ETOs
{
    [EventName("Dividend.DividendUserTokenChanged")]
    public class DividendUserTokenEto : DividendUserToken
    {
    }
}