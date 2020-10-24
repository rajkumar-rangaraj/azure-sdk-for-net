// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics.Tracing;
using System.Runtime.CompilerServices;

namespace OpenTelemetry.Exporter.AzureMonitor.Storage
{
    [EventSource(Name = EventSourceName)]
    internal sealed class SharedEventSource : EventSource
    {
        private const string EventSourceName = "OpenTelemetry-Shared";
        public static SharedEventSource Log = new SharedEventSource();

        [NonEvent]
        public void Critical(string message, object value = null)
        {
            Write(EventLevel.Critical, message, value);
        }

        [NonEvent]
        public void Error(string message, object value = null)
        {
            Write(EventLevel.Error, message, value);
        }

        [NonEvent]
        public void Warning(string message, object value = null)
        {
            Write(EventLevel.Warning, message, value);
        }

        [NonEvent]
        public void Informational(string message, object value = null)
        {
            Write(EventLevel.Informational, message, value);
        }

        [NonEvent]
        public void Verbose(string message, object value = null)
        {
            Write(EventLevel.Verbose, message, value);
        }

        [Event(1, Message = "{0}", Level = EventLevel.Critical)]
        public void WriteCritical(string message) => this.WriteEvent(1, message);

        [Event(2, Message = "{0}", Level = EventLevel.Error)]
        public void WriteError(string message) => this.WriteEvent(2, message);

        [Event(3, Message = "{0}", Level = EventLevel.Warning)]
        public void WriteWarning(string message) => this.WriteEvent(3, message);

        [Event(4, Message = "{0}", Level = EventLevel.Informational)]
        public void WriteInformational(string message) => this.WriteEvent(4, message);

        [Event(5, Message = "{0}", Level = EventLevel.Verbose)]
        public void WriteVerbose(string message) => this.WriteEvent(5, message);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static string GetMessage(object value)
        {
            return value is Exception exception ? exception.ToInvariantString() : value.ToString();
        }

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private void Write(EventLevel eventLevel, string message, object value)
        {
            if (this.IsEnabled(eventLevel, EventKeywords.All))
            {
                var logMessage = value == null ? message : $"{message} - {GetMessage(value)}";

                switch (eventLevel)
                {
                    case EventLevel.Critical:
                        WriteCritical(logMessage);
                        break;
                    case EventLevel.Error:
                        WriteError(logMessage);
                        break;
                    case EventLevel.Informational:
                        WriteInformational(logMessage);
                        break;
                    case EventLevel.Verbose:
                        WriteVerbose(logMessage);
                        break;
                    case EventLevel.Warning:
                        WriteWarning(logMessage);
                        break;
                }
            }
        }
    }
}
