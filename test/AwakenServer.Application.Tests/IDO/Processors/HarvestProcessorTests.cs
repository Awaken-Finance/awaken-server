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
        public async Task HarvestAsync_Should_Success()
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

            var harvestAddress = SampleAccount.Accounts[1].Address;
            var harvestUser = harvestAddress.ToBase58();
            var harvestAmount = 1000L;
            await InvestAsync(txContext, pid, harvestAddress, 1000L, 1000L);
            await HarvestAsync(txContext, harvestAddress, pid, harvestAmount);

            var userInfo =
                await _userOfferingRepository.GetAsync(x => x.ChainId == ChainId && x.User == harvestUser);
            userInfo.TokenAmount.ShouldBe(harvestAmount);
            userInfo.IsHarvest.ShouldBeTrue();
            userInfo.PublicOfferingId.ShouldNotBe(Guid.Empty);

            var records = await _publicOfferingRecordRepository.GetListAsync(x => x.User == harvestUser && x.OperateType == OperationType.Harvest);
            records.Count.ShouldBe(1);
            var record = records[0];
            record.TokenAmount.ShouldBe(harvestAmount);
            record.RaiseTokenAmount.ShouldBe(0);
            record.ChainId.ShouldBe(ChainId);
            record.PublicOfferingId.ShouldNotBe(Guid.Empty);
        }

        [Fact(Skip = "no need")]
        public async Task HarvestAsync_With_Invalid_PublicOffering_Id_Should_Fail()
        {
            var harvestAddress = SampleAccount.Accounts[1].Address;
            var harvestAmount = 1000L;
            var txContext = GetEventContext(AElfChainId);
            var exception = await Assert.ThrowsAsync<Exception>(() =>
                HarvestAsync(txContext, harvestAddress, 0, harvestAmount));
            exception.Message.ShouldContain("Failed to find public id");
        }


        private async Task HarvestAsync(EventContext eventContext, Address to, int pid, long amount)
        {
            var processor = GetRequiredService<IEventHandlerTestProcessor<Harvest>>();
            await processor.HandleEventAsync(new Harvest
            {
                To = to,
                PublicId = pid,
                Amount = amount
            }, eventContext);
        }
    }
}