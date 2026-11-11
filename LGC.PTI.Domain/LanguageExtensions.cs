using LanguageExt.Common;
using SoftSolute.Common.Utilities;
using System.Linq.Expressions;
using System.Runtime.CompilerServices;
using Unit = LanguageExt.Unit;

namespace LGC.PTI.Domain
{
    /// <summary>
    /// Provides extension methods for LanguageExt functional programming types and utilities.
    /// Includes conversion methods between different result and error handling types,
    /// binding operations for functional composition, and other functional programming helpers.
    /// </summary>
    public static class LanguageExtensions
    {
        /// <summary>
        /// Converts a Validation result to a Fin (final result) type, combining validation errors into a single exception message.
        /// </summary>
        /// <typeparam name="T">The type of the validated value.</typeparam>
        /// <param name="validation">The validation result to convert.</param>
        /// <returns>A Fin result containing either the validated value or a failure with combined error messages.</returns>
        public static Fin<T> AsFin<T>(this Validation<string, T> validation)
        {
            return validation.Match((T r) => Fin<T>.Succ(r), (Seq<string> exs) => Fin<T>.Fail(new Exception(exs.ConvertToString())));
        }

        /// <summary>
        /// Provides lazy evaluation of an Option by wrapping it in a function.
        /// </summary>
        /// <typeparam name="T">The type of the optional value.</typeparam>
        /// <param name="source">The Option to wrap.</param>
        /// <returns>A function that returns the Option when invoked.</returns>
        [Obsolete("Use Prelude.fun()")]
        public static Func<Option<T>> AsFunc<T>(this Option<T> source)
        {
            return Prelude.fun(() => source);
        }

        //
        // Summary:
        //     Bind an LanguageExt.Option`1 to Option<B>
        //
        // Parameters:
        //   source:
        //
        //   f:
        //
        // Type parameters:
        //   A:
        //
        //   B:
        //
        // Remarks:
        //     BindAsync from LanguageExt will return LanguageExt.OptionAsync`1. When Awaiting
        //     in LanguageExt.OptionAsync`1 if it is None it will throw an LanguageExt.ValueIsNoneException
        //     instead of returning an option
        public static Task<Option<B>> BindSafeAsync<A, B>(this Option<A> source, Func<A, Task<Option<B>>> f)
        {
            return source.MatchAsync(async (A a) => await f(a), () => Task.FromResult(Option<B>.None));
        }

        //
        // Summary:
        //     it evaluate the sequence until if found the first LanguageExt.Option`1 that is
        //     LanguageExt.Option`1.IsSome,"/> else it will return LanguageExt.Option`1.None
        //
        //
        // Parameters:
        //   source:
        //
        //   values:
        //
        // Type parameters:
        //   T:
        public static Option<T> Choices<T>(this Option<T> source, params Func<Option<T>>[] values)
        {
            Func<Option<T>> func = Prelude.fun(() => source);
            Func<Option<T>>[] array = values.ToArray();
            int num = 0;
            Func<Option<T>>[] array2 = new Func<Option<T>>[1 + array.Length];
            array2[num] = func;
            num++;
            ReadOnlySpan<Func<Option<T>>> readOnlySpan = new ReadOnlySpan<Func<Option<T>>>(array);
            readOnlySpan.CopyTo(new Span<Func<Option<T>>>(array2).Slice(num, readOnlySpan.Length));
            num += readOnlySpan.Length;
            return array2.FoldWhile(Option<T>.None, (Option<T> state, Func<Option<T>> fun) => fun(), (Option<T> state) => state.IsNone);
        }

        //
        // Summary:
        //     Execute action independent of LanguageExt.Aff`1 result
        //
        // Parameters:
        //   source:
        //
        //   action:
        //
        // Type parameters:
        //   T:
        public static Aff<T> Finally<T>(this Aff<T> source, Action action)
        {
            return source.OnFail(action).OnSucc((Action<T>)delegate
            {
                action();
            });
        }

        //
        // Summary:
        //     Executes a specified action after the completion of a source operation, regardless
        //     of its success or failure.
        //
        // Parameters:
        //   source:
        //     The operation whose completion triggers the execution of the specified action.
        //
        //
        //   action:
        //     The operation to be executed after the source operation completes.
        //
        // Type parameters:
        //   A:
        //     Represents the type of the result produced by the source operation.
        //
        //   B:
        //     Represents the type of the result produced by the action to be executed.
        //
        // Returns:
        //     Returns a new operation that encapsulates the result of the action after the
        //     source operation.
        public static Aff<B> Finally<A, B>(this Aff<A> source, Aff<B> action)
        {
            return source.BiBind((A _) => action, (Error _) => action);
        }

        //
        // Summary:
        //     Executes a specified action after the completion of a given asynchronous computation.
        //
        //
        // Parameters:
        //   source:
        //     The asynchronous computation whose completion triggers the execution of the action.
        //
        //
        //   action:
        //     The asynchronous action to be executed after the completion of the source computation.
        //
        //
        // Type parameters:
        //   A:
        //     Represents the type of the result produced by the asynchronous computation.
        //
        // Returns:
        //     Returns a new asynchronous computation that represents the result of the source
        //     followed by the action.
        public static Aff<Unit> Finally<A>(this Aff<A> source, Aff<Unit> action)
        {
            return source.BiBind((A _) => action, (Error _) => action);
        }

        //
        // Summary:
        //     Execute action independent of LanguageExt.Eff`1 result
        //
        // Parameters:
        //   source:
        //
        //   action:
        //
        // Type parameters:
        //   T:
        public static Eff<T> Finally<T>(this Eff<T> source, Action action)
        {
            return source.OnFail(action).OnSucc((Action<T>)delegate
            {
                action();
            });
        }

        //
        // Parameters:
        //   sequence:
        //
        //   pred:
        //
        // Type parameters:
        //   T:
        [Obsolete("Use HeadOrNone or Find")]
        public static Option<T> FirstOrNone<T>(this IEnumerable<T> sequence, Func<T, bool> pred = null)
        {
            if (!pred.IsNullOrDefault())
            {
                return sequence.Find(pred);
            }

            return sequence.HeadOrNone();
        }

        //
        // Parameters:
        //   source:
        //
        // Type parameters:
        //   T:
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        public static Exception ForceGetException<T>(this Fin<T> source)
        {
            return source.Match<Error>(delegate
            {
                throw new InvalidOperationException("It was a valid FIN");
            }, (Error ex) => ex);
        }

        //
        // Parameters:
        //   source:
        //
        //   expression:
        //
        // Type parameters:
        //   A:
        //
        //   B:
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        public static A ForceGetLeft<A, B>(this Either<A, B> source, [System.Runtime.CompilerServices.CallerArgumentExpression("source")] string expression = null)
        {
            return source.Match<A>(delegate
            {
                throw new InvalidOperationException("'" + expression + "' is not of type Left");
            }, (A b) => b);
        }

        //
        // Summary:
        //     Retrieves the right value from an Either type, throwing an exception if the value
        //     is not present.
        //
        // Parameters:
        //   source:
        //     An Either instance from which the right value is to be extracted.
        //
        //   expression:
        //
        // Type parameters:
        //   A:
        //     Represents the type of the left value in the Either structure.
        //
        //   B:
        //     Represents the type of the right value in the Either structure.
        //
        // Returns:
        //     The right value contained in the Either instance.
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        //     Thrown when attempting to access the right value while it is not present.
        public static B ForceGetRight<A, B>(this Either<A, B> source, [System.Runtime.CompilerServices.CallerArgumentExpression("source")] string expression = null)
        {
            return source.Match<B>((B b) => b, delegate
            {
                throw new InvalidOperationException("'" + expression + "' is not of type Right");
            });
        }

        //
        // Parameters:
        //   source:
        //
        //   errorMsg:
        //
        // Type parameters:
        //   T:
        public static T ForceGetValue<T>(this Fin<T> source, string errorMsg = null)
        {
            if (errorMsg == null)
            {
                errorMsg = string.Empty;
            }

            return source.Match<T>((T r) => r, delegate (Error ex)
            {
                throw (Exception)(errorMsg.IsNullOrEmpty() ? ex : ((Error)new InvalidOperationException(errorMsg, ex)));
            });
        }

        //
        // Parameters:
        //   source:
        //
        //   expression:
        //
        // Type parameters:
        //   T:
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        public static T ForceGetValue<T>(this Option<T> source, [System.Runtime.CompilerServices.CallerArgumentExpression("source")] string expression = null)
        {
            return source.Match<T>((T v) => v, delegate
            {
                throw new InvalidOperationException(expression + " was of type none");
            });
        }

        //
        // Parameters:
        //   source:
        //
        //   func:
        //
        // Type parameters:
        //   T:
        public static async Task<Unit> IfNoneAsync<T>(this Option<T> source, Func<Task> func)
        {
            if (source.IsNone)
            {
                await func().IgnoreCtx();
            }

            return Unit.Default;
        }

        //
        // Parameters:
        //   option:
        //
        //   predicate:
        //
        // Type parameters:
        //   T:
        public static Option<T> KeepAsSomeIf<T>(this Option<T> option, Func<bool> predicate)
        {
            return option.Map((T r) => predicate() ? ((Option<T>)r) : Option<T>.None).Flatten();
        }

        //
        // Parameters:
        //   option:
        //
        //   predicate:
        //
        // Type parameters:
        //   T:
        public static Option<T> KeepAsSomeIf<T>(this Option<T> option, Func<T, bool> predicate)
        {
            return option.Map((T r) => predicate(r) ? ((Option<T>)r) : Option<T>.None).Flatten();
        }

        //
        // Parameters:
        //   option:
        //
        //   predicate:
        //
        // Type parameters:
        //   T:
        public static Task<Option<T>> KeepAsSomeIfAsync<T>(this Option<T> option, Func<T, Task<bool>> predicate)
        {
            return option.MapAsync(async (T r) => (await predicate(r)) ? ((Option<T>)r) : Option<T>.None).Value;
        }

        public static Fin<T> KeepAsSuccIf<T>(this Fin<T> source, Func<bool> predicate, string messageOnFail = null)
        {
            return source.Match((T r) => (!predicate()) ? Fin<T>.Fail(new Exception(messageOnFail)) : ((Fin<T>)r), (Error ex) => Fin<T>.Fail(ex));
        }

        //
        // Parameters:
        //   source:
        //
        //   predicate:
        //
        //   messageOnFail:
        //
        // Type parameters:
        //   T:
        public static Fin<T> KeepAsSuccIf<T>(this Fin<T> source, Func<T, bool> predicate, string messageOnFail = null)
        {
            return source.Match((T r) => (!predicate(r)) ? Fin<T>.Fail(new Exception(messageOnFail)) : ((Fin<T>)r), (Error ex) => Fin<T>.Fail(ex));
        }

        //
        // Parameters:
        //   source:
        //
        //   action:
        //
        // Type parameters:
        //   T:
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        public static Aff<T> KeepAsSuccIf<T>(this Aff<T> source, Func<T, bool> action)
        {
            return source.Bind((T val) => Prelude.Eff(delegate
            {
                if (action(val))
                {
                    return val;
                }

                Expression<Func<T, bool>> arg = (T a) => action(a);
                throw new InvalidOperationException($"Failed, Did not satisfied predicate {arg}");
            }).ToAsync());
        }

        //
        // Parameters:
        //   source:
        //
        //   action:
        //
        // Type parameters:
        //   T:
        //
        // Exceptions:
        //   T:System.InvalidOperationException:
        public static Eff<T> KeepAsSuccIf<T>(this Eff<T> source, Func<T, bool> action)
        {
            return source.Bind((T val) => Prelude.Eff(delegate
            {
                if (action(val))
                {
                    return val;
                }

                Expression<Func<T, bool>> arg = (T a) => action(a);
                throw new InvalidOperationException($"Failed, Did not satisfied predicate {arg}");
            }));
        }

        //
        // Parameters:
        //   source:
        //
        //   map:
        //
        // Type parameters:
        //   T:
        public static Option<string> Map<T>(this Option<T> source, Func<T, string> map)
        {
            return Prelude.map(source, (T s) => map(s));
        }

        //
        // Summary:
        //     Wrap a System.Threading.Tasks.Task`1 into Task of LanguageExt.Option`1
        //
        // Parameters:
        //   source:
        //
        // Type parameters:
        //   A:
        public static Task<Option<A>> MapToOption<A>(this Task<A> source)
        {
            return source.Map((A s) => Prelude.Optional(s));
        }

        //
        // Summary:
        //     Map the result to a default Result
        //
        // Parameters:
        //   self:
        //
        // Type parameters:
        //   T:
        public static Fin<Unit> MapToUnit<T>(this Fin<T> self)
        {
            return self.Map((T r) => Unit.Default);
        }

        //
        // Parameters:
        //   self:
        //
        // Type parameters:
        //   T:
        public static Option<Unit> MapToUnit<T>(this Option<T> self)
        {
            return self.Map((T r) => Unit.Default);
        }

        //
        // Summary:
        //     Executes a specified action if the source operation fails, returning a new operation.
        //
        //
        // Parameters:
        //   source:
        //     The operation that may succeed or fail, determining the flow of execution.
        //
        //   action:
        //     The action to execute when the source operation fails.
        //
        // Type parameters:
        //   T:
        //     Represents the type of the result produced by the source operation.
        //
        // Returns:
        //     A new operation that encapsulates the original behavior with added failure handling.
        public static Aff<T> OnFail<T>(this Aff<T> source, Action action)
        {
            return source.BiBind((Func<T, Aff<T>>)Prelude.SuccessAff, (Func<Error, Aff<T>>)((Error err) => from _ in Prelude.Eff(() => Prelude.fun(action)())
                                                                                                           from _2 in Prelude.FailEff<T>(err)
                                                                                                           select _2));
        }

        //
        // Summary:
        //     Handles failures in an operation by executing a specified action when an error
        //     occurs.
        //
        // Parameters:
        //   source:
        //     The operation that may result in a success or failure.
        //
        //   action:
        //     An action to execute when an error occurs during the operation.
        //
        // Type parameters:
        //   T:
        //     Represents the type of the successful result from the operation.
        //
        // Returns:
        //     Returns a new operation that either succeeds or handles the failure with the
        //     specified action.
        public static Eff<T> OnFail<T>(this Eff<T> source, Action<Exception> action)
        {
            return source.BiBind(Prelude.SuccessEff, (Error err) => from _ in Prelude.Eff(() => Prelude.fun(action)(err.ToException()))
                                                                    from _2 in Prelude.FailEff<T>(err)
                                                                    select _2);
        }

        //
        // Summary:
        //     Handles failures in an operation by executing a specified action when an error
        //     occurs.
        //
        // Parameters:
        //   source:
        //     The operation that may succeed or fail, which is being monitored for errors.
        //
        //
        //   action:
        //     An action to execute when an error occurs, allowing for custom error handling.
        //
        //
        // Type parameters:
        //   T:
        //     Represents the type of the successful result from the operation.
        //
        // Returns:
        //     Returns a new operation that reflects the original operation's success or failure.
        public static Aff<T> OnFail<T>(this Aff<T> source, Action<Exception> action)
        {
            return source.BiBind((Func<T, Aff<T>>)Prelude.SuccessAff, (Func<Error, Aff<T>>)((Error err) => from _ in Prelude.Eff(() => Prelude.fun(action)(err.ToException()))
                                                                                                           from _1 in Prelude.FailEff<T>(err)
                                                                                                           select _1));
        }

        //
        // Summary:
        //     Executes a specified action if the source operation fails, returning a new effort
        //     instance.
        //
        // Parameters:
        //   source:
        //     The effort instance that is being evaluated for success or failure.
        //
        //   action:
        //     The action to execute when the source operation fails.
        //
        // Type parameters:
        //   T:
        //     Represents the type of the result produced by the source operation.
        //
        // Returns:
        //     A new effort instance that reflects the outcome of the original operation and
        //     the action.
        public static Eff<T> OnFail<T>(this Eff<T> source, Action action)
        {
            return source.BiBind(Prelude.SuccessEff, (Error err) => from _ in Prelude.Eff(() => Prelude.fun(action)())
                                                                    from _2 in Prelude.FailEff<T>(err)
                                                                    select _2);
        }

        //
        // Summary:
        //     Handles failures in an operation by executing a specified action when an error
        //     occurs.
        //
        // Parameters:
        //   source:
        //     The operation that may succeed or fail, which is being monitored for errors.
        //
        //
        //   action:
        //     An action to execute when an error is encountered during the operation.
        //
        // Type parameters:
        //   T:
        //     Represents the type of the successful result from the operation.
        //
        // Returns:
        //     Returns a new operation that either succeeds or handles the error with the specified
        //     action.
        public static Eff<T> OnFailError<T>(this Eff<T> source, Action<Error> action)
        {
            return source.BiBind(Prelude.SuccessEff, (Error err) => from _ in Prelude.Eff(() => Prelude.fun(action)(err))
                                                                    from _1 in Prelude.FailEff<T>(err)
                                                                    select _1);
        }

        //
        // Summary:
        //     Executes a specified action when the source operation fails, converting the error
        //     to an exception.
        //
        // Parameters:
        //   source:
        //     The operation that may succeed or fail, triggering the action on failure.
        //
        //   action:
        //     The action to execute when the operation fails, receiving the error as an exception.
        //
        //
        // Type parameters:
        //   T:
        //     Represents the type of the successful result from the operation.
        //
        // Returns:
        //     Returns a new operation that reflects the original operation's success or failure.
        public static Aff<T> OnFailError<T>(this Aff<T> source, Action<Error> action)
        {
            return source.BiBind((Func<T, Aff<T>>)Prelude.SuccessAff, (Func<Error, Aff<T>>)((Error err) => from _ in Prelude.Eff(() => Prelude.fun(action)(err))
                                                                                                           from _1 in Prelude.FailEff<T>(err)
                                                                                                           select _1));
        }

        //
        // Parameters:
        //   source:
        //
        //   action:
        //
        // Type parameters:
        //   T:
        public static Option<T> OnSome<T>(this Option<T> source, Action<T> action)
        {
            return source.Bind(delegate (T val)
            {
                action(val);
                return Prelude.Some(val);
            });
        }

        //
        // Parameters:
        //   source:
        //
        //   action:
        //
        // Type parameters:
        //   T:
        public static Aff<T> OnSucc<T>(this Aff<T> source, Action action)
        {
            return from val in source
                   from _ in Prelude.Eff(() => Prelude.fun(action)())
                   select (val);
        }

        //
        // Parameters:
        //   source:
        //
        //   action:
        //
        // Type parameters:
        //   T:
        public static Eff<T> OnSucc<T>(this Eff<T> source, Action action)
        {
            return from val in source
                   from _ in Prelude.Eff(() => Prelude.fun(action)())
                   select (val);
        }

        //
        // Parameters:
        //   source:
        //
        //   action:
        //
        // Type parameters:
        //   T:
        public static Aff<T> OnSucc<T>(this Aff<T> source, Action<T> action)
        {
            return from val in source
                   from _ in Prelude.Eff(() => Prelude.fun(action)(val))
                   select (val);
        }

        //
        // Parameters:
        //   source:
        //
        //   action:
        //
        // Type parameters:
        //   T:
        public static Eff<T> OnSucc<T>(this Eff<T> source, Action<T> action)
        {
            return from val in source
                   from _ in Prelude.Eff(() => Prelude.fun(action)(val))
                   select (val);
        }

        //
        // Summary:
        //     Executes the specified action if the result is a left value.
        //
        // Parameters:
        //   source:
        //     The source asynchronous effect.
        //
        //   action:
        //     The action to execute if the result is a left value.
        //
        // Type parameters:
        //   TLeft:
        //     The type of the left value.
        //
        //   TRight:
        //     The type of the right value.
        //
        // Returns:
        //     An asynchronous effect with the same result type.
        public static Aff<Either<TLeft, TRight>> OnSuccLeft<TLeft, TRight>(this Aff<Either<TLeft, TRight>> source, Action<TLeft> action)
        {
            return source.OnSucc(delegate (Either<TLeft, TRight> r)
            {
                r.IfLeft(action);
            });
        }

        //
        // Summary:
        //     Executes the specified action if the result is a left value.
        //
        // Parameters:
        //   source:
        //     The source asynchronous effect.
        //
        //   action:
        //     The action to execute if the result is a left value.
        //
        // Type parameters:
        //   TLeft:
        //     The type of the left value.
        //
        //   TRight:
        //     The type of the right value.
        //
        // Returns:
        //     An asynchronous effect with the same result type.
        public static Eff<Either<TLeft, TRight>> OnSuccLeft<TLeft, TRight>(this Eff<Either<TLeft, TRight>> source, Action<TLeft> action)
        {
            return source.OnSucc(delegate (Either<TLeft, TRight> r)
            {
                r.IfLeft(action);
            });
        }

        //
        // Summary:
        //     Executes the specified action if the result is a right value.
        //
        // Parameters:
        //   source:
        //     The source asynchronous effect.
        //
        //   action:
        //     The action to execute if the result is a right value.
        //
        // Type parameters:
        //   TLeft:
        //     The type of the left value.
        //
        //   TRight:
        //     The type of the right value.
        //
        // Returns:
        //     An asynchronous effect with the same result type.
        public static Eff<Either<TLeft, TRight>> OnSuccRight<TLeft, TRight>(this Eff<Either<TLeft, TRight>> source, Action<TRight> action)
        {
            return source.OnSucc(delegate (Either<TLeft, TRight> r)
            {
                r.IfRight(action);
            });
        }

        //
        // Summary:
        //     Executes the specified action if the result is a right value.
        //
        // Parameters:
        //   source:
        //     The source asynchronous effect.
        //
        //   action:
        //     The action to execute if the result is a right value.
        //
        // Type parameters:
        //   TLeft:
        //     The type of the left value.
        //
        //   TRight:
        //     The type of the right value.
        //
        // Returns:
        //     An asynchronous effect with the same result type.
        public static Aff<Either<TLeft, TRight>> OnSuccRight<TLeft, TRight>(this Aff<Either<TLeft, TRight>> source, Action<TRight> action)
        {
            return source.OnSucc(delegate (Either<TLeft, TRight> r)
            {
                r.IfRight(action);
            });
        }

        //
        // Summary:
        //     Runs the asynchronous effect synchronously and returns the result as a LanguageExt.Fin`1.
        //
        //
        // Parameters:
        //   source:
        //     The asynchronous effect to run.
        //
        // Type parameters:
        //   A:
        //     The type of the result.
        //
        // Returns:
        //     A LanguageExt.Fin`1 representing the result of the asynchronous effect.
        public static Fin<A> RunSynchronous<A>(this Aff<A> source)
        {
            try
            {
                ValueTask<Fin<A>> valueTask = source.Run();
                return valueTask.IsCompletedSuccessfully ? valueTask.Result : valueTask.AsTask().GetAwaiter().GetResult();
            }
            catch (Exception thisException)
            {
                return Prelude.FinFail<A>(Error.New(thisException));
            }
        }

        //
        // Summary:
        //     Creates an option containing a value if it satisfies a specified condition.
        //
        // Parameters:
        //   opt:
        //     The value that will be checked to see if it meets the specified condition.
        //
        //   predicate:
        //     A function that defines the condition the value must satisfy to be included in
        //     the option.
        //
        // Type parameters:
        //   T:
        //     Represents the type of the value being evaluated against the condition.
        //
        // Returns:
        //     An option containing the value if the condition is met, otherwise an empty option.
        public static Option<T> SomeIf<T>(this T opt, Func<T, bool> predicate)
        {
            Guard.Against.NullOrDefault(predicate, "predicate");
            return from o in Prelude.Optional(opt)
                   where predicate(o)
                   select o;
        }

        //
        // Parameters:
        //   opt:
        //
        //   predicate:
        //
        //   result:
        //
        // Type parameters:
        //   T:
        public static Option<T> SomeIf<T>(this T opt, Func<T, bool> predicate, T result)
        {
            Guard.Against.NullOrDefault(predicate, "predicate");
            return from o in Prelude.Optional(opt)
                   where predicate(o)
                   from val in Prelude.Optional(result)
                   select val;
        }

        //
        // Parameters:
        //   opt:
        //
        //   predicate:
        //
        //   result:
        //
        // Type parameters:
        //   T:
        public static Option<T> SomeIf<T>(this bool opt, Func<bool, bool> predicate, T result)
        {
            Guard.Against.NullOrDefault(predicate, "predicate");
            if (!predicate(opt))
            {
                return Option<T>.None;
            }

            return Prelude.Optional(result);
        }

        //
        // Parameters:
        //   source:
        //
        // Type parameters:
        //   T:
        public static Aff<Unit> ToAffUnit<T>(this Aff<T> source)
        {
            return source.Map((T _) => Prelude.unit);
        }

        //
        // Summary:
        //     Validate if a value is not null, if it is it will return an error
        //
        // Parameters:
        //   value:
        //
        //   arg:
        //
        // Type parameters:
        //   T:
        public static Eff<T> ToEff<T>(this T value, [System.Runtime.CompilerServices.CallerArgumentExpression("value")] string arg = null) where T : class
        {
            return Prelude.Eff(() => Prelude.Optional(value).ForceGetValue(arg + " is null"));
        }

        //
        // Parameters:
        //   source:
        //
        // Type parameters:
        //   T:
        public static Eff<Unit> ToEffUnit<T>(this Eff<T> source)
        {
            return source.Map((T _) => Prelude.unit);
        }

        //
        // Summary:
        //     Converts an Either type to an Option type, returning the left value if present.
        //
        //
        // Parameters:
        //   source:
        //     The Either instance being converted to an Option.
        //
        // Type parameters:
        //   A:
        //     Represents the type of the left value in the Either type.
        //
        //   B:
        //     Represents the type of the right value in the Either type.
        //
        // Returns:
        //     An Option containing the left value if it exists, otherwise None.
        public static Option<A> ToOptionLeft<A, B>(this Either<A, B> source)
        {
            return source.Match((B a) => Prelude.None, (A b) => Prelude.Some(b));
        }

        //
        // Parameters:
        //   source:
        //
        //   exceptionMsg:
        //
        //   memberName:
        //
        //   sourceFilePath:
        //
        //   sourceLineNumber:
        //
        // Type parameters:
        //   T:
        public static Fin<T> UpdateFailure<T>(this Fin<T> source, NotEmptyString exceptionMsg, [CallerMemberName] string memberName = "", [CallerFilePath] string sourceFilePath = "", [CallerLineNumber] int sourceLineNumber = 0)
        {
            string item = "Caller method: " + memberName;
            string item2 = "source file path: " + sourceFilePath;
            string item3 = $"source file line: {sourceLineNumber}";
            List<string> source2 = new List<string> { exceptionMsg, item, item2, item3 };
            string messages = source2.ConvertToString();
            return source.Match((T r) => r, (Error ex) => (!source.IsBottom) ? Fin<T>.Fail(new Exception(messages, ex)) : Fin<T>.Fail(new Exception(messages)));
        }

        //
        // Summary:
        //     Gets the sequence inside the option or returns an empty sequence if None.
        //
        // Parameters:
        //   option:
        //     The option.
        //
        // Type parameters:
        //   T:
        //     The type of the elements in the sequence.
        //
        // Returns:
        //     The sequence inside the option or an empty sequence if None.
        public static IEnumerable<T> GetSeqOrDefault<T>(this Option<IEnumerable<T>> option)
        {
            return option.AsEnumerable().SelectMany((IEnumerable<T> r) => r).ToList();
        }

        //
        // Summary:
        //     Maps the result of an asynchronous computation to a unit value.
        //
        // Parameters:
        //   source:
        //     The asynchronous computation.
        //
        // Type parameters:
        //   T:
        //     The type of the result of the asynchronous computation.
        //
        // Returns:
        //     An asynchronous computation that returns a unit value.
        public static Aff<Unit> MapToUnit<T>(this Aff<T> source)
        {
            return source.Map((T _) => Unit.Default);
        }

        //
        // Summary:
        //     Wraps an IEnumerable into an Option.
        //
        // Parameters:
        //   value:
        //     The IEnumerable to wrap.
        //
        // Type parameters:
        //   T:
        //     The type of the elements in the IEnumerable.
        //
        // Returns:
        //     An Option that contains the IEnumerable if it is not null or empty, otherwise
        //     None.
        public static Option<IEnumerable<T>> SeqToOption<T>(this IEnumerable<T> value)
        {
            if (value == null)
            {
                return Option<IEnumerable<T>>.None;
            }

            if (!value.Any())
            {
                return Option<IEnumerable<T>>.None;
            }

            return Prelude.Some(value);
        }

        //
        // Summary:
        //     Wraps an array into an Option.
        //
        // Parameters:
        //   value:
        //     The array to wrap.
        //
        // Type parameters:
        //   T:
        //     The type of the elements in the array.
        //
        // Returns:
        //     An Option that contains the array if it is not null or empty, otherwise None.
        public static Option<T[]> SeqToOption<T>(this T[] value)
        {
            if (value == null)
            {
                return Option<T[]>.None;
            }

            if (!value.Length.IsGt(0))
            {
                return Option<T[]>.None;
            }

            return Prelude.Some(value);
        }

        //
        // Summary:
        //     Wraps a List into an Option.
        //
        // Parameters:
        //   value:
        //     The List to wrap.
        //
        // Type parameters:
        //   T:
        //     The type of the elements in the List.
        //
        // Returns:
        //     An Option that contains the List if it is not null or empty, otherwise None.
        public static Option<List<T>> SeqToOption<T>(this List<T> value)
        {
            if (value == null)
            {
                return Option<List<T>>.None;
            }

            if (!value.Count.IsGt(0))
            {
                return Option<List<T>>.None;
            }

            return Prelude.Some(value);
        }

        //
        // Summary:
        //     Wraps an IList into an Option.
        //
        // Parameters:
        //   value:
        //     The IList to wrap.
        //
        // Type parameters:
        //   T:
        //     The type of the elements in the IList.
        //
        // Returns:
        //     An Option that contains the IList if it is not null or empty, otherwise None.
        public static Option<IList<T>> SeqToOption<T>(this IList<T> value)
        {
            if (value == null)
            {
                return Option<IList<T>>.None;
            }

            if (!value.Any())
            {
                return Option<IList<T>>.None;
            }

            return Prelude.Some(value);
        }

        //
        // Summary:
        //     Wraps an IReadOnlyList into an Option.
        //
        // Parameters:
        //   value:
        //     The IReadOnlyList to wrap.
        //
        // Type parameters:
        //   T:
        //     The type of the elements in the IReadOnlyList.
        //
        // Returns:
        //     An Option that contains the IReadOnlyList if it is not null or empty, otherwise
        //     None.
        public static Option<IReadOnlyList<T>> SeqToOption<T>(this IReadOnlyList<T> value)
        {
            if (value == null)
            {
                return Option<IReadOnlyList<T>>.None;
            }

            if (!value.Any())
            {
                return Option<IReadOnlyList<T>>.None;
            }

            return Prelude.Some(value);
        }

        //
        // Summary:
        //     Wraps an IReadOnlyCollection into an Option.
        //
        // Parameters:
        //   value:
        //     The IReadOnlyCollection to wrap.
        //
        // Type parameters:
        //   T:
        //     The type of the elements in the IReadOnlyCollection.
        //
        // Returns:
        //     An Option that contains the IReadOnlyCollection if it is not null or empty, otherwise
        //     None.
        public static Option<IReadOnlyCollection<T>> SeqToOption<T>(this IReadOnlyCollection<T> value)
        {
            if (value == null)
            {
                return Option<IReadOnlyCollection<T>>.None;
            }

            if (!value.Count.IsGt(0))
            {
                return Option<IReadOnlyCollection<T>>.None;
            }

            return Prelude.Some(value);
        }

        //
        // Summary:
        //     Unwraps an LanguageExt.Option`1 inside an LanguageExt.Aff`1 into an LanguageExt.Aff`1.
        //
        //
        // Parameters:
        //   source:
        //
        // Type parameters:
        //   T:
        //
        // Returns:
        //     Success if the option is some otherwiser a fail Eff
        public static Aff<T> ToAff<T>(this Aff<Option<T>> source)
        {
            return from r in source.Where(delegate (Option<T> r)
            {
                Option<T> option = r;
                return option.IsSome;
            })
                   select r.ForceGetValue("r");
        }

        //
        // Summary:
        //     Unwraps an LanguageExt.Option`1 inside an LanguageExt.Eff`1 into an LanguageExt.Eff`1.
        //
        //
        // Parameters:
        //   source:
        //
        // Type parameters:
        //   T:
        //
        // Returns:
        //     Success if the option is some otherwiser a fail Eff
        public static Eff<T> ToEff<T>(this Eff<Option<T>> source)
        {
            return from r in source.Where(delegate (Option<T> r)
            {
                Option<T> option = r;
                return option.IsSome;
            })
                   select r.ForceGetValue("r");
        }

        //
        // Summary:
        //     Unwraps an LanguageExt.Either`2 inside an LanguageExt.Aff`1 into an LanguageExt.Aff`1
        //     containing the left value.
        //
        // Parameters:
        //   source:
        //     The asynchronous computation containing an Either.
        //
        // Type parameters:
        //   TLeft:
        //     The type of the left value in the Either.
        //
        //   TRight:
        //     The type of the right value in the Either.
        //
        // Returns:
        //     An asynchronous computation that returns the left value if the Either is left,
        //     otherwise a failed computation.
        public static Aff<TLeft> ToLeftAff<TLeft, TRight>(this Aff<Either<TLeft, TRight>> source)
        {
            return source.Where((Either<TLeft, TRight> r) => r.IsLeft).Map((Either<TLeft, TRight> r) => r.First().Left);
        }

        //
        // Summary:
        //     Unwraps an LanguageExt.Either`2 inside an LanguageExt.Eff`1 into an LanguageExt.Eff`1
        //     containing the left value.
        //
        // Parameters:
        //   source:
        //     The asynchronous computation containing an Either.
        //
        // Type parameters:
        //   TLeft:
        //     The type of the left value in the Either.
        //
        //   TRight:
        //     The type of the right value in the Either.
        //
        // Returns:
        //     An asynchronous computation that returns the left value if the Either is left,
        //     otherwise a failed computation.
        public static Eff<TLeft> ToLeftEff<TLeft, TRight>(this Eff<Either<TLeft, TRight>> source)
        {
            return source.Where((Either<TLeft, TRight> r) => r.IsLeft).Map((Either<TLeft, TRight> r) => r.First().Left);
        }

        //
        // Summary:
        //     Unwraps an LanguageExt.Either`2 inside an LanguageExt.Aff`1 into an LanguageExt.Aff`1
        //     containing the right value.
        //
        // Parameters:
        //   source:
        //     The asynchronous computation containing an Either.
        //
        // Type parameters:
        //   TLeft:
        //     The type of the left value in the Either.
        //
        //   TRight:
        //     The type of the right value in the Either.
        //
        // Returns:
        //     An asynchronous computation that returns the right value if the Either is right,
        //     otherwise a failed computation.
        public static Aff<TRight> ToRightAff<TLeft, TRight>(this Aff<Either<TLeft, TRight>> source)
        {
            return source.Where((Either<TLeft, TRight> r) => r.IsRight).Map((Either<TLeft, TRight> r) => r.First().Right);
        }

        //
        // Summary:
        //     Unwraps an LanguageExt.Either`2 inside an LanguageExt.Eff`1 into an LanguageExt.Eff`1
        //     containing the right value.
        //
        // Parameters:
        //   source:
        //     The asynchronous computation containing an Either.
        //
        // Type parameters:
        //   TLeft:
        //     The type of the left value in the Either.
        //
        //   TRight:
        //     The type of the right value in the Either.
        //
        // Returns:
        //     An asynchronous computation that returns the right value if the Either is right,
        //     otherwise a failed computation.
        public static Eff<TRight> ToRightEff<TLeft, TRight>(this Eff<Either<TLeft, TRight>> source)
        {
            return source.Where((Either<TLeft, TRight> r) => r.IsRight).Map((Either<TLeft, TRight> r) => r.First().Right);
        }
    }
}
