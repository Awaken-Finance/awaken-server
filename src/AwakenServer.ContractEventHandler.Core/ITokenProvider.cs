using System;
using System.Threading.Tasks;
using AwakenServer.Chains;
using AwakenServer.Tokens;
using Volo.Abp.DependencyInjection;
using Volo.Abp.ObjectMapping;
using Volo.Abp.Threading;

namespace AwakenServer.ContractEventHandler
{
    public interface ITokenProvider
    {
        Task<TokenDto> GetOrAddTokenAsync(string chainId, string chainName, string address, string symbol = null);
        Token GetToken(Guid tokenId);
    }

    public class TokenProvider: ITokenProvider, ISingletonDependency
    {
        private readonly ITokenAppService _tokenAppService;
        private readonly IBlockchainAppService _blockchainAppService;
        private readonly IObjectMapper _objectMapper;
        
        public TokenProvider(ITokenAppService tokenAppService, IBlockchainAppService blockchainAppService, IObjectMapper objectMapper)
        {
            _tokenAppService = tokenAppService;
            _blockchainAppService = blockchainAppService;
            _objectMapper = objectMapper;
        }
        
        public async Task<TokenDto> GetOrAddTokenAsync(string chainId, string chainName, string address, string symbol = null)
        {
            var token = await _tokenAppService.GetAsync(new GetTokenInput
            {
                ChainId = chainId,
                Address =  address,
                Symbol = symbol
            });
            if (token == null)
            {
                var tokenInfo = await _blockchainAppService.GetTokenInfoAsync(chainName, address, symbol);

                token = await _tokenAppService.CreateAsync(new TokenCreateDto
                {
                    Address = tokenInfo.Address,
                    Decimals = tokenInfo.Decimals,
                    Symbol = tokenInfo.Symbol,
                    ChainId = chainId
                });
            }

            return token;
        }

        public Token GetToken(Guid tokenId)
        {
            var tokenDto = AsyncHelper.RunSync(async () => await _tokenAppService.GetAsync(tokenId));
            return _objectMapper.Map<TokenDto, Token>(tokenDto);
        }
    }
}