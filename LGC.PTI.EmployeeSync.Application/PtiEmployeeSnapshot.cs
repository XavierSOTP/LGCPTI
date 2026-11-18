using LanguageExt;
using LanguageExt.Common;
using LGC.PTI.EmployeeSync.Domain;

namespace LGC.PTI.EmployeeSync.Application
{
    /// <summary>Read projection from PTI; remote data may be incomplete.</summary>
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

        /// <summary>Only complete, non-deleted remote records can become write commands.</summary>
        public Fin<PtiEmployee> ToEmployee() =>
            Deleted
                ? Prelude.FinFail<PtiEmployee>(Error.New("Cannot synchronize a deleted PTI employee."))
                : PtiEmployee.Create(
                    Number, Pin, FirstName, LastName, DepartmentNumber,
                    Active, Type, UnrestrictedUsage);
    }
}
