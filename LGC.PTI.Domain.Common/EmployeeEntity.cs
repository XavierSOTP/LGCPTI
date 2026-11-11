using PTIDtos = LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;

namespace LGC.PTI.Domain.Models;

/// <summary>
/// Represents an employee synchronized between Genetec and the PTI system.
/// </summary>
/// <remarks>
/// The entity contains employee information together with the associated Genetec
/// cardholder and credential identifiers used during PTI synchronization.
/// </remarks>
public sealed class EmployeeEntity
{
    #region Public Properties

    /// <summary>
    /// Gets or sets a value indicating whether the employee is active.
    /// </summary>
    public bool Active { get; set; }

    /// <summary>
    /// Gets or sets the badge value associated with the employee.
    /// </summary>
    public string Badge { get; set; }

    /// <summary>
    /// Gets or sets the Genetec cardholder identifier associated with the employee.
    /// </summary>
    public Guid CardholderGuid { get; set; }

    /// <summary>
    /// Gets or sets the Genetec credential identifier associated with the employee.
    /// </summary>
    public Guid CredentialGuid { get; set; }

    /// <summary>
    /// Gets or sets the name of the credential associated with the employee.
    /// </summary>
    public string CredName { get; set; }

    /// <summary>
    /// Gets or sets the employee department.
    /// </summary>
    public string Department { get; set; }

    /// <summary>
    /// Gets or sets the employee number used by PTI.
    /// </summary>
    public string EmployeeNumber { get; set; }

    /// <summary>
    /// Gets or sets the employee first name.
    /// </summary>
    public string FirstName { get; set; }

    /// <summary>
    /// Gets or sets the employee language used by PTI.
    /// </summary>
    /// <remarks>
    /// The default value is <see cref="PTIDtos.Language.French"/>.
    /// </remarks>
    public PTIDtos.Language Language { get; set; } = PTIDtos.Language.French;

    /// <summary>
    /// Gets or sets the employee last name.
    /// </summary>
    public string LastName { get; set; }

    /// <summary>
    /// Gets the employee full name by combining <see cref="FirstName"/> and <see cref="LastName"/>.
    /// </summary>
    public string Name => $"{FirstName} {LastName}";

    /// <summary>
    /// Gets or sets the employee PIN.
    /// </summary>
    public int Pin { get; set; }

    #endregion Public Properties

    #region Public Constructors

    /// <summary>
    /// Initializes a new instance of the <see cref="EmployeeEntity"/> class.
    /// </summary>
    public EmployeeEntity()
    {
    }

    #endregion Public Constructors

    #region Public Methods

    /// <summary>
    /// Determines whether the specified object represents the same employee.
    /// </summary>
    /// <param name="obj">
    /// The object to compare with the current instance.
    /// </param>
    /// <returns>
    /// <see langword="true"/> when <paramref name="obj"/> is an <see cref="EmployeeEntity"/>
    /// with the same <see cref="CardholderGuid"/> and <see cref="EmployeeNumber"/>;
    /// otherwise, <see langword="false"/>.
    /// </returns>
    public override bool Equals(object obj)
    {
        return obj is EmployeeEntity model &&
               CardholderGuid.Equals(model.CardholderGuid) &&
               EmployeeNumber == model.EmployeeNumber;
    }

    /// <summary>
    /// Returns a hash code for the current employee.
    /// </summary>
    /// <returns>
    /// A hash code based on <see cref="CardholderGuid"/> and <see cref="EmployeeNumber"/>.
    /// </returns>
    public override int GetHashCode()
    {
        int hashCode = -805205156;
        hashCode = hashCode * -1521134295 + StringComparer.Ordinal.GetHashCode(CardholderGuid.ToString());
        hashCode = hashCode * -1521134295 + EmployeeNumber.GetHashCode();
        return hashCode;
    }

    #endregion Public Methods
}