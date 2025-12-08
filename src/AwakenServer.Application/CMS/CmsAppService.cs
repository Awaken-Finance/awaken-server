using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AElf.Client.Service;
using Microsoft.Extensions.Options;
using Serilog;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.CMS;

public class CmsAppService : AwakenServerAppService, ICmsAppService, ISingletonDependency
{
    private readonly int _updateCmsSymbolListIntervalMs;
    private const string PinnedTokensUrl = "items/pinned_tokens";
    private static readonly Dictionary<string, List<PinnedTokensDto>> PinnedTokens = new();
    private static DateTimeOffset _lastUpdateCmsSymbolListTime = DateTimeOffset.MinValue;
    private readonly CmsOptions _cmsOptions;
    private readonly IHttpService _httpService;
    private readonly ILogger _logger;
    private readonly object _lockObject = new();

    public CmsAppService(IHttpService httpService, IOptions<CmsOptions> cmsOptions)
    {
        _httpService = httpService;
        _cmsOptions = cmsOptions.Value;
        _logger = Log.ForContext<CmsAppService>();

        _updateCmsSymbolListIntervalMs = _cmsOptions.CmsLoopIntervalMs > 0 ? _cmsOptions.CmsLoopIntervalMs : CmsConst.CmsLoopIntervalMs;
    }

    public async Task<List<PinnedTokensDto>> GetCmsSymbolListAsync(string chainId)
    {
        if (DateTimeOffset.UtcNow.Subtract(_lastUpdateCmsSymbolListTime) >
            TimeSpan.FromMilliseconds(_updateCmsSymbolListIntervalMs))
        {
            _lastUpdateCmsSymbolListTime = DateTimeOffset.UtcNow;
            
            var url = _cmsOptions.CmsAddress + PinnedTokensUrl;
            var response = await _httpService.GetResponseAsync<CmsResponseDto<List<PinnedTokensDto>>>(url);
            if (response?.Data?.Count > 0)
            {
                UpdateCmsSymbol(response.Data);
            }
        }

        return GetPinnedTokens(chainId);
    }

    private List<PinnedTokensDto> GetPinnedTokens(string chainId)
    {
        lock (_lockObject)
        {
            if (PinnedTokens.TryGetValue(chainId, out var list))
            {
                return list;
            }
        }

        return null;
    }

    private void UpdateCmsSymbol(List<PinnedTokensDto> pinnedTokensDtos)
    {
        lock (_lockObject)
        {
            PinnedTokens.Clear();
            foreach (var pinnedTokensDto in pinnedTokensDtos)
            {
                if (PinnedTokens.TryGetValue(pinnedTokensDto.ChainId, out var list))
                {
                    list.Add(pinnedTokensDto);
                }
                else
                {
                    PinnedTokens.Add(pinnedTokensDto.ChainId, new List<PinnedTokensDto> { pinnedTokensDto });
                }
            }
            _logger.Information("Update cms symbol list success.");
            foreach (var keyValuePair in PinnedTokens)
            {
                _logger.Information("ChainId: {chainId}, Symbol: {symbol}", keyValuePair.Key, string.Join(",", keyValuePair.Value));
            }
        }
    }
}