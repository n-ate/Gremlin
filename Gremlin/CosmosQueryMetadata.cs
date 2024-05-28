using n_ate.Gremlin.Models;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.Steps;
using System;
using System.Collections;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using n_ate.Essentials;

namespace n_ate.Gremlin
{
    internal static class CosmosQueryMetadata
    {
        internal static Dictionary<string, object> Empty
        {
            get { return new Dictionary<string, object>(new Dictionary<string, object>()); }
        }

        private static readonly ConcurrentDictionary<TraceId, (DateTime Timestamp, Dictionary<string, object> Metadata)> _cache = new ConcurrentDictionary<TraceId, (DateTime, Dictionary<string, object>)>();

        internal static TraceId GetKey(IGremlinQueryBase query)
        {
            if (query.TryGetValue("Steps", out object? querySteps))
            {
                if (querySteps is Traversal traversal)
                {
                    if (TryGetKey(traversal, out TraceId? traceId))
                    {
                        return traceId!.Value;
                    }
                }
            }
            throw new NotImplementedException();
        }

        internal static Dictionary<string, object> Pop(TraceId key)
        {
            Dictionary<string, object> result;
            if (_cache.TryRemove(key, out var entry)) result = entry.Metadata;
            else result = Empty;
            RemoveExpiredMetadata(90);
            return result;
        }

        internal static void Push(TraceId key, Dictionary<string, object> metadata)
        {
            RemoveExpiredMetadata(90);
            var value = (DateTime.UtcNow, metadata);
            _cache.AddOrUpdate(key, value, (k, v) => value);
        }

        internal static bool TryGetKey(Traversal traversal, out TraceId? traceId)
        {
            if (traversal.TryGetValue("Steps", out object? traversalSteps))
            {
                if (traversalSteps!.IsCollection() && traversalSteps is IEnumerable wrappedSteps)
                {
                    var steps = wrappedSteps.AsCollection<Step[]>();
                    if (steps is not null && steps.Any())
                    {
                        var injectValues = steps.OfType<InjectStep>().SelectMany(s => s.Elements);
                        TraceId? trace = default;
                        if (injectValues.Any(v => TraceId.TryParse(v.ToString()!, out trace)))
                        {
                            traceId = trace;
                            return true;
                        }
                        else
                        {
                            var traversals = steps.Select(s => s.GetTraversal()).Where(t => t.HasValue).Select(t => t!.Value);
                            if (traversals.Any(t => TryGetKey(t, out trace)))
                            {
                                traceId = trace;
                                return true;
                            }
                        }
                    }
                }
            }
            traceId = null;
            return false;
        }

        private static void RemoveExpiredMetadata(int preserveSeconds)
        {
            foreach (var keyValue in _cache.Where(kv => kv.Value.Timestamp < DateTime.UtcNow - TimeSpan.FromSeconds(preserveSeconds)))
            {
                _cache.TryRemove(keyValue);
            }
        }
    }
}