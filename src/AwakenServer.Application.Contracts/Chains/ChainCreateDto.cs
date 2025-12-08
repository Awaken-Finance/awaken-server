using System;
using System.ComponentModel;
using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Chains
{
    public class ChainCreateDto
    {
        public string Id { get; set; }
        public string Name { get; set; }
        
        public int AElfChainId { get; set; }
    }
}