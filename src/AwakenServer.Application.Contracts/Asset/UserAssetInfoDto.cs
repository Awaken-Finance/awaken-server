using System.Collections.Generic;
using Volo.Abp.Application.Dtos;

namespace AwakenServer.Asset;

public class UserAssetInfoDto
{
    public List<UserTokenInfo> ShowList { get; set; }
    public List<UserTokenInfo> HiddenList { get; set; }
}

public class UserTokenInfo : UserTokenDto
{
    public string Amount { get; set; }
    public string PriceInUsd { get; set; }
}