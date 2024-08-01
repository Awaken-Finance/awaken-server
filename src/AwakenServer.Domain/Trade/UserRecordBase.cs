using System;
using System.Collections.Generic;
using AwakenServer.Entities;
using Nest;

namespace AwakenServer.Trade
{
    public abstract class UserRecordBase : MultiChainEntity<Guid>
    {
        [Keyword]
        public string Address { get; set; }
        public DateTime Timestamp { get; set; }
        [Keyword]
        public string TransactionHash { get; set; }
        public double TransactionFee { get; set; }
        [Keyword]
        public string Token0Amount { get; set; }
        [Keyword]
        public string Token1Amount { get; set; }
        public double TotalPriceInUsd { get; set; }
        public double TotalFee { get; set; }
        [Keyword] 
        public string Channel { get; set; }
        
        [Keyword] public string Sender { get; set; }
        public long BlockHeight { get; set; } 
        public bool IsConfirmed { get; set; }
        public List<SwapRecord> SwapRecords { get; set; }
        [Keyword] public string MethodName { get; set; }
        public List<PercentRoute> PercentRoutes { get; set; }
        protected UserRecordBase()
        {
        }

        protected UserRecordBase(Guid id)
            : base(id)
        {
        }
    }
}