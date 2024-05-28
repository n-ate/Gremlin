using System.Collections.Generic;

namespace n_ate.Gremlin.Serialization
{
    internal interface IGremlinResponse
    {
        public string Id { get; set; }

        public string Label { get; set; }

        public string Type { get; set; }

        public Dictionary<string, object?> GetAllProperties();
    }
}