using System.Data;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AElf.EthereumNode.EventHandler.TestBase;
using AwakenServer.ContractEventHandler.Price.Dtos;
using AwakenServer.Price.Dtos;
using AwakenServer.Tokens;
using Nethereum.Util;
using Shouldly;
using Volo.Abp.Uow;
using Xunit;

namespace AwakenServer.Price
{
    public sealed class SyncEventProcessorTest : PriceProcessorTestBase
    {
        private readonly IOtherLpTokenRepository _otherLpTokenRepository;
        private readonly IOtherLpTokenAppService _otherLpTokenAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly IUnitOfWorkManager _unitOfWorkManager;
        private readonly IEventHandlerTestProcessor<SyncEventDto> _syncEventProcessor;

        public SyncEventProcessorTest()
        {
            _otherLpTokenRepository = GetRequiredService<IOtherLpTokenRepository>();
            _tokenAppService = GetRequiredService<ITokenAppService>();
            _unitOfWorkManager = GetRequiredService<IUnitOfWorkManager>();
            _otherLpTokenAppService = GetRequiredService<IOtherLpTokenAppService>();
            _syncEventProcessor = GetRequiredService<IEventHandlerTestProcessor<SyncEventDto>>();
        }

        [Fact(Skip = "no need")]
        public async Task SyncEventTest()
        {
            //using var uow = _unitOfWorkManager.Begin(true, true, IsolationLevel.ReadCommitted);
            const string otherLpTokenAddress = "0xOtherLpToken";

            var syncEventDto = new SyncEventDto
            {
                Reserve0 = 6000000000000000000,
                Reserve1 = 300000000000000000
            };
            await SyncEventAsync(syncEventDto, otherLpTokenAddress, ContractEventStatus.Unconfirmed);
            var otherLpTokenDto = await _otherLpTokenAppService.GetByAddressAsync(ChainId, otherLpTokenAddress);
            otherLpTokenDto.ShouldBeNull();

            await SyncEventAsync(syncEventDto, otherLpTokenAddress);
            otherLpTokenDto = await _otherLpTokenAppService.GetByAddressAsync(ChainId, otherLpTokenAddress);
            otherLpTokenDto.Address.ShouldBe(otherLpTokenAddress);
            otherLpTokenDto.ChainId.ShouldBe(ChainId);
            await CheckOtherLpToken(otherLpTokenDto, syncEventDto);


            syncEventDto.Reserve0++;
            syncEventDto.Reserve1++;
            await SyncEventAsync(syncEventDto, otherLpTokenAddress);
            var newOtherLpTokenDto = await _otherLpTokenAppService.GetByAddressAsync(ChainId, otherLpTokenAddress);
            newOtherLpTokenDto.Address.ShouldBe(otherLpTokenAddress);
            newOtherLpTokenDto.ChainId.ShouldBe(ChainId);
            await CheckOtherLpToken(newOtherLpTokenDto, syncEventDto);

            const string newOtherLpTokenAddress = "0xNewOtherLpToken";

            syncEventDto.Reserve0++;
            syncEventDto.Reserve1++;
            await _syncEventProcessor.HandleEventAsync(syncEventDto,
                GetDefaultEventContext(newOtherLpTokenAddress, confirmStatus: ContractEventStatus.Confirmed));

            var newOtherLpToken = await _otherLpTokenAppService.GetByAddressAsync(ChainId, newOtherLpTokenAddress);
            newOtherLpToken.Address.ShouldBe(newOtherLpTokenAddress);
            newOtherLpToken.ChainId.ShouldBe(ChainId);
            await CheckOtherLpToken(newOtherLpToken, syncEventDto);
        }

        private async Task CheckOtherLpToken(OtherLpTokenDto otherLpToken, SyncEventDto syncEventDto)
        {
            var token0 = await _tokenAppService.GetAsync(otherLpToken.Token0Id);
            var token1 = await _tokenAppService.GetAsync(otherLpToken.Token0Id);
            var reserve0 = (BigDecimal) syncEventDto.Reserve0 / BigInteger.Pow(10, token0.Decimals);
            var reserve1 = (BigDecimal) syncEventDto.Reserve1 / BigInteger.Pow(10, token1.Decimals);
            otherLpToken.Reserve0.ShouldBe(reserve0.ToString());
            otherLpToken.Reserve1.ShouldBe(reserve1.ToString());
            otherLpToken.Reserve0Value.ShouldBe((double)reserve0);
            otherLpToken.Reserve1Value.ShouldBe((double)reserve1);
        }

        private async Task SyncEventAsync(SyncEventDto syncEventDto, string otherLpTokenAddress, ContractEventStatus confirmStatus = ContractEventStatus.Confirmed)
        {
            using var uow = _unitOfWorkManager.Begin(true, true, IsolationLevel.ReadCommitted);
            await _syncEventProcessor.HandleEventAsync(syncEventDto,
                GetDefaultEventContext(otherLpTokenAddress, confirmStatus: confirmStatus));
            await uow.CompleteAsync();
        }
    }
}