using System.Numerics;
using Nethereum.ABI.FunctionEncoding.Attributes;

namespace AwakenServer.ContractEventHandler.Price.Dtos
{
    [Event("Sync")]
    public class SyncEventDto : IEventDTO
    {
        [Parameter("uint112", "reserve0", 1, false)]
        public BigInteger Reserve0 { get; set; }

        [Parameter("uint112", "reserve1", 2, false)] 
        public BigInteger Reserve1 { get; set; }
    }
}