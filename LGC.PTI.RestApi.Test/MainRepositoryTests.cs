using LGC.PTI.Domain.Models;
using LGC.PTI.Infrastructure;
using SoftSolute.Common.Utilities.Tasks;
using System;

namespace LGC.PTI.RestApi.Test
{
    internal sealed class MainRepositoryTests
    {
        #region Public Fields

        public ILogger Logger = AssemblyInitializer.Logger;

        public IMainRepository Repository { get; private set; }

        [Test]
        public async Task ClientSecretExistInTheMainDB_WhenTryingToGetClientSecret()
        {
            var clientSecret = "Password";
            Logger.Info(Repository.MainLiteDBContext.ConnectionString.Filename);

            await Repository.UpdateClientSecretAsync(clientSecret);

            var result = await Repository.TryGetClientSecretAsync();

            result.IfSucc(s => s.Should().BeEquivalentTo(clientSecret));
            result.IfFail(Logger.Error);
            result.IfFail(_ => Assert.Fail());
        }

        [Test]
        public async Task ClientSecretIsEmptyTheMainDB_WhenTryingToGetEmptyClientSecret()
        {
            var result = await Repository.TryGetClientSecretAsync();

            var savedPassword = result.IfFailOrNone(string.Empty);
            result.IfSucc(_ => Assert.Fail());
            savedPassword.Should().BeEmpty();
        }

        [OneTimeSetUp]
        public void Setup()
        {
            Repository = AssemblyInitializer.NinjectKernel.Get<IMainRepository>();
        }

        #endregion Public Fields
    }
}