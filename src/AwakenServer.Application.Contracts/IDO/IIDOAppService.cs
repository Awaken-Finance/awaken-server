using System.Threading.Tasks;
using AwakenServer.IDO.Dtos;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.IDO
{
    public interface IIdoAppService
    {
        Task<PagedResultDto<PublicOfferingDto>> GetPublicOfferingsAsync(GetPublicOfferingInput input);
        Task<PagedResultDto<UserPublicOfferingDto>> GetUserPublicOfferingsAsync(GetUserPublicOfferingInfoInput input);
        Task<UserAssetDto> GetUserPublicOfferingsAssetAsync(GetUserAssetInput input);
        Task<PublicOfferingAssetDto> GetPublicOfferingsTokensAsync(GetAssetTokenInfoInput input);
        Task<PagedResultDto<PublicOfferingRecordDto>> GetPublicOfferingRecordsAsync(GetUserRecordInput input);
    }
}