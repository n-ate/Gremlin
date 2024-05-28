using System.Collections.Generic;

namespace n_ate.Gremlin.Serialization
{
    internal class GremlinEdge : IGremlinResponse
    {
        public string Id { get; set; } = string.Empty;

        public string InV { get; set; } = string.Empty;

        public string InVLabel { get; set; } = string.Empty;

        public string Label { get; set; } = string.Empty;

        public string OutV { get; set; } = string.Empty;

        public string OutVLabel { get; set; } = string.Empty;

        public Dictionary<string, object?> Properties { get; set; } = new Dictionary<string, object?>();

        public string Type { get; set; } = string.Empty;

        public Dictionary<string, object?> GetAllProperties()
        {
            var result = new Dictionary<string, object?>(Properties)
                {
                    {nameof(Id), Id },
                    {nameof(InV), InV},
                    {nameof(InVLabel), InVLabel},
                    {nameof(Label), Label},
                    {nameof(OutV), OutV},
                    {nameof(OutVLabel), OutVLabel},
                    {nameof(Type), Type}
                };
            return result;
        }
    }
}