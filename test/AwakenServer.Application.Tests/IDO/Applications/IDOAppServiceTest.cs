using System;
using System.Linq;
using System.Threading.Tasks;
using AElf.ContractTestKit;
using AElf.Types;
using AwakenServer.IDO.Dtos;
using AwakenServer.IDO.Entities.Ef;
using AwakenServer.Tokens;
using Shouldly;
using Volo.Abp.Domain.Repositories;
using Xunit;

namespace AwakenServer.IDO.Tests
{
    public partial class IDOTests : IDOTestBase
    {
        private readonly IRepository<Entities.Ef.PublicOffering> _publicOfferingRepository;
        private readonly IRepository<PublicOfferingRecord> _publicOfferingRecordRepository;
        private readonly IRepository<UserPublicOffering> _userOfferingRepository;
        private readonly TokenAppService _tokenAppService;
        private readonly IIdoAppService _idoAppService;

        public IDOTests()
        {
            _publicOfferingRepository = GetRequiredService<IRepository<Entities.Ef.PublicOffering>>();
            _publicOfferingRecordRepository = GetRequiredService<IRepository<PublicOfferingRecord>>();
            _userOfferingRepository = GetRequiredService<IRepository<UserPublicOffering>>();
            _tokenAppService = GetRequiredService<TokenAppService>();
            _idoAppService = GetRequiredService<IIdoAppService>();
        }

        [Fact(Skip = "no need")]
        public async Task GetPublicOfferingAsync_Without_ChainId_Should_Return_All_Data()
        {
            var publisherOne = SampleAccount.Accounts[0].Address;
            var publisherTwo = SampleAccount.Accounts[1].Address;
            var publisherThree = SampleAccount.Accounts[2].Address;
            await AddPublishOfferingAsync(publisherOne, 0);
            await AddPublishOfferingAsync(publisherTwo, 1);
            await AddPublishOfferingAsync(publisherThree, 2);
            var publishOfferings = await _idoAppService.GetPublicOfferingsAsync(new GetPublicOfferingInput());
            publishOfferings.TotalCount.ShouldBe(3);
        }
        
        [Fact(Skip = "no need")]
        public async Task GetPublicOfferingAsync_With_ChainId_Should_Return_All_Data()
        {
            var publisherOne = SampleAccount.Accounts[0].Address;
            var publisherTwo = SampleAccount.Accounts[1].Address;
            var publisherThree = SampleAccount.Accounts[2].Address;
            await AddPublishOfferingAsync(publisherOne, 0);
            await AddPublishOfferingAsync(publisherTwo, 1);
            await AddPublishOfferingAsync(publisherThree, 2);
            var publishOfferings = await _idoAppService.GetPublicOfferingsAsync(new GetPublicOfferingInput
            {
                ChainId = ChainId
            });
            publishOfferings.TotalCount.ShouldBe(3);
            var targetAddress = publisherOne.ToBase58();
            var targetOne = publishOfferings.Items.Single(x => x.Publisher == targetAddress);
            targetOne.Token.Id.ShouldNotBe(Guid.Empty);
            targetOne.RaiseToken.Id.ShouldNotBe(Guid.Empty);
        }
        
        [Fact(Skip = "no need")]
        public async Task GetPublicOfferingAsync_With_Skip_And_Size_Should_Return_All_Data()
        {
            var publisherOne = SampleAccount.Accounts[0].Address;
            var publisherTwo = SampleAccount.Accounts[1].Address;
            var publisherThree = SampleAccount.Accounts[2].Address;
            await AddPublishOfferingAsync(publisherOne, 0);
            await AddPublishOfferingAsync(publisherTwo, 1);
            await AddPublishOfferingAsync(publisherThree, 2);
            var publishOfferings = await _idoAppService.GetPublicOfferingsAsync(new GetPublicOfferingInput
            {
                SkipCount = 1,
                Size = 1
            });
            publishOfferings.TotalCount.ShouldBe(3);
            publishOfferings.Items.Count.ShouldBe(1);
            publishOfferings.Items[0].Publisher.ShouldBe(publisherTwo.ToBase58());
        }

        [Fact(Skip = "no need")]
        public async Task GetUserPublicOfferingInfoAsync_Success()
        {
            var publisherOne = SampleAccount.Accounts[0].Address;
            await AddPublishOfferingAsync(publisherOne, 0);
            var txContext = GetEventContext(AElfChainId);
            var investor = SampleAccount.Accounts[1].Address;
            var raiseAmount = 200L;
            var tokenAmount = 100L;
            await InvestAsync(txContext, 0, investor, raiseAmount, tokenAmount);
            var publicOfferings = await _idoAppService.GetPublicOfferingsAsync(new GetPublicOfferingInput
            {
                ChainId = ChainId
            });
            var publicOffering = publicOfferings.Items[0];
            publicOffering.RaiseCurrentAmount.ShouldBe(200);
            publicOffering.CurrentAmount.ShouldBe(1000000L - tokenAmount);
            
            var userInfos = await _idoAppService.GetUserPublicOfferingsAsync(new GetUserPublicOfferingInfoInput
            {
                ChainId = ChainId,
                User = investor.ToBase58()
            });
            userInfos.TotalCount.ShouldBe(1);
            var userInfo = userInfos.Items[0];
            userInfo.TokenAmount.ShouldBe(tokenAmount);
            userInfo.RaiseTokenAmount.ShouldBe(raiseAmount);
            userInfo.PublicOfferingInfo.Id.ShouldBe(publicOffering.Id);
        }

        [Fact(Skip = "no need")]
        public async Task GetUserAssetAsync_Success()
        {
            var publisherOne = SampleAccount.Accounts[0].Address;
            await AddPublishOfferingAsync(publisherOne, 0);
            var txContext = GetEventContext(AElfChainId);
            var investor = SampleAccount.Accounts[1].Address;
            var raiseAmount = 200L;
            var tokenAmount = 100L;
            await InvestAsync(txContext, 0, investor, raiseAmount, tokenAmount);
            await InvestAsync(txContext, 0, investor, raiseAmount, tokenAmount);
            var userAsset = await _idoAppService.GetUserPublicOfferingsAssetAsync(new GetUserAssetInput
            {
                ChainId = ChainId,
                User = investor.ToBase58()
            });
            userAsset.UsdtValue.ShouldBe(0.000002m);
            userAsset.BtcValue.ShouldBe(0.000002m);
        }

        [Fact(Skip = "no need")]
        public async Task GetAssetTokenInfoAsync_Success()
        {
            var publisherOne = SampleAccount.Accounts[0].Address;
            await AddPublishOfferingAsync(publisherOne, 0);
            await AddPublishOfferingAsync(publisherOne, 1);
            var tokensInfo = await _idoAppService.GetPublicOfferingsTokensAsync(new GetAssetTokenInfoInput
            {
                ChainId = ChainId
            });
            tokensInfo.Token.Count.ShouldBe(1);
            tokensInfo.Token[0].Symbol.ShouldBe(IDOTestConstants.TokenOneSymbol);
            tokensInfo.RaiseToken.Count.ShouldBe(1);
            tokensInfo.RaiseToken[0].Symbol.ShouldBe(IDOTestConstants.ElfTokenSymbol);
        }
        
        [Fact(Skip = "no need")]
        public async Task GetUserRecordAsync_Without_Filter_Should_Get_All_Records()
        {
            var userAddress = SampleAccount.Accounts[1].Address;
            await GenerateRecordsAsync(userAddress);
            var user = userAddress.ToBase58();
            var userRecords = await _idoAppService.GetPublicOfferingRecordsAsync(new GetUserRecordInput
            {
                ChainId = ChainId,
                User = user
            });
            userRecords.TotalCount.ShouldBe(4);
            var targetRecord = userRecords.Items.Single(x => x.PublicOfferingInfo.OrderRank == 1);
            targetRecord.Id.ShouldNotBe(Guid.Empty);
            targetRecord.PublicOfferingInfo.Token.Id.ShouldNotBe(Guid.Empty);
            targetRecord.PublicOfferingInfo.RaiseToken.Id.ShouldNotBe(Guid.Empty);
            targetRecord.PublicOfferingInfo.Token.Symbol.ShouldBe(IDOTestConstants.ElfTokenSymbol);
            targetRecord.PublicOfferingInfo.RaiseToken.Symbol.ShouldBe(IDOTestConstants.TokenOneSymbol);

            var lastRecord = userRecords.Items.Last();
            userRecords = await _idoAppService.GetPublicOfferingRecordsAsync(new GetUserRecordInput
            {
                ChainId = ChainId,
                User = user,
                SkipCount = 3,
                Size = 1
            });
            userRecords.Items.Count.ShouldBe(1);
            userRecords.Items[0].Id.ShouldBe(lastRecord.Id);
        }
        
        [Fact(Skip = "no need")]
        public async Task GetUserRecordAsync_With_Time_Filter_Should_Get_Right_Records()
        {
            var startTime = DateTime.UtcNow.AddMinutes(-1);
            var endTime = startTime.AddMinutes(30);
            var userAddress = SampleAccount.Accounts[1].Address;
            await GenerateRecordsAsync(userAddress);
            var user = userAddress.ToBase58();
            var userRecords = await _idoAppService.GetPublicOfferingRecordsAsync(new GetUserRecordInput
            {
                ChainId = ChainId,
                User = user,
                TimestampMin = DateTimeHelper.ToUnixTimeMilliseconds(startTime),
                TimestampMax = DateTimeHelper.ToUnixTimeMilliseconds(endTime)
            });
            userRecords.TotalCount.ShouldBe(2);
        }
        
        [Fact(Skip = "no need")]
        public async Task GetUserRecordAsync_With_Token_Id_Filter_Should_Get_Right_Records()
        {
            var userAddress = SampleAccount.Accounts[1].Address;
            await GenerateRecordsAsync(userAddress);
            var user = userAddress.ToBase58();
            var tokenOne = await _tokenAppService.GetAsync(new GetTokenInput
            {
                Symbol = IDOTestConstants.TokenOneSymbol
            });
            var elf = await _tokenAppService.GetAsync(new GetTokenInput
            {
                Symbol = IDOTestConstants.ElfTokenSymbol
            });
            var userRecords = await _idoAppService.GetPublicOfferingRecordsAsync(new GetUserRecordInput
            {
                ChainId = ChainId,
                User = user,
                TokenId = tokenOne.Id,
                RaiseTokenId = elf.Id
            });
            userRecords.TotalCount.ShouldBe(3);
            userRecords.Items.All(x =>
                    x.PublicOfferingInfo.Token.Id == tokenOne.Id && x.PublicOfferingInfo.RaiseToken.Id == elf.Id)
                .ShouldBeTrue();
        }

        [Fact(Skip = "no need")]
        public async Task GetUserRecordAsync_With_Operation_Type_Filter_Should_Get_Right_Records()
        {
            var userAddress = SampleAccount.Accounts[1].Address;
            await GenerateRecordsAsync(userAddress);
            var user = userAddress.ToBase58();
            var userRecords = await _idoAppService.GetPublicOfferingRecordsAsync(new GetUserRecordInput
            {
                ChainId = ChainId,
                User = user,
                OperateType = 1
            });
            userRecords.TotalCount.ShouldBe(3);
            
            userRecords = await _idoAppService.GetPublicOfferingRecordsAsync(new GetUserRecordInput
            {
                ChainId = ChainId,
                User = user,
                OperateType = 2
            });
            userRecords.TotalCount.ShouldBe(1);
        }

        private async Task GenerateRecordsAsync(Address user)
        {
            var publisherOne = SampleAccount.Accounts[0].Address;
            await AddPublishOfferingAsync(publisherOne, 0, 1000000L, 2000000L, IDOTestConstants.TokenOneSymbol,
                IDOTestConstants.ElfTokenSymbol);
            await AddPublishOfferingAsync(publisherOne, 1, 2000000L, 1000000L, IDOTestConstants.ElfTokenSymbol, IDOTestConstants.TokenOneSymbol);
            var raiseAmount = 200L;
            var tokenAmount = 100L;
            var dateTime = DateTime.UtcNow;
            var txContext = GetEventContext(AElfChainId, blockTimestamp: DateTimeHelper.ToUnixTimeMilliseconds(dateTime));
            //record 1
            await InvestAsync(txContext, 0, user, raiseAmount, tokenAmount);
            //record 2
            await InvestAsync(txContext, 1, user, raiseAmount, tokenAmount);
            dateTime = dateTime.AddHours(1);
            txContext = GetEventContext(AElfChainId, blockTimestamp: DateTimeHelper.ToUnixTimeMilliseconds(dateTime));
            //record 3
            await InvestAsync(txContext, 0, user, raiseAmount, tokenAmount);
            dateTime = dateTime.AddHours(1);
            txContext = GetEventContext(AElfChainId, blockTimestamp: DateTimeHelper.ToUnixTimeMilliseconds(dateTime));
            //record 4
            await HarvestAsync(txContext, user, 0, 200);
        }

        private async Task AddPublishOfferingAsync(Address publisher, int pid, long tokenAmount = 1000000L,
            long raiseTokenAmount = 2000000L, string token = null, string raiseToken = null)
        {
            token ??= IDOTestConstants.TokenOneSymbol;
            raiseToken ??= IDOTestConstants.ElfTokenSymbol;
            var startTime = DateTime.UtcNow;
            var endTime = startTime.AddDays(1);
            var publicOffering = GetPublicOffering(publisher, pid, token, tokenAmount, raiseToken, raiseTokenAmount,
                startTime, endTime);
            var txContext = GetEventContext(AElfChainId);
            await AddPublicOfferingAsync(publicOffering, txContext);
        }
    }
}