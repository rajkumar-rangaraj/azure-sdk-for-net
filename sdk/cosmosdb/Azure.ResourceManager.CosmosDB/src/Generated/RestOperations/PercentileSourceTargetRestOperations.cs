// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

// <auto-generated/>

#nullable disable

using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Azure;
using Azure.Core;
using Azure.Core.Pipeline;
using Azure.ResourceManager.Core;
using Azure.ResourceManager.CosmosDB.Models;

namespace Azure.ResourceManager.CosmosDB
{
    internal partial class PercentileSourceTargetRestOperations
    {
        private readonly string _userAgent;
        private readonly HttpPipeline _pipeline;
        private readonly Uri _endpoint;
        private readonly string _apiVersion;

        /// <summary> The ClientDiagnostics is used to provide tracing support for the client library. </summary>
        internal ClientDiagnostics ClientDiagnostics { get; }

        /// <summary> Initializes a new instance of PercentileSourceTargetRestOperations. </summary>
        /// <param name="clientDiagnostics"> The handler for diagnostic messaging in the client. </param>
        /// <param name="pipeline"> The HTTP pipeline for sending and receiving REST requests and responses. </param>
        /// <param name="applicationId"> The application id to use for user agent. </param>
        /// <param name="endpoint"> server parameter. </param>
        /// <param name="apiVersion"> Api Version. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="apiVersion"/> is null. </exception>
        public PercentileSourceTargetRestOperations(ClientDiagnostics clientDiagnostics, HttpPipeline pipeline, string applicationId, Uri endpoint = null, string apiVersion = default)
        {
            _endpoint = endpoint ?? new Uri("https://management.azure.com");
            _apiVersion = apiVersion ?? "2021-10-15";
            ClientDiagnostics = clientDiagnostics;
            _pipeline = pipeline;
            _userAgent = Core.HttpMessageUtilities.GetUserAgentName(this, applicationId);
        }

        internal HttpMessage CreateListMetricsRequest(string subscriptionId, string resourceGroupName, string accountName, string sourceRegion, string targetRegion, string filter)
        {
            var message = _pipeline.CreateMessage();
            var request = message.Request;
            request.Method = RequestMethod.Get;
            var uri = new RawRequestUriBuilder();
            uri.Reset(_endpoint);
            uri.AppendPath("/subscriptions/", false);
            uri.AppendPath(subscriptionId, true);
            uri.AppendPath("/resourceGroups/", false);
            uri.AppendPath(resourceGroupName, true);
            uri.AppendPath("/providers/Microsoft.DocumentDB/databaseAccounts/", false);
            uri.AppendPath(accountName, true);
            uri.AppendPath("/sourceRegion/", false);
            uri.AppendPath(sourceRegion, true);
            uri.AppendPath("/targetRegion/", false);
            uri.AppendPath(targetRegion, true);
            uri.AppendPath("/percentile/metrics", false);
            uri.AppendQuery("api-version", _apiVersion, true);
            uri.AppendQuery("$filter", filter, true);
            request.Uri = uri;
            request.Headers.Add("Accept", "application/json");
            message.SetProperty("SDKUserAgent", _userAgent);
            return message;
        }

        /// <summary> Retrieves the metrics determined by the given filter for the given account, source and target region. This url is only for PBS and Replication Latency data. </summary>
        /// <param name="subscriptionId"> The ID of the target subscription. </param>
        /// <param name="resourceGroupName"> The name of the resource group. The name is case insensitive. </param>
        /// <param name="accountName"> Cosmos DB database account name. </param>
        /// <param name="sourceRegion"> Source region from which data is written. Cosmos DB region, with spaces between words and each word capitalized. </param>
        /// <param name="targetRegion"> Target region to which data is written. Cosmos DB region, with spaces between words and each word capitalized. </param>
        /// <param name="filter"> An OData filter expression that describes a subset of metrics to return. The parameters that can be filtered are name.value (name of the metric, can have an or of multiple names), startTime, endTime, and timeGrain. The supported operator is eq. </param>
        /// <param name="cancellationToken"> The cancellation token to use. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="subscriptionId"/>, <paramref name="resourceGroupName"/>, <paramref name="accountName"/>, <paramref name="sourceRegion"/>, <paramref name="targetRegion"/> or <paramref name="filter"/> is null. </exception>
        public async Task<Response<PercentileMetricListResult>> ListMetricsAsync(string subscriptionId, string resourceGroupName, string accountName, string sourceRegion, string targetRegion, string filter, CancellationToken cancellationToken = default)
        {
            if (subscriptionId == null)
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }
            if (resourceGroupName == null)
            {
                throw new ArgumentNullException(nameof(resourceGroupName));
            }
            if (accountName == null)
            {
                throw new ArgumentNullException(nameof(accountName));
            }
            if (sourceRegion == null)
            {
                throw new ArgumentNullException(nameof(sourceRegion));
            }
            if (targetRegion == null)
            {
                throw new ArgumentNullException(nameof(targetRegion));
            }
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            using var message = CreateListMetricsRequest(subscriptionId, resourceGroupName, accountName, sourceRegion, targetRegion, filter);
            await _pipeline.SendAsync(message, cancellationToken).ConfigureAwait(false);
            switch (message.Response.Status)
            {
                case 200:
                    {
                        PercentileMetricListResult value = default;
                        using var document = await JsonDocument.ParseAsync(message.Response.ContentStream, default, cancellationToken).ConfigureAwait(false);
                        value = PercentileMetricListResult.DeserializePercentileMetricListResult(document.RootElement);
                        return Response.FromValue(value, message.Response);
                    }
                default:
                    throw await ClientDiagnostics.CreateRequestFailedExceptionAsync(message.Response).ConfigureAwait(false);
            }
        }

        /// <summary> Retrieves the metrics determined by the given filter for the given account, source and target region. This url is only for PBS and Replication Latency data. </summary>
        /// <param name="subscriptionId"> The ID of the target subscription. </param>
        /// <param name="resourceGroupName"> The name of the resource group. The name is case insensitive. </param>
        /// <param name="accountName"> Cosmos DB database account name. </param>
        /// <param name="sourceRegion"> Source region from which data is written. Cosmos DB region, with spaces between words and each word capitalized. </param>
        /// <param name="targetRegion"> Target region to which data is written. Cosmos DB region, with spaces between words and each word capitalized. </param>
        /// <param name="filter"> An OData filter expression that describes a subset of metrics to return. The parameters that can be filtered are name.value (name of the metric, can have an or of multiple names), startTime, endTime, and timeGrain. The supported operator is eq. </param>
        /// <param name="cancellationToken"> The cancellation token to use. </param>
        /// <exception cref="ArgumentNullException"> <paramref name="subscriptionId"/>, <paramref name="resourceGroupName"/>, <paramref name="accountName"/>, <paramref name="sourceRegion"/>, <paramref name="targetRegion"/> or <paramref name="filter"/> is null. </exception>
        public Response<PercentileMetricListResult> ListMetrics(string subscriptionId, string resourceGroupName, string accountName, string sourceRegion, string targetRegion, string filter, CancellationToken cancellationToken = default)
        {
            if (subscriptionId == null)
            {
                throw new ArgumentNullException(nameof(subscriptionId));
            }
            if (resourceGroupName == null)
            {
                throw new ArgumentNullException(nameof(resourceGroupName));
            }
            if (accountName == null)
            {
                throw new ArgumentNullException(nameof(accountName));
            }
            if (sourceRegion == null)
            {
                throw new ArgumentNullException(nameof(sourceRegion));
            }
            if (targetRegion == null)
            {
                throw new ArgumentNullException(nameof(targetRegion));
            }
            if (filter == null)
            {
                throw new ArgumentNullException(nameof(filter));
            }

            using var message = CreateListMetricsRequest(subscriptionId, resourceGroupName, accountName, sourceRegion, targetRegion, filter);
            _pipeline.Send(message, cancellationToken);
            switch (message.Response.Status)
            {
                case 200:
                    {
                        PercentileMetricListResult value = default;
                        using var document = JsonDocument.Parse(message.Response.ContentStream);
                        value = PercentileMetricListResult.DeserializePercentileMetricListResult(document.RootElement);
                        return Response.FromValue(value, message.Response);
                    }
                default:
                    throw ClientDiagnostics.CreateRequestFailedException(message.Response);
            }
        }
    }
}
