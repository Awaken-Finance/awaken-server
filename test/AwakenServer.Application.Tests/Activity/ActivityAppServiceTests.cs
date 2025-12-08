using System;
using System.Text;
using System.Threading.Tasks;
using AElf;
using AElf.Types;
using AwakenServer.Activity.Dtos;
using AwakenServer.Grains.Tests;
using AwakenServer.Trade;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Xunit;
using CryptoHelper = AElf.Cryptography.CryptoHelper;

namespace AwakenServer.Activity;

[Collection(ClusterCollection.Name)]
public class ActivityAppServiceTests : TradeTestBase
{
    private readonly IActivityAppService _activityAppService;

    public ActivityAppServiceTests()
    {
        _activityAppService = GetRequiredService<IActivityAppService>();
    }

    [Fact]
    public async Task JoinTest()
    {
        var address = Address.FromPublicKey("AAA".HexToByteArray());
        var message = "Join";
        var keyPair = CryptoHelper.GenerateKeyPair();
        var messageHash = ByteExtensions.ToHex(Encoding.UTF8.GetBytes(message));
        var sign = ByteExtensions.ToHex(CryptoHelper.SignWithPrivateKey(keyPair.PrivateKey, HashHelper.ComputeFrom(messageHash).ToByteArray()));
        
        var joinStatusDto = await _activityAppService.GetJoinStatusAsync(new GetJoinStatusInput()
        {
            ActivityId = 1,
            Address = address.ToBase58()
        });
        joinStatusDto.Status.ShouldBe(0);
        joinStatusDto.NumberOfJoin.ShouldBe(0);

        await _activityAppService.JoinAsync(new JoinInput()
        {
            Message = message,
            Signature = sign,
            PublicKey = ByteExtensions.ToHex(keyPair.PublicKey),
            Address = address.ToBase58(),
            ActivityId = 1
        });
        await Task.Delay(3000);
        joinStatusDto = await _activityAppService.GetJoinStatusAsync(new GetJoinStatusInput()
        {
            ActivityId = 1,
            Address = address.ToBase58()
        });
        joinStatusDto.Status.ShouldBe(1);
        joinStatusDto.NumberOfJoin.ShouldBe(1);
    }

    [Fact]
    public async Task MyRankingTest()
    {
        var address = Address.FromPublicKey("AAA".HexToByteArray());

        try
        {
            await _activityAppService.GetMyRankingAsync(new GetMyRankingInput()
            {
                Address = address.ToBase58(),
                ActivityId = 3
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("Activity not existed");
        }
        
        try
        {
            await _activityAppService.GetRankingListAsync(new ActivityBaseDto()
            {
                ActivityId = 3
            });
        }
        catch (Exception e)
        {
            e.Message.ShouldBe("Activity not existed");
        }

        var myRankingDto = await _activityAppService.GetMyRankingAsync(new GetMyRankingInput()
        {
            Address = address.ToBase58(),
            ActivityId = 1
        });
        myRankingDto.Ranking.ShouldBe(0);
        myRankingDto.TotalPoint.ShouldBe("0");
    }
}