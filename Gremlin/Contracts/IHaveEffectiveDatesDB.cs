namespace n_ate.Gremlin.Contracts
{
    public interface IHaveEffectiveDatesDB
    {
        long effectiveEndDate { get; set; }
        long effectiveStartDate { get; set; }
    }
}