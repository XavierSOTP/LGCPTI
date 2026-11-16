using System;
using LGC.PTI.EmployeeSync.Domain;

namespace LGC.PTI.EmployeeSync.Application
{
    /// <summary>
    /// Read model from PTI. It intentionally accepts partial remote data.
    /// We only require complete details when turning a record into a write command.
    /// </summary>
    public sealed class PtiEmployeeSnapshot
    {
        public string Number { get; }
        public string Pin { get; }
        public string FirstName { get; }
        public string LastName { get; }
        public string DepartmentNumber { get; }
        public bool Active { get; }
        public bool Deleted { get; }
        public string Type { get; }
        public bool? UnrestrictedUsage { get; }

        public PtiEmployeeSnapshot(
            string number, string pin, string firstName, string lastName,
            string departmentNumber, bool active, bool deleted, string type = null,
            bool? unrestrictedUsage = null)
        {
            Number = number;
            Pin = pin;
            FirstName = firstName;
            LastName = lastName;
            DepartmentNumber = departmentNumber;
            Active = active;
            Deleted = deleted;
            Type = type;
            UnrestrictedUsage = unrestrictedUsage;
        }

        public PtiEmployee ToEmployee()
        {
            if (Deleted)
                throw new InvalidOperationException("Cannot synchronize a deleted PTI employee.");
            if (string.IsNullOrWhiteSpace(Number) || string.IsNullOrWhiteSpace(Pin) ||
                string.IsNullOrWhiteSpace(FirstName) || string.IsNullOrWhiteSpace(LastName) ||
                string.IsNullOrWhiteSpace(DepartmentNumber))
                throw new InvalidOperationException("Cannot synchronize an incomplete PTI employee record.");

            return new PtiEmployee(
                new EmployeeNumber(Number), new EmployeePin(Pin), FirstName,
                LastName, DepartmentNumber, Active, Type, UnrestrictedUsage);
        }
    }
}
