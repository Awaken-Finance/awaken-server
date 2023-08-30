using System.Collections.Generic;
using AwakenServer.GameOfTrust.DTos.Dto;

namespace AwakenServer.IDO.Dtos
{
    public class PublicOfferingAssetDto
    {
        public List<TokenDto> Token { get; set; }
        public List<TokenDto> RaiseToken { get; set; }
    }
}