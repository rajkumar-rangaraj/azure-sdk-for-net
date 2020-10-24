// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using OpenTelemetry.Exporter.AzureMonitor.Storage;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Xunit;

namespace OpenTelemetry.Exporter.AzureMonitor
{
    public class LocalFileBlobTests
    {
        [Fact]
        public void LocalFileBlobTests_E2E_Test()
        {
            var testFile = new FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            IPersistentBlob blob = new LocalFileBlob(testFile.FullName);

            var data = Encoding.UTF8.GetBytes("Hello, World!");
            IPersistentBlob blob1 = blob.Write(data);
            var blobContent = blob.Read();

            Assert.Equal(testFile.FullName, ((LocalFileBlob)blob1).FullPath);
            Assert.Equal(data, blobContent);

            blob1.Delete();
            Assert.False(testFile.Exists);
        }

        [Fact]
        public void LocalFileBlobTests_Lease()
        {
            var testFile = new FileInfo(Path.Combine(Path.GetTempPath(), Path.GetRandomFileName()));
            IPersistentBlob blob = new LocalFileBlob(testFile.FullName);

            var data = Encoding.UTF8.GetBytes("Hello, World!");
            IPersistentBlob blob1 = blob.Write(data);
            IPersistentBlob leasedBlob = blob1.Lease(1000);

            Assert.Contains(".lock", ((LocalFileBlob)leasedBlob).FullPath);

            blob1.Delete();
            Assert.False(testFile.Exists);
        }
    }
}
