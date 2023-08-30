using System;

namespace AwakenServer.IDO.Dtos
{
    public class UserPublicOfferingDto
    {
        public Guid Id { get; set; }
        public string ChainId { get; set; }
        public string User { get; set; }
        public long TokenAmount { get; set; }
        public long RaiseTokenAmount { get; set; }
        public bool IsHarvest { get; set; }
        public PublicOfferingWithTokenDto PublicOfferingInfo { get; set; }
    }
}