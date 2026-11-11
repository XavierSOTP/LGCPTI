using Genetec.Sdk.Credentials;
using Genetec.Sdk.Entities;
using LanguageExt;
using LanguageExt.Common;
using SoftSolute.Common.Utilities;
using SoftSolute.Common.Utilities.Functional;
using SoftSolute.Common.Utilities.LanguageExtSS;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace LGC.PTI.Domain.Models;

/// <summary>
/// Provides a fluent builder for creating and configuring <see cref="EmployeeEntity"/>
/// instances from Genetec cardholders and credentials.
/// </summary>
public sealed class EmployeeBuilder : FunctionalBuilder<EmployeeEntity, EmployeeBuilder>
{
    #region Public Methods

    /// <summary>
    /// Creates a new <see cref="EmployeeBuilder"/> instance.
    /// Mandatory settings are <see cref="New(NotNull{Cardholder})"/>,
    /// <see cref="SetBadge(NotNull{Credential})"/>, and <see cref="SetDepartment(string)"/>.
    /// </summary>
    /// <returns>
    /// A new <see cref="EmployeeBuilder"/> instance.
    /// </returns>
    public static EmployeeBuilder New() => new();

    /// <summary>
    /// Creates a new <see cref="EmployeeBuilder"/> instance intended for employee deactivation.
    /// </summary>
    /// <returns>
    /// A new <see cref="EmployeeBuilder"/> instance that does not require a credential.
    /// </returns>
    public static EmployeeBuilder NewForDeactivation() => new(false);

    /// <summary>
    /// Configures the employee using information from the specified Genetec cardholder.
    /// </summary>
    /// <param name="cardholder">
    /// The non-null Genetec cardholder used to populate the employee information.
    /// </param>
    /// <returns>
    /// The current <see cref="EmployeeBuilder"/> instance.
    /// </returns>
    public EmployeeBuilder New(NotNull<Cardholder> cardholder)
    {
        var ch = cardholder.Value;
        return Setter(setter: p =>
        {
            p.FirstName = ch.FirstName;
            p.LastName = ch.LastName;
            p.CardholderGuid = ch.Guid;
            var digits = new string([.. ch.Guid.ToString().Where(char.IsDigit)]);
            var validLong = digits.PadRight(8, '0').Substring(0, 8);
            p.EmployeeNumber = validLong;
            return LanguageExt.Unit.Default;
        }, validation: () => ch.FirstName.IsNotNullOrEmpty() && ch.LastName.IsNotNullOrEmpty()
         ? new Result<LanguageExt.Unit>(LanguageExt.Unit.Default)
        : new Result<LanguageExt.Unit>(new InvalidOperationException($"invalid cardholder : {ch.Name}." +
        $" {nameof(Cardholder.FirstName)} and {nameof(Cardholder.LastName)} should not be null or empty string.")));
    }

    /// <summary>
    /// Configures the employee badge and PIN using the specified Genetec credential.
    /// </summary>
    /// <param name="cr">
    /// The non-null credential used to populate badge, PIN, credential identifier,
    /// and credential name information.
    /// </param>
    /// <returns>
    /// The current <see cref="EmployeeBuilder"/> instance.
    /// </returns>
    public EmployeeBuilder SetBadge(NotNull<Credential> cr)
    {
        var credential = cr.Value;
        var badge = GetWiegandFormatString(credential);

        var pin = CardNumberHelper.TryGetCardNumber(credential.Name);
        return Setter(setter: p =>
        {
            p.Pin = pin.IfNone(0);
            p.CredentialGuid = credential.Guid;
            p.Badge = badge.ForceGetValue();
            p.CredName = credential.Name;
            return LanguageExt.Unit.Default;
        }, validation: () => badge.IsSome && pin.IsSome
         ? new Result<LanguageExt.Unit>(LanguageExt.Unit.Default)
        : new Result<LanguageExt.Unit>(new InvalidOperationException($"invalid credential: {credential.Name}." +
        $"{nameof(Credential)} Name should containg digits.")));
    }

    /// <summary>
    /// Sets the employee department.
    /// </summary>
    /// <param name="depart">
    /// The department value to assign to the employee.
    /// </param>
    /// <returns>
    /// The current <see cref="EmployeeBuilder"/> instance.
    /// </returns>
    public EmployeeBuilder SetDepartment(string depart)
    {
        return Setter(setter: p =>
        {
            p.Department = depart.Trim();
            return LanguageExt.Unit.Default;
        }, validation: () => depart.IsNotNullOrEmpty()
                ? new Result<LanguageExt.Unit>(LanguageExt.Unit.Default)

        : new Result<LanguageExt.Unit>(new InvalidOperationException($"{nameof(depart)}, can not be null or empty.")));
    }

    /// <summary>
    /// Sets the active state of the employee.
    /// </summary>
    /// <param name="state">
    /// <see langword="true"/> to mark the employee as active; otherwise,
    /// <see langword="false"/>.
    /// </param>
    /// <returns>
    /// The current <see cref="EmployeeBuilder"/> instance.
    /// </returns>
    public EmployeeBuilder SetState(bool state)
    {
        return Setter(setter: p =>
        {
            p.Active = state;
            return LanguageExt.Unit.Default;
        }, validation: () => new Result<LanguageExt.Unit>(LanguageExt.Unit.Default));
    }

    /// <summary>
    /// Attempts to create the external badge representation for a supported
    /// Wiegand credential format.
    /// </summary>
    /// <param name="cred">
    /// The credential whose Wiegand format should be converted.
    /// </param>
    /// <returns>
    /// An <see cref="Option{T}"/> containing the formatted badge value when the
    /// credential uses a supported Wiegand format; otherwise, an empty option.
    /// </returns>
    public static Option<string> GetWiegandFormatString(Credential cred)
    {
        static string CreateCardId(long cardId)
        {
            var cardIdStr = cardId.ToString(CultureInfo.InvariantCulture).PadLeft(5, '0');
            return cardIdStr;
        }
        if (!(cred.Format is WiegandCredentialFormat)) return Option<string>.None;
        return cred.Format switch
        {
            Wiegand48BitCorporate1000CredentialFormat format => (Option<string>)$"{format.Facility}-{CreateCardId(format.CardId)}",
            WiegandCorporate1000CredentialFormat format => (Option<string>)$"{format.Facility}-{CreateCardId(format.CardId)}",
            WiegandH10304CredentialFormat format => (Option<string>)$"{format.Facility}-{CreateCardId(format.CardId)}",
            WiegandH10306CredentialFormat format => (Option<string>)$"{format.Facility}-{CreateCardId(format.CardId)}",
            WiegandStandardCredentialFormat format => (Option<string>)$"{format.Facility}-{CreateCardId(format.CardId)}",
            WiegandUndecodedCredentialFormat format => (Option<string>)$"{CreateCardId(format.CardId)}",
            WiegandCsn32CredentialFormat format => (Option<string>)$"{CreateCardId(format.CardId)}",
            WiegandH10302CredentialFormat format => (Option<string>)$"{CreateCardId(format.CardId)}",
            _ => Option<string>.None,
        };
    }

    #endregion Public Methods

    #region Private Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeBuilder"/> class.
    /// </summary>
    /// <param name="requireCredential">
    /// Indicates whether configuring a credential is required before the employee can be built.
    /// </param>
    private EmployeeBuilder(bool requireCredential = true) : base()
    {
        RequiredSetActions = requireCredential
            ? new List<string> { nameof(New), nameof(SetBadge), nameof(SetDepartment) }
            : new List<string> { nameof(New) };
    }

    #endregion Private Constructors
}