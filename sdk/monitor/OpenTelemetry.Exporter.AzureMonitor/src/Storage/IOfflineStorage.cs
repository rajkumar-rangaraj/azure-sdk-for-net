// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace OpenTelemetry.Exporter.AzureMonitor.Storage
{
    internal interface IOfflineStorage
    {
        IEnumerable<IOfflineBlob> GetBlobs();

        LocalFileBlob GetBlob(string name = null);

        string PutBlob(byte[] buffer, int leasePeriod = 0);
    }
}
