using System.Numerics;

namespace AwakenServer.Debits.Providers.InterestModel
{
    public interface IInterestModel
    {
        BigInteger GetBorrowRate(BigInteger cash, BigInteger borrows, BigInteger reserves);

        BigInteger GetSupplyRate(BigInteger cash, BigInteger borrows, BigInteger reserves,
            BigInteger reserveFactorMantissa);
    }
}