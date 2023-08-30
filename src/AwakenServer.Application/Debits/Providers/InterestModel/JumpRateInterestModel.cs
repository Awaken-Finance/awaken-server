using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using AElf.CSharp.Core;
using AElf.Types;

namespace AwakenServer.Debits.Providers.InterestModel
{
    public class JumpRateInterestModel: IInterestModel
    {
        public const string InterestModelName = "JumpRate";
        private long _mantissa;
        private long _kink;
        private long _multiplierPerBlock;
        private long _baseRatePerBlock;
        private long _jumpMultiplierPerBlock;
        
        public JumpRateInterestModel(Dictionary<string, string> parameters)
        {
            if (parameters == null || !parameters.Any())
            {
                _multiplierPerBlock = 1000;
                _baseRatePerBlock = 1000;
                _kink = 10;
                _jumpMultiplierPerBlock = 100;
                _mantissa = 100000000L;
                return;
            }
            
            if (!parameters.TryGetValue("Mantissa", out var mantissa))
            {
                throw new Exception("Lack of Mantissa");
            }
            
            if (!parameters.TryGetValue("MultiplierPerBlock", out var multiplierPerBlock))
            {
                throw new Exception("Lack of MultiplierPerBlock");
            }
            
            if (!parameters.TryGetValue("BaseRatePerBlock", out var baseRatePerBlock))
            {
                throw new Exception("Lack of BaseRatePerBlock");
            }
            
            if (!parameters.TryGetValue("Kink", out var kink))
            {
                throw new Exception("Lack of Kink");
            }
            
            if (!parameters.TryGetValue("JumpMultiplierPerBlock", out var jumpMultiplierPerBlock))
            {
                throw new Exception("Lack of JumpMultiplierPerBlock");
            }

            _mantissa = long.Parse(mantissa);
            _kink = long.Parse(kink);
            _multiplierPerBlock = long.Parse(multiplierPerBlock);
            _baseRatePerBlock = long.Parse(baseRatePerBlock);
            _jumpMultiplierPerBlock = long.Parse(jumpMultiplierPerBlock);
        }

        public BigInteger GetBorrowRate(BigInteger cash, BigInteger borrows, BigInteger reserves)
        {
            return GetBorrowRateInternal((long)cash, (long)borrows, (long)reserves);
        }

        public BigInteger GetSupplyRate(BigInteger cash, BigInteger borrows, BigInteger reserves, BigInteger reserveFactorMantissa)
        {
            var oneMinusReserveFactor = _mantissa.Sub((long)reserveFactorMantissa);
            var cashConversion = (long)cash;
            var borrowsConversion = (long)borrows;
            var reservesConversion = (long)reserves;
            var borrowRate = GetBorrowRateInternal(cashConversion, borrowsConversion, reservesConversion);
            var rateToPool = new BigIntValue(borrowRate).Mul(oneMinusReserveFactor).Div(_mantissa);
            var util = GetUtilizationRateInternal(cashConversion, borrowsConversion, reservesConversion);
            var supplyRateStr = new BigIntValue(util).Mul(rateToPool).Div(_mantissa).Value;
            return long.Parse(supplyRateStr);
        }
        
        private long GetBorrowRateInternal(long cash, long borrows, long reserves)
        {
            var util = GetUtilizationRateInternal(cash, borrows, reserves);
            string borrowRateStr;
            if (util <= _kink)
            {
                borrowRateStr = new BigIntValue(_kink).Mul(_multiplierPerBlock).Div(_mantissa)
                    .Add(_baseRatePerBlock).Value;
            }
            else
            {
                var normalRate = new BigIntValue(_kink).Mul(_multiplierPerBlock).Div(_mantissa)
                    .Add(_baseRatePerBlock);
                var excessUtil = new BigIntValue(util.Sub(_kink));
                borrowRateStr = excessUtil.Mul(_jumpMultiplierPerBlock).Div(_mantissa)
                    .Add(normalRate).Value;
            }
            
            return long.Parse(borrowRateStr);
        }

        private long GetUtilizationRateInternal(long cash, long borrows, long reserves)
        {
            if (borrows == 0)
            {
                return 0;
            }

            var utilizationRateStr = new BigIntValue(borrows).Mul(_mantissa)
                .Div(cash.Add(borrows).Sub(reserves)).Value;
            return long.Parse(utilizationRateStr);
        }
    }
}