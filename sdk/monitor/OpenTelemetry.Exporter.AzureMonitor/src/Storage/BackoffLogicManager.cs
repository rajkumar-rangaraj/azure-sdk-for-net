// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;

namespace OpenTelemetry.Exporter.AzureMonitor.Storage
{
    internal class BackoffLogicManager
    {
        private const int SlotDelayInSeconds = 10;
        private const int MaxDelayInSeconds = 3600;
        private const int DefaultBackoffEnabledReportingIntervalInMin = 30;

        private static readonly Random Random = new Random();
        private readonly object lockConsecutiveErrors = new object();
        private readonly TimeSpan minIntervalToUpdateConsecutiveErrors;

        private DateTimeOffset nextMinTimeToUpdateConsecutiveErrors = DateTimeOffset.MinValue;

        internal BackoffLogicManager(TimeSpan defaultBackoffEnabledReportingInterval = default, TimeSpan minIntervalToUpdateConsecutiveErrors = default)
        {
            this.DefaultBackoffEnabledReportingInterval = defaultBackoffEnabledReportingInterval == default ? TimeSpan.FromMinutes(DefaultBackoffEnabledReportingIntervalInMin) : defaultBackoffEnabledReportingInterval;
            this.minIntervalToUpdateConsecutiveErrors = minIntervalToUpdateConsecutiveErrors == default ? TimeSpan.FromSeconds(SlotDelayInSeconds) : minIntervalToUpdateConsecutiveErrors;
            this.CurrentDelay = TimeSpan.FromSeconds(SlotDelayInSeconds);
        }

        internal int ConsecutiveErrors { get; private set; }

        internal TimeSpan DefaultBackoffEnabledReportingInterval { get; set; }

        internal TimeSpan CurrentDelay { get; private set; }

        internal bool ExponentialBackoffReported { get; private set; } = false;

        internal void ResetConsecutiveErrors()
        {
            lock (this.lockConsecutiveErrors)
            {
                this.ConsecutiveErrors = 0;
            }
        }

        internal void ReportBackoffEnabled(int statusCode = default)
        {
            if (!this.ExponentialBackoffReported && this.CurrentDelay > this.DefaultBackoffEnabledReportingInterval)
            {
                _ = statusCode; // To keep compiler happy, remove once the log is enabled.
                // TelemetryChannelEventSource.Log.BackoffEnabled(this.CurrentDelay.TotalMinutes, statusCode);
                this.ExponentialBackoffReported = true;
            }

            lock (this.lockConsecutiveErrors)
            {
                if (DateTimeOffset.UtcNow > this.nextMinTimeToUpdateConsecutiveErrors)
                {
                    this.ConsecutiveErrors++;
                    this.nextMinTimeToUpdateConsecutiveErrors = DateTimeOffset.UtcNow + this.minIntervalToUpdateConsecutiveErrors;
                }
            }
        }

        internal void ReportBackoffDisabled()
        {
            if (this.ExponentialBackoffReported)
            {
                // TelemetryChannelEventSource.Log.BackoffDisabled();
                this.ExponentialBackoffReported = false;
                ResetConsecutiveErrors();
            }
        }

        // Calculates the time to wait before retrying in case of an error based on
        // http://en.wikipedia.org/wiki/Exponential_backoff
        internal TimeSpan GetBackOffTime()
        {
            double delayInSeconds;

            if (this.ConsecutiveErrors <= 1)
            {
                delayInSeconds = SlotDelayInSeconds;
            }
            else
            {
                double backOffSlot = (Math.Pow(2, this.ConsecutiveErrors) - 1) / 2;
                var backOffDelay = Random.Next(1, (int)Math.Min(backOffSlot * SlotDelayInSeconds, int.MaxValue));
                delayInSeconds = Math.Max(Math.Min(backOffDelay, MaxDelayInSeconds), SlotDelayInSeconds);
            }

            // TelemetryChannelEventSource.Log.BackoffTimeSetInSeconds(delayInSeconds);
            var retryAfterTimeSpan = TimeSpan.FromSeconds(delayInSeconds);

            this.CurrentDelay = retryAfterTimeSpan;
            // TelemetryChannelEventSource.Log.BackoffInterval(retryAfterTimeSpan.TotalSeconds);
            return retryAfterTimeSpan;
        }
    }
}
