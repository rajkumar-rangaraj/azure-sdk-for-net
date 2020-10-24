// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace OpenTelemetry.Exporter.AzureMonitor.Storage
{
    /// <summary>
    /// Represents a persistent blob.
    /// </summary>
    public interface IPersistentBlob
    {
        /// <summary>
        /// Reads the content from the blob.
        /// </summary>
        /// <returns>
        /// The content of the blob if the operation succeeded, otherwise null.
        /// </returns>
        /// <remarks>
        /// This function should never throw exception.
        /// </remarks>
        byte[] Read();

        /// <summary>
        /// Writes the given content to the blob.
        /// </summary>
        /// <param name="buffer">
        /// The content to be written.
        /// </param>
        /// <param name="leasePeriodMilliseconds">
        /// The number of milliseconds to lease after the write operation finished.
        /// </param>
        /// <returns>
        /// The same blob if the operation succeeded, otherwise null.
        /// </returns>
        /// <remarks>
        /// This function should never throw exception.
        /// </remarks>
        IPersistentBlob Write(byte[] buffer, int leasePeriodMilliseconds = 0);

        /// <summary>
        /// Creates a lease on the blob.
        /// </summary>
        /// <param name="leasePeriodMilliseconds">
        /// The number of milliseconds to lease.
        /// </param>
        /// <returns>
        /// The same blob if the lease operation succeeded, otherwise null.
        /// </returns>
        /// <remarks>
        /// This function should never throw exception.
        /// </remarks>
        IPersistentBlob Lease(int leasePeriodMilliseconds);

        /// <summary>
        /// Attempts to delete the blob.
        /// </summary>
        /// <remarks>
        /// This function should never throw exception.
        /// </remarks>
        void Delete();
    }
}
