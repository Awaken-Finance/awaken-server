using System.Collections.Generic;
using System.Threading.Tasks;

namespace AwakenServer.CMS;

public interface ICmsAppService
{
    Task<List<PinnedTokensDto>> GetCmsSymbolListAsync(string chainId);
}