using System;

namespace AwakenServer.Price.Dtos
{
    public class OtherLpTokenCreateDto
    {
        public string ChainId { get; set; }
        public Guid Token0Id { get; set; }
        public Guid Token1Id { get; set; }
        public string Address { get; set; }
        public string Reserve0 { get; set; }
        public double Reserve0Value { get; set; }
        public string Reserve1 { get; set; }
        public double Reserve1Value { get; set; }
    }
}