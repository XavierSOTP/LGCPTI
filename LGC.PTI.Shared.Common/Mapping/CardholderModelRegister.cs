using LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;
using LGC.PTI.Domain.Models;
using Mapster;
using System.Globalization;

namespace LGC.PTI.Domain.Mapping
{
    /// <summary>
    /// Configures Mapster mappings for employee-related domain,
    /// API, and persistence models.
    /// </summary>
    public sealed class EmployeeModelRegister : IRegister
    {
        #region Public Methods

        /// <summary>
        /// Registers employee-related type mappings in the specified
        /// <see cref="TypeAdapterConfig"/>.
        /// </summary>
        /// <param name="config">
        /// The Mapster configuration instance where the mappings are registered.
        /// </param>
        public void Register(TypeAdapterConfig config)
        {
            //PTI
            config
             .NewConfig<EmployeeInDto, EmployeeEntity>()
             .Map(dest => dest.Department, src => src.Department)
             .Map(dest => dest.Language, src => src.Language);

            //Reset Pin
            config
            .NewConfig<EmployeeEntity, FullEmployeeResetPin>()
            .AfterMapping((src, dest) => dest.EmployeeNumber = src.EmployeeNumber.ToString(CultureInfo.InvariantCulture))
            .AfterMapping((src, dest) => dest.Pin = src.Pin.ToString(CultureInfo.InvariantCulture));

            // Create and update new employee
            config
               .NewConfig<EmployeeEntity, EmployeeOutDto>()
               .AfterMapping((src, dest) => dest.EmployeeNumber = src.EmployeeNumber.ToString(CultureInfo.InvariantCulture));

            //DB

            config
             .NewConfig<EmployeeEntity, EmployeeModel>()
             .TwoWays();

            // can use .Map to specify wich properties to map can use .IgnoreNonMapped(true); to
            // avoid mapping all properties not specified with .Map can add a condition to mapping
            // to ensure specific values ex:
            // - ex:.Map(dest => dest.Active, src => src.Active, srcCond => srcCond.Active == true)
        }

        #endregion Public Methods
    }
}