using System;
using LGC.PTI.EmployeeSync.Domain;

namespace LGC.PTI.EmployeeSync.Application
{
    public sealed class EmployeeSyncOutcome
    {
        public EmployeeNumber Number { get; }
        public EmployeeChangeKind Change { get; }
        public PtiEmployeeSnapshot Employee { get; }

        public EmployeeSyncOutcome(EmployeeNumber number, EmployeeChangeKind change, PtiEmployeeSnapshot employee)
        {
            Number = number ?? throw new ArgumentNullException(nameof(number));
            Change = change;
            Employee = employee ?? throw new ArgumentNullException(nameof(employee));
        }
    }
}
