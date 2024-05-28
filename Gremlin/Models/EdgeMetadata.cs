using n_ate.Gremlin.Contracts;

namespace n_ate.Gremlin.Models
{
    public class EdgeMetadata<TEdge> where TEdge : IDatabaseEdge
    {
        private string _id = string.Empty;
        private string _label = string.Empty;
        private TEdge? _properties;

        /// <summary>Id.</summary>
        public string Id
        { get { return _id; } set { if (Properties is IHaveId edge) edge.Id = value; _id = value; } }

        /// <summary>Label.</summary>
        public string Label
        { get { return _label; } set { if (Properties is IHaveLabel edge) edge.Label = value; _label = value; } }

        /// <summary>Graph Node Type.</summary>
        public string Type { get; set; } = string.Empty;

        /// <summary>In Vertex Label.</summary>
        public string InVLabel { get; set; } = string.Empty;

        /// <summary>Out Vertex Label.</summary>
        public string OutVLabel { get; set; } = string.Empty;

        /// <summary>In Vertex Id.</summary>
        public string InV { get; set; } = string.Empty;

        /// <summary>Out Vertex Id.</summary>
        public string OutV { get; set; } = string.Empty;

        /// <summary>The Edge Object.</summary>
        public TEdge? Properties
        {
            get { return _properties; }
            set
            {
                _properties = value;
                if (_properties is IHaveId haveId) haveId.Id = Id;
                if (_properties is IHaveLabel haveLabel) haveLabel.Label = Label;
            }
        }
    }
}