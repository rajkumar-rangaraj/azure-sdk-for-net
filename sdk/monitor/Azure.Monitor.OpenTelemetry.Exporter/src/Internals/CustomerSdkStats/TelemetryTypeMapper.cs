// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Monitor.OpenTelemetry.Exporter.Models;

namespace Azure.Monitor.OpenTelemetry.Exporter.Internals.CustomerSdkStats
{
    /// <summary>
    /// Maps internal telemetry types to customer SDK stats telemetry type strings.
    /// </summary>
    internal static class TelemetryTypeMapper
    {
        /// <summary>
        /// Maps telemetry item base type to customer SDK stats telemetry type.
        /// </summary>
        /// <param name="baseType">The base type from TelemetryItem</param>
        /// <returns>The telemetry type string for customer SDK stats</returns>
        public static string MapTelemetryType(string? baseType)
        {
            return baseType switch
            {
                "RequestData" => "REQUEST",
                "RemoteDependencyData" => "DEPENDENCY",
                "MessageData" => "TRACE",
                "ExceptionData" => "EXCEPTION",
                "MetricData" => "CUSTOM_METRIC",
                "EventData" => "CUSTOM_EVENT",
                "PageViewData" => "PAGE_VIEW",
                "AvailabilityData" => "AVAILABILITY",
                "PerformanceCounterData" => "PERFORMANCE_COUNTER",
                _ => "UNKNOWN"
            };
        }
    }
}
