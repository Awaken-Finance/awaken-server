namespace AwakenServer.GameOfTrust
{
    public enum BehaviorType
    {
        Deposit = 1,  // deposit token
        Withdraw, // withdraw token
        Harvest,  // harvest unlock token
        Reward    // receive fine liquidate reward
    }
}