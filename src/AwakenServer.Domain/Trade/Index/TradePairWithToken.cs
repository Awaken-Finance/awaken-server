using System;
using AwakenServer.Tokens;
using Orleans;

namespace AwakenServer.Trade.Index
{
    public class TradePairWithToken : TradePairBase
    {
        public Token Token0 { get; set; }
        public Token Token1 { get; set; }
        
        public TradePairWithToken()
        {
        }

        public TradePairWithToken(Guid id)
            : base(id)
        {
        }
    }
}