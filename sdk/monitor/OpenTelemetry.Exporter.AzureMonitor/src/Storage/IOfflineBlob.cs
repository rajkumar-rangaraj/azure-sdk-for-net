// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace OpenTelemetry.Exporter.AzureMonitor.Storage
{
    internal interface IOfflineBlob
    {
        byte[] Read();
        void Write(byte[] buffer, int leasePeriod = 0);
        void Lease(int seconds);
        void Delete();
        string FullPath { get; }
    }
}
