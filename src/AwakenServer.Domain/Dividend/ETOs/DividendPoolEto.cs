using AwakenServer.Dividend.Entities.Es;
using Volo.Abp.EventBus;

namespace AwakenServer.Dividend.ETOs
{
    [EventName("Dividend.DividendPoolChanged")]
    public class DividendPoolEto : DividendPool
    {
    }
}