// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace OpenTelemetry.Exporter.AzureMonitor.Storage
{
    public class LocalFileBlob : IPersistentBlob
    {
        public LocalFileBlob(string fullPath)
        {
            this.FullPath = fullPath;
        }

        public string FullPath { get; private set; }

        public byte[] Read()
        {
            try
            {
                return File.ReadAllBytes(this.FullPath);
            }
            catch (Exception ex)
            {
                SharedEventSource.Log.Warning($"Reading a blob from file {this.FullPath} has failed.", ex);
            }

            return null;
        }

        public IPersistentBlob Write(byte[] buffer, int leasePeriodMilliseconds = 0)
        {
            string path = this.FullPath + ".tmp";

            try
            {
                File.WriteAllBytes(path, buffer);

                if (leasePeriodMilliseconds > 0)
                {
                    var timestamp = DateTime.Now.ToUniversalTime() + TimeSpan.FromMilliseconds(leasePeriodMilliseconds);
                    this.FullPath += $"@{timestamp:yyy-MM-ddTHHmmss.ffffff}.lock";
                }

                File.Move(path, this.FullPath);
            }
            catch (Exception ex)
            {
                SharedEventSource.Log.Warning($"Writing a blob to file {path} has failed.", ex);
            }

            return this;
        }

        public IPersistentBlob Lease(int leasePeriodMilliseconds)
        {
            var path = this.FullPath;
            var leaseTimestamp = DateTime.Now.ToUniversalTime() + TimeSpan.FromMilliseconds(leasePeriodMilliseconds);
            if (path.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.LastIndexOf('@'));
            }

            path += $"@{leaseTimestamp:yyy-MM-ddTHHmmss.ffffff}.lock";

            try
            {
                File.Move(this.FullPath, path);
            }
            catch (Exception ex)
            {
                SharedEventSource.Log.Warning($"Acquiring a lease to file {this.FullPath} has failed.", ex);
            }

            this.FullPath = path;
            return this;
        }

        public void Delete()
        {
            try
            {
                if (File.Exists(this.FullPath))
                {
                    File.Delete(this.FullPath);
                }
            }
            catch (Exception ex)
            {
                SharedEventSource.Log.Warning($"Deletion of file blob {this.FullPath} has failed.", ex);
            }
        }
    }
}
