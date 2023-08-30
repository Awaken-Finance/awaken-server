using System;

namespace AwakenServer.Entities.GameOfTrust.Ef
{
    public class UserGameOfTrust: UserGameOfTrustBase
    {
        public Guid GameOfTrustId { get; set; }
    }
}