using AwakenServer.Dividend.Entities.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.Dividend.ETOs
{
    [EventName("Dividend.DividendPoolTokenChanged")]
    public class DividendPoolTokenEto : DividendPoolToken
    {
    }
}