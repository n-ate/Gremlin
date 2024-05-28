using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace n_ate.Gremlin.Logging
{
    internal partial class Trace
    {
        internal class Telemetry
        {
            internal Telemetry(IContractLogger contractLogger)
            {
                this.Basic = contractLogger;
            }

            internal IContractLogger Basic { get; }

            /// <summary>
            /// Stops tracing a method's execution because of error.
            /// </summary>
            /// <param name="token">The token for tracing this request.</param>
            /// <param name="exception">The caught exception that ended the method's execution.</param>
            /// <returns>the elapsed time</returns>
            internal TimeSpan TraceException(Trace token, Exception exception)
            {
                if (_keys.Remove(token, out var timer))
                {
                    timer.Stop();
                    var properties = new Dictionary<string, string> {
                        { "error", $"Telemetry Error: {token.MethodSignature}" },
                        { "TotalMilliSeconds", timer.ElapsedMilliseconds.ToString() }
                    };
                    Basic.TelemetryLogException(exception, properties);
                    return timer.Elapsed;
                }
                else throw new NotImplementedException();
            }

            /// <summary>
            /// Starts tracing a method's execution.
            /// </summary>
            /// <param name="methodName">nameof method</param>
            /// <param name="arguments">arguments of method</param>
            /// <returns>A token for tracing this request.</returns>
            internal Trace TraceStart(string methodName, params object[] arguments)
            {
                var token = new Trace(methodName, arguments);
                Basic.LoggerLogMessage($"Call " + token.MethodSignature);
                var timer = new Stopwatch();
                //if (token == null || timer == null)
                //{
                //    var k = _keys;
                //}
                if (!_keys.TryAdd(token, timer)) Debug.WriteLine("Could not insert trace token.");
                timer.Start();
                return token;
            }

            /// <summary>
            /// Stops tracing a method's execution
            /// </summary>
            /// <param name="token">Trace token. Supports multi-threaded trace start and stop.</param>
            /// <returns>the elapsed time</returns>
            internal TimeSpan TraceStop(Trace token)
            {
                if (_keys.Remove(token, out var timer))
                {
                    timer.Stop();
                    var properties = new Dictionary<string, string> {
                        { "trace", $"Telemetry Trace: {token.MethodSignature}" },
                        { "TotalMilliSeconds", timer.ElapsedMilliseconds.ToString() }
                    };
                    Basic.TelemetryLogMessage($"Telemetry: {token.MethodSignature}", properties);
                    return timer.Elapsed;
                }
                else throw new NotImplementedException();
            }

            /// <summary>
            /// Add TrackDependency
            /// </summary>
            /// <param name="dependencyTypeName"></param>
            /// <param name="dependencyName"></param>
            /// <param name="data"></param>
            internal void TrackDependency(string dependencyTypeName, string dependencyName, string data)
            {
                var properties = new Dictionary<string, string>{
                    { "dependency", $"Telemetry Dependency" },
                    { "DependencyTypeName", dependencyTypeName},
                    { "Data",  data}
                };

                this.Basic.TelemetryLogMessage($"Telemetry Dependency: {dependencyName} ", properties);
            }
        }
    }
}