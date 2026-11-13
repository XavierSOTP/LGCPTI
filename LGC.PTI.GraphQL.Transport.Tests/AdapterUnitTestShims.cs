// Test-only shims: compile the production adapter source without installing the Genetec SDK
// or private SoftSolute packages. These classes are NEVER included in a production project.
global using LanguageExt;
global using LGC.PTI.Common.Application.Dtos;
global using LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;
global using SoftSolute.Common.Utilities;
global using Newtonsoft.Json;
global using Newtonsoft.Json.Converters;
global using System;
global using System.ComponentModel;
global using System.Threading.Tasks;

namespace LanguageExt
{
    public readonly struct Option<T>
    {
        private readonly T _value;
        public bool IsSome { get; }
        private Option(T value) { _value = value; IsSome = true; }
        public static Option<T> None => default;
        public static implicit operator Option<T>(T value) => value == null ? None : new Option<T>(value);
        public T IfNone(T fallback) => IsSome ? _value : fallback;
        public Option<U> Map<U>(Func<T, U> map) => IsSome ? (Option<U>)map(_value) : Option<U>.None;
    }

    public readonly struct Aff<T>
    {
        private readonly Func<Task<T>> _run;
        public Aff(Func<Task<T>> run) => _run = run;
        public Task<T> RunAsync() => _run();
    }
}

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

    public static class LanguageExtAffExtensions
    {
        public static Aff<T> Aff<T>(Func<Task<T>> action) => new Aff<T>(action);
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
