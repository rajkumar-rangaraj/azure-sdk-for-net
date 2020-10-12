// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Timers;

namespace OpenTelemetry.Exporter.AzureMonitor.Storage
{
    internal class LocalFileStorage : IOfflineStorage, IDisposable
    {
        private readonly string path;
        private readonly long maxSize;
        private readonly int maintenancePeriod;
        private readonly long retentionPeriod;
        private readonly int writeTimeout;
        private readonly System.Timers.Timer maintenanceTimer;

        internal LocalFileStorage(
                                string path,
                                long maxSize = 52428800,
                                int maintenancePeriod = 6000,
                                long retentionPeriod = 172800,
                                int writeTimeout = 60)
        {
            if (path == null)
            {
                throw new ArgumentNullException(nameof(path));
            }

            this.path = CreateTelemetrySubdirectory(path);
            this.maxSize = maxSize;
            this.maintenancePeriod = maintenancePeriod;
            this.retentionPeriod = retentionPeriod;
            this.writeTimeout = writeTimeout;
            this.maintenanceTimer = new System.Timers.Timer(this.maintenancePeriod);
            this.maintenanceTimer.Elapsed += this.OnMaintenanceEvent;
            this.maintenanceTimer.AutoReset = true;
            this.maintenanceTimer.Enabled = true;
        }

        public void Dispose()
        {
            this.maintenanceTimer.Dispose();
        }

        public IEnumerable<IOfflineBlob> GetBlobs()
        {
            var currentUtcDateTime = DateTime.Now.ToUniversalTime();

            var leaseDeadline = currentUtcDateTime;
            var retentionDeadline = currentUtcDateTime - TimeSpan.FromSeconds(this.retentionPeriod);
            var timeoutDeadline = currentUtcDateTime - TimeSpan.FromSeconds(this.writeTimeout);

            foreach (var file in Directory.GetFiles(this.path))
            {
                var filePath = file;
                if (filePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime fileDateTime = GetDateTimeFromFileName(filePath, '-');
                    if (fileDateTime < timeoutDeadline)
                    {
                        DeleteFile(filePath);
                    }
                }

                if (filePath.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime fileDateTime = GetDateTimeFromFileName(filePath, '@');
                    if (fileDateTime > leaseDeadline)
                    {
                        continue;
                    }

                    var newFilePath = filePath.Substring(0, filePath.LastIndexOf('@'));
                    try
                    {
                        File.Move(filePath, newFilePath);
                    }
                    catch (Exception)
                    {
                        // TODO: Log Exception
                        newFilePath = filePath;
                    }

                    filePath = newFilePath;
                }

                if (filePath.EndsWith(".blob", StringComparison.OrdinalIgnoreCase))
                {
                    DateTime fileDateTime = GetDateTimeFromFileName(filePath, '-');
                    if (fileDateTime < retentionDeadline)
                    {
                        DeleteFile(filePath);
                    }
                    else
                    {
                        yield return new LocalFileBlob(filePath);
                    }
                }
            }
        }

        public IOfflineBlob GetBlob(string name = null)
        {
            Console.WriteLine(name);
            var iterator = this.GetBlobs().GetEnumerator();
            iterator.MoveNext();
            return iterator.Current;
        }

        public string PutBlob(byte[] buffer, int leasePeriod = 0)
        {
            if (!this.CheckStorageSize())
            {
                // TODO: Log Error
                return null;
            }

            var blobName = GetUniqueFileName(".blob");
            var blob = new LocalFileBlob(Path.Combine(this.path, blobName));
            blob.Write(buffer, leasePeriod);
            return blobName;
        }

        private static DateTime GetDateTimeFromFileName(string filePath, char separator)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var time = fileName.Substring(0, fileName.LastIndexOf(separator));
            DateTime.TryParseExact(time, "yyyy-MM-ddTHHmmss.ffffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime);
            return dateTime;
        }

        private static string GetUniqueFileName(string extension)
        {
            string fileName = string.Format(CultureInfo.InvariantCulture, $"{DateTime.Now.ToUniversalTime():yyy-MM-ddTHHmmss.ffffff}-{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}{extension}");
            return fileName;
        }

        private void OnMaintenanceEvent(object source, ElapsedEventArgs e)
        {
            try
            {
                if (!Directory.Exists(this.path))
                {
                    Directory.CreateDirectory(this.path);
                }
            }
            catch (Exception)
            {
                // TODO: Log Exception
            }

            try
            {
                foreach (var blobItem in this.GetBlobs())
                {
                }
            }
            catch (Exception)
            {
                // TODO: Log Exception
            }
        }

        private bool CheckStorageSize()
        {
            var size = this.CalculateFolderSize(this.path);
            if (size >= this.maxSize)
            {
                // TODO: Log Error
                return false;
            }

            return true;
        }

        private float CalculateFolderSize(string path)
        {
            if (!Directory.Exists(path))
            {
                return 0;
            }

            float directorySize = 0.0f;
            try
            {
                foreach (string file in Directory.GetFiles(path))
                {
                    if (File.Exists(file))
                    {
                        FileInfo fileInfo = new FileInfo(file);
                        directorySize += fileInfo.Length;
                    }
                }

                foreach (string dir in Directory.GetDirectories(path))
                {
                    directorySize += this.CalculateFolderSize(dir);
                }
            }
            catch (Exception)
            {
                // TODO: Log Exception
            }

            return directorySize;
        }

        private static string CreateTelemetrySubdirectory(string path)
        {
            string subdirectoryPath = string.Empty;

            try
            {
                string baseDirectory = string.Empty;
#if !NETSTANDARD
                baseDirectory = AppDomain.CurrentDomain.BaseDirectory;
#else
                baseDirectory = AppContext.BaseDirectory;
#endif

                string appIdentity = Environment.UserName + "@" + Path.Combine(baseDirectory, Process.GetCurrentProcess().ProcessName);
                string subdirectoryName = GetSHA256Hash(appIdentity);
                subdirectoryPath = Path.Combine(path, subdirectoryName);
                Directory.CreateDirectory(subdirectoryPath);
            }
            catch (Exception)
            {
                // TODO: Log Exception
            }

            return subdirectoryPath;
        }

        private static string GetSHA256Hash(string input)
        {
            var hashString = new StringBuilder();

            byte[] inputBits = Encoding.Unicode.GetBytes(input);
            using (var sha256 = SHA256.Create())
            {
                byte[] hashBits = sha256.ComputeHash(inputBits);
                foreach (byte b in hashBits)
                {
                    hashString.Append(b.ToString("x2", CultureInfo.InvariantCulture));
                }
            }

            return hashString.ToString();
        }

        private static void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception)
            {
                // TODO: Log Exception
            }
        }

        LocalFileBlob IOfflineStorage.GetBlob(string name)
        {
            throw new NotImplementedException();
        }
    }
}
