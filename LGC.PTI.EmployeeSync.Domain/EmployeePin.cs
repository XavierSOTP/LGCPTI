using System;

namespace LGC.PTI.EmployeeSync.Domain
{
    /// <summary>Immutable PIN value object; keeps the original text (e.g. "00123").</summary>
    public sealed class EmployeePin : IEquatable<EmployeePin>
    {
        public string Value { get; }

        public EmployeePin(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
                throw new ArgumentException("Employee PIN is required.", nameof(value));
            Value = value;
        }

        public bool Equals(EmployeePin other) =>
            other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as EmployeePin);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

        // Do not expose the PIN in exception messages or ordinary logs.
        public override string ToString() => "[REDACTED]";
    }
}
