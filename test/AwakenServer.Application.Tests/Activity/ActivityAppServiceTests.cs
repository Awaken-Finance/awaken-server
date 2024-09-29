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
        // var message = "Join";
        var message =
            "Welcome to ETransfer!Click to sign in and accept the ETransfer Terms of Service (https://etransfer.gitbook.io/docs/more-information/terms-of-service) and Privacy Policy (https://etransfer.gitbook.io/docs/more-information/privacy-policy).This request will not trigger a blockchain transaction or cost any gas fees.Nonce:1727580775893";
        var keyPair = CryptoHelper.GenerateKeyPair();
        var privateKey = "45339b89430250b44a9cf6bae3e0254760d980456999b884e385196db2c72599";
        var publicKey =
            "04ccd3e4705a446f176d4829d076e6191a4c263524dbb812569166fbc107ae47a6ada236a5fa80e24793804cc6721a3b6830aa4c6297ffb6cf56fa2c0218352742";
        var sign = CryptoHelper.SignWithPrivateKey(privateKey.HexToByteArray(), HashHelper.ComputeFrom(message).ToByteArray());
        
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
            Signature = ByteExtensions.ToHex(sign),
            PublicKey = publicKey,
            // PublicKey = ByteExtensions.ToHex(keyPair.PublicKey),
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

}