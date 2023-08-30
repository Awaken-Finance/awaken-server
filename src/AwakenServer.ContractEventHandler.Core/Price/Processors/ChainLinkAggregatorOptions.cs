using System.Collections.Generic;

namespace AwakenServer.ContractEventHandler.Price.Processors
{
    public class ChainlinkAggregatorOptions
    {
        public Dictionary<string,ChainlinkAggregator> Aggregators { get; set; }
    }

    public class ChainlinkAggregator
    {
        public string Token { get; set; }
        
        public int Decimals { get; set; }
    }
}