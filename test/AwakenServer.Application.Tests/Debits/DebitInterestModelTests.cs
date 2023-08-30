using AwakenServer.Debits.DebitAppDto;
using AwakenServer.Debits.Providers;
using AwakenServer.Debits.Providers.InterestModel;
using Shouldly;
using Xunit;
using Xunit.Abstractions;

namespace AwakenServer.Debits
{
    public class DebitInterestModelTests : AwakenServerTestBase<DebitInterestModelTestModule>
    {
        private readonly IInterestModelProvider _InterestModelProvider;

        public DebitInterestModelTests(ITestOutputHelper testOutputHelper)
        {
            _InterestModelProvider = GetRequiredService<IInterestModelProvider>();
        }

        [Fact(Skip = "no need")]
        public void GetInterestModel_Should_Return_Right_InterestModel()
        {
            var tokenA = "TokenA";
            var tokenAInterestModel = _InterestModelProvider.GetInterestModel(new CTokenDto
            {
                Symbol = tokenA
            });
            tokenAInterestModel.GetType().ShouldBe(typeof(WhitePaperInterestRateModel));

            var otherToken = "OtherToken";
            var otherTokenInterestModel = _InterestModelProvider.GetInterestModel(new CTokenDto
            {
                Symbol = otherToken
            });
            otherTokenInterestModel.GetType().ShouldBe(typeof(JumpRateInterestModel));
        }

        [Theory(Skip = "no need")]
        [InlineData(1000, 500, 500, 15000)]
        [InlineData(1000, 0, 500, 15000)]
        [InlineData(1000, 900, 300, 15625)]
        public void JumpRateInterestModel_GetBorrowRate_Should_Get_Right_Rate(long cash, long borrow, long reserves,
            long targetRate)
        {
            var defaultInterestModel = _InterestModelProvider.GetInterestModel(new CTokenDto
            {
                Symbol = "SomeToken"
            });
            var borrowRate = defaultInterestModel.GetBorrowRate(cash, borrow, reserves);
            borrowRate.ShouldBe(targetRate);
        }

        [Theory(Skip = "no need")]
        [InlineData(1000, 500, 500, 1000000000000000000, 0)]
        [InlineData(1000, 500, 500, 500000000000000000, 3750)]
        [InlineData(1000, 0, 500, 500000000000000000, 0)]
        [InlineData(1000, 900, 300, 500000000000000000, 4394)]
        public void JumpRateInterestModel_GetSupplyRate_Should_Get_Right_Rate(long cash, long borrow, long reserves,
            long reserveFactorMantissa, long targetRate)
        {
            var defaultInterestModel = _InterestModelProvider.GetInterestModel(new CTokenDto
            {
                Symbol = "SomeToken"
            });
            var supplyRate = defaultInterestModel.GetSupplyRate(cash, borrow, reserves, reserveFactorMantissa);
            supplyRate.ShouldBe(targetRate);
        }
    }
}