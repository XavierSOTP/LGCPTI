using SoftSolute.Common.Utilities.CollectionSS;

namespace LGC.PTI.Domain.Models;

/// <summary>
/// Represents an employee associated with a Genetec cardholder and credential.
/// </summary>
public sealed class EmployeeModel
{
    #region Public Properties

    /// <summary>
    /// Gets a comparer that determines employee equality based on the credential identifier.
    /// </summary>
    public static GenericComparer<EmployeeModel> CredentialEqualityComparer => new(x => x.CredentialGuid);

    /// <summary>
    /// Gets or sets the unique identifier of the associated cardholder.
    /// </summary>
    public Guid CardholderGuid { get; set; }

    /// <summary>
    /// Gets or sets the unique identifier of the associated credential.
    /// </summary>
    public Guid CredentialGuid { get; set; }

    /// <summary>
    /// Gets or sets the employee number.
    /// </summary>
    public string EmployeeNumber { get; set; }

    /// <summary>
    /// Gets the employee's full name composed of the first and last name.
    /// </summary>
    public string Name => $"{FirstName} {LastName}";

    /// <summary>
    /// Gets or sets the employee's first name.
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the employee's last name.
    /// </summary>
    public string LastName { get; set; }

    #endregion Public Properties

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeModel"/> class.
    /// </summary>
    [JsonConstructor]
    public EmployeeModel()
    {
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>
    /// Determines whether the specified object represents the same employee
    /// based on the cardholder identifier and employee number.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when the specified object represents the same employee;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object obj)
    {
        return obj is EmployeeModel model
            && StringComparer.Ordinal.Equals(CardholderGuid.ToString(), model.CardholderGuid.ToString())
            && string.Equals(EmployeeNumber, model.EmployeeNumber, StringComparison.OrdinalIgnoreCase);
    }

    /// <summary>
    /// Returns a hash code for the current employee instance.
    /// </summary>
    /// <returns>
    /// A hash code derived from the cardholder identifier and employee number.
    /// </returns>
    public override int GetHashCode()
    {
        int hashCode = -805205156;
        hashCode = hashCode * -1521134295 + StringComparer.Ordinal.GetHashCode(CardholderGuid.ToString());
        hashCode = hashCode * -1521134295 + StringComparer.Ordinal.GetHashCode(EmployeeNumber);
        return hashCode;
    }

    #endregion Public Methods
}