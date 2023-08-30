using System.Collections.Generic;

namespace AwakenServer.ContractEventHandler.Trade
{
    public class FactoryContractOptions
    {
        public Dictionary<string, double> Contracts { get; set; } = new();
    }
}