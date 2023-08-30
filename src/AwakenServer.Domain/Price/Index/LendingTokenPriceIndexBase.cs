using System;
using AwakenServer.Tokens;

namespace AwakenServer.Price.Index
{
    public class LendingTokenPriceIndexBase : LendingTokenPriceBase
    {
        public Token Token { get; set; }
        
        protected LendingTokenPriceIndexBase()
        {

        }

        protected LendingTokenPriceIndexBase(Guid id) : base(id)
        {

        }
    }
}