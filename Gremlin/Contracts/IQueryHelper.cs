using ExRam.Gremlinq.Core;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace n_ate.Gremlin
{
    public interface IQueryHelper
    {
        Task<(long Result, Dictionary<string, object> Metadata)> ExecuteCount<TResult>(IGremlinQueryBase<TResult> query);

        Task<(bool Result, Dictionary<string, object> Metadata)> ExecuteDrop<TResult>(IGremlinQueryBase<TResult> query);

        Task<(TResult[] Result, Dictionary<string, object> Metadata)> ExecutePagedQuery<TResult>(string queryType, IGremlinQueryBase<TResult> query, int skip, int take);

        Task<(TResult? Result, Dictionary<string, object> Metadata)> ExecuteScalar<TResult>(string queryType, IGremlinQueryBase<TResult> query);
    }
}