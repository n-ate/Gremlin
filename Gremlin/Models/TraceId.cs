using System;
using System.Diagnostics.CodeAnalysis;

namespace n_ate.Gremlin.Models
{
    internal struct TraceId
    {
        public const string IDENTIFIER = "TRACE";
        private Guid _id;

        public TraceId(Guid id) => _id = id;

        public static TraceId New() => new TraceId(Guid.NewGuid());

        public static bool TryParse(string value, out TraceId? traceId)
        {
            var pieces = value.Split(':');
            if (pieces.Length == 2)
            {
                if (pieces[0].Equals(IDENTIFIER))
                {
                    if (Guid.TryParse(pieces[1], out var id))
                    {
                        traceId = new TraceId(id);
                        return true;
                    }
                }
            }
            traceId = null;
            return false;
        }

        public override bool Equals([NotNullWhen(true)] object? obj)
        {
            if (obj is TraceId traceId)
            {
                return _id.Equals(traceId._id);
            }
            return false;
        }

        public override int GetHashCode() => _id.GetHashCode();

        public override string ToString() => $"{IDENTIFIER}:{_id:n}";
    }
}