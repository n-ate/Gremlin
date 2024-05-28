using System;

namespace n_ate.Gremlin.Contracts
{
    public interface IHaveChangeMetadata
    {
        string CreatedBy { get; set; }
        DateTime CreationDate { get; set; }
        DateTime LastUpdateDate { get; set; }
        string LastUpdatedBy { get; set; }
    }
}