using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;

namespace AwakenServer.Debits.Providers.InterestModel
{
    public class WhitePaperInterestRateModel : IInterestModel
    {
        public const string InterestModelName = "WhitePaper";
        private readonly BigInteger _multiplierPerBlock;
        private readonly BigInteger _baseRatePerBlock;
        private readonly BigInteger _mantissa;

        public WhitePaperInterestRateModel(Dictionary<string, string> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                int _blocksPerYear = 63072000;
                _multiplierPerBlock = BigInteger.Pow(10, 13) / _blocksPerYear;
                _baseRatePerBlock = BigInteger.Pow(10, 13)  / _blocksPerYear;
                _mantissa = BigInteger.Pow(10, 8);
                return;
            }
            
            if (!parameters.TryGetValue("Mantissa", out var mantissa))
            {
                throw new Exception("Lack of Mantissa");
            }

            if (!parameters.TryGetValue("MultiplierPerBlock", out var multiplier))
            {
                throw new Exception("Lack of Multiplier");
            }
            
            if (!parameters.TryGetValue("BaseRatePerBlock", out var baseRate))
            {
                throw new Exception("Lack of BaseRate");
            }
            _mantissa = BigInteger.Parse(mantissa);
            _multiplierPerBlock = BigInteger.Parse(multiplier);
            _baseRatePerBlock = BigInteger.Pow(10, 13);
        }

        public BigInteger GetBorrowRate(BigInteger cash, BigInteger borrows, BigInteger reserves)
        {
            var ur = UtilizationRate(cash, borrows, reserves);
            return ur * _multiplierPerBlock / _mantissa + _baseRatePerBlock;
        }

        public BigInteger GetSupplyRate(BigInteger cash, BigInteger borrows, BigInteger reserves,
            BigInteger reserveFactorMantissa)
        {
            var oneMinusReserveFactor = _mantissa - reserveFactorMantissa;
            var borrowRate = GetBorrowRate(cash, borrows, reserves);
            var rateToPool = borrowRate * oneMinusReserveFactor / _mantissa;
            return UtilizationRate(cash, borrows, reserves) * rateToPool / _mantissa;
        }

        private BigInteger UtilizationRate(BigInteger cash, BigInteger borrows, BigInteger reserves)
        {
            if (borrows == BigInteger.Zero)
            {
                return BigInteger.Zero;
            }

            var denominator = cash + borrows - reserves;
            if (denominator == BigInteger.Zero)
            {
                return BigInteger.Zero;
            }

            return borrows * _mantissa / denominator;
        }
    }
}