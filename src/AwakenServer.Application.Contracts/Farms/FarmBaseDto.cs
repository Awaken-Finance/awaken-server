using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Farms
{
    public class FarmBaseDto: EntityDto<Guid>
    {
        public string ChainId { get; set; }
        public string FarmAddress { get; set; }
        public FarmType FarmType { get; set; }
    }
}