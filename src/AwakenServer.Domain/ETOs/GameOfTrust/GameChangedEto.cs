using AutoMapper;
using Volo.Abp.EventBus;

namespace AwakenServer.ETOs.GameOfTrust
{   
    [AutoMap(typeof(Entities.GameOfTrust.Es.GameOfTrust))]
    [EventName("GameOfTrust.GameChanged")]
    public class GameChangedEto : Entities.GameOfTrust.Es.GameOfTrust
    {
    }
}