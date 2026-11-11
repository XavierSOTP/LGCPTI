using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using System.Runtime.CompilerServices;
using System.Threading.Tasks;

namespace LGC.PIT.Plugin.Common.Database;

public class SqlExecutor : ISqlExecutor
{
    public string ConnectionString { get; }

    public SqlExecutor(string connectionString)
    {
        if (string.IsNullOrWhiteSpace(connectionString))
            throw new ArgumentNullException(nameof(connectionString));

        ConnectionString = connectionString;
    }

    public SqlConnection GetConnection() => new SqlConnection(ConnectionString);

    public SqlCommand CreateCommand(string query)
    {
        var connection = GetConnection();
        var command = connection.CreateCommand();

        command.CommandText = query;
        command.CommandType = CommandType.Text;

        return command;
    }

    #region Execute

    public int Execute(string query, Action<SqlCommand> commandConfiguration)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentNullException(nameof(query));

        if (commandConfiguration == null)
            throw new ArgumentNullException(nameof(commandConfiguration));

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = query;
        command.CommandType = CommandType.Text;

        try
        {
            connection.Open();

            commandConfiguration(command);

            System.Diagnostics.Debug.WriteLine($"Executing SQL Command: {command}");

            return command.ExecuteNonQuery();
        }
        catch (SqlException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }
    }

    public async Task<int> ExecuteAsync(string query, Action<SqlCommand> commandConfiguration)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentNullException(nameof(query));

        if (commandConfiguration == null)
            throw new ArgumentNullException(nameof(commandConfiguration));

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = query;
        command.CommandType = CommandType.Text;

        try
        {
            await connection.OpenAsync();

            commandConfiguration(command);

            System.Diagnostics.Debug.WriteLine($"Executing SQL Command: {command}");

            return await command.ExecuteNonQueryAsync();
        }
        catch (SqlException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }
    }

    #endregion

    #region Scalar

    public object GetScalar(string query, Action<SqlCommand> commandConfiguration)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentNullException(nameof(query));

        if (commandConfiguration == null)
            throw new ArgumentNullException(nameof(commandConfiguration));

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = query;
        command.CommandType = CommandType.Text;

        try
        {
            connection.Open();

            commandConfiguration(command);

            return command.ExecuteScalar();
        }
        catch (SqlException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }
    }

    public async Task<object> GetScalarAsync(string query, Action<SqlCommand> commandConfiguration)
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentNullException(nameof(query));

        if (commandConfiguration == null)
            throw new ArgumentNullException(nameof(commandConfiguration));

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = query;
        command.CommandType = CommandType.Text;

        try
        {
            await connection.OpenAsync();

            commandConfiguration(command);

            return await command.ExecuteScalarAsync();
        }
        catch (SqlException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }
    }

    #endregion

    #region Query

    public IList<T> GetQueryResults<T>(
        string query,
        Action<SqlCommand> commandConfiguration,
        Func<SqlDataReader, T> fieldMapper,
        [CallerMemberName] string callerName = "")
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentNullException(nameof(query));

        if (commandConfiguration == null)
            throw new ArgumentNullException(nameof(commandConfiguration));

        if (fieldMapper == null)
            throw new ArgumentNullException(nameof(fieldMapper));

        List<T> results = new();

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = query;
        command.CommandType = CommandType.Text;

        try
        {
            connection.Open();

            commandConfiguration(command);

            using var dataReader = command.ExecuteReader();

            while (dataReader.Read())
            {
                var record = fieldMapper(dataReader);
                results.Add(record);
            }

            dataReader.Close();
        }
        catch (SqlException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }

        return results;
    }

    public async Task<IList<T>> GetQueryResultsAsync<T>(
        string query,
        Action<SqlCommand> commandConfiguration,
        Func<SqlDataReader, T> fieldMapper,
        [CallerMemberName] string callerName = "")
    {
        if (string.IsNullOrWhiteSpace(query))
            throw new ArgumentNullException(nameof(query));

        if (commandConfiguration == null)
            throw new ArgumentNullException(nameof(commandConfiguration));

        if (fieldMapper == null)
            throw new ArgumentNullException(nameof(fieldMapper));

        List<T> results = new();

        using var connection = GetConnection();
        using var command = connection.CreateCommand();

        command.CommandText = query;
        command.CommandType = CommandType.Text;

        try
        {
            await connection.OpenAsync();

            commandConfiguration(command);

            using var dataReader = await command.ExecuteReaderAsync();

            while (await dataReader.ReadAsync())
            {
                var record = fieldMapper(dataReader);
                results.Add(record);
            }

            dataReader.Close();
        }
        catch (SqlException ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine(ex, nameof(SqlExecutor));
            throw;
        }

        return results;
    }

    #endregion
}

