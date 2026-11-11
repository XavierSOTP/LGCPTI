using LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;
using LGC.PTI.Domain.Common;
using LGC.PTI.Domain.Models;
using LGC.PTI.Shared.Common;
using MapsterMapper;
using System.Globalization;

namespace LGC.PTI.RestApi.Test
{
    internal sealed class MappingTests
    {
        #region Public Fields

        public ILogger Logger = AssemblyInitializer.Logger;

        public IMapper Mapper { get; private set; }

        [OneTimeSetUp]
        public void Setup()
        {
            Mapper = AssemblyInitializer.NinjectKernel.Get<IMapper>();
        }

        #endregion Public Fields

        //[Test]
        //public void PropertiesAreEqual_WhenMappingCardholderEntityToEmployeeOutDto()
        //{
        //    var expected = new CardholderEntity
        //    {
        //        CardholderGuid= Guid.NewGuid(),
        //        EmployeeNumber = "9999993",
        //        FirstName = "Test",
        //        LastName = "SS",
        //        Department = "1OPERATIONS",
        //    };

        // var sut = Mapper.Map<EmployeeOutDto>(expected); //Logger.InfoAsJson(sut); var jsonString
        // = JsonConvert.SerializeObject(sut, Formatting.Indented,
        // JsonSerializationHelper.DefaultJsonSerializerSettings); Logger.Info(jsonString);

        // sut.EmployeeNumber.Should().Be(expected.EmployeeNumber);
        // sut.FirstName.Should().Be(expected.FirstName);
        // sut.LastName.Should().Be(expected.LastName); sut.Department.Should().Be(expected.Department);

        //}

        #region Public Methods

        [Test]
        public void BuilderTest()
        {
            PTITodoCode.Instance.IgnoringWhileTesting(Logger, "Add department and Language from Genetec Cardholder");
            PTITodoCode.Instance.IgnoringWhileTesting(Logger, " builder not fully implemented");

            var department = "Operation";
            var builder = NewCardholderBuilder.New()
           .SetDepartment(department)
           .SetState(true);
            var sut = builder.TryBuild();
            sut.IfSucc(ch =>
            {
                ch.Active.Should().BeTrue();
                ch.Department.Should().BeEquivalentTo(department);
            });
            sut.IfSucc(r => Logger.InfoAsJson(r));
            sut.IfFail(ex => Logger.InfoAsJson(ex));
        }

        [Test]
        public void PropertiesAreEqual_WhenMappingCardholderEntityToCardholderModel()
        {
            var expected = new CardholderEntity
            {
                CardholderGuid = Guid.NewGuid(),
                CredentialGuid = Guid.NewGuid(),
                Pin = 1235,
                EmployeeNumber = 9999993,
                FirstName = "Test",
                LastName = "SS",
            };

            var sut = Mapper.Map<CardholderModel>(expected);
            Logger.InfoAsJson(sut);

            sut.EmployeeNumber.Should().Be(expected.EmployeeNumber);
            sut.CardholderGuid.Should().Be(expected.CardholderGuid);
            sut.CredentialGuid.Should().Be(expected.CredentialGuid);
            sut.Pin.Should().Be(expected.Pin);
            sut.Name.Should().Be(expected.Name);
        }

        [Test]
        public void PropertiesAreEqual_WhenMappingCardholderEntityToEmployeeOutDto()
        {
            var expected = new CardholderEntity
            {
                EmployeeNumber = 9999993,
                FirstName = "Test",
                LastName = "SS",
                Badge = "999-9993",
                Language = Language.Français,
                Active = true,
                Department = "1OPERATIONS",
            };

            var sut = Mapper.Map<EmployeeOutDto>(expected);
            Logger.InfoAsJson(sut);

            sut.EmployeeNumber.Should().Be(expected.EmployeeNumber.ToString(CultureInfo.InvariantCulture));
            sut.FirstName.Should().Be(expected.FirstName);
            sut.LastName.Should().Be(expected.LastName);
            sut.Badge.Should().Be(expected.Badge);
            sut.Language.Should().Be(expected.Language);
            sut.Active.Should().Be(expected.Active);
            sut.Department.Should().Be(expected.Department);
        }

        [Test]
        public void PropertiesAreEqual_WhenMappingCardholderEntityToFullEmployeeResetPin()
        {
            var expected = new CardholderEntity
            {
                EmployeeNumber = 9999993,
                FirstName = "Test",
                LastName = "SS",
                Badge = "999-9993",
                Language = Language.Français,
                Active = true,
                Department = "1OPERATIONS",
                Pin = 12345,
            };

            var sut = Mapper.Map<FullEmployeeResetPin>(expected);
            Logger.InfoAsJson(sut);

            sut.EmployeeNumber.Should().Be(expected.EmployeeNumber.ToString());
            sut.FirstName.Should().Be(expected.FirstName);
            sut.LastName.Should().Be(expected.LastName);
            sut.Badge.Should().Be(expected.Badge);
            sut.Language.Should().Be(expected.Language);
            sut.Active.Should().Be(expected.Active);
            sut.Department.Should().Be(expected.Department);
            sut.Pin.Should().Be(expected.Pin.ToString(CultureInfo.InvariantCulture));
        }

        [Test]
        public void PropertiesAreEqual_WhenMappingEmployeeInDtoToCardholderEntity()
        {
            var expected = new EmployeeInDto
            {
                EmployeeNumber = "9999993",
                FirstName = "Test",
                LastName = "SS",
                Department = "1OPERATIONS",
            };

            var sut = Mapper.Map<CardholderEntity>(expected);
            var jsonString = JsonConvert.SerializeObject(sut, Formatting.Indented, JsonSerializationHelper.DefaultJsonSerializerSettings);
            Logger.Info(jsonString);
            sut.EmployeeNumber.ToString().Should().Be(expected.EmployeeNumber);
            sut.FirstName.Should().Be(expected.FirstName);
            sut.LastName.Should().Be(expected.LastName);
            sut.Department.Should().Be(expected.Department);
        }

        #endregion Public Methods
    }
}