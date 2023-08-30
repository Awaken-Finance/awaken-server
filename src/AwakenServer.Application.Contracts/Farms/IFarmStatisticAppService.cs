using System.Threading.Tasks;

namespace AwakenServer.Farms
{
    public interface IFarmStatisticAppService
    {
        Task<TotalPoolStatistics> GetPoolsStatisticInfo(GetPoolsTotalStatisticInput input);
        Task<TotalUserStatistics> GetUsersStatisticInfo(GetUsersTotalStatisticInput input);
    }
}