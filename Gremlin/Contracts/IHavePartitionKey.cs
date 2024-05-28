namespace n_ate.Gremlin.Contracts
{
    public interface IHavePartitionKey
    {
        string PartitionKey { get; set; }
    }
}