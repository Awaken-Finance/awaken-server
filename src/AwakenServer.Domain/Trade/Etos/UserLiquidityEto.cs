using System;
using AutoMapper;

namespace AwakenServer.Trade.Etos
{
    [AutoMap(typeof(UserLiquidity))]
    public class UserLiquidityEto : UserLiquidity
    {
        public UserLiquidityEto()
        {
        }

        public UserLiquidityEto(Guid id)
            : base(id)
        {
            Id = id;
        }
    }
}