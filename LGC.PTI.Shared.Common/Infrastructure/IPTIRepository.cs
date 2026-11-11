using LGC.PTI.Domain.Models;
using SoftSolute.Common.Utilities;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace LGC.PTI.Domain
{
    public interface IPTIRepository
    {
        #region Public Methods

        Task<bool> CardholderModelExists(Guid credentialGuid);

        Task DropCardholdersAsync();

        Task InsertBatchAsync(IEnumerable<CardholderModel> records);

        Task<Option<CardholderModel>> TryAddOrUpdate(CardholderModel model);

        Task<Option<CardholderModel>> TryDeleteByCardholderId(Guid guid);

        Task<Option<CardholderModel>> TryDeleteByCredId(Guid credId);

        Task<IEnumerable<CardholderModel>> TryFindAllCardholders();

        Task<Option<CardholderModel>> TryFindByCredId(Guid guid);

        Task<Option<CardholderModel>> TryFindCardholderAsync(Guid guid);

        Task<Option<string>> TryGetTokenPassword();

        Task<bool> UpdatePassword(string password);

        #endregion Public Methods
    }
}