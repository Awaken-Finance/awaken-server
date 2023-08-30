using Nethereum.Util;
using Volo.Abp.DependencyInjection;
using System.Numerics;
using AwakenServer.Debits.DebitAppDto;
using AwakenServer.Debits.Providers;

namespace AwakenServer.Debits.Services.Impl
{
    public class DefaultTokenRateCalculateService : ITokenRateCalculateService, ITransientDependency
    {
        private readonly IInterestModelProvider _interestModelProvider;

        public DefaultTokenRateCalculateService(IInterestModelProvider interestModelProvider)
        {
            _interestModelProvider = interestModelProvider;
        }

        public BigDecimal GetBorrowRate(CTokenDto cToken, BigInteger cash, BigInteger borrows,
            BigInteger reserves)
        {
            var interestModel = _interestModelProvider.GetInterestModel(cToken);
            return interestModel?.GetBorrowRate(cash, borrows, reserves) ?? 0;
        }

        public BigDecimal GetSupplyRate(CTokenDto cToken, BigInteger cash, BigInteger borrows,
            BigInteger reserves, BigInteger reserveFactorMantissa)
        {
            var interestModel = _interestModelProvider.GetInterestModel(cToken);
            return interestModel?.GetSupplyRate(cash, borrows, reserves, reserveFactorMantissa) ?? 0;
        }
    }
}