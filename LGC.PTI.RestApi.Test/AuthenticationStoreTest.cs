using LGC.PTI.Domain.Common;

namespace LGC.PTI.RestApi.Test
{
    internal sealed class AuthenticationStoreTest
    {
        #region Public Fields

        public ILogger Logger = AssemblyInitializer.Logger;

        public IAuthCredentialStore AuthCredentialStore { get; private set; }

        [Test]
        public async Task TokenShouldBeReseted_WhenTokenUrlIsChanged()
        {
            var tokenUrl = "url.com";
            var token = "my token";

            CheckToken(token);

            AuthCredentialStore.UpdateTokenUrl(tokenUrl);

            await Task.Delay(TimeSpan.FromSeconds(15));

            AuthCredentialStore.TokenUrl.Should().BeEquivalentTo(tokenUrl);
            AuthCredentialStore.TryGetToken().IsNone.Should().BeTrue();
        }

        [Test]
        public async Task TokenShouldBeReseted_WhenClientIdIsChanged()
        {
            var clientId = "Kendrick";
            var token = "my token";

            CheckToken(token);
            AuthCredentialStore.UpdateClientId(clientId);
            await Task.Delay(TimeSpan.FromSeconds(15));

            AuthCredentialStore.ClientId.Should().BeEquivalentTo(clientId);
            AuthCredentialStore.TryGetToken().IsNone.Should().BeTrue();
        }

        [Test]
        public async Task TokenShouldBeReseted_WhenClientSecretIsChanged()
        {
            var clientSecret = "password";
            var token = "my token";

            CheckToken(token);

            AuthCredentialStore.UpdateClientSecret(clientSecret);
            await Task.Delay(TimeSpan.FromSeconds(15));

            AuthCredentialStore.ClientSecret.Should().BeEquivalentTo(clientSecret);
            AuthCredentialStore.TryGetToken().IsNone.Should().BeTrue();
        }

        [OneTimeSetUp]
        public void Setup()
        {
            AuthCredentialStore = AssemblyInitializer.NinjectKernel.Get<IAuthCredentialStore>();
        }

        #endregion Public Fields

        #region Private Methods

        private void CheckToken(string token)
        {
            AuthCredentialStore.UpdateToken(token);
            Logger.Info(AuthCredentialStore.TryGetToken().IfNone("No Token"));
            AuthCredentialStore.TryGetToken().IfNone(() => Assert.Fail("Not a valid token"));
            AuthCredentialStore.TryGetToken().IfSome(t => t.Should().BeEquivalentTo(token));
        }

        #endregion Private Methods
    }
}