using System;
using System.ComponentModel.DataAnnotations;
using JetBrains.Annotations;

namespace AwakenServer.Chains;

public class ChainUpdateDto
{
    [Required]
    public string Id { get; set; }
    
    [CanBeNull] public string Name { get; set; }
    public int? BlocksPerDay { get; set; }
    public long? LatestBlockHeight { get; set; }
    public long? LatestBlockHeightExpireMs { get; set; }
    public int? AElfChainId { get; set; }
}