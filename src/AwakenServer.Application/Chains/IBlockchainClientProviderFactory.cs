namespace AwakenServer.Chains
{
    public interface IBlockchainClientProviderFactory
    {
        IBlockchainClientProvider GetBlockChainClientProvider(string chainName);
    }
}