namespace n_ate.Gremlin.Contracts
{
    public interface IHaveChangeMetadataDB
    {
        string CreatedBy { get; set; }
        long creationDate { get; set; }
        long lastUpdateDate { get; set; }
        string LastUpdatedBy { get; set; }
    }
}