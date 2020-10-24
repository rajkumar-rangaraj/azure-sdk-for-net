// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;

namespace OpenTelemetry.Exporter.AzureMonitor.Storage
{
    /// <summary>
    /// Persistent storage API.
    /// </summary>
    public interface IPersistentStorage
    {
        /// <summary>
        /// Reads a sequence of blobs from storage.
        /// </summary>
        /// <returns>
        /// Sequence of blobs from storage.
        /// </returns>
        /// <remarks>
        /// This function should never throw exception.
        /// </remarks>
        IEnumerable<IPersistentBlob> GetBlobs();

        /// <summary>
        /// Attempts to get a blob from storage.
        /// </summary>
        /// <returns>
        /// A blob if there is an available one, or null if there is no blob available.
        /// </returns>
        /// <remarks>
        /// This function should never throw exception.
        /// </remarks>
        IPersistentBlob GetBlob();

        /// <summary>
        /// Creates a new blob with the provided data.
        /// </summary>
        /// <param name="buffer">
        /// The content to be written.
        /// </param>
        /// <param name="leasePeriodMilliseconds">
        /// The number of milliseconds to lease after the blob is created.
        /// </param>
        /// <returns>
        /// The created blob.
        /// </returns>
        /// <remarks>
        /// This function should never throw exception.
        /// </remarks>
        IPersistentBlob CreateBlob(byte[] buffer, int leasePeriodMilliseconds = 0);
    }
}
