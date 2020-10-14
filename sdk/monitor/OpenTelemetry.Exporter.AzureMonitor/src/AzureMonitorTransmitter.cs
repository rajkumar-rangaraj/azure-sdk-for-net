// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text.Json;
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
        private static List<int> whiteListedStatusCode = new List<int> { 429, 439, 500, 502, 503, 504 };
        private readonly ApplicationInsightsRestClient applicationInsightsRestClient;
        private readonly AzureMonitorExporterOptions options;
        private readonly LocalFileStorage storage;
        private static BackoffLogicManager backoffLogicManager = new BackoffLogicManager();

        public AzureMonitorTransmitter(AzureMonitorExporterOptions exporterOptions)
        {
            ConnectionStringParser.GetValues(exporterOptions.ConnectionString, out _, out string ingestionEndpoint);
            options = exporterOptions;
            applicationInsightsRestClient = new ApplicationInsightsRestClient(new ClientDiagnostics(options), HttpPipelineBuilder.Build(options), host: ingestionEndpoint);

            storage = new LocalFileStorage(Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "test"));
            backoffLogicManager = new BackoffLogicManager();
        }

        public async ValueTask<int> TrackAsync(IEnumerable<TelemetryItem> telemetryItems, bool async, CancellationToken cancellationToken)
        {
            // Prevent Azure Monitor's HTTP operations from being instrumented.
            using var scope = SuppressInstrumentationScope.Begin();

            if (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }

            if (backoffLogicManager.ExponentialBackoffReported)
            {
                SendTelemetryItemsToStorage(storage, telemetryItems);
                return 0;
            }

            HttpMessage message = default;

            try
            {
                if (async)
                {
                    message = await this.applicationInsightsRestClient.InternalTrackAsync(telemetryItems, cancellationToken).ConfigureAwait(false);
                }
                else
                {
                    message = this.applicationInsightsRestClient.InternalTrackAsync(telemetryItems, cancellationToken).Result;
                }
            }
            catch (Exception ex)
            {
                if (ex?.InnerException?.InnerException?.Source == "System.Net.Http")
                {
                    // Check if this required.
                }

                // TODO: Log the exception to new event source. If we get a common logger we could just log exception to it.
                AzureMonitorTraceExporterEventSource.Log.FailedExport(ex);
            }

            // TODO: Handle exception, check telemetryItems has items
            return ParseResponse(applicationInsightsRestClient, storage, telemetryItems, message, cancellationToken);
        }

        private static void SendDataFromStorage(ApplicationInsightsRestClient applicationInsightsRestClient, LocalFileStorage storage, CancellationToken cancellationToken)
        {
            foreach (LocalFileBlob blob in storage.GetBlobs())
            {
                blob.Lease(10);
                var message = applicationInsightsRestClient.InternalTrackAsync(blob.Read()).Result;
                int itemsAccepted = ParseResponse(applicationInsightsRestClient, storage, message, cancellationToken);
                if (itemsAccepted > 0)
                {
                    blob.Delete();
                }
            }
        }

        private static int ParseResponse(ApplicationInsightsRestClient applicationInsightsRestClient, LocalFileStorage storage, IEnumerable<TelemetryItem> telemetryItems, HttpMessage message, CancellationToken cancellationToken)
        {
            var httpStatus = message?.Response?.Status;
            int itemsAccepted = 0;

            switch (httpStatus)
            {
                case 200:
                    backoffLogicManager.ReportBackoffDisabled();
                    itemsAccepted = telemetryItems.Count(); // GetItemsAccepted(message, cancellationToken);
                    break;
                case 400:
                case 429:
                case 439:
                    if (TryParseRetryInterval(message, out var interval))
                    {
                        // TODO
                    }

                    SendMessagesToStorage(storage, message);
                    break;
                case 500:
                case 502:
                case 503:
                case 504:
                    backoffLogicManager.ReportBackoffEnabled();
                    backoffLogicManager.GetBackOffTime();
                    SendMessagesToStorage(storage, message);
                    _ = applicationInsightsRestClient.InternalTrackAsync(storage.GetBlob().Read()).Result;
                    break;
                case 206:
                    if (TryParseRetryInterval(message, out interval))
                    {
                        // TODO
                    }
                    itemsAccepted = StoreFailedMessage(storage, telemetryItems, message, cancellationToken);
                    break;
                case null:
                    SendTelemetryItemsToStorage(storage, telemetryItems);
                    break;
                default:
                    backoffLogicManager.ReportBackoffDisabled();
                    ReportNonRetriableStatus(applicationInsightsRestClient._clientDiagnostics, message);
                    break;
            }

            return itemsAccepted;
        }

        private static int ParseResponse(ApplicationInsightsRestClient applicationInsightsRestClient, LocalFileStorage storage, HttpMessage message, CancellationToken cancellationToken)
        {
            var httpStatus = message?.Response?.Status;
            int itemsAccepted = 0;

            switch (httpStatus)
            {
                case 200:
                    backoffLogicManager.ReportBackoffDisabled();
                    itemsAccepted = GetItemsAccepted(message, cancellationToken);
                    break;
                case 429:
                case 439:
                case 500:
                case 502:
                case 503:
                case 504:
                case null:
                    itemsAccepted = 0;
                    break;
                case 206:
                    itemsAccepted = StoreFailedMessage(storage, message, cancellationToken);
                    break;
                default:
                    ReportNonRetriableStatus(applicationInsightsRestClient._clientDiagnostics, message);
                    break;
            }

            return itemsAccepted;
        }

        internal static int GetItemsAccepted(HttpMessage message, CancellationToken cancellationToken)
        {
            int itemsAccepted = 0;
            using (JsonDocument document = JsonDocument.ParseAsync(message.Response.ContentStream, default, cancellationToken).Result)
            {
                var value = TrackResponse.DeserializeTrackResponse(document.RootElement);
                Response.FromValue(value, message.Response);
                itemsAccepted = value.ItemsAccepted.GetValueOrDefault();
            }

            return itemsAccepted;
        }

        internal static bool TryParseRetryInterval(HttpMessage message, out double? interval)
        {
            string retryAfter = message.Response.Headers.TryGetValue("Retry-After", out string value) ? value : null;
            interval = null;

            if (string.IsNullOrEmpty(retryAfter))
            {
                return false;
            }

            // TelemetryChannelEventSource.Log.RetryAfterHeaderIsPresent(retryAfter);

            var now = DateTimeOffset.UtcNow;
            if (DateTimeOffset.TryParse(retryAfter, out DateTimeOffset retryAfterDate))
            {
                if (retryAfterDate > now)
                {
                    var retryAfterTimeSpan = retryAfterDate - now;
                    interval = retryAfterTimeSpan.TotalSeconds;
                    return true;
                }

                return false;
            }

            // TelemetryChannelEventSource.Log.TransmissionPolicyRetryAfterParseFailedWarning(retryAfter);

            return false;
        }

        internal static void ReportNonRetriableStatus(ClientDiagnostics clientDiagnostics, HttpMessage message)
        {
            try
            {
                _ = clientDiagnostics.CreateRequestFailedExceptionAsync(message.Response).Result;
            }
            catch (Exception)
            {
                // Log
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SendMessagesToStorage(LocalFileStorage storage, HttpMessage message)
        {
            try
            {
                MemoryStream stream = new MemoryStream();
                message.Response.ContentStream.CopyTo(stream);
                storage.PutBlob(stream.ToArray());
            }
            catch // (Exception ex)
            {
                // Log
            }
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static void SendTelemetryItemsToStorage(LocalFileStorage storage, IEnumerable<TelemetryItem> telemetryItems)
        {
            try
            {
                using var content = new NDJsonWriter();
                foreach (var telemetryItem in telemetryItems)
                {
                    content.JsonWriter.WriteObjectValue(telemetryItem);
                    content.WriteNewLine();
                }

                storage.PutBlob(content.ToBytes().Span.ToArray());
            }
            catch // (Exception ex)
            {
                // Log
            }
        }

        internal static int StoreFailedMessage(LocalFileStorage storage, IEnumerable<TelemetryItem> telemetryItems, HttpMessage message, CancellationToken cancellationToken)
        {
            int itemsAccepted = 0;
            using (JsonDocument document = JsonDocument.ParseAsync(message.Response.ContentStream, default, cancellationToken).Result)
            {
                var value = TrackResponse.DeserializeTrackResponse(document.RootElement);
                Response.FromValue(value, message.Response);
                itemsAccepted = value.ItemsAccepted.GetValueOrDefault();

                using var content = new NDJsonWriter();
                foreach (var item in value.Errors)
                {
                    if (item.StatusCode != null && item.Index != null && whiteListedStatusCode.Contains((int)item.StatusCode))
                    {
                        content.JsonWriter.WriteObjectValue(telemetryItems.ElementAt((int)item.Index));
                        content.WriteNewLine();
                    }
                }

                storage.PutBlob(content.ToBytes().Span.ToArray());
            }

            return itemsAccepted;
        }

        internal static int StoreFailedMessage(LocalFileStorage storage, HttpMessage message, CancellationToken cancellationToken)
        {
            int itemsAccepted = 0;
            using (JsonDocument document = JsonDocument.ParseAsync(message.Response.ContentStream, default, cancellationToken).Result)
            {
                var value = TrackResponse.DeserializeTrackResponse(document.RootElement);
                Response.FromValue(value, message.Response);
                itemsAccepted = value.ItemsAccepted.GetValueOrDefault();

                using var readMemoryStream = new MemoryStream();
                message.Request.Content.WriteTo(readMemoryStream, CancellationToken.None);
                readMemoryStream.Position = 0;
                using StreamReader streamReader = new StreamReader(readMemoryStream);
                var str = streamReader.ReadToEnd();
                var requestContent = str.Split('\n');

                using var writeMemoryStream1 = new MemoryStream();
                using var writer = new StreamWriter(writeMemoryStream1);
                foreach (var item in value.Errors)
                {
                    if (item.StatusCode != null && item.Index != null && whiteListedStatusCode.Contains((int)item.StatusCode))
                    {
                        writer.Write(requestContent[(int)item.Index]);
                        writer.Write(Environment.NewLine);
                    }
                }
                writer.Flush();
                storage.PutBlob(writeMemoryStream1.ToArray());
            }

            return itemsAccepted;
        }

        public void Dispose()
        {
            storage.Dispose();
        }
    }
}
