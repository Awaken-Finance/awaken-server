using AElf.Types;
using AwakenServer.Signature.Options;
using Nethereum.Hex.HexConvertors.Extensions;
using Xunit;

namespace AwakenServer.Signature.Provider
{
    public class SignatureServerOptionsTests : AwakenServerSignatureTestBase
    {
        
        [Fact]
        public void SignatureServerOptions_ShouldHaveDefaultValues()
        {
            var options = new SignatureServerOptions();
        
            Assert.Null(options.BaseUrl);
            Assert.Null(options.AppId);
            Assert.Null(options.AppSecret);
            Assert.Equal(60, options.SecretCacheSeconds);
            Assert.NotNull(options.KeyIds);
        }

        [Fact]
        public void SignatureServerOptions_ShouldAllowPropertiesToBeSet()
        {
            var options = new SignatureServerOptions();
            var keyIds = new KeyIds();

            options.BaseUrl = "https://example.com";
            options.AppId = "TestAppId";
            options.AppSecret = "TestAppSecret";
            options.SecretCacheSeconds = 120;
            options.KeyIds = keyIds;

            // Assert
            Assert.Equal("https://example.com", options.BaseUrl);
            Assert.Equal("TestAppId", options.AppId);
            Assert.Equal("TestAppSecret", options.AppSecret);
            Assert.Equal(120, options.SecretCacheSeconds);
            Assert.Equal(keyIds, options.KeyIds);
        }

    }
}