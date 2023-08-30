
namespace AwakenServer.Debits.DebitAppDto
{
    public class CompControllerDto: CompControllerBaseDto
    {
        public string CloseFactorMantissa { get; set; }
        public DebitTokenDto DividendToken { get; set; }
        // public string LiquidationIncentive { get; set; }
        // public string CompInitialIndex { get; set; }
    }
}