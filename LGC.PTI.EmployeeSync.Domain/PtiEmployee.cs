using System;
using LanguageExt;
using LanguageExt.Common;

namespace LGC.PTI.EmployeeSync.Domain
{
    /// <summary>Validated employee entity independent of PTI's GraphQL schema.</summary>
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

        private PtiEmployee(
            EmployeeNumber number, EmployeePin pin, string firstName, string lastName,
            string departmentNumber, bool active, string type, bool? unrestrictedUsage)
        {
            Number = number;
            Pin = pin;
            FirstName = firstName;
            LastName = lastName;
            DepartmentNumber = departmentNumber;
            Active = active;
            Type = type;
            UnrestrictedUsage = unrestrictedUsage;
        }

        /// <summary>Validates the complete record without throwing for missing business data.</summary>
        public static Fin<PtiEmployee> Create(
            string number, string pin, string firstName, string lastName,
            string departmentNumber, bool active, string type = null,
            bool? unrestrictedUsage = null) =>
            EmployeeNumber.Create(number).Match(
                Fail: error => Prelude.FinFail<PtiEmployee>(error),
                Succ: validNumber => EmployeePin.Create(pin).Match(
                    Fail: error => Prelude.FinFail<PtiEmployee>(error),
                    Succ: validPin => Create(
                        validNumber, validPin, firstName, lastName, departmentNumber,
                        active, type, unrestrictedUsage)));

        public static Fin<PtiEmployee> Create(
            EmployeeNumber number, EmployeePin pin, string firstName, string lastName,
            string departmentNumber, bool active, string type = null,
            bool? unrestrictedUsage = null)
        {
            if (number == null || pin == null ||
                string.IsNullOrWhiteSpace(firstName) ||
                string.IsNullOrWhiteSpace(lastName) ||
                string.IsNullOrWhiteSpace(departmentNumber))
                return Prelude.FinFail<PtiEmployee>(
                    Error.New("PTI employee requires number, PIN, first name, last name and department."));

            return Prelude.FinSucc(new PtiEmployee(
                number, pin, firstName, lastName, departmentNumber,
                active, type, unrestrictedUsage));
        }

        /// <summary>Immutable activation change; validated fields remain unchanged.</summary>
        public PtiEmployee WithActive(bool active) =>
            new PtiEmployee(Number, Pin, FirstName, LastName, DepartmentNumber,
                active, Type, UnrestrictedUsage);
    }
}
