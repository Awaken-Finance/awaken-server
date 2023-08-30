using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AwakenServer.CMS;
using AwakenServer.Favorite;
using Nest;

namespace AwakenServer.Trade;

public class TradePairListQueryBuilder
{
    private List<Func<QueryContainerDescriptor<Index.TradePair>, QueryContainer>> _mustQueries;
    private readonly ICmsAppService _cmsAppService;
    private readonly IFavoriteAppService  _favoriteAppService;

    public TradePairListQueryBuilder(ICmsAppService cmsAppService, IFavoriteAppService favoriteAppService)
    {
        _cmsAppService = cmsAppService;
        _favoriteAppService = favoriteAppService;
        _mustQueries = new List<Func<QueryContainerDescriptor<Index.TradePair>, QueryContainer>>();
    }

    public TradePairListQueryBuilder WithChainId(string chainId)
    {
        if (!string.IsNullOrWhiteSpace(chainId))
        {
            _mustQueries.Add(q => q.Term(i => i.Field(f => f.ChainId).Value(chainId)));
        }

        return this;
    }

    public TradePairListQueryBuilder WithIdList(List<Guid> idList)
    {
        if (idList?.Count > 0)
        {
            _mustQueries.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(idList)));
        }

        return this;
    }

    public TradePairListQueryBuilder WithToken0Id(Guid? token0Id)
    {
        if (token0Id != null)
        {
            _mustQueries.Add(q => q.Term(i => i.Field(f => f.Token0.Id).Value(token0Id)));
        }

        return this;
    }

    public TradePairListQueryBuilder WithToken1Id(Guid? token1Id)
    {
        if (token1Id != null)
        {
            _mustQueries.Add(q => q.Term(i => i.Field(f => f.Token1.Id).Value(token1Id)));
        }

        return this;
    }
    
    public TradePairListQueryBuilder WithToken0Symbol(string token0Symbol)
    {
        if (!string.IsNullOrWhiteSpace(token0Symbol))
        {
            _mustQueries.Add(q => q.Term(i => i.Field(f => f.Token0.Symbol).Value(token0Symbol.ToUpper())));
        }

        return this;
    }
    
    public TradePairListQueryBuilder WithToken1Symbol(string token1Symbol)
    {
        if (!string.IsNullOrWhiteSpace(token1Symbol))
        {
            _mustQueries.Add(q => q.Term(i => i.Field(f => f.Token1.Symbol).Value(token1Symbol.ToUpper())));
        }

        return this;
    }

    public TradePairListQueryBuilder WithFeeRate(double feeRate)
    {
        if (feeRate != 0)
        {
            _mustQueries.Add(q => q.Term(i => i.Field(f => f.FeeRate).Value(feeRate)));
        }

        return this;
    }
    
    public TradePairListQueryBuilder WithSearchTokenSymbol(string searchTokenSymbol)
    {
        if (!string.IsNullOrWhiteSpace(searchTokenSymbol))
        {
            _mustQueries.Add(q => q.Wildcard(t => t.Field(f => f.Token0.Symbol).Value($"*{searchTokenSymbol.ToUpper()}*"))
                                  || q.Wildcard(t => t.Field(f => f.Token1.Symbol).Value($"*{searchTokenSymbol.ToUpper()}*")));
        }

        return this;
    }
    
    public TradePairListQueryBuilder WithTokenSymbol(string tokenSymbol)
    {
        if (!string.IsNullOrWhiteSpace(tokenSymbol))
        {
            _mustQueries.Add(q => q.Term(t => t.Field(f => f.Token0.Symbol).Value(tokenSymbol))
                                  || q.Term(t => t.Field(f => f.Token1.Symbol).Value(tokenSymbol)));
        }

        return this;
    }

    public async Task<TradePairListQueryBuilder> WithTradePairFeatureAsync(string chainId, string address,
        TradePairFeature feature)
    {
        switch (feature)
        {
            case TradePairFeature.Fav:
                if (string.IsNullOrWhiteSpace(address))
                {
                    _mustQueries.Add(q => q.Term(i => i.Field(f => f.Id).Value(Guid.Empty)));
                    break;
                }
                var favoriteList = await _favoriteAppService.GetListAsync(address);
                if (favoriteList?.Count > 0)
                {
                    _mustQueries.Add(q => q.Terms(i => i.Field(f => f.Id).Terms(favoriteList.Select(x => x.TradePairId))));
                }
                else
                {
                    //if user do not have a favorite list, return a empty list
                    _mustQueries.Add(q => q.Term(i => i.Field(f => f.Id).Value(Guid.Empty)));
                }
                break;
            case TradePairFeature.OtherSymbol:
                var cmsSymbolList = await _cmsAppService.GetCmsSymbolListAsync(chainId);
                if (cmsSymbolList?.Count > 0)
                {
                    _mustQueries.Add(q => !q.Terms(i => i.Field(f => f.Token0.Symbol).Terms(cmsSymbolList))
                                          && !q.Terms(i => i.Field(f => f.Token1.Symbol).Terms(cmsSymbolList)));
                }
                break;
        }

        return this;
    }
    
    public List<Func<QueryContainerDescriptor<Index.TradePair>, QueryContainer>> Build()
    {
        return _mustQueries;
    }
}