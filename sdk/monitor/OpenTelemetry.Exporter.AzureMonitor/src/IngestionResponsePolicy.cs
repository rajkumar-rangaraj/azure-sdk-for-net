// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Linq;
using Azure.Core;
using Azure.Core.Pipeline;
using OpenTelemetry.Exporter.AzureMonitor.Models;
using OpenTelemetry.Exporter.AzureMonitor.Storage;

namespace OpenTelemetry.Exporter.AzureMonitor
{
    internal class IngestionResponsePolicy : HttpPipelineSynchronousPolicy
    {
        private readonly BackoffLogicManager backoffLogicManager;
        private readonly ClientDiagnostics clientDiagnostics;
        private readonly LocalFileStorage storage;

        internal IngestionResponsePolicy(ClientDiagnostics clientDiagnostics, LocalFileStorage storage, BackoffLogicManager backoffLogicManager)
        {
            this.backoffLogicManager = backoffLogicManager;
            this.clientDiagnostics = clientDiagnostics;
            this.storage = storage;
        }

        public override void OnReceivedResponse(HttpMessage message)
        {
            base.OnReceivedResponse(message);

            int itemsAccepted;

            if (message.TryGetProperty("TelemetryItems", out var telemetryItems))
            {
                itemsAccepted = ParseResponse(message, (IEnumerable<TelemetryItem>)telemetryItems);
            }
            else
            {
                itemsAccepted = ParseResponse(message);
            }

            message.SetProperty("ItemsAccepted", itemsAccepted);
        }

        internal int ParseResponse(HttpMessage message, IEnumerable<TelemetryItem> telemetryItems)
        {
            var httpStatus = message?.Response?.Status;
            int itemsAccepted = 0;

            switch (httpStatus)
            {
                case 200:
                    backoffLogicManager.ReportBackoffDisabled();
                    itemsAccepted = telemetryItems.Count(); 
                    break;
                case 400:
                case 429:
                case 439:
                    if (AzureMonitorTransmitter.TryParseRetryInterval(message, out var interval))
                    {
                        // TODO
                    }

                    AzureMonitorTransmitter.SendMessagesToStorage(storage, message);
                    break;
                case 500:
                case 502:
                case 503:
                case 504:
                    backoffLogicManager.ReportBackoffEnabled();
                    backoffLogicManager.GetBackOffTime();
                    AzureMonitorTransmitter.SendMessagesToStorage(storage, message);
                    break;
                case 206:
                    if (AzureMonitorTransmitter.TryParseRetryInterval(message, out interval))
                    {
                        // TODO
                    }
                    itemsAccepted = AzureMonitorTransmitter.StoreFailedMessage(storage, telemetryItems, message);
                    break;
                case null:
                    AzureMonitorTransmitter.SendTelemetryItemsToStorage(storage, telemetryItems);
                    break;
                default:
                    backoffLogicManager.ReportBackoffDisabled();
                    AzureMonitorTransmitter.ReportNonRetriableStatus(clientDiagnostics, message);
                    break;
            }

            return itemsAccepted;
        }

        internal int ParseResponse(HttpMessage message)
        {
            var httpStatus = message?.Response?.Status;
            int itemsAccepted = 0;

            switch (httpStatus)
            {
                case 200:
                    backoffLogicManager.ReportBackoffDisabled();
                    itemsAccepted = AzureMonitorTransmitter.GetItemsAccepted(message);
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
                    itemsAccepted = AzureMonitorTransmitter.StoreFailedMessage(storage, message);
                    break;
                default:
                    AzureMonitorTransmitter.ReportNonRetriableStatus(clientDiagnostics, message);
                    break;
            }

            return itemsAccepted;
        }

    }
}
