namespace AwakenServer.Grains.Grain.Favorite;

public class FavoriteMessage
{
    public const int MaxLimit = 500;
    public const string NotExistMessage = "Favorite not exist.";
    public const string ExistedMessage = "Favorite already existed.";
    public const string ExceededMessage = "Favorite limit exceeded.";
}