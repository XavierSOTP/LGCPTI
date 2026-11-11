using LGC.PTI.Domain.Models;
using LGC.PTI.Infrastructure;

namespace LGC.PTI.RestApi.Test
{
    internal sealed class PtiRepositoryTests
    {
        #region Public Fields

        public ILogger Logger = AssemblyInitializer.Logger;

        public IPtiRepository Repository { get; private set; }

        [Test]
        public async Task RemovesCardHolder_WhenDeletingCardholderByCredentialId()
        {
            var guid = Guid.Parse("83623636-84ed-4c63-a448-fcaa1b782b91");
            var x = await Repository.TryPopByCredId(guid);
            x.IfFail(Logger.Error);
            x.IfSucc(Logger.InfoAsJson);
        }

        [Test]
        public async Task RemovesCardHolder_WhenDeletingCardholderById()
        {
            var guid = Guid.Parse("8540db2d-38fc-4b94-bc29-0c67b071cee3");
            var x = await Repository.TryPopByCardholderId(guid);
            x.IfFail(Logger.Error);
            x.IfSucc(Logger.InfoAsJson);
        }

        [Test]
        public async Task AddsCardholdersToTheDatabase_WhenInsertingABatchOfCardHoldersModels()
        {
            var chModels = CreateModels(5, 9999000);
            var x = await Repository.InsertBatchAsync(chModels);
        }

        [Test]
        public async Task ReceiveAListOfCardholders_WhenAtteptingToFindAllCardholders()
        {
            var expected = await Repository.TryFindAllCardholders();
            expected.Should().HaveCountGreaterThan(1);
            Logger.InfoAsJson(expected);
        }

        [Test]
        public async Task RemovesTable_WhenDroppingTable()
        {
            var x = await Repository.DropTableAsync<CardholderModel>();
            x.IfFail(Logger.Error);
        }

        [Test]
        public async Task FindCardholder_WhenSearchinByCardholderId()
        {
            var guid = Guid.Parse("26526647-4331-418f-a685-373abb5715b8");
            var x = await Repository.TryFindCardholderAsync(guid);
            x.IfFail(Logger.Error);
            x.IfSucc(Logger.InfoAsJson);
        }

        [Test]
        public async Task FindCardholder_WhenSearchinByCredentialId()
        {
            var guid = Guid.Parse("eed717f8-4e3e-43a5-bc0d-1bc134d4f56a");
            var x = await Repository.TryFindByCredId(guid);
            x.IfFail(Logger.Error);
            x.IfSucc(Logger.InfoAsJson);
        }

        [OneTimeSetUp]
        public void Setup()
        {
            Repository = AssemblyInitializer.NinjectKernel.Get<IPtiRepository>();
        }

        private static IReadOnlyCollection<CardholderModel> CreateModels(int numOfModels, int employeeSeedNumber)
        {
            return Enumerable
                .Range(1, numOfModels)
                .Select(id =>
                new CardholderModel
                {
                    CardholderGuid = Guid.NewGuid(),
                    CredentialGuid = Guid.NewGuid(),
                    Pin = id * 100,
                    EmployeeNumber = employeeSeedNumber + id,
                    Name = $"Test SS {id}",
                }).ToList();
        }

        #endregion Public Fields
    }
}