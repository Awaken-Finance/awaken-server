using System;
using System.Linq;
using System.Threading.Tasks;
using Shouldly;
using Volo.Abp;
using Volo.Abp.Validation;
using Xunit;

namespace AwakenServer.Favorite;

public class FavoriteTests : AwakenServerApplicationTestBase
{
    private Guid TradePairId = Guid.NewGuid();
    private const string Address = "DefaultAddress";

    private readonly IFavoriteAppService _favoriteAppService;

    public FavoriteTests()
    {
        _favoriteAppService = GetRequiredService<IFavoriteAppService>();
    }

    [Fact]
    public async Task Favorite_Success_Test()
    {
        var dto = new FavoriteCreateDto
        {
            TradePairId = Guid.Empty,
            Address = ""
        };

        //create
        var exception = await Assert.ThrowsAsync<AbpValidationException>(async () => await _favoriteAppService.CreateAsync(dto));
        //exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem.Contains("TradePairId")));
        exception.ValidationErrors.ShouldContain(err => err.MemberNames.Any(mem => mem.Contains("Address")));

        dto = new FavoriteCreateDto
        {
            TradePairId = TradePairId,
            Address = Address
        };
        var result = await _favoriteAppService.CreateAsync(dto);
        result.ShouldNotBeNull();
        result.TradePairId.ShouldBe(TradePairId);
        result.Address.ShouldBe(Address);
        
        var existed = await Assert.ThrowsAsync<UserFriendlyException>(async () => await _favoriteAppService.CreateAsync(dto));
        existed.Message.ShouldContain("Favorite already existed");
        
        //getList
        var list = await _favoriteAppService.GetListAsync(dto.Address);
        list.Count.ShouldBe(1);
        
        //delete
        var invalid = await Assert.ThrowsAsync<UserFriendlyException>(async () => await _favoriteAppService.DeleteAsync(""));
        invalid.Message.ShouldContain("Invalid id");
        invalid = await Assert.ThrowsAsync<UserFriendlyException>(async () => await _favoriteAppService.DeleteAsync("-"));
        invalid.Message.ShouldContain("Invalid id");
        invalid = await Assert.ThrowsAsync<UserFriendlyException>(async () => await _favoriteAppService.DeleteAsync("a-b"));
        invalid.Message.ShouldContain("Favorite not exist");
        await _favoriteAppService.DeleteAsync(TradePairId + "-" + Address);
    }
}