using System;
using System.Threading.Tasks;
using AElf.AElfNode.EventHandler.BackgroundJob;
using AElf.AElfNode.EventHandler.BackgroundJob.Processors;
using AwakenServer.ContractEventHandler.Dividend.AElf.Services;
using AwakenServer.ContractEventHandler.Helpers;
using AwakenServer.Dividend;
using AwakenServer.Dividend.Entities.Ef;
using Awaken.Contracts.DividendPoolContract;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Dividend.AElf.Processors
{
    public class HarvestProcessor : AElfEventProcessorBase<Harvest>
    {
        private readonly IDividendCacheService _dividendCacheService;
        private readonly IRepository<DividendUserToken> _dividendUserTokenRepository;
        private readonly ITokenProvider _tokenProvider;
        private readonly IRepository<DividendUserRecord> _recordRepository;
        private readonly string _processorName = "DividendHarvestProcessor";

        public HarvestProcessor(
            IRepository<DividendUserToken> dividendUserTokenRepository,
            IDividendCacheService dividendCacheService,
            IRepository<DividendUserRecord> recordRepository, ITokenProvider tokenProvider)
        {
            _dividendUserTokenRepository = dividendUserTokenRepository;
            _dividendCacheService = dividendCacheService;
            _recordRepository = recordRepository;
            _tokenProvider = tokenProvider;
        }
        
        public override string GetProcessorName()
        {
            return _processorName;
        }

        protected override async Task HandleEventAsync(Harvest eventDetailsEto, EventContext txInfoDto)
        {
            var chain = await _dividendCacheService.GetCachedChainAsync(txInfoDto.ChainId);
            var dividendBaseInfo =
                await _dividendCacheService.GetDividendBaseInfoAsync(chain.Id, txInfoDto.EventAddress);
            var pool = await _dividendCacheService.GetDividendPoolBaseInfoAsync(dividendBaseInfo.Id,
                eventDetailsEto.Pid);
            var token = await _tokenProvider.GetOrAddTokenAsync(chain.Id, chain.Name, null, eventDetailsEto.Token);
            var user = eventDetailsEto.To.ToBase58();

            // add harvest record
            await AddHarvestRecordAsync(user, eventDetailsEto.Amount.Value, chain.Id, pool.Id, token.Id,
                txInfoDto);

            var userInfo =
                await _dividendUserTokenRepository.FindAsync(x =>
                    x.User == user && x.PoolId == pool.Id && x.DividendTokenId == token.Id);
            if (userInfo == null)
            {
                await _dividendUserTokenRepository.InsertAsync(new DividendUserToken
                {
                    ChainId = chain.Id,
                    User = user,
                    AccumulativeDividend = eventDetailsEto.Amount.Value,
                    PoolId = pool.Id,
                    DividendTokenId = token.Id
                });
                return;
            }

            userInfo.AccumulativeDividend =
                CalculationHelper.Add(userInfo.AccumulativeDividend, eventDetailsEto.Amount.Value);
            await _dividendUserTokenRepository.UpdateAsync(userInfo);
        }

        private async Task AddHarvestRecordAsync(string user, string amount, string chainId, Guid poolId, Guid tokenId,
            EventContext txInfoDto)
        {
            await _recordRepository.InsertAsync(new DividendUserRecord
            {
                ChainId = chainId,
                TransactionHash = txInfoDto.TransactionId,
                User = user,
                DateTime = txInfoDto.BlockTime,
                Amount = amount,
                BehaviorType = BehaviorType.Harvest,
                PoolId = poolId,
                DividendTokenId = tokenId
            });
        }
    }
}