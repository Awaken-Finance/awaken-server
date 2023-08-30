namespace AwakenServer.Grains;

public static class GrainIdHelper
{
    public static string GenerateGrainId(params object[] ids)
    {
        return ids.JoinAsString("-");
    }
    
    public static string[] SplitByLastSeparator(String id)
    {
        if (string.IsNullOrWhiteSpace(id) || !id.Contains("-"))
        {
            return null;
        }

        var split = new string[2];
        split[0] = id.Substring(0, id.LastIndexOf("-"));
        split[1] = id.Substring(id.LastIndexOf("-") + 1);
        return split;
    }
}