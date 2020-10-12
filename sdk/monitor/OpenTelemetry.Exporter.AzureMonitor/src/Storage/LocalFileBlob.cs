// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.IO;

namespace OpenTelemetry.Exporter.AzureMonitor.Storage
{
    internal class LocalFileBlob : IOfflineBlob
    {
        internal LocalFileBlob(string fullPath)
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
            catch (Exception)
            {
                // TODO: Log Exception
            }

            return null;
        }

        public void Write(byte[] buffer, int leasePeriod = 0)
        {
            string path = this.FullPath + ".tmp";

            try
            {
                File.WriteAllBytes(path, buffer);

                if (leasePeriod > 0)
                {
                    var timestamp = DateTime.Now.ToUniversalTime() + TimeSpan.FromSeconds(leasePeriod);
                    this.FullPath += $@"{timestamp:yyy-MM-ddTHHmmss.ffffff}.lock";
                }

                File.Move(path, this.FullPath);
            }
            catch (Exception)
            {
                // TODO: Log Exception
            }
        }

        public void Lease(int seconds)
        {
            var path = this.FullPath;
            var leaseTimestamp = DateTime.Now.ToUniversalTime() + TimeSpan.FromSeconds(seconds);
            if (path.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
            {
                path = path.Substring(0, path.LastIndexOf('@'));
            }

            path += $"@{leaseTimestamp:yyy-MM-ddTHHmmss.ffffff}.lock";

            try
            {
                File.Move(this.FullPath, path);
            }
            catch (Exception)
            {
                // TODO: Log Exception
            }

            this.FullPath = path;
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
            catch (Exception)
            {
                // Log Exception
            }
        }
    }
}
