using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LGC.PIT.Plugin.Common.Database;

public interface ISqlExecutor
{
    /// <summary>
    /// Returns a SQL Server database connection that was obtained from the Genetec plugin run.
    /// </summary>
    /// <remarks>Ensure proper disposal of the connection to avoid resource leaks.</remarks>
    /// <returns>An instance of <see cref="SqlConnection"/> that is open and ready for use. The caller is responsible for
    /// open and close the connection when it is no longer needed.</returns>
    SqlConnection GetConnection();

    /// <summary>
    /// Creates a new SqlCommand object for the specified SQL query.
    /// </summary>
    /// <param name="query">The SQL query string to be executed. Cannot be null or empty.</param>
    /// <returns>A SqlCommand instance configured with the provided query.</returns>
    SqlCommand CreateCommand(string query);

    /// <summary>
    /// Executes the specified SQL query and allows custom configuration of the underlying SqlCommand before
    /// execution.
    /// </summary>
    /// <remarks>Use the commandConfiguration parameter to set command properties such as parameters,
    /// timeout, or transaction before the query is executed. The method does not return any result sets; it only
    /// reports the number of affected rows.</remarks>
    /// <param name="query">The SQL query string to execute. Cannot be null or empty.</param>
    /// <param name="commandConfiguration">A delegate that configures the SqlCommand instance prior to execution. Cannot be null.</param>
    /// <returns>The number of rows affected by the execution of the query.</returns>
    Task<int> ExecuteAsync(string query, Action<SqlCommand> commandConfiguration);

    /// <summary>
    /// Executes the specified SQL query and returns the first column of the first row in the result set.
    /// </summary>
    /// <remarks>Use this method to retrieve a single value from the database, such as a count or
    /// aggregate result. The commandConfiguration action allows customization of the SqlCommand, such as adding
    /// parameters or modifying command settings.</remarks>
    /// <param name="query">The SQL query to execute. Cannot be null or empty.</param>
    /// <param name="commandConfiguration">An action to configure the SqlCommand before execution. Used to set parameters or command properties.</param>
    /// <returns>The value of the first column in the first row of the result set, or null if the result set is empty.</returns>
    object GetScalar(string query, Action<SqlCommand> commandConfiguration);

    /// <summary>
    /// Executes the specified SQL query and maps the results to a collection of type T using the provided fieldMapper function. 
    /// </summary>
    /// <typeparam name="T"></typeparam>
    /// <param name="query"></param>
    /// <param name="commandConfiguration"></param>
    /// <param name="fieldMapper"></param>
    /// <returns></returns>
    Task<IList<T>> GetQueryResultsAsync<T>(string query, Action<SqlCommand> commandConfiguration, Func<SqlDataReader, T> fieldMapper, [CallerMemberName] string callerName = "");
    Task<object> GetScalarAsync(string query, Action<SqlCommand> commandConfiguration);
    int Execute(string query, Action<SqlCommand> commandConfiguration);
    IList<T> GetQueryResults<T>(string query, Action<SqlCommand> commandConfiguration, Func<SqlDataReader, T> fieldMapper, [CallerMemberName] string callerName = "");
}

