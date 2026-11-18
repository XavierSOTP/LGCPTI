using System;
using LanguageExt;
using LanguageExt.Common;

namespace LGC.PTI.EmployeeSync.Domain
{
    /// <summary>PTI employee identifier; keeps leading zeroes.</summary>
    public sealed class EmployeeNumber : IEquatable<EmployeeNumber>
    {
        public string Value { get; }

        private EmployeeNumber(string value) => Value = value;

        /// <summary>An invalid identifier is a domain failure, not an exception.</summary>
        public static Fin<EmployeeNumber> Create(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? Prelude.FinFail<EmployeeNumber>(Error.New("Employee number is required."))
                : Prelude.FinSucc(new EmployeeNumber(value));

        public bool Equals(EmployeeNumber other) =>
            other != null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => Equals(obj as EmployeeNumber);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public override string ToString() => Value;
    }
}
