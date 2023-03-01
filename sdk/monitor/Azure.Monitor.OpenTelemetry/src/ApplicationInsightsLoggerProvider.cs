// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

#nullable disable

using System;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using OpenTelemetry.Logs;

namespace Azure.Monitor.OpenTelemetry
{
    internal class ApplicationInsightsLoggerProvider : ILoggerProvider
    {
        private readonly AzureMonitorOpenTelemetryOptions _options;
        private readonly string _connectionString;

        public ApplicationInsightsLoggerProvider(IOptionsMonitor<AzureMonitorOpenTelemetryOptions> azureMonitorOpenTelemetryOptions)
        {
            _options = azureMonitorOpenTelemetryOptions?.CurrentValue ?? throw new ArgumentNullException(nameof(azureMonitorOpenTelemetryOptions));
            _connectionString = _options.ConnectionString;
        }

        public ILogger CreateLogger(string categoryName)
        {
            return NullLogger.Instance;
        }

        public void Dispose()
        {
        }

        public AzureMonitorOpenTelemetryOptions GetAzureMonitorOpenTelemetryOptions()
        {
            return _options;
        }
    }
}
