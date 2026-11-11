using LGC.PTI.Domain.Common;
using LGC.PTI.Domain.Infrastructure.Mapping;
using LGC.PTI.HTTPClient.Refit;
using LGC.PTI.Infrastructure;
using LGC.PTI.Refit.Authentication;

namespace LGC.PTI.RestApi.Test
{
    [SetUpFixture]
    public static class AssemblyInitializer
    {
        #region Public Properties

        public static ILogger Logger { get; private set; }

        public static IKernel NinjectKernel { get; private set; }

        #endregion Public Properties

        #region Public Methods

        [OneTimeTearDown]
        public static void AssemblyCleanup()
        {
        }

        [OneTimeSetUp]
        public static void AssemblyInit()
        {
            LogManager.Configuration = DefaultNLogConfigurator.New()
                .CreateConfig(LogLevel.Debug, (config) => LogManagerWrapper.CreateDailyAndArchiveLogFile(config));
            Logger = LogManager.GetCurrentClassLogger();
            var resource = ProjectResourcesHelper.GetResourcesPath();
            NinjectKernel = new StandardKernel(new HtppClientModule(), new LiteDBRepositoryModule(resource));
            NinjectKernel.Bind<JsonSerializerSettings>().ToConstant(JsonSerializationHelper.DefaultJsonSerializerSettings);
            NinjectKernel.Bind<ILogger>().ToConstant(Logger);
            NinjectKernel.Load(MapsterModule.NewUsing(typeof(AssemblyInitializer).Assembly));

            //! dependencies that should go into AppNinjectModule
            NinjectKernel.Bind<IAuthCredentialStore>().To<AuthCredentialStore>().InSingletonScope();
            TestDI();
        }

        private static void TestDI()
        {
            try
            {
                var x = NinjectKernel.Get<IAuthenticationSrv>();
            }
            catch (Exception ex)
            {
                Logger.Error(ex);
                throw;
            }
        }

        #endregion Public Methods
    }
}