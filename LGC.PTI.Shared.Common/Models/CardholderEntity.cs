using Genetec.Sdk;
using LGC.PTI.Shared.Common;
using System;
using System.Collections.Generic;
using PTIDtos = LGC.PTI.Common.Infrastructure.ExternalApi.Dtos;

namespace LGC.PTI.Domain.Models
{
    public sealed class CardholderEntity
    {
        #region Public Properties

        public bool Active { get; set; }
        public string Badge { get; set; }
        public Guid CardholderGuid { get; set; }
        public Guid CredentialGuid { get; set; }
        public string CredName { get; set; }
        public string Department { get; set; }
        public string EmployeeNumber { get; set; }
        public string FirstName { get; set; }

        public PTIDtos.Language Language { get; set; }
        public string LastName { get; set; }
        public string Name => $"{FirstName} {LastName}";
        public string Pin { get; set; }

        #endregion Public Properties

        #region Public Constructors

        public CardholderEntity()
        {
        }

        #endregion Public Constructors

        #region Public Methods

        public override bool Equals(object obj)
        {
            return obj is CardholderEntity model &&
                   CardholderGuid.Equals(model.CardholderGuid) &&
                   EmployeeNumber == model.EmployeeNumber;
        }

        public override int GetHashCode()
        {
            int hashCode = -805205156;
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(CardholderGuid.ToString());
            hashCode = hashCode * -1521134295 + EqualityComparer<string>.Default.GetHashCode(EmployeeNumber);
            return hashCode;
        }

        #endregion Public Methods
    }
}