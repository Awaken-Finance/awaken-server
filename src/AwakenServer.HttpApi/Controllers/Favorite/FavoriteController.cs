using System.Threading.Tasks;
using Asp.Versioning;
using AwakenServer.Favorite;
using Microsoft.AspNetCore.Mvc;
using Volo.Abp;
using Volo.Abp.AspNetCore.Mvc;

namespace AwakenServer.Controllers.Favorite
{
    [RemoteService]
    [Area("app")]
    [ControllerName("Favorite")]
    [Route("api/app/favs")]
    public class FavoriteController : AbpController
    {
        private readonly IFavoriteAppService _favoriteAppService;

        public FavoriteController(IFavoriteAppService favoriteAppService)
        {
            _favoriteAppService = favoriteAppService;
        }
        
        [HttpPost]
        public async Task<FavoriteDto> CreateAsync(FavoriteCreateDto input)
        {
            return await _favoriteAppService.CreateAsync(input);
        }
        
        [HttpDelete]
        [Route("{id}")]
        public async Task DeleteAsync(string id)
        {
            await _favoriteAppService.DeleteAsync(id);
        }
    }
}