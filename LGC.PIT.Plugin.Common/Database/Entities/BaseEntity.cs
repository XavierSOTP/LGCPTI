using System;

namespace LGC.PIT.Plugin.Common.Database.Entities;

public class BaseEntity<TKey> where TKey : struct
{
    public TKey Id { get; set; }
    public string CreatedBy { get; set; }
    public DateTime CreatedOn { get; set; }
    public string ModifiedBy { get; set; }
    public DateTime ModifiedOn { get; set; }
}

