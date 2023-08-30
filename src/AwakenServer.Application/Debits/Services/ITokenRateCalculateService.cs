using System.Numerics;
using AwakenServer.Debits.DebitAppDto;
using Nethereum.Util;

namespace AwakenServer.Debits.Services
{
    public interface ITokenRateCalculateService
    {
        BigDecimal GetBorrowRate(CTokenDto cToken, BigInteger cash, BigInteger borrows, BigInteger reserves);

        BigDecimal GetSupplyRate(CTokenDto cToken, BigInteger cash, BigInteger borrows, BigInteger reserves,
            BigInteger reserveFactorMantissa);
    }
}