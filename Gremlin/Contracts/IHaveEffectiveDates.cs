using System;

namespace n_ate.Gremlin.Contracts
{
    public interface IHaveEffectiveDates
    {
        DateTime EffectiveEndDate { get; set; }
        DateTime EffectiveStartDate { get; set; }
    }
}