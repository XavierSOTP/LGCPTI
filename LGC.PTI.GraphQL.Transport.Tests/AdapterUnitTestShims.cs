// Test-only shims for SoftSolute/Genetec dependencies.
// IMPORTANT: Aff<T> and Option<T> are now the real LanguageExt.Core package types.
// The test-only Aff factory delegates to the same LanguageExt Prelude.Aff as the production helper.
// These shims are NEVER included in a production project.
global using LanguageExt;
global using LGC.PTI.Common.Application.Dtos;
global using LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;
global using SoftSolute.Common.Utilities;
global using Newtonsoft.Json;
global using Newtonsoft.Json.Converters;
global using System;
global using System.ComponentModel;
global using System.Collections.Generic;
global using System.Threading.Tasks;

namespace SoftSolute.Common.Utilities
{
    public readonly struct NotNull<T>
    {
        public T Value { get; }
        private NotNull(T value) => Value = value ?? throw new ArgumentNullException(nameof(value));
        public static implicit operator NotNull<T>(T value) => new NotNull<T>(value);
    }

    public readonly struct NotEmptyString
    {
        private readonly string _value;
        private NotEmptyString(string value) => _value = value ?? throw new ArgumentNullException(nameof(value));
        public static implicit operator NotEmptyString(string value) => new NotEmptyString(value);
        public static implicit operator string(NotEmptyString value) => value._value;
        public override string ToString() => _value;
    }


}

namespace SoftSolute.Common.Utilities.LanguageExtSS
{
    public static class TestOptionExtensions
    {
        public static Option<T> SomeIf<T>(this T value, Func<T, bool> predicate) =>
            predicate(value) ? (Option<T>)value : Option<T>.None;
    }
}

namespace LGC.PTI.Common.Infrastructure.ExternalApi.Dtos
{
    public sealed class FullEmployeeResetPin
    {
        public string EmployeeNumber { get; set; }
        public string Pin { get; set; }
    }
}

namespace LGC.PTI.Domain.Models
{
    public sealed class EmployeeModel
    {
        public string EmployeeNumber { get; set; }
    }
}
