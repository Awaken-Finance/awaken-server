using System.ComponentModel.DataAnnotations;

namespace AwakenServer.StatInfo.Dtos;

public class GetStatHistoryInput
{
    [Required]
    public string ChainId { get; set; }
    [Required]
    public int PeriodType { get; set; } //1:day, 2:week, 3:month, 4:year
    public string Symbol { get; set; }
    public string PairAddress { get; set; }
    public long TimestampMin { get; set; }
    public long TimestampMax { get; set; }
}

public enum PeriodType
{
    Day = 1,
    Week,
    Month,
    Year
}