using System;
using System.Numerics;
using System.Threading.Tasks;
using AElf.EthereumNode.EventHandler.BackgroundJob.DTO;
using AElf.EthereumNode.EventHandler.BackgroundJob.Processors;
using AElf.EthereumNode.EventHandler.Core.Enums;
using AwakenServer.Chains;
using AwakenServer.ContractEventHandler.Price.Dtos;
using AwakenServer.Price;
using AwakenServer.Tokens;
using AwakenServer.Web3;
using Nethereum.Contracts;
using Nethereum.Util;
using Volo.Abp.Domain.Repositories;

namespace AwakenServer.ContractEventHandler.Price.Processors
{
    public class OtherLpSyncEventProcessor : EthereumEthereumEventProcessorBase<SyncEventDto>
    {
        private readonly IChainAppService _chainAppService;
        private readonly ITokenAppService _tokenAppService;
        private readonly IOtherLpTokenRepository _otherLpTokenRepository;
        private readonly IWeb3Provider _web3Provider;

        public OtherLpSyncEventProcessor(ITokenAppService tokenAppService, IChainAppService chainAppService,
            IWeb3Provider web3Provider, IOtherLpTokenRepository otherLpTokenRepository)
        {
            _tokenAppService = tokenAppService;
            _chainAppService = chainAppService;
            _web3Provider = web3Provider;
            _otherLpTokenRepository = otherLpTokenRepository;
        }

        protected override async Task HandleEventAsync(SyncEventDto eventDetailsDto,
            ContractEventDetailsDto contractEventDetailsDto)
        {
            if (contractEventDetailsDto.StatusEnum != ContractEventStatus.Confirmed)
            {
                return;
            }

            var nodeName = contractEventDetailsDto.NodeName;
            var chain = await _chainAppService.GetByNameCacheAsync(nodeName);
            var otherLpToken = await _otherLpTokenRepository.FirstOrDefaultAsync(o=>o.ChainId == chain.Id && o.Address == contractEventDetailsDto.Address);
            if (otherLpToken == null)
            {
                var token0 = await GetOrAddTokenAsync<Token0Function>(chain.Id, nodeName, contractEventDetailsDto.Address);
                var token1 = await GetOrAddTokenAsync<Token1Function>(chain.Id, nodeName, contractEventDetailsDto.Address);
                var reserve0 = (BigDecimal) eventDetailsDto.Reserve0 / BigInteger.Pow(10, token0.Decimals);
                var reserve1 = (BigDecimal) eventDetailsDto.Reserve1 / BigInteger.Pow(10, token1.Decimals);
                otherLpToken = new OtherLpToken
                {
                    ChainId = chain.Id,
                    Token0Id = token0.Id,
                    Token1Id = token1.Id,
                    Address = contractEventDetailsDto.Address,
                    Reserve0 = reserve0.ToString(),
                    Reserve0Value = (double) reserve0,
                    Reserve1 = reserve1.ToString(),
                    Reserve1Value = (double) reserve1
                };
                await _otherLpTokenRepository.InsertAsync(otherLpToken);
            }
            else
            {
                var token0 = await _tokenAppService.GetAsync(otherLpToken.Token0Id);
                var token1 = await _tokenAppService.GetAsync(otherLpToken.Token1Id);
                var reserve0 = (BigDecimal) eventDetailsDto.Reserve0 / BigInteger.Pow(10, token0.Decimals);
                var reserve1 = (BigDecimal) eventDetailsDto.Reserve1 / BigInteger.Pow(10, token1.Decimals);
                otherLpToken.Reserve0 = reserve0.ToString();
                otherLpToken.Reserve0Value = (double) reserve0;
                otherLpToken.Reserve1 = reserve1.ToString();
                otherLpToken.Reserve1Value = (double) reserve1;
                await _otherLpTokenRepository.UpdateAsync(otherLpToken);
            }
        }
        
        private async Task<TokenDto> GetOrAddTokenAsync<T>(string chainId, string chainName, string address)
            where T : FunctionMessage, new()
        {
            var tokenAddress = await _web3Provider.QueryAsync<T, string>(chainName, address);
            var token = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Address = tokenAddress
            });
            if (token == null)
            {
                var tokenDto = await _web3Provider.GetTokenInfoAsync(chainName, tokenAddress);
                token = await _tokenAppService.CreateAsync(new TokenCreateDto
                {
                    Address = tokenAddress,
                    Decimals = tokenDto.Decimals,
                    Symbol = tokenDto.Symbol,
                    ChainId = chainId
                });
            }

            return token;
        }
    }
}