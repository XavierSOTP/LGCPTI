using LanguageExt.Pipes;
using LGC.PTI.Domain.Common;
using LGC.PTI.HTTPClient;
using LGC.PTI.Infrastructure;
using LGC.PTI.Refit.Authentication;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace LGC.PTI.RestApi.Test
{
    internal sealed class RegisterTests
    {
        public ILogger Logger = AssemblyInitializer.Logger;
        public IKernel NinjectContainer = AssemblyInitializer.NinjectKernel;

        [OneTimeSetUp]
        public void SetUp()
        {
            AuthenticationStore = NinjectContainer.Get<IAuthCredentialStore>();
            AuthSrv = NinjectContainer.Get<IAuthenticationSrv>();

            AuthenticationStore.RegisterToCredentialsChanged(nameof(AuthenticationSrv), AuthSrv.CredentialsChangedAsync);
            AuthenticationStore.TokenReceived += AuthenticationStore_TokenReceived;
        }

        private IAuthCredentialStore AuthenticationStore { get; set; }
        private IAuthenticationSrv AuthSrv { get; set; }

        [Test]
        public async Task EventIsReceived_WhenClientIdlsChanged()
        {
            string clientId = "QSL - PTI";

            AuthenticationStore.UpdateClientId(clientId);
            await Task.Delay(TimeSpan.FromSeconds(20));
        }

        [Test]
        public async Task EventIsReceived_WhenClientSecretlsChanged()
        {
            string clientSecret = "rCrr19";

            AuthenticationStore.UpdateClientSecret(clientSecret);
            await Task.Delay(TimeSpan.FromSeconds(20));
        }

        [Test]
        public async Task EventIsReceived_WhenTokenUrllsChanged()
        {
            string tokenUrl = "https://account.iqtechportal.com/connect";

            AuthenticationStore.UpdateTokenUrl(tokenUrl);
            await Task.Delay(TimeSpan.FromSeconds(20));
        }

        [Test]
        public async Task EventIsReceived_WhenAllTokenCredentialsChanged()
        {
            string tokenUrl = "https://account.iqtechportal.com/connect";

            string clientId = "QSL - PTI";
            string clientSecret = "rCrr19";

            AuthenticationStore.UpdateClientId(clientId);
            AuthenticationStore.UpdateClientSecret(clientSecret);
            AuthenticationStore.UpdateTokenUrl(tokenUrl);
            await Task.Delay(TimeSpan.FromSeconds(40));
        }

        [Test]
        public async Task TokenIsUpdated_WhenClientIdIsChangedWithEmptyToken()
        {
            string clientId = "QSL - PTI";
            var token = string.Empty;

            await CheckToken(token);
            Logger.Info(AuthenticationStore.TryGetToken());
            AuthenticationStore.UpdateClientId(clientId);
            await Task.Delay(TimeSpan.FromSeconds(20));

            Logger.Info(AuthenticationStore.TryGetToken());
        }

        private void AuthenticationStore_TokenReceived(object sender, EventArgs e)
        {
            Logger.Info("Event received");
        }

        private async Task CheckToken(string token)
        {
            AuthenticationStore.UpdateToken(token);
            await Task.Delay(TimeSpan.FromSeconds(20));
            Logger.Info(AuthenticationStore.TryGetToken().IfNone("No Token"));
            AuthenticationStore.TryGetToken().IfNone(() => Assert.Fail("Not a valid token"));
            AuthenticationStore.TryGetToken().IfSome(t => t.Should().BeEquivalentTo(token));
        }
    }
}