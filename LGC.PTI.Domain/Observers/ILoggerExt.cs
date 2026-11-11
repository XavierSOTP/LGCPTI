namespace LGC.PTI.Domain.Observers
{
    /// <summary>
    /// Defines the employee actions that can be performed against PTI.
    /// </summary>
    internal enum CRUDAction
    {
        /// <summary>
        /// Indicates that no action has been performed.
        /// </summary>
        None = 0,

        /// <summary>
        /// Indicates that an employee was added.
        /// </summary>
        Added = 1,

        /// <summary>
        /// Indicates that an employee was updated.
        /// </summary>
        Updated = 2,

        /// <summary>
        /// Indicates that an employee was deleted or deactivated.
        /// </summary>
        Deleted = 3,
    }

    /// <summary>
    /// Provides logging extension methods for employee operations performed against PTI.
    /// </summary>
    public static class ILoggerExt
    {
        /// <summary>
        /// Logs an error when an employee action cannot be completed successfully in PTI.
        /// </summary>
        /// <param name="logger">
        /// The logger used to write the error message.
        /// </param>
        /// <param name="dto">
        /// The employee associated with the failed action.
        /// </param>
        /// <param name="action">
        /// The employee action that could not be completed.
        /// </param>
        internal static void LogErrorOnEmployeeAction(this Microsoft.Extensions.Logging.ILogger logger, EmployeeEntity dto, CRUDAction action)
        {
            logger.Error($"{nameof(PTI)} Error, could not {action} employee: {dto.FirstName} {dto.LastName} with credential: {dto.CredentialGuid}.");
        }

        /// <summary>
        /// Logs a successfully completed employee action.
        /// </summary>
        /// <param name="logger">
        /// The logger used to write the informational message.
        /// </param>
        /// <param name="dto">
        /// The employee associated with the completed action.
        /// </param>
        /// <param name="action">
        /// The employee action that was completed.
        /// </param>
        internal static void LogEmployeeAction(this Microsoft.Extensions.Logging.ILogger logger, EmployeeEntity dto, CRUDAction action)
        {
            logger.Info($"Employee: {dto.FirstName} {dto.LastName} with credential: {dto.CredentialGuid} was {action}.");
        }
    }
}