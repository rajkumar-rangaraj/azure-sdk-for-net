// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Core.Pipeline;

using OpenTelemetry.Exporter.AzureMonitor.ConnectionString;
using OpenTelemetry.Exporter.AzureMonitor.Models;
using OpenTelemetry.Exporter.AzureMonitor.Storage;

namespace OpenTelemetry.Exporter.AzureMonitor
{
    /// <summary>
    /// This class encapsulates transmitting a collection of <see cref="TelemetryItem"/> to the configured Ingestion Endpoint.
    /// </summary>
    internal class AzureMonitorTransmitter : IDisposable
    {
        private readonly ApplicationInsightsRestClient applicationInsightsRestClient;
        private readonly AzureMonitorExporterOptions options;
        private readonly LocalFileStorage storage;

        public AzureMonitorTransmitter(AzureMonitorExporterOptions exporterOptions)
        {
            ConnectionStringParser.GetValues(exporterOptions.ConnectionString, out _, out string ingestionEndpoint);

            options = exporterOptions;
            applicationInsightsRestClient = new ApplicationInsightsRestClient(new ClientDiagnostics(options), HttpPipelineBuilder.Build(options), host: ingestionEndpoint);

            storage = new LocalFileStorage(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test"));
        }

        public async ValueTask<int> TrackAsync(IEnumerable<TelemetryItem> telemetryItems, bool async, CancellationToken cancellationToken)
        {
            // Prevent Azure Monitor's HTTP operations from being instrumented.
            using var scope = SuppressInstrumentationScope.Begin();

            if (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }

            // Azure.Response<TrackResponse> response = null;
            (Response<TrackResponse> response, RequestContent content) message;

            try
            {
                if (async)
                {
                    // TODO: RequestFailedException is thrown when http response is not equal to 200 or 206. Implement logic to catch exception.
                    message = await this.applicationInsightsRestClient.InternalTrackAsync(telemetryItems, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    message = this.applicationInsightsRestClient.InternalTrackAsync(telemetryItems, cancellationToken).Result;
                }

                if (message.response.GetRawResponse().Status != 200)
                {
                    MemoryStream stream = new MemoryStream();
#pragma warning disable AZC0110 // DO NOT use await keyword in possibly synchronous scope.
                    await message.content.WriteToAsync(stream, CancellationToken.None).ConfigureAwait(false);
#pragma warning restore AZC0110 // DO NOT use await keyword in possibly synchronous scope.
                    storage.PutBlob(stream.ToArray());

                    _ = this.applicationInsightsRestClient.InternalTrackAsync(storage.GetBlob().Read()).Result;
                }
            }
            catch (Exception ex)
            {
                // TODO: Log the exception to new event source. If we get a common logger we could just log exception to it.
                AzureMonitorTraceExporterEventSource.Log.FailedExport(ex);
            }

            // TODO: Handle exception, check telemetryItems has items
            // return message.response.Value.ItemsAccepted.GetValueOrDefault();
            return default;
        }

        public void Dispose()
        {
            storage.Dispose();
        }
    }
}
