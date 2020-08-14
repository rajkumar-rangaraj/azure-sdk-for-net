﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Azure.Core.Pipeline;
using OpenTelemetry.Exporter.AzureMonitor.Models;
using OpenTelemetry.Trace;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace OpenTelemetry.Exporter.AzureMonitor
{
    internal class AzureMonitorTransmitter
    {
        private readonly ServiceRestClient serviceRestClient;
        private readonly AzureMonitorExporterOptions options;

        private static readonly IReadOnlyDictionary<TelemetryType, string> Telemetry_Base_Type_Mapping = new Dictionary<TelemetryType, string>
        {
            [TelemetryType.Request] = "RequestData",
            [TelemetryType.Dependency] = "RemoteDependencyData",
            [TelemetryType.Message] = "MessageData",
            [TelemetryType.Event] = "EventData",
        };

        public AzureMonitorTransmitter(AzureMonitorExporterOptions exporterOptions)
        {
            options = exporterOptions;
            serviceRestClient = new ServiceRestClient(new ClientDiagnostics(options), HttpPipelineBuilder.Build(options));
        }

        internal async ValueTask<int> AddBatchActivityAsync(IEnumerable<Activity> batchActivity, CancellationToken cancellationToken)
        {
            if (cancellationToken.IsCancellationRequested)
            {
                return 0;
            }

            List<TelemetryEnvelope> telemetryItems = new List<TelemetryEnvelope>();
            TelemetryEnvelope telemetryItem = null;

            foreach (var activity in batchActivity)
            {
                telemetryItem = GeneratePartAEnvelope(activity);
                telemetryItem.Data = GenerateTelemetryData(activity);
                telemetryItems.Add(telemetryItem);
            }

            // TODO: Handle exception, check telemetryItems has items
            var response = await this.serviceRestClient.TrackAsync(telemetryItems, cancellationToken).ConfigureAwait(false);
            return response.Value.ItemsAccepted.GetValueOrDefault();
        }

        private static TelemetryEnvelope GeneratePartAEnvelope(Activity activity)
        {
            // TODO: Get TelemetryEnvelope name changed in swagger
            TelemetryEnvelope envelope = new TelemetryEnvelope(activity.DisplayName, activity.StartTimeUtc);
            // TODO: Extract IKey from connectionstring
            envelope.IKey = "6c49c07c-e95c-48fe-8a7b-eff230955cc5";
            // TODO: Validate if Azure SDK has common function to generate role instance
            envelope.Tags[ContextTagKeys.AiCloudRoleInstance.ToString()] = "testRoleInstance";

            envelope.Tags[ContextTagKeys.AiOperationId.ToString()] = activity.TraceId.ToHexString();
            if (activity.Parent != null)
            {
                envelope.Tags[ContextTagKeys.AiOperationParentId.ToString()] = activity.Parent.SpanId.ToHexString();
            }

            // TODO: "ai.location.ip"

            envelope.Tags[ContextTagKeys.AiInternalSdkVersion.ToString()] = "dotnet5:ot0.4.0-beta:ext1.0.0-alpha.1"; // {language}{sdkVersion}:ot{OpenTelemetryVersion}:ext{ExporterVersion}

            return envelope;
        }

        private Base GenerateTelemetryData(Activity activity)
        {
            Base telemetry = new Base();

            var telemetryType = activity.GetTelemetryType();
            telemetry.BaseType = Telemetry_Base_Type_Mapping[telemetryType];
            string url = ProcessHttpTags(activity.Tags);

            if (telemetryType == TelemetryType.Request)
            {
                var request = new RequestData(2, activity.Context.SpanId.ToHexString(), activity.Duration, activity.GetStatus().IsOk, activity.GetStatusCode())
                {
                    Name = activity.DisplayName,
                    Url = url,
                    // TODO: Handle activity.TagObjects
                    Properties = ExtractPropertiesFromTags(activity.Tags)
                    // TODO: Handle request.source.
                };

                telemetry.BaseData = request;
            }
            else if (telemetryType == TelemetryType.Dependency)
            {
                var dependency = new RemoteDependencyData(2, activity.DisplayName, activity.Duration)
                {
                    Id = activity.Context.SpanId.ToHexString(),
                    Success = activity.GetStatus().IsOk,
                    // TODO: Handle activity.TagObjects
                    Properties = ExtractPropertiesFromTags(activity.Tags)
                };

                if (url != null)
                {
                    dependency.Data = url;
                    dependency.Type = "HTTP"; // TODO: Parse for storage / SB.
                    dependency.ResultCode = activity.GetStatusCode();
                }

                // TODO: Handle dependency.target.
                telemetry.BaseData = dependency;
            }

            return telemetry;
        }

        private static string ProcessHttpTags(IEnumerable<KeyValuePair<string, string>> tags)
        {
            var httpTags = tags.Where(item => item.Key.StartsWith("http.", StringComparison.InvariantCulture))
                               .ToDictionary(item => item.Key, item => item.Value);


            httpTags.TryGetValue(SemanticConventions.AttributeHttpUrl, out var url);
            if (url != null)
            {
                return url;
            }

            httpTags.TryGetValue(SemanticConventions.AttributeHttpScheme, out var httpScheme);
            httpTags.TryGetValue(SemanticConventions.AttributeHttpHost, out var httpHost);
            httpTags.TryGetValue(SemanticConventions.AttributeHttpTarget, out var httpTarget);

            if (httpHost != null)
            {
                url = httpScheme + httpHost  + httpTarget;
                return url;
            }

            // TODO: Follow spec - https://github.com/open-telemetry/opentelemetry-specification/blob/master/specification/trace/semantic_conventions/http.md#http-client

            return url;
        }

        private static IDictionary<string, string> ExtractPropertiesFromTags(IEnumerable<KeyValuePair<string, string>> tags)
        {
            return tags.Where(item => !item.Key.StartsWith("http.", StringComparison.InvariantCulture))
                               .ToDictionary(item => item.Key, item => item.Value);
        }
    }
}
