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
        public async Task DepositProcessor_Should_Modify_Pool_User_And_Add_Record()
        {
            var token = DividendTestConstants.ElfTokenSymbol;
            var pid = 0;
            var pool = new AddPool
            {
                Token = token,
                AllocPoint = 1,
                LastRewardBlock = 100,
                Pid = pid
            };
            await AddPoolAsync(pool);

            var user = DividendTestConstants.Xue;
            var depositAmount = "10000";
            var deposit = new Deposit
            {
                User = user,
                Pid = pid,
                Amount = depositAmount
            };
            await DepositAsync(deposit);

            var poolInfo = (await _esPoolRepository.GetListAsync()).Item2.First();
            poolInfo.DepositAmount.ShouldBe(depositAmount);

            var userPoolInfo = (await _esUserPoolRepository.GetListAsync()).Item2.First();
            userPoolInfo.PoolBaseInfo.Id.ShouldNotBe(Guid.Empty);
            userPoolInfo.User.ShouldBe(user.ToBase58());
            userPoolInfo.DepositAmount.ShouldBe(depositAmount);

            var userRecordInfo = (await _esUserRecordRepository.GetListAsync()).Item2.First();
            userRecordInfo.Amount.ShouldBe(depositAmount);
            userRecordInfo.User.ShouldBe(user.ToBase58());
            userRecordInfo.BehaviorType.ShouldBe(BehaviorType.Deposit);
            userRecordInfo.TransactionHash.ShouldNotBe(string.Empty);
            userRecordInfo.DividendToken.ShouldBeNull();
        }

        [Fact(Skip = "no need")]
        public async Task DepositProcessor_Should_Add_Deposit_Amount_Accumulatively()
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
            await DepositAsync(deposit);

            var totalDepositAmount = (depositAmount * 2).ToString();
            var poolInfo = (await _esPoolRepository.GetListAsync()).Item2.First();
            poolInfo.DepositAmount.ShouldBe(totalDepositAmount);

            var userPoolInfo = (await _esUserPoolRepository.GetListAsync()).Item2.First();
            userPoolInfo.DepositAmount.ShouldBe(totalDepositAmount);
        }

        private async Task DepositAsync(Deposit deposit, EventContext eventContext = null)
        {
            eventContext ??= GetEventContext(AElfChainId, eventAddress: DividendTestConstants.DividendTokenAddress);
            var processor = GetRequiredService<IEventHandlerTestProcessor<Deposit>>();
            await processor.HandleEventAsync(deposit, eventContext);
        }
    }
}