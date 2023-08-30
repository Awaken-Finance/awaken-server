using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.TestBase;
using AElf.ContractTestKit;
using AElf.Types;
using Awaken.Contracts.Shadowfax;
using Google.Protobuf.WellKnownTypes;
using Shouldly;
using Xunit;

namespace AwakenServer.IDO.Tests
{
    public partial class IDOTests
    {

        [Fact(Skip = "no need")]
        public async Task AddPublicOffering_Success()
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
            var publicOfferingInDb =
                await _publicOfferingRepository.FindAsync(x => x.OrderRank == pid && x.ChainId == ChainId);
            publicOfferingInDb.ShouldNotBeNull();
            publicOfferingInDb.Publisher.ShouldBe(publisher.ToBase58());
            publicOfferingInDb.TokenContractAddress.ShouldNotBeNull();
            publicOfferingInDb.TokenId.ShouldNotBe(Guid.Empty);
            publicOfferingInDb.RaiseTokenId.ShouldNotBe(Guid.Empty);
            publicOfferingInDb.CurrentAmount.ShouldBe(tokenAmount);
            publicOfferingInDb.RaiseCurrentAmount.ShouldBe(0);
            publicOfferingInDb.MaxAmount.ShouldBe(tokenAmount);
            publicOfferingInDb.RaiseMaxAmount.ShouldBe(raiseTokenAmount);
        }

        [Fact(Skip = "no need")]
        public async Task AddPublicOffering_With_Repeat_Pid_Should_Fail()
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
            publicOffering.Publisher = SampleAccount.Accounts[2].Address;
            await AddPublicOfferingAsync(publicOffering, txContext);
            var publicOfferings =
                await _publicOfferingRepository.GetListAsync(x => x.OrderRank == pid && x.ChainId == ChainId);
            publicOfferings.Count.ShouldBe(1);
            publicOfferings[0].Publisher.ShouldBe(SampleAccount.Accounts[0].Address.ToBase58());
        }

        private AddPublicOffering GetPublicOffering(Address publisher, long publicId, string token, long tokenAmount,
            string raiseToken, long raiseTokenAmount, DateTime startTime, DateTime endTime)
        {
            return new AddPublicOffering
            {
                OfferingTokenSymbol = token,
                OfferingTokenAmount = tokenAmount,
                WantTokenSymbol = raiseToken,
                WantTokenAmount = raiseTokenAmount,
                Publisher = publisher,
                StartTime = startTime.ToTimestamp(),
                EndTime = endTime.ToTimestamp(),
                PublicId = publicId,
            };
        }

        private EventContext GetEventContext(int chainId, long blockNumber = 0L,
            string status = "Mined",
            string txHash = null, string blockHash = null, long blockTimestamp = 0, string fromAddress = null,
            string toAddress = null,
            string methodName = null, string returnValue = null)
        {
            var blockTime = blockTimestamp == 0
                ? DateTime.UtcNow
                : DateTimeHelper.FromUnixTimeMilliseconds(blockTimestamp);
            return new EventContext
            {
                TransactionId = txHash,
                Status = status,
                ReturnValue = returnValue,
                ChainId = chainId,
                BlockNumber = blockNumber,
                MethodName = methodName,
                BlockTime = blockTime,
                FromAddress = fromAddress,
                ToAddress = toAddress,
                BlockHash = blockHash
            };
        }

        private async Task AddPublicOfferingAsync(AddPublicOffering publicOffering, EventContext eventContext)
        {
            var processor = GetRequiredService<IEventHandlerTestProcessor<AddPublicOffering>>();
            await processor.HandleEventAsync(publicOffering, eventContext);
        }
    }
}