using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.TestBase;
using Awaken.Contracts.DividendPoolContract;
using Shouldly;
using Xunit;

namespace AwakenServer.Dividend
{
    public partial class DividendTests
    {
        [Fact(Skip = "no need")]
        public async Task WithdrawProcessor_Should_Modify_Pool_User_And_Add_Record()
        {
            var pid = 0;
            var pool = new AddPool
            {
                Token = DividendTestConstants.ElfTokenSymbol,
                AllocPoint = 1,
                LastRewardBlock = 100,
                Pid = pid
            };
            await AddPoolAsync(pool);

            var user = DividendTestConstants.Xue;
            var depositAmount = 10000;
            var deposit = new Deposit
            {
                User = user,
                Pid = pid,
                Amount = depositAmount
            };
            await DepositAsync(deposit);

            var withdrawAmount = 5000;
            var withdraw = new Withdraw
            {
                Amount = withdrawAmount,
                User = user,
                Pid = pid
            };
            await WithdrawAsync(withdraw);

            var currentDepositAmount = depositAmount - withdrawAmount;
            var poolInfo = (await _esPoolRepository.GetListAsync()).Item2.First();
            poolInfo.DepositAmount.ShouldBe(currentDepositAmount.ToString());

            var userPoolInfo = (await _esUserPoolRepository.GetListAsync()).Item2.First();
            userPoolInfo.PoolBaseInfo.Id.ShouldNotBe(Guid.Empty);
            userPoolInfo.User.ShouldBe(user.ToBase58());
            userPoolInfo.DepositAmount.ShouldBe(currentDepositAmount.ToString());

            var userRecordInfo =
                (await _esUserRecordRepository.GetListAsync()).Item2.First(x =>
                    x.BehaviorType == BehaviorType.Withdraw);
            userRecordInfo.Amount.ShouldBe(withdrawAmount.ToString());
            userRecordInfo.User.ShouldBe(user.ToBase58());
            userRecordInfo.BehaviorType.ShouldBe(BehaviorType.Withdraw);
            userRecordInfo.TransactionHash.ShouldNotBe(string.Empty);
            userRecordInfo.DividendToken.ShouldBeNull();
        }

        private async Task WithdrawAsync(Withdraw withdraw, EventContext eventContext = null)
        {
            eventContext ??= GetEventContext(AElfChainId, eventAddress: DividendTestConstants.DividendTokenAddress);
            var processor = GetRequiredService<IEventHandlerTestProcessor<Withdraw>>();
            await processor.HandleEventAsync(withdraw, eventContext);
        }
    }
}