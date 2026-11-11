using Genetec.Sdk.Plugin;
using SampleServer.Persistence;
using SampleServer.Persistence.Contracts;
using SampleServer.Persistence.Modules;
using SampleServer.Persistence.Scripts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SampleServer.DBManager
{
    public class PluginDBManager : DatabaseManager
    {

        private IConnectionFactory m_connectionFactory;
        public IDbManager DbManager { get; set; }


        public PluginDBManager()

        {
            m_connectionFactory = new ConnectionFactory(null);
            DbManager = new DbManager(m_connectionFactory);
        }

        public override string GetSpecificCreationScript(string databaseName)
        {
            return Resources.CreateDatabase.Replace(DbInformation.DBNAMEPLACEHOLDER, databaseName);
        }

        public override void SetDatabaseInformation(DatabaseConfiguration databaseConfiguration)
        {
            m_connectionFactory.UpdateInformation(databaseConfiguration);

        }



        /*
         
         
        public string GetSpecificCreationScript(string databaseName)
        {
            return Resources.CreateDatabase.Replace(DbInformation.DBNAMEPLACEHOLDER, databaseName);
        }

        public void SetDatabaseInformation(IDatabaseConfiguration databaseConfiguration)
        {
            m_connectionFactory.UpdateInformation(new DbInformation(databaseConfiguration.DatabaseName, databaseConfiguration.DatabaseInstance));
        }

        public IEnumerable<IDatabaseCleanupThreshold> GetDatabaseCleanupThresholds()
        {
            yield return new DatabaseCleanupThreshold("Regular", "Regular Cleanup");
        }

        public void DatabaseCleanup(string name, int retentionPeriod)
        {
            // if (name == "Regular") do cleanup stuff with the user-specified retention period
        }
         
         */

    }
}
