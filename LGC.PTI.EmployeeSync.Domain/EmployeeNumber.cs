using System;

namespace LGC.PTI.EmployeeSync.Domain
{
    /// <summary>
    /// Identity of an employee inside the PTI synchronization bounded context.
    /// Never convert it to an integer: zeroes can be significant.
    /// </summary>
    public sealed class EmployeeNumber : IEquatable<EmployeeNumber>
    {
        public string Value { get; }

        public EmployeeNumber(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Employee number is required.", nameof(value));
            Value = value;
        }

        public bool Equals(EmployeeNumber other) =>
            other != null && string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);

        public override bool Equals(object obj) => Equals(obj as EmployeeNumber);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public override string ToString() => Value;
    }
}
