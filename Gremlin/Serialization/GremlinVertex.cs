using System.Collections.Generic;
using System.Linq;

namespace n_ate.Gremlin.Serialization
{
    internal class GremlinVertex : IGremlinResponse
    {
        public string Id { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public Dictionary<string, Property[]> Properties { get; set; } = new Dictionary<string, Property[]>();

        public string Type { get; set; } = string.Empty;

        public Dictionary<string, object?> GetAllProperties()
        {
            var result = Properties.ToDictionary(kv => kv.Key, kv => kv.Value.FirstOrDefault()?.Value);
            result.Add(nameof(Id), Id);
            result.Add(nameof(Label), Label);
            result.Add(nameof(Type), Type);
            return result;
        }

        internal class Property
        {
            public string? Id { get; set; }
            public object? Value { get; set; }
        }
    }
}