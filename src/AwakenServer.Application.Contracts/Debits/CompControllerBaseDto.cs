using System;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Debits
{
    public class CompControllerBaseDto: EntityDto<Guid>
    {
        public string ChainId { get; set; }
        public string ControllerAddress { get; set; }
    }
}