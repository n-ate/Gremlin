using System.Collections.Concurrent;
using System.Diagnostics;
using System.Linq;

namespace n_ate.Gremlin.Logging
{
    internal partial class Trace
    {
        private static ConcurrentDictionary<Trace, Stopwatch> _keys = new ConcurrentDictionary<Trace, Stopwatch>();

        internal Trace(string methodName, object[] arguments)
        {
            MethodSignature = $"{methodName}({string.Join(", ", arguments.Select(a => a.ToString()))})";
        }

        internal string MethodSignature { get; }
    }
}