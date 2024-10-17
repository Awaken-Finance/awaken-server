using System;
using AwakenServer.Tokens;
using Orleans;

namespace AwakenServer.Trade.Index
{
    [GenerateSerializer]
    public class TradePairWithToken : TradePairBase
    {
        [Id(0)] public Token Token0 { get; set; }
        [Id(1)] public Token Token1 { get; set; }
        
        public TradePairWithToken()
        {
        }

        public TradePairWithToken(Guid id)
            : base(id)
        {
        }
    }
}