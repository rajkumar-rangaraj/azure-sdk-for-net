// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure.Core;

namespace Azure.ResourceManager.RecoveryServicesSiteRecovery
{
    internal class SiteRecoveryJobOperationSource : IOperationSource<SiteRecoveryJobResource>
    {
        private readonly ArmClient _client;

        internal SiteRecoveryJobOperationSource(ArmClient client)
        {
            _client = client;
        }

        SiteRecoveryJobResource IOperationSource<SiteRecoveryJobResource>.CreateResult(Response response, CancellationToken cancellationToken)
        {
            using var document = JsonDocument.Parse(response.ContentStream);
            var data = SiteRecoveryJobData.DeserializeSiteRecoveryJobData(document.RootElement);
            return new SiteRecoveryJobResource(_client, data);
        }

        async ValueTask<SiteRecoveryJobResource> IOperationSource<SiteRecoveryJobResource>.CreateResultAsync(Response response, CancellationToken cancellationToken)
        {
            using var document = await JsonDocument.ParseAsync(response.ContentStream, default, cancellationToken).ConfigureAwait(false);
            var data = SiteRecoveryJobData.DeserializeSiteRecoveryJobData(document.RootElement);
            return new SiteRecoveryJobResource(_client, data);
        }
    }
}
