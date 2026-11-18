using System;
using LanguageExt;
using LanguageExt.Common;

namespace LGC.PTI.EmployeeSync.Domain
{
    /// <summary>Immutable PIN value object; preserves leading zeroes.</summary>
    public sealed class EmployeePin : IEquatable<EmployeePin>
    {
        public string Value { get; }

        private EmployeePin(string value) => Value = value;

        /// <summary>Missing PINs return Fin.Fail without exposing sensitive content.</summary>
        public static Fin<EmployeePin> Create(string value) =>
            string.IsNullOrWhiteSpace(value)
                ? Prelude.FinFail<EmployeePin>(Error.New("Employee PIN is required."))
                : Prelude.FinSucc(new EmployeePin(value));

        public bool Equals(EmployeePin other) =>
            other != null && string.Equals(Value, other.Value, StringComparison.Ordinal);

        public override bool Equals(object obj) => Equals(obj as EmployeePin);

        public override int GetHashCode() => StringComparer.Ordinal.GetHashCode(Value);

        public override string ToString() => "[REDACTED]";
    }
}
