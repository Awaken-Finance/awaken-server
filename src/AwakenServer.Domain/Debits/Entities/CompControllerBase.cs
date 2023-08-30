using System;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Debits.Entities
{
    public class CompControllerBase : MultiChainEntity<Guid>
    {
        [Keyword] public override Guid Id { get; set; }
        public string ControllerAddress { get; set; }
    }

    public class EditableCompController : CompControllerBase
    {
        public string CloseFactorMantissa { get; set; }
        // public string LiquidationIncentive { get; set; }
        // public string CompInitialIndex { get; set; }
    }
}