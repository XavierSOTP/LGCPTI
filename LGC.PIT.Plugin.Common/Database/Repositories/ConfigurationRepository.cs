using LGC.PIT.Plugin.Common.Database.Encryption;
using LGC.PIT.Plugin.Common.Database.Entities;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading.Tasks;
using System.Linq;

namespace LGC.PIT.Plugin.Common.Database.Repositories;

public class ConfigurationRepository : IConfigurationRepository
{
    #region Queries

    private const string SelectByKey = @"SELECT * FROM dbo.[Configuration] WHERE [Key] = @key";
    private const string Insert = @"INSERT INTO dbo.[Configuration] ([Key], [Value], [CreatedBy], [CreatedOn], [ModifiedBy], [ModifiedOn]) VALUES (@Key, @Value, @createdBy, @createdOn, @modifiedBy, @modifiedOn)";
    private const string Update = @"UPDATE dbo.[Configuration] SET
                                        [Value]      = @Value,
                                        [ModifiedBy] = @ModifiedBy,
                                        [ModifiedOn] = @ModifiedOn
                                    WHERE [Key] = @Key";
    private const string SelectAll = @"
    SELECT
        [Id],
        [Key],
        [Value],
        [CreatedBy],
        [CreatedOn],
        [ModifiedBy],
        [ModifiedOn]
    FROM dbo.[Configuration]";

    #endregion

    public ISqlExecutor SqlExecutor { get; }

    public ConfigurationRepository(ISqlExecutor sqlExecutor)
    {
        SqlExecutor = sqlExecutor;
    }

    public async Task AddAsync(Configuration entity)
    {
        await SqlExecutor.ExecuteAsync(Insert, command =>
        {
            command.Parameters.AddWithValue("@Key", entity.Key);
            command.Parameters.AddWithValue("@Value", EncryptionHelper.Encrypt(entity.Value));
            command.Parameters.AddWithValue("@createdBy", Environment.UserName);
            command.Parameters.AddWithValue("@createdOn", DateTime.Now);
            command.Parameters.AddWithValue("@modifiedBy", Environment.UserName);
            command.Parameters.AddWithValue("@modifiedOn", DateTime.Now);
        });
    }

    public Task DeleteAsync(Configuration entity)
    {
        throw new NotImplementedException();
    }

    public async Task<IReadOnlyList<Configuration>> GetAllAsync(
    Expression<Func<Configuration, bool>> predicate = null)
    {
        var collection = await SqlExecutor.GetQueryResultsAsync<Configuration>(
            SelectAll,
            command => { },
            fieldMapper: reader =>
            {
                var conf = new Configuration();

                conf.Id = reader.GetInt32(reader.GetOrdinal("Id"));
                conf.Key = reader.GetString(reader.GetOrdinal("Key"));
                var encryptedValue = reader.GetString(reader.GetOrdinal("Value"));
                conf.Value = string.IsNullOrWhiteSpace(encryptedValue) ? encryptedValue : EncryptionHelper.Decrypt(encryptedValue);

                conf.CreatedBy = reader.GetString(reader.GetOrdinal("CreatedBy"));
                conf.CreatedOn = reader.GetDateTime(reader.GetOrdinal("CreatedOn"));
                conf.ModifiedBy = reader.GetString(reader.GetOrdinal("ModifiedBy"));
                conf.ModifiedOn = reader.GetDateTime(reader.GetOrdinal("ModifiedOn"));

                return conf;
            });

        return predicate is null ? (IReadOnlyList<Configuration>)collection : collection.AsQueryable().Where(predicate).ToList().AsReadOnly();
    }

    public async Task<Configuration> GetByIdAsync(int id)
    {
        throw new NotImplementedException();
    }

    public async Task<Configuration> GetByKeyAsync(string key)
    {
        var collection = await SqlExecutor.GetQueryResultsAsync<Configuration>(SelectByKey, command =>
        {
            command.Parameters.AddWithValue("@key", key);
        },
        fieldMapper: (reader) => {

            var conf = new Configuration();
            conf.Id = reader.GetInt32(reader.GetOrdinal("Id"));
            conf.Key = reader.GetString(reader.GetOrdinal("Key"));

            var encryptedValue = reader.GetString(reader.GetOrdinal("Value"));
            conf.Value = string.IsNullOrWhiteSpace(encryptedValue) ? encryptedValue : EncryptionHelper.Decrypt(encryptedValue);

            conf.CreatedBy = Environment.UserName;
            conf.CreatedOn = DateTime.Now;
            conf.ModifiedBy = Environment.UserName;
            conf.ModifiedOn = DateTime.Now;

            return conf;
        });

        return collection.FirstOrDefault();
    }

    public async Task UpdateAsync(Configuration entity)
    {
        await SqlExecutor.ExecuteAsync(Update, command =>
        {
            command.Parameters.AddWithValue("@Key", entity.Key);
            command.Parameters.AddWithValue("@Value", EncryptionHelper.Encrypt(entity.Value));
            command.Parameters.AddWithValue("@modifiedBy", Environment.UserName);
            command.Parameters.AddWithValue("@modifiedOn", DateTime.Now);
        });
    }

    public async Task UpsertAsync(string key, string value)
    {
        var existingConfig = await GetByKeyAsync(key);
        if (existingConfig != null)
        {
            existingConfig.Value = value;
            await UpdateAsync(existingConfig);
        }
        else
        {
            var newConfig = new Configuration
            {
                Key = key,
                Value = value
            };
            await AddAsync(newConfig);
        }
    }
}
