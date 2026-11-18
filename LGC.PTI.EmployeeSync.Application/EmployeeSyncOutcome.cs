using LanguageExt;
using LanguageExt.Common;
using LGC.PTI.EmployeeSync.Domain;

namespace LGC.PTI.EmployeeSync.Application
{
    public sealed class EmployeeSyncOutcome
    {
        public EmployeeNumber Number { get; }
        public EmployeeChangeKind Change { get; }
        public PtiEmployeeSnapshot Employee { get; }

        private EmployeeSyncOutcome(
            EmployeeNumber number, EmployeeChangeKind change, PtiEmployeeSnapshot employee)
        {
            Number = number;
            Change = change;
            Employee = employee;
        }

        public static Fin<EmployeeSyncOutcome> Create(
            EmployeeNumber number, EmployeeChangeKind change, PtiEmployeeSnapshot employee) =>
            number == null || employee == null
                ? Prelude.FinFail<EmployeeSyncOutcome>(Error.New("Employee synchronization outcome is incomplete."))
                : Prelude.FinSucc(new EmployeeSyncOutcome(number, change, employee));
    }
}
