using n_ate.Gremlin.Logging;
using n_ate.Gremlin.Models;
using ExRam.Gremlinq.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using static n_ate.Gremlin.Logging.Trace;
using n_ate.Essentials.Enumerations;
using n_ate.Essentials;

namespace n_ate.Gremlin
{
    public class QueryHelper : IQueryHelper
    {
        public const string EXECUTION_MS_KEY = "api-execution-time-ms";
        public const string EXECUTION_STATUS_KEY = "api-execution-status";
        public const string MS_STATUS_CODE_KEY = "x-ms-status-code";
        private readonly Telemetry _telemetry;

        public QueryHelper(IContractLogger logger)
        {
            _telemetry = new Telemetry(logger);
        }

        public async Task<(long Result, Dictionary<string, object> Metadata)> ExecuteCount<TResult>(IGremlinQueryBase<TResult> query)
        {
            Trace? token = null;
            query = AddTraceId(query);
            try
            {
                var info = query.GetQueryInformation();
                token = _telemetry.TraceStart(nameof(ExecuteScalar), info.Arguments!.Values);

                var result = await query.Count().FirstOrDefaultAsync();
                var metadata = query.GetMetadata();
                AddApiExecutionTimeToMetadata(metadata, _telemetry.TraceStop(token));
                AddApiExecutionStatusToMetadata(metadata, result != default ? QueryExecutionStatus.Success : QueryExecutionStatus.NoResults);

                return (result, metadata);
            }
            catch (Exception ex)
            {
                if (token != null)
                {
                    var elapsedTime = _telemetry.TraceException(token, ex);
                }
                throw;
            }
        }

        public async Task<(bool Result, Dictionary<string, object> Metadata)> ExecuteDrop<TResult>(IGremlinQueryBase<TResult> query)
        {
            Trace? token = null;
            query = AddTraceId(query);
            try
            {
                var info = query.GetQueryInformation();
                token = _telemetry.TraceStart(nameof(ExecuteDrop), info.Arguments!.Values);

                var result = await query.Drop().FirstOrDefaultAsync();
                var metadata = query.GetMetadata();
                AddApiExecutionTimeToMetadata(metadata, _telemetry.TraceStop(token));
                AddApiExecutionStatusToMetadata(metadata, result != default ? QueryExecutionStatus.Success : QueryExecutionStatus.NoResults);

                return (true, metadata);
            }
            catch (Exception ex)
            {
                if (token != null)
                {
                    var elapsedTime = _telemetry.TraceException(token, ex);
                }
                throw;
            }
        }

        public async Task<(TResult[] Result, Dictionary<string, object> Metadata)> ExecutePagedQuery<TResult>(string queryType, IGremlinQueryBase<TResult> query, int skip, int take)
        {
            Trace? token = null;
            query = AddTraceId(query);
            try
            {
                var info = query.GetQueryInformation();
                token = _telemetry.TraceStart(nameof(ExecutePagedQuery), info.Arguments!.Values);
                switch (query)
                {
                    case IEdgeOrVertexGremlinQuery<TResult> _query:
                        if (skip == 0 && (take == int.MaxValue || take == 0)) query = _query; //do nothing; do not add range.
                        else query = _query.Range(skip, skip + take);
                        break;

                    default: throw new NotImplementedException();
                }
                var result = await query.ToArrayAsync();
                var metadata = query.GetMetadata();
                AddApiExecutionTimeToMetadata(metadata, _telemetry.TraceStop(token));
                AddApiExecutionStatusToMetadata(metadata, result.Any() ? QueryExecutionStatus.Success : QueryExecutionStatus.NoResults);

                return (result, metadata);
            }
            catch (Exception ex)
            {
                if (token != null)
                {
                    var elapsedTime = _telemetry.TraceException(token, ex);
                }
                throw;
            }
        }

        public async Task<(TResult? Result, Dictionary<string, object> Metadata)> ExecuteScalar<TResult>(string queryType, IGremlinQueryBase<TResult> query)
        {
            Trace? token = null;
            query = AddTraceId(query);
            try
            {
                var info = query.GetQueryInformation();
                token = _telemetry.TraceStart(nameof(ExecuteScalar), info.Arguments!.Values);

                var result = await query.FirstOrDefaultAsync();
                var metadata = query.GetMetadata();
                AddApiExecutionTimeToMetadata(metadata, _telemetry.TraceStop(token));
                AddApiExecutionStatusToMetadata(metadata, result != null ? QueryExecutionStatus.Success : QueryExecutionStatus.NoResults);

                return (result, metadata);
            }
            catch (Exception ex)
            {
                if (token != null)
                {
                    var elapsedTime = _telemetry.TraceException(token, ex);
                }
                throw;
            }
        }

        private void AddApiExecutionStatusToMetadata(IDictionary<string, object> metadata, QueryExecutionStatus status)
        {
            metadata[EXECUTION_STATUS_KEY] = status.GetDescription();
        }

        private void AddApiExecutionTimeToMetadata(IDictionary<string, object> metadata, TimeSpan executionTime)
        {
            metadata[EXECUTION_MS_KEY] = executionTime.TotalMilliseconds;
        }

        private IGremlinQueryBase<TResult> AddTraceId<TResult>(IGremlinQueryBase<TResult> query)
        {
            switch (query)
            {
                //case IValueTupleGremlinQuery<TResult> tuple:
                //    return tuple.SideEffect(x => x.Inject(TraceId.New().ToString()));

                case IEdgeOrVertexGremlinQuery<TResult> edgeOrVertex:
                    return edgeOrVertex.SideEffect(x => x.Inject(TraceId.New().ToString()).As("traceId"));

                default: throw new NotImplementedException();
            }
        }
    }
}