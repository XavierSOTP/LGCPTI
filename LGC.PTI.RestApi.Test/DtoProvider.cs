using SoftSolute.Common.Utilities.FileSS;
using SoftSolute.Common.Utilities.Tasks;
using System.Text;
using System.Threading;

namespace LGC.PTI.RestApi.Test;

public static class DtoProvider
{
    #region Public Methods

    public static async Task<LanguageExt.Common.Result<TOut>> TryGetDtoAsync<TOut>(NotEmptyString fileName, CancellationToken cancellationToken = default)
    {
        var jsonString = await FileHelper.ReadAsync(fileName, Encoding.UTF8, cancellationToken).IgnoreCtx();
        return jsonString.Map(content => JsonConvert.DeserializeObject<TOut>(content));
    }

    #endregion Public Methods
}