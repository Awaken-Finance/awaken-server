namespace AwakenServer.Chains
{
    public interface IBlockchainClientFactory<T> 
        where T : class
    {
        T GetClient(string chainName);
    }
}