using n_ate.Essentials.PropertySets;
using n_ate.Gremlin.Contracts;
using n_ate.Gremlin.Models;
using n_ate.Gremlin.Serialization;
using ExRam.Gremlinq.Core;
using ExRam.Gremlinq.Core.Deserialization;
using ExRam.Gremlinq.Core.Steps;
using ExRam.Gremlinq.Providers.CosmosDb;
using ExRam.Gremlinq.Providers.WebSocket;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Text.RegularExpressions;
using n_ate.Essentials;

namespace n_ate.Gremlin
{
    public static class GremlinqExtensions
    {
        private static readonly Regex _getPropertyBindings = new Regex(@"\.[^.\(\) ]+\([^_]*(?<bindingkeys>(_[^)_]+)+)\)");
        private static readonly Regex _removeFirstOrDefaultLimit = new Regex(@"\.limit\(_[a-z]+\)$");

        public static IVertexGremlinQuery<T> AddProperties<T>(this IVertexGremlinQuery<T> query, IDictionary<string, object?> properties, bool camelCaseNames = true, params string[] ignoreKeys)
            where T : IDatabaseVertex
        {
            query = TrimPropertyStepsFromEndOfTraversal(query);//remove any automatically added properties
            foreach (var property in properties.Where(p => ignoreKeys.All(k => !p.Key.Equals(k, StringComparison.OrdinalIgnoreCase)))) //each property that is not ignored..
            {
                query = query.Property(camelCaseNames ? property.Key.FirstCharToLower() : property.Key, property.Value);
            }
            return query;
        }

        public static IEdgeGremlinQuery<T> AddProperties<T>(this IEdgeGremlinQuery<T> query, IDictionary<string, object?> properties, bool camelCaseNames = true, params string[] ignoreKeys)
            where T : IDatabaseEdge
        {
            query = TrimPropertyStepsFromEndOfTraversal(query);//remove any automatically added properties
            foreach (var property in properties.Where(p => ignoreKeys.All(k => !p.Key.Equals(k, StringComparison.OrdinalIgnoreCase)))) //each property that is not ignored..
            {
                query = query.Property(camelCaseNames ? property.Key.FirstCharToLower() : property.Key, property.Value);
            }
            return query;
        }

        public static IInOrOutEdgeGremlinQuery<TEdge, TAdjacentVertex> AddProperties<TEdge, TAdjacentVertex>(this IInOrOutEdgeGremlinQuery<TEdge, TAdjacentVertex> query, IDictionary<string, object?> properties, bool camelCaseNames = true, params string[] ignoreKeys)
            where TEdge : IDatabaseEdge
            where TAdjacentVertex : IDatabaseVertex
        {
            query = TrimPropertyStepsFromEndOfTraversal(query);//remove any automatically added properties
            foreach (var property in properties.Where(p => ignoreKeys.All(k => !p.Key.Equals(k, StringComparison.OrdinalIgnoreCase)))) //each property that is not ignored..
            {
                query = query.Property(camelCaseNames ? property.Key.FirstCharToLower() : property.Key, property.Value);
            }
            return query;
        }

        /// <summary>
        /// Replaces the deserializer with a <see cref="PropertySetDeserializer"/> for classes that have a <see cref="PropertySetAttribute"/>. Otherwise the original deserializer is used.
        /// </summary>
        /// <param name="propertySetName"></param>
        /// <returns>The updated deserializer that must be returned from the configuration method.</returns>
        public static PropertySetDeserializer AddPropertySetDeserializer(this IGremlinQueryExecutionResultDeserializer activeDeserializer, string propertySetName)
        {
            return new PropertySetDeserializer(activeDeserializer, propertySetName);
        }

        public static Dictionary<string, object> GetMetadata(this IGremlinQueryBase query)
        {
            //var key = new CosmosQueryMetadata.Key(query);
            var key = CosmosQueryMetadata.GetKey(query);
            var result = CosmosQueryMetadata.Pop(key);
            if (result == CosmosQueryMetadata.Empty) throw new ArgumentException($"Unexpected missing metadata. The query must be executed prior to calling {nameof(GetMetadata)}. {nameof(GetMetadata)} must only be called once after query execution.", nameof(query));
            return result;
        }

        public static QueryInformation GetQueryInformation(this IGremlinQueryBase query) => new QueryInformation(query);

        public static Traversal? GetTraversal(this Step step)
        {
            switch (step)
            {
                case NotStep notStep: // not steps have additional steps
                    return notStep.Traversal;

                case AddEStep.ToTraversalStep traversalStep: // traversal steps have additional steps
                    return traversalStep.Traversal;

                case RepeatStep repeatStep: // repeat steps have additional steps
                    return repeatStep.Traversal;

                case ProjectStep.ByTraversalStep byTraversalStep: // repeat steps have additional steps
                    return byTraversalStep.Traversal;

                case SideEffectStep sideEffectStep: // repeat steps have additional steps
                    return sideEffectStep.Traversal;

                default: return null;
            }
        }

        public static ICosmosDbConfigurator IncludeCosmosQueryMetadata(this ICosmosDbConfigurator configurator)
        {
            var explanation = $"Tracing is added via {nameof(GremlinqExtensions)}.{nameof(IncludeCosmosQueryMetadata)}() which hangs off of CosmosDbProviderConfiguration. {nameof(IncludeCosmosQueryMetadata)}() cannot be used alone. It is intended to function in concert with {nameof(QueryHelper)}. Ensure that you are using {nameof(QueryHelper)} for each query.";
            return configurator.ConfigureWebSocket(
                config => config.ConfigureClient(
                    client => client.ObserveResultStatusAttributes(
                        (request, readOnlyMetadata) =>
                        {
                            if (request.Arguments.TryGetValue("gremlin", out object? gremlinQuery))
                            {
                                if (gremlinQuery is string query)
                                {
                                    var isDrop = query.EndsWith("drop()"); //TODO: test if this exception is necessary, having moved the inject into a side effect..
                                    if (isDrop) return; // drop queries don't have a trace id
                                    query = _removeFirstOrDefaultLimit.Replace(query, string.Empty);
                                    if (request.Arguments.TryGetValue("bindings", out object? argumentBindings))
                                    {
                                        if (argumentBindings is Dictionary<string, object> bindings)
                                        {
                                            if (bindings.Any())
                                            {
                                                TraceId? traceId = null;
                                                if (bindings.Select(b => b.Value).Any(v => TraceId.TryParse(v.ToString()!, out traceId))) //any bindings can be parsed as a trace id..
                                                {
                                                    var metadata = readOnlyMetadata.ToDictionary(kv => kv.Key, kv => kv.Value);
                                                    if (metadata.ContainsKey(QueryHelper.MS_STATUS_CODE_KEY)) metadata[QueryHelper.MS_STATUS_CODE_KEY] = metadata[QueryHelper.MS_STATUS_CODE_KEY]?.ToString()!;//makes status code into a string so that it does not get summed during aggregation.
                                                    CosmosQueryMetadata.Push(traceId!.Value, metadata);
                                                    return;
                                                }
                                                else throw new ArgumentException($"First binding is not an injected guid. Verify that query source is injecting a guid. {explanation}");
                                            }
                                            else throw new ArgumentException($"Bindings are missing. Verify that query source is injecting a guid. {explanation}");
                                        }
                                    }
                                }
                            }
                            throw new NotImplementedException($"Expected an query of type string and a binding of type string[]. {explanation}");
                        }
                    )
                )
            );
        }

        public static string[] ToStringArray(this Step step)
        {
            switch (step)
            {
                case DedupStep:
                case DropStep:
                case EmitStep:
                case IdStep:
                case InVStep:
                case OutVStep:
                case PathStep:
                    return new string[0]; //do nothing...

                case PropertyStep.ByKeyStep keyStep:
                    if (keyStep.Key is ExRam.Gremlinq.Core.Key key)
                    {
                        if (key.RawKey is string value)
                        {
                            return new string[] { value, keyStep.Value.ToString()! };
                        }
                        else if (key.RawKey.GetType().Name == "T")
                        {
                            return (keyStep.Value as string)!.ToSingleItemArray();
                        }
                    }
                    throw new NotImplementedException(); //Must implement step above so that it matches the key found in CosmosQueryMetadata._cache
                case AddVStep addVStep:
                    return addVStep.Label.ToSingleItemArray();

                case EStep:
                case VStep:
                    var identities = (step as EStep)?.Ids ?? (step as VStep)?.Ids ?? new ImmutableArray<object>();
                    var ids = new List<string>();
                    foreach (var id in identities)
                    {
                        if (id is CosmosDbKey dbKey)
                        {
                            if (!string.IsNullOrWhiteSpace(dbKey.PartitionKey)) ids.Add(dbKey.PartitionKey);
                            ids.Add(dbKey.Id);
                        }
                        else throw new NotImplementedException(); //Must implement step above so that it matches the key found in CosmosQueryMetadata._cache
                    }
                    return ids.ToArray();

                case HasLabelStep labelStep:
                    return labelStep.Labels.ToArray();

                case InEStep inEStep:
                    return inEStep.Labels.ToArray();

                case OutEStep outEStep:
                    return outEStep.Labels.ToArray();

                case AddEStep addEStep:
                    return addEStep.Label.ToSingleItemArray();

                case NotStep notStep: // not steps have additional steps
                    return notStep.Traversal.SelectMany(s => s.ToStringArray()).ToArray();

                case AddEStep.ToTraversalStep traversalStep: // traversal steps have additional steps
                    return traversalStep.Traversal.SelectMany(s => s.ToStringArray()).ToArray();

                case RepeatStep repeatStep: // repeat steps have additional steps
                    return repeatStep.Traversal.SelectMany(s => s.ToStringArray()).ToArray();

                case ProjectStep.ByTraversalStep byTraversalStep: // repeat steps have additional steps
                    return byTraversalStep.Traversal.SelectMany(s => s.ToStringArray()).ToArray();

                case SideEffectStep sideEffectStep: // repeat steps have additional steps
                    return sideEffectStep.Traversal.SelectMany(s => s.ToStringArray()).ToArray();

                case RangeStep rangeStep:
                    return new[] { rangeStep.Lower.ToString(), rangeStep.Upper.ToString() };

                //g.V(_a).hasLabel(_b).outE(_c).as(_d).where(__.inV().hasLabel(_e,_f).has(_g,_h).has(id,_i)).select(_d).range(_j,_k)
                case AsStep asStep:
                    return asStep.StepLabel.GetValue("Identity")!.ToString()!.ToSingleItemArray();

                case AggregateStep aggregateStep:
                    return aggregateStep.StepLabel.GetValue("Identity")!.ToString()!.ToSingleItemArray();

                case SelectStepLabelStep selectStepLabelStep:
                    return selectStepLabelStep.StepLabels.Select(l => l.GetValue("Identity")!.ToString()!).ToArray();

                case HasPredicateStep hasPredicateStep:
                    if (hasPredicateStep.Key.RawKey.ToString()!.StartsWith("T.")) return ((object)hasPredicateStep.Predicate.Value).ToString()!.ToSingleItemArray();
                    //return new[] { hasPredicateStep.Key.RawKey.ToString(), hasPredicateStep.Predicate.OperatorName };
                    return new[] { hasPredicateStep.Key.RawKey.ToString()!, ((object)hasPredicateStep.Predicate.Value).ToString()! };

                case FilterStep.ByTraversalStep filterByTraversalStep:
                    return filterByTraversalStep.Traversal.SelectMany(s => s.ToStringArray()).ToArray();

                case ProjectStep projectStep:
                    return projectStep.Projections.ToArray();

                case LimitStep:
                case TailStep:
                    var count = (step as LimitStep)?.Count ?? (step as TailStep)?.Count;
                    return count.ToString()!.ToSingleItemArray();

                case InjectStep injectStep:
                    return injectStep.Elements.Select(e => e.ToString()!).ToArray();

                default: throw new NotImplementedException(); //Must implement step above so that it matches the key found in CosmosQueryMetadata._cache
            }
        }

        private static TQuery TrimPropertyStepsFromEndOfTraversal<TQuery>(TQuery query)
            where TQuery : IEdgeOrVertexGremlinQueryBase, IElementGremlinQueryBase, IGremlinQueryBase, IStartGremlinQuery
        {
            var env = query.GetValue<IGremlinQueryEnvironment>("Environment");
            var flags = query.GetValue("Flags");
            var projs = query.GetValue<IImmutableDictionary<StepLabel, LabelProjections>>("LabelProjections");
            var steps = query.GetValue<Traversal>("Steps");
            while (steps.Last() is PropertyStep) steps = steps.Pop();
            var queryType = query.GetType();
            var constructors = queryType.GetConstructors();
            if (!constructors.Any()) throw new ArgumentException();
            dynamic queryWithoutTrailingPropertySteps = Activator.CreateInstance(queryType, env, steps, projs, flags)!;
            return queryWithoutTrailingPropertySteps;
        }
    }
}