using System;
using AutoMapper;

namespace AwakenServer.Price.Etos
{
    [AutoMap(typeof(OtherLpToken))]
    public class OtherLpTokenEto : OtherLpToken
    {
        public OtherLpTokenEto()
        {
        }

        public OtherLpTokenEto(Guid id) : base(id)
        {
        }
    }
}