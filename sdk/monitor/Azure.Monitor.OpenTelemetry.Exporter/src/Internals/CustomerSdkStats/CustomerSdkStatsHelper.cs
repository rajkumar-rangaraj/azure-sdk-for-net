// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using Azure.Monitor.OpenTelemetry.Exporter.Internals.Diagnostics;
using Azure.Monitor.OpenTelemetry.Exporter.Internals.Platform;

namespace Azure.Monitor.OpenTelemetry.Exporter.Internals.CustomerSdkStats
{
    /// <summary>
    /// Helper class for tracking customer SDK statistics.
    /// </summary>
    internal static class CustomerSdkStatsHelper
    {
        private static bool? s_isEnabled;

        /// <summary>
        /// Checks if customer SDK stats are enabled.
        /// </summary>
        /// <returns>True if enabled, false otherwise</returns>
        public static bool IsEnabled()
        {
            if (s_isEnabled == null)
            {
                var enabledValue = DefaultPlatform.Instance.GetEnvironmentVariable(EnvironmentVariableConstants.APPLICATIONINSIGHTS_SDKSTATS_ENABLED_PREVIEW);
                s_isEnabled = string.Equals(enabledValue, "true", StringComparison.OrdinalIgnoreCase);
            }
            return s_isEnabled.Value;
        }

        /// <summary>
        /// Gets the configured export interval for customer SDK stats in milliseconds.
        /// </summary>
        /// <returns>Export interval in milliseconds (default: 900000 = 15 minutes)</returns>
        public static int GetExportIntervalMilliseconds()
        {
            var intervalValue = DefaultPlatform.Instance.GetEnvironmentVariable(EnvironmentVariableConstants.APPLICATIONINSIGHTS_SDKSTATS_EXPORT_INTERVAL);

            if (int.TryParse(intervalValue, out int intervalSeconds) && intervalSeconds > 0)
            {
                var intervalMs = Math.Max(60_000, intervalSeconds * 1_000); // Minimum 1 minute
                return Math.Min(intervalMs, 24 * 60 * 60 * 1_000); // Maximum 24 hours
            }

            return 900_000; // Default: 15 minutes
        }

        /// <summary>
        /// Tracks successful telemetry transmission using pre-computed counts.
        /// </summary>
        /// <param name="telemetryTypeCounts">Pre-computed telemetry type counts</param>
        public static void TrackSuccess(Dictionary<string, int> telemetryTypeCounts)
        {
            if (!IsEnabled())
                return;

            try
            {
                foreach (var kvp in telemetryTypeCounts)
                {
                    var tags = CustomerSdkStatsDimensions.GetBaseTags(kvp.Key);
                    CustomerSdkStatsMeters.ItemSuccessCount.Add(kvp.Value, tags);
                }
            }
            catch (Exception ex)
            {
                AzureMonitorExporterEventSource.Log.CustomerSdkStatsTrackingFailed("success", ex);
            }
        }
    }
}
