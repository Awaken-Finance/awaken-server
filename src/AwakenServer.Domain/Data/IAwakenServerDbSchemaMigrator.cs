using System.Threading.Tasks;

namespace AwakenServer.Data
{
    public interface IAwakenServerDbSchemaMigrator
    {
        Task MigrateAsync();
    }
}
