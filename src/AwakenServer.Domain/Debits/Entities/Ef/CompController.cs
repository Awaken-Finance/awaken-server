using System;

namespace AwakenServer.Debits.Entities.Ef
{
    public class CompController: EditableCompController
    {
        public Guid DividendTokenId { get; set; }
    }
}