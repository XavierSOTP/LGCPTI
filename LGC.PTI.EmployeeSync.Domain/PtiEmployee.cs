using System;

namespace LGC.PTI.EmployeeSync.Domain
{
    /// <summary>
    /// Employee as understood by the PTI synchronization domain (not by GraphQL or Genetec).
    /// The aggregate owns its activation change and retains the rest of its properties.
    /// </summary>
    public sealed class PtiEmployee
    {
        public EmployeeNumber Number { get; }
        public EmployeePin Pin { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string DepartmentNumber { get; }
        public bool Active { get; }
        public string Type { get; }
        public bool? UnrestrictedUsage { get; }

        public PtiEmployee(
            EmployeeNumber number,
            EmployeePin pin,
            string firstName,
            string lastName,
            string departmentNumber,
            bool active,
            string type = null,
            bool? unrestrictedUsage = null)
        {
            Number = number ?? throw new ArgumentNullException(nameof(number));
            Pin = pin ?? throw new ArgumentNullException(nameof(pin));

            if (string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(departmentNumber))
                throw new InvalidOperationException("PTI employee requires first name, last name and department.");

            FirstName = firstName;
            LastName = lastName;
            DepartmentNumber = departmentNumber;
            Active = active;
            Type = type;
            UnrestrictedUsage = unrestrictedUsage;
        }

        /// <summary>Activation/deactivation is a state change, never a deletion.</summary>
        public PtiEmployee WithActive(bool active) =>
            new PtiEmployee(Number, Pin, FirstName, LastName, DepartmentNumber, active, Type, UnrestrictedUsage);
    }
}
