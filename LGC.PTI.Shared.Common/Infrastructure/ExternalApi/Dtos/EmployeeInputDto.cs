using SoftSolute.Common.Utilities;
using SoftSolute.Common.Utilities.Validations;

namespace LGC.PTI.Common.Application.Dtos
{
    /// <summary>
    /// EmployeeInput as defined in PTI system
    /// </summary>
    public class EmployeeInputDto
    {
        #region Public Properties

        public bool Active { get; set; }
        public string DepartmentNumber { get; set; }
        public string FirstName { get; set; }
        public string LastName { get; set; }
        public string Number { get; set; }
        public string Pin { get; set; }
        public string Type { get; set; }
        public bool UnrestrictedUsage { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public EmployeeInputDto()
        {
        }

        #endregion Public Constructors

        #region Public Methods

        public static EmployeeInputDto NewActiveUser(
            string firstName,
            string lastName,
            int number,
            int pin,
            string department = default,
            string type = default)
        {
            return new EmployeeInputDto(firstName, lastName, number, pin, active: true, department, type);
        }

        public static EmployeeInputDto NewDeActiveUser(
            string firstName,
            string lastName,
            int number,
            int pin,
            string department = default,
            string type = default)
        {
            return new EmployeeInputDto(firstName, lastName, number, pin, active: false, department, type);
        }

        #endregion Public Methods

        #region Private Constructors

        private EmployeeInputDto(
                            string firstName,
            string lastName,
            int number,
            int pin,
            bool active,
            string department,
            string type)
        {
            FirstName = firstName.GuardNullOrEmptyArg(nameof(firstName));
            LastName = lastName.GuardNullOrEmptyArg(nameof(lastName));
            Number = number.GuardArgPredicate(n => n.IsBetween(1, int.MaxValue), nameof(number)).ToString();
            Pin = pin.GuardArgPredicate(n => n.IsBetween(1, int.MaxValue), nameof(number)).ToString();
            Active = active;
            DepartmentNumber = department ?? string.Empty;
            Type = type;
        }

        #endregion Private Constructors
    }
}