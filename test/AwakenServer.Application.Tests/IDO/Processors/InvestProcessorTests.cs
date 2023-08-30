using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.ContractTestKit;
using AElf.Types;
using Awaken.Contracts.Shadowfax;
using Shouldly;
using Xunit;

namespace AwakenServer.IDO.Tests
{
    public partial class IDOTests
    {
        [Fact(Skip = "no need")]
        public async Task Invest_Success()
        {
            var publisher = SampleAccount.Accounts[0].Address;
            var pid = 0;
            var token = IDOTestConstants.TokenOneSymbol;
            var tokenAmount = 1000000L;
            var raiseTokenAmount = 2000000L;
            var raiseToken = IDOTestConstants.ElfTokenSymbol;
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddDays(1);
            var publicOffering = GetPublicOffering(publisher, pid, token, tokenAmount, raiseToken, raiseTokenAmount,
                startTime, endTime);
            var txContext = GetEventContext(AElfChainId);
            await AddPublicOfferingAsync(publicOffering, txContext);
            
            var investorAddress = SampleAccount.Accounts[1].Address;
            var investor = investorAddress.ToBase58();
            var investRaiseTokenAmount = 200;
            var investTokenAmount = 100;
            var channel = "xxfoundation";
            await InvestAsync(txContext, pid, investorAddress, investRaiseTokenAmount, investTokenAmount, channel);

            var publicOfferingInDb =
                await _publicOfferingRepository.FindAsync(x => x.OrderRank == pid && x.ChainId == ChainId);
            publicOfferingInDb.CurrentAmount.ShouldBe(tokenAmount - investTokenAmount);
            publicOfferingInDb.RaiseCurrentAmount.ShouldBe(investRaiseTokenAmount);
                
            var investorInfo =
                await _userOfferingRepository.GetAsync(x => x.ChainId == ChainId && x.User == investor);
            investorInfo.TokenAmount.ShouldBe(investTokenAmount);
            investorInfo.RaiseTokenAmount.ShouldBe(investRaiseTokenAmount);
            investorInfo.IsHarvest.ShouldBeFalse();
            investorInfo.PublicOfferingId.ShouldBe(publicOfferingInDb.Id);

            var records = await _publicOfferingRecordRepository.GetListAsync(x => x.User == investor);
            records.Count.ShouldBe(1);
            var record = records[0];
            record.Channel.ShouldBe(channel);
            record.OperateType.ShouldBe(OperationType.Invest);
            record.ChainId.ShouldBe(ChainId);
            record.PublicOfferingId.ShouldBe(publicOfferingInDb.Id);
        }

        [Fact(Skip = "no need")]
        public async Task Invest_With_Invalid_PublicOffering_Id_Should_Fail()
        {
            var investorAddress = SampleAccount.Accounts[1].Address;
            var investRaiseTokenAmount = 200;
            var investTokenAmount = 100;
            var txContext = GetEventContext(AElfChainId);
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                InvestAsync(txContext, 0, investorAddress, investRaiseTokenAmount, investTokenAmount));
            exception.Message.ShouldContain("Failed to find public id");
        }

        private async Task InvestAsync(EventContext eventContext, int publicId,
            Address investor,
            long spend,
            long income,
            string channel = "")
        {
            var processor = GetRequiredService<IEventHandlerTestProcessor<Invest>>();
            await processor.HandleEventAsync(new Invest
            {
                PublicId = publicId,
                Investor = investor,
                Spend = spend,
                Income = income,
                Channel = channel
            }, eventContext);
        }
    }
}