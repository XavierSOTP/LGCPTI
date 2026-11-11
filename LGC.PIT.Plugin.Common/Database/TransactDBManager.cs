using Genetec.Sdk.Plugin;
using SoftSolute.Plugin.Common.Helpers;
using System;

namespace LGC.PIT.Plugin.Common.Database;

public class TransactDBManager : DatabaseManager
{
    private DatabaseConfiguration _databaseConfiguration;

    public override string GetSpecificCreationScript(string databaseName)
    {
        return (Properties.Resources.CreateDatabase).Replace("DATABASE_NAME", databaseName);
    }

    public override void SetDatabaseInformation(DatabaseConfiguration databaseConfiguration) => _databaseConfiguration = databaseConfiguration;

    public string GetConnectionString()
    {
        var connectionString = _databaseConfiguration.ConnectionString;

        if (connectionString.IsNullOrEmpty())
        {
            throw new InvalidOperationException("Connection string is not set in the database configuration.");
        }

        return connectionString;
    }
}

