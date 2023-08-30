using Volo.Abp.EventBus;

namespace AwakenServer.Dividend.ETOs
{
    [EventName("Dividend.DividendChanged")]
    public class DividendEto : Entities.Dividend
    {
    }
}