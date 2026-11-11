using Newtonsoft.Json;
using Newtonsoft.Json.Converters;

namespace LGC.PTI.Common.Application.Dtos
{
    public enum TransactionStatus
    {
        None = 0,
        New,
        Failed,
        Updated
    }

    /// <summary>
    /// Employee as defined in PTI system
    /// </summary>
    public class EmployeeDtoOld : BaseEmployeeDto
    {
        [JsonConverter(typeof(StringEnumConverter))]
        public TransactionStatus? Status { get; set; }

        public string GetFullName()
            => $"{FirstName} {LastName}";
    }
}