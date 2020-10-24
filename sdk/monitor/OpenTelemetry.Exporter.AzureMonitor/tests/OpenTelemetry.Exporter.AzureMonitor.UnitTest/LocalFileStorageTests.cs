// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using OpenTelemetry.Exporter.AzureMonitor.Storage;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OpenTelemetry.Exporter.AzureMonitor
{
    public class LocalFileStorageTests
    {
        [Fact]
        public void LocalFileStorage_E2E_Test()
        {
            var testDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            using var storage = new LocalFileStorage(testDirectory.FullName);

            var data = Encoding.UTF8.GetBytes("Hello, World!");
            IPersistentBlob blob1 = storage.CreateBlob(data);
            IPersistentBlob blob2 = storage.GetBlob();

            Assert.Single(storage.GetBlobs());
            Assert.Equal(((LocalFileBlob)blob1).FullPath, ((LocalFileBlob)blob2).FullPath);
            Assert.Equal(data, blob1.Read());

            testDirectory.Delete(true);
        }

        [Fact]
        public void LocalFileStorageTests_Lease()
        {
            var testDirectory = new DirectoryInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            using var storage = new LocalFileStorage(testDirectory.FullName, 10000, 500);

            var data = Encoding.UTF8.GetBytes("Hello, World!");
            IPersistentBlob blob = storage.CreateBlob(data, 100);

            Assert.Contains(".lock", ((LocalFileBlob)blob).FullPath);
            Thread.Sleep(1000);
            Assert.False(File.Exists(((LocalFileBlob)blob).FullPath));

            testDirectory.Delete(true);
        }
    }
}
