﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace OpenTelemetry.Exporter.AzureMonitor.Storage
{
    internal static class PersistentStorageHelper
    {
        internal static void RemoveExpiredBlob(DateTime retentionDeadline, string filePath)
        {
            if (filePath.EndsWith(".blob", StringComparison.OrdinalIgnoreCase))
            {
                DateTime fileDateTime = GetDateTimeFromBlobName(filePath);
                if (fileDateTime < retentionDeadline)
                {
                    DeleteFile(filePath);
                    SharedEventSource.Log.Warning("File write exceeded retention. Dropping telemetry");
                }
            }
        }

        internal static bool RemoveExpiredLease(DateTime leaseDeadline, string filePath)
        {
            bool success = false;

            if (filePath.EndsWith(".lock", StringComparison.OrdinalIgnoreCase))
            {
                DateTime fileDateTime = GetDateTimeFromLeaseName(filePath);
                if (fileDateTime < leaseDeadline)
                {
                    var newFilePath = filePath.Substring(0, filePath.LastIndexOf('@'));
                    try
                    {
                        File.Move(filePath, newFilePath);
                        success = true;
                    }
                    catch (Exception ex)
                    {
                        SharedEventSource.Log.Warning("File rename of {filePath} to {newFilePath} has failed.", ex);
                    }
                }
            }

            return success;
        }

        internal static bool RemoveTimedOutTmpFiles(DateTime timeoutDeadline, string filePath)
        {
            bool success = false;

            if (filePath.EndsWith(".tmp", StringComparison.OrdinalIgnoreCase))
            {
                DateTime fileDateTime = GetDateTimeFromBlobName(filePath);
                if (fileDateTime < timeoutDeadline)
                {
                    DeleteFile(filePath);
                    success = true;
                    SharedEventSource.Log.Warning("File write exceeded timeout. Dropping telemetry");
                }
            }

            return success;
        }

        internal static void RemoveExpiredBlobs(string directoryPath, long retentionPeriodInMilliseconds, long writeTimeoutInMilliseconds)
        {
            var currentUtcDateTime = DateTime.Now.ToUniversalTime();

            var leaseDeadline = currentUtcDateTime;
            var retentionDeadline = currentUtcDateTime - TimeSpan.FromMilliseconds(retentionPeriodInMilliseconds);
            var timeoutDeadline = currentUtcDateTime - TimeSpan.FromMilliseconds(writeTimeoutInMilliseconds);

            foreach (var file in Directory.GetFiles(directoryPath).OrderByDescending(f => f))
            {
                var filePath = file;
                var success = RemoveTimedOutTmpFiles(timeoutDeadline, file);

                if (success)
                {
                    continue;
                }

                success = RemoveExpiredLease(leaseDeadline, file);

                if (!success)
                {
                    RemoveExpiredBlob(retentionDeadline, file);
                }
            }
        }

        internal static string GetUniqueFileName(string extension)
        {
            return string.Format(CultureInfo.InvariantCulture, $"{DateTime.Now.ToUniversalTime():yyy-MM-ddTHHmmss.ffffff}-{Guid.NewGuid().ToString("N", CultureInfo.InvariantCulture)}{extension}");
        }

        internal static string CreateSubdirectory(string path)
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
            catch (Exception ex)
            {
                SharedEventSource.Log.Error($"Error creating sub-directory {path}.", ex);
            }

            return subdirectoryPath;
        }

        internal static void DeleteFile(string path)
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch (Exception ex)
            {
                SharedEventSource.Log.Warning($"Deletion of file {path} has failed.", ex);
            }
        }

        internal static float CalculateFolderSize(string path)
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
                    directorySize += CalculateFolderSize(dir);
                }
            }
            catch (Exception ex)
            {
                SharedEventSource.Log.Error("Error calculating folder size.", ex);
            }

            return directorySize;
        }

        internal static DateTime GetDateTimeFromBlobName(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var time = fileName.Substring(0, fileName.LastIndexOf('-'));
            DateTime.TryParseExact(time, "yyyy-MM-ddTHHmmss.ffffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime);
            return dateTime;
        }

        internal static DateTime GetDateTimeFromLeaseName(string filePath)
        {
            var fileName = Path.GetFileNameWithoutExtension(filePath);
            var startIndex = fileName.LastIndexOf('@') + 1;
            var time = fileName.Substring(startIndex, fileName.Length - startIndex);
            DateTime.TryParseExact(time, "yyyy-MM-ddTHHmmss.ffffff", CultureInfo.InvariantCulture, DateTimeStyles.None, out var dateTime);
            return dateTime;
        }

        internal static string GetSHA256Hash(string input)
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
    }
}
