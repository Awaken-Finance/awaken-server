using AElf.Types;
using AwakenServer.Signature.Options;
using Nethereum.Hex.HexConvertors.Extensions;
using Shouldly;
using Xunit;

namespace AwakenServer.Signature.Provider
{
    public class SignatureProviderTests : AwakenServerSignatureTestBase
    {

        private readonly ISignatureProvider _signatureProvider;
        
        public SignatureProviderTests()
        {
            _signatureProvider = GetRequiredService<ISignatureProvider>();
        }

        [Fact]
        public async void SignTxMsgTest()
        {
            var ownAddress = Address.FromPublicKey("AAA".HexToByteArray()).ToBase58();
            var transactionId = "0x1";
            var result = await _signatureProvider.SignTxMsg(ownAddress, transactionId);
            result.ShouldBe("123");
        }

    }
}