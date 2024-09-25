using System.ComponentModel.DataAnnotations;

namespace AwakenServer.Activity.Dtos;

public class JoinInput : ActivityBaseDto
{
    [Required] public string Message { get; set; }
    [Required] public string Signature { get; set; }
    [Required] public string PublicKey { get; set; }
    [Required] public string Address { get; set; }
}