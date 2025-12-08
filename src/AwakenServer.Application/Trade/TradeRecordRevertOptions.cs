using System;
using System.Collections.Generic;

namespace AwakenServer.Trade
{
    public class BlockHeightSetDto
    {
        public const int DefaultNextNodeIndex = -1;
        public HashSet<long> BlockHeight { get; set; } = new HashSet<long>();
        public int NextNode { get; set; } = DefaultNextNodeIndex;
    }

    public class TransactionHashSetDto
    {
        public HashSet<string> TransactionHash { get; set; } = new HashSet<string>();
    }

    public class TransactionHashDto
    {
        public string Address { get; set; }
        public Guid TradePairId { get; set; }
        public long BlockHeight { get; set; }
        public string TransactionHash { get; set; }
        public int Retry { get; set; }
    }
}