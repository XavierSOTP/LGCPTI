using LanguageExt;
using SoftSolute.Common.Utilities;
using SoftSolute.Common.Utilities.LanguageExtSS;
using System.Text.RegularExpressions;

namespace LGC.PTI.Domain;

/// <summary>
/// Provides helper methods for extracting card numbers from credential values.
/// </summary>
public static class CardNumberHelper
{
    #region Public Methods

    /// <summary>
    /// Attempts to extract the card number from the specified credential value.
    /// </summary>
    /// <param name="input">
    /// The credential value containing the card number.
    /// </param>
    /// <returns>
    /// An <see cref="Option{T}"/> containing the extracted card number when the
    /// input matches the expected format; otherwise, an empty option.
    /// </returns>
    public static Option<int> TryGetCardNumber(NotEmptyString input)
    {
        Match match = _regex.Match(input);
        var opt = match.SomeIf(m => m.Success);
        var result = opt.Map(m =>
        {
            if (m.Groups.Count > 1 && m.Groups[1].Success)
            {
                return m.Groups[1].Value;
            }

            return GET_DEFAULT_VALUE;
        });

        return result.Map(int.Parse);
    }

    /// <summary>Returns the original card number text, retaining leading zeros for GraphQL PINs.</summary>
    public static Option<string> TryGetCardNumberText(string credentialName)
    {
        if (string.IsNullOrWhiteSpace(credentialName)) return Option<string>.None;
        var match = _regex.Match(credentialName);
        return match.Success && match.Groups["cardNumber"].Success ? (Option<string>)match.Groups["cardNumber"].Value : Option<string>.None;
    }

    #endregion Public Methods

    #region Private Fields

    private const string GET_DEFAULT_VALUE = "0000";
    private static readonly Regex _regex = new(@"^\w+[_-](?<cardNumber>\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase, TimeSpan.FromMilliseconds(250));

    #endregion Private Fields
}