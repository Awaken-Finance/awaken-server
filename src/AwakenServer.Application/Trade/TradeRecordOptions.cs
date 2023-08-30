using System;
using System.Collections.Generic;

namespace AwakenServer.Trade
{
    public class TradeRecordOptions
    {
        public const string BlockHeightSetPrefix = "TradeRecord:BlockHeightSet";
        public const string TransactionHashSetPrefix = "TradeRecord:TransactionHashSet";
        public const string TransactionHashPrefix = "TradeRecord:TransactionHash";
        public const int QueryOnceLimit = 1000;
        public const int BlockHeightLimit = 100;
        public const int DefaultNextNodeIndex = -1;
        public const int RetryLimit = 2;
        public const int TransactionHashExpirationTime = 180;
        public const int RevertTimePeriod = 75000;
    }
    
    public class BlockHeightSetDto
    {
        public HashSet<long> BlockHeight { get; set; } = new HashSet<long>();
        public int NextNode { get; set; } = TradeRecordOptions.DefaultNextNodeIndex;
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