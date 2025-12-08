using Volo.Abp.Data;
using Volo.Abp.MongoDB;

namespace AwakenServer.EntityFrameworkCore
{
    [ConnectionStringName("Default")]
    public class AwakenServerDbContext : AbpMongoDbContext
    {
        
    }
}