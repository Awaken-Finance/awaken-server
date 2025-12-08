using System;
using AwakenServer.Tokens;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Price.Dtos
{
    public class OtherLpTokenIndexDto : EntityDto<Guid>
    {
        public string ChainId { get; set; }
        public string Address { get; set; }
        
        public TokenDto Token0 { get; set; }

        public TokenDto Token1 { get; set; }
        
        public string Reserve0 { get; set; }
        
        public double Reserve0Value { get; set; }
        
        public string Reserve1 { get; set; }
        
        public double Reserve1Value { get; set; }
    }
}