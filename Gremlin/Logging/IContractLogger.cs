using System;
using System.Collections.Generic;

namespace n_ate.Gremlin.Logging
{
    /// <summary>
    /// The contract logger leverages both an ILogger instance and Telemetry while enforcing messaging formatting.
    /// </summary>
    public interface IContractLogger
    {
        /// <summary>
        /// Logger logs a message.
        /// </summary>
        /// <param name="message">The message to log.</param>
        void LoggerLogMessage(string message);

        /// <summary>
        /// Telemetry logs an exception with properties.
        /// </summary>
        /// <param name="exception">The exception to log.</param>
        /// <param name="properties">The properties to log.</param>
        void TelemetryLogException(Exception ex, IDictionary<string, string> properties);

        /// <summary>
        /// Telemetry logs a message with properties.
        /// </summary>
        /// <param name="message">The message to log.</param>
        /// <param name="properties">The properties to log.</param>
        void TelemetryLogMessage(string message, IDictionary<string, string> properties);
    }
}