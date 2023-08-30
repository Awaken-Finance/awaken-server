using System.Collections.Generic;
using System.Threading.Tasks;

namespace AwakenServer.Favorite
{
    public interface IFavoriteAppService
    {
        Task<FavoriteDto> CreateAsync(FavoriteCreateDto input);

        Task DeleteAsync(string id);

        Task<List<FavoriteDto>> GetListAsync(string address);
    }
}