using System.Threading.Tasks;
using Volo.Abp.DependencyInjection;

namespace AwakenServer.Data
{
    /* This is used if database provider does't define
     * IAwakenServerDbSchemaMigrator implementation.
     */
    public class NullAwakenServerDbSchemaMigrator : IAwakenServerDbSchemaMigrator, ITransientDependency
    {
        public Task MigrateAsync()
        {
            return Task.CompletedTask;
        }
    }
}