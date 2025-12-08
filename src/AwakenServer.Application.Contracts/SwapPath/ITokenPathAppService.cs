using System.Threading.Tasks;
using AwakenServer.SwapTokenPath.Dtos;
using Volo.Abp.Application.Dtos;
using Volo.Abp.Application.Services;

namespace AwakenServer.SwapTokenPath;

public interface ITokenPathAppService : IApplicationService
{
    Task<PagedResultDto<TokenPathDto>> GetListAsync(GetTokenPathsInput input);
}