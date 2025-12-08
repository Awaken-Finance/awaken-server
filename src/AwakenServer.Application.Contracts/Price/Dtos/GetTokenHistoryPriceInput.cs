using System;

namespace AwakenServer.Price.Dtos;

public class GetTokenHistoryPriceInput
{
    public string Symbol { get; set; }
    public DateTime DateTime { get; set; }
}