using System;
using AutoMapper;

namespace AwakenServer.Price.Etos
{
    [AutoMap(typeof(LendingTokenPrice))]
    public class LendingTokenPriceEto : LendingTokenPrice
    {
        public LendingTokenPriceEto()
        {
        }

        public LendingTokenPriceEto(Guid id) : base(id)
        {
        }
    }
}